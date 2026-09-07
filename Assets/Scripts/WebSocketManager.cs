using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NativeWebSocket;

// ============================================================
// CLASES DE DATOS (deben coincidir con el JSON que manda Python)
// ============================================================

[Serializable]
public class AgentData
{
    public string id;
    public string tipo;          // "cosechadora" o "tractor"
    public int x;
    public int z;
    public int grano;
    public int capacidad;
    public float combustible;
    public float combustible_maximo;
    public int recolectado;      // solo cosechadoras
    public bool terminado;       // solo cosechadoras
    public int entregado;        // solo tractores
}

[Serializable]
public class CeldaCambiada
{
    public int x;
    public int z;
    public int tipo;             // siempre 0 (vacio) por ahora
}

[Serializable]
public class SiloData
{
    public int x;
    public int z;
}

[Serializable]
public class SimulationData
{
    public bool paused;
    public int step;
    public bool terminado;
    public float tick;
    public int size;
    public SiloData silo;
    public int total_cultivo;
    public int[] terreno;                    // SOLO viene lleno en el primer
                                              // mensaje o tras un reset
    public CeldaCambiada[] celdas_cambiadas;
    public AgentData[] agentes;
}

// ============================================================
// INTERPOLACION DE UN AGENTE ENTRE DOS ESTADOS
// ============================================================

public class AgenteVisual
{
    public GameObject objeto;
    public Vector3 posInicial;
    public Vector3 posObjetivo;
    public float tiempoInicio;
}

public class WebSocketManager : MonoBehaviour
{
    private WebSocket websocket;

    [Header("Prefabs de agentes")]
    public GameObject cosechadoraPrefab;
    public GameObject tractorPrefab;

    [Header("Terreno")]
    public GameObject tilePrefab;            // un Cube o Plane, escala 1x1
    public GameObject siloPrefab;            // opcional

    [Header("Colores del terreno")]
    public Color colorVacio = new Color(0.87f, 0.83f, 0.69f);
    public Color colorCultivo = new Color(0.29f, 0.69f, 0.31f);
    public Color colorObstaculo = new Color(0.36f, 0.23f, 0.13f);

    [Header("Interfaz")]
    public TMP_Text statusText;
    public TMP_Text agentCountText;
    public TMP_Text pauseButtonText;
    public TMP_InputField inputSize;
    public TMP_InputField inputCosechadoras;
    public TMP_InputField inputTractores;
    public TMP_InputField inputObstaculo;

    // ------------------------------------------------------
    // Estado interno
    // ------------------------------------------------------

    private Dictionary<string, AgenteVisual> agentes =
        new Dictionary<string, AgenteVisual>();

    private Renderer[] tiles;                // tiles[fila * size + columna]
    private List<GameObject> tileObjects = new List<GameObject>();
    private int tamanoActual = -1;
    private float tickActual = 0.25f;
    private GameObject siloInstancia;

    // ------------------------------------------------------
    // Conexion
    // ------------------------------------------------------

    async void Start()
    {
        statusText.text = "Estado: Conectando...";
        agentCountText.text = "Agentes: 0";

        websocket = new WebSocket("ws://localhost:8765");

        websocket.OnOpen += () =>
        {
            Debug.Log("Conectado al servidor");
        };

        websocket.OnError += (error) =>
        {
            Debug.LogError("Error WebSocket: " + error);
        };

        websocket.OnClose += (closeCode) =>
        {
            Debug.Log("Conexion cerrada");
            statusText.text = "Estado: Desconectado";
        };

        websocket.OnMessage += (bytes) =>
        {
            string message = System.Text.Encoding.UTF8.GetString(bytes);
            SimulationData data = JsonUtility.FromJson<SimulationData>(message);
            UpdateSimulation(data);
        };

        await websocket.Connect();
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        websocket?.DispatchMessageQueue();
#endif
        InterpolarAgentes();
    }

    private async void OnApplicationQuit()
    {
        if (websocket != null)
        {
            await websocket.Close();
        }
    }

    // ------------------------------------------------------
    // Procesar un estado recibido
    // ------------------------------------------------------

    void UpdateSimulation(SimulationData data)
    {
        if (data == null || data.agentes == null)
        {
            return;
        }

        tickActual = data.tick > 0f ? data.tick : tickActual;

        if (data.terreno != null && data.terreno.Length > 0)
        {
            ReconstruirTerreno(data);
        }

        if (data.celdas_cambiadas != null)
        {
            AplicarCeldasCambiadas(data.celdas_cambiadas);
        }

        UpdateAgents(data);
        RemoveMissingAgents(data);
        UpdateInterface(data);
    }

    // ------------------------------------------------------
    // Terreno
    // ------------------------------------------------------

    void ReconstruirTerreno(SimulationData data)
    {
        // Si ya habia un grid armado, lo destruimos primero
        // (esto pasa cuando cambia el tamaño en un reset).
        foreach (GameObject tile in tileObjects)
        {
            Destroy(tile);
        }
        tileObjects.Clear();

        int size = data.size;
        tamanoActual = size;
        tiles = new Renderer[size * size];

        for (int fila = 0; fila < size; fila++)
        {
            for (int columna = 0; columna < size; columna++)
            {
                Vector3 pos = new Vector3(fila, 0.1f, columna);

                GameObject tile = Instantiate(
                    tilePrefab, pos, Quaternion.identity, transform
                );
                tile.name = $"Tile_{fila}_{columna}";
                tileObjects.Add(tile);

                int indice = fila * size + columna;
                Renderer renderer = tile.GetComponent<Renderer>();
                tiles[indice] = renderer;

                PintarTile(renderer, data.terreno[indice]);
            }
        }

        // Silo
        if (siloPrefab != null)
        {
            Vector3 posSilo = new Vector3(data.silo.x, 0.1f, data.silo.z);

            if (siloInstancia == null)
            {
                siloInstancia = Instantiate(siloPrefab, posSilo, Quaternion.identity);
            }
            else
            {
                siloInstancia.transform.position = posSilo;
            }
        }
    }

    void AplicarCeldasCambiadas(CeldaCambiada[] celdas)
    {
        if (tiles == null || tamanoActual <= 0)
        {
            return;
        }

        foreach (CeldaCambiada celda in celdas)
        {
            int indice = celda.x * tamanoActual + celda.z;

            if (indice >= 0 && indice < tiles.Length && tiles[indice] != null)
            {
                PintarTile(tiles[indice], celda.tipo);
            }
        }
    }

    void PintarTile(Renderer renderer, int tipo)
    {
        if (renderer == null)
        {
            return;
        }

        // 0 = vacio, 1 = cultivo, 2 = obstaculo
        if (tipo == 1)
        {
            renderer.material.color = colorCultivo;
        }
        else if (tipo == 2)
        {
            renderer.material.color = colorObstaculo;
        }
        else
        {
            renderer.material.color = colorVacio;
        }
    }

    // ------------------------------------------------------
    // Agentes
    // ------------------------------------------------------

    void UpdateAgents(SimulationData data)
    {
        foreach (AgentData agentData in data.agentes)
        {
            Vector3 nuevaPos = new Vector3(agentData.x, 0.5f, agentData.z);

            if (!agentes.ContainsKey(agentData.id))
            {
                GameObject prefab = agentData.tipo == "tractor"
                    ? tractorPrefab
                    : cosechadoraPrefab;

                GameObject nuevoAgente = Instantiate(prefab, nuevaPos, Quaternion.identity);
                nuevoAgente.name = "Agent_" + agentData.id;

                AgenteVisual visual = new AgenteVisual
                {
                    objeto = nuevoAgente,
                    posInicial = nuevaPos,
                    posObjetivo = nuevaPos,
                    tiempoInicio = Time.time
                };

                agentes.Add(agentData.id, visual);
                Debug.Log("Nuevo agente creado: " + agentData.id);
            }

            AgenteVisual av = agentes[agentData.id];

            // Partimos desde donde esta ahora visualmente (no
            // desde el ultimo objetivo) para que la interpolacion
            // no de un salto si el mensaje anterior no termino.
            av.posInicial = av.objeto.transform.position;
            av.posObjetivo = nuevaPos;
            av.tiempoInicio = Time.time;

            ActualizarInfoAgente(av.objeto, agentData);
        }
    }

    void InterpolarAgentes()
    {
        foreach (AgenteVisual av in agentes.Values)
        {
            if (av.objeto == null)
            {
                continue;
            }

            float t = tickActual > 0f
                ? (Time.time - av.tiempoInicio) / tickActual
                : 1f;

            t = Mathf.Clamp01(t);

            av.objeto.transform.position = Vector3.Lerp(
                av.posInicial, av.posObjetivo, t
            );
        }
    }

    void ActualizarInfoAgente(GameObject objeto, AgentData agentData)
    {
        // Si el prefab tiene un TextMeshPro hijo (por ejemplo
        // llamado "InfoText"), le actualizamos el texto con el
        // grano y el combustible. Si no existe, no pasa nada.
        TextMeshPro info = objeto.GetComponentInChildren<TextMeshPro>();

        if (info == null)
        {
            return;
        }

        if (agentData.tipo == "cosechadora")
        {
            info.text = $"{agentData.id}\n" +
                        $"grano {agentData.grano}/{agentData.capacidad}\n" +
                        $"comb {agentData.combustible:0}/{agentData.combustible_maximo:0}";
        }
        else
        {
            info.text = $"{agentData.id}\n" +
                        $"grano {agentData.grano}/{agentData.capacidad}\n" +
                        $"comb {agentData.combustible:0}/{agentData.combustible_maximo:0}\n" +
                        $"entregado {agentData.entregado}";
        }
    }

    void RemoveMissingAgents(SimulationData data)
    {
        HashSet<string> idsRecibidos = new HashSet<string>();

        foreach (AgentData agentData in data.agentes)
        {
            idsRecibidos.Add(agentData.id);
        }

        List<string> idsAEliminar = new List<string>();

        foreach (string id in agentes.Keys)
        {
            if (!idsRecibidos.Contains(id))
            {
                idsAEliminar.Add(id);
            }
        }

        foreach (string id in idsAEliminar)
        {
            Destroy(agentes[id].objeto);
            agentes.Remove(id);
            Debug.Log("Agente eliminado: " + id);
        }
    }

    // ------------------------------------------------------
    // Interfaz
    // ------------------------------------------------------

    void UpdateInterface(SimulationData data)
    {
        if (data.terminado)
        {
            statusText.text = $"Estado: Terminado (paso {data.step})";
        }
        else if (data.paused)
        {
            statusText.text = $"Estado: Pausado (paso {data.step})";
            pauseButtonText.text = "CONTINUAR";
        }
        else
        {
            statusText.text = $"Estado: Ejecutando (paso {data.step})";
            pauseButtonText.text = "PAUSAR";
        }

        agentCountText.text = "Agentes: " + data.agentes.Length;
    }

    // ------------------------------------------------------
    // Comandos hacia Python
    // ------------------------------------------------------

    async void EnviarComando(string json)
    {
        if (websocket == null || websocket.State != WebSocketState.Open)
        {
            return;
        }

        await websocket.SendText(json);
    }

    public void TogglePause()
    {
        string command = pauseButtonText.text == "PAUSAR"
            ? "{\"command\":\"pause\"}"
            : "{\"command\":\"resume\"}";

        EnviarComando(command);
    }

    public void ResetSimulation()
    {
        int size = LeerEntero(inputSize, 25);
        int cosechadoras = LeerEntero(inputCosechadoras, 3);
        int tractores = LeerEntero(inputTractores, 2);
        float obstaculo = LeerFlotante(inputObstaculo, 0.05f);

        string json =
            "{\"command\":\"reset\",\"config\":{" +
            $"\"size\":{size}," +
            $"\"n_cosechadoras\":{cosechadoras}," +
            $"\"n_tractores\":{tractores}," +
            $"\"densidad_obstaculo\":{obstaculo.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
            "}}";

        EnviarComando(json);
    }

    int LeerEntero(TMP_InputField campo, int porDefecto)
    {
        if (campo == null || string.IsNullOrEmpty(campo.text))
        {
            return porDefecto;
        }

        return int.TryParse(campo.text, out int valor) ? valor : porDefecto;
    }

    float LeerFlotante(TMP_InputField campo, float porDefecto)
    {
        if (campo == null || string.IsNullOrEmpty(campo.text))
        {
            return porDefecto;
        }

        return float.TryParse(
            campo.text,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out float valor
        ) ? valor : porDefecto;
    }
}