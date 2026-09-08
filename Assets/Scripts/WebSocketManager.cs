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
    public Quaternion rotInicial;
    public Quaternion rotObjetivo;
    public float tiempoInicio;
}

public class WebSocketManager : MonoBehaviour
{
    private WebSocket websocket;

    [Header("Prefabs de agentes")]
    public GameObject cosechadoraPrefab;
    public GameObject tractorPrefab;

    [Header("Terreno")]
    public GameObject siloPrefab;            // opcional

    // Prefab especifico para las celdas con cultivo (por
    // ejemplo un modelo de trigo o una planta).
    public GameObject cultivoPrefab;

    // Varios prefabs posibles para obstaculos (rocas, arboles,
    // etc): en cada celda con obstaculo se elige uno al azar,
    // para que no se vea todo repetido.
    public GameObject[] obstaculoPrefabs;

    [Header("Rotacion de los agentes al moverse (grados en Y)")]
    [Tooltip("Hacia donde debe mirar el prefab cuando se mueve hacia X positivo (la fila aumenta)")]
    public float anguloMovXPositivo = 0f;
    [Tooltip("Hacia donde debe mirar el prefab cuando se mueve hacia X negativo (la fila disminuye)")]
    public float anguloMovXNegativo = 180f;
    [Tooltip("Hacia donde debe mirar el prefab cuando se mueve hacia Z positivo (la columna aumenta)")]
    public float anguloMovZPositivo = 90f;
    [Tooltip("Hacia donde debe mirar el prefab cuando se mueve hacia Z negativo (la columna disminuye)")]
    public float anguloMovZNegativo = 270f;

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

    private GameObject[] decoraciones;        // decoracion actual de cada celda (o null)
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
        Debug.Log(
            $"[DIAGNOSTICO] ReconstruirTerreno llamado. size={data.size} " +
            $"terreno.Length={data.terreno.Length} " +
            $"decoraciones previas={(decoraciones == null ? "null" : decoraciones.Length.ToString())}"
        );

        // Si ya habia decoraciones puestas, las destruimos
        // primero (esto pasa cuando cambia el tamaño en un reset).
        if (decoraciones != null)
        {
            foreach (GameObject decoracion in decoraciones)
            {
                if (decoracion != null)
                {
                    Destroy(decoracion);
                }
            }
        }

        int size = data.size;
        tamanoActual = size;
        decoraciones = new GameObject[size * size];

        int contadorCultivo = 0;
        int contadorObstaculo = 0;
        int contadorDecoracionesCreadas = 0;

        for (int fila = 0; fila < size; fila++)
        {
            for (int columna = 0; columna < size; columna++)
            {
                int indice = fila * size + columna;
                Vector3 pos = new Vector3(fila, 0f, columna);
                int tipo = data.terreno[indice];

                if (tipo == 1) contadorCultivo++;
                if (tipo == 2) contadorObstaculo++;

                ActualizarDecoracion(indice, pos, tipo);

                if (decoraciones[indice] != null) contadorDecoracionesCreadas++;
            }
        }

        Debug.Log(
            $"[DIAGNOSTICO] Terreno reconstruido: cultivo={contadorCultivo} " +
            $"obstaculo={contadorObstaculo} decoracionesCreadas={contadorDecoracionesCreadas} " +
            $"(cultivoPrefab asignado={cultivoPrefab != null}, " +
            $"obstaculoPrefabs asignados={obstaculoPrefabs != null && obstaculoPrefabs.Length > 0})"
        );

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
        if (decoraciones == null || tamanoActual <= 0)
        {
            return;
        }

        foreach (CeldaCambiada celda in celdas)
        {
            int indice = celda.x * tamanoActual + celda.z;

            if (indice >= 0 && indice < decoraciones.Length)
            {
                Vector3 pos = new Vector3(celda.x, 0f, celda.z);
                ActualizarDecoracion(indice, pos, celda.tipo);
            }
        }
    }

    // Coloca (o quita) la decoracion de una celda: un prefab
    // especifico para cultivo, uno elegido al azar de la lista
    // para obstaculo, o nada si esta vacia (queda solo el Plane
    // de la escena, sin ningun objeto encima).
    void ActualizarDecoracion(int indice, Vector3 pos, int tipo)
    {
        if (decoraciones[indice] != null)
        {
            Destroy(decoraciones[indice]);
            decoraciones[indice] = null;
        }

        // 0 = vacio, 1 = cultivo, 2 = obstaculo
        if (tipo == 1 && cultivoPrefab != null)
        {
            decoraciones[indice] = Instantiate(
                cultivoPrefab, pos, Quaternion.identity, transform
            );
        }
        else if (tipo == 2 && obstaculoPrefabs != null && obstaculoPrefabs.Length > 0)
        {
            GameObject elegido = obstaculoPrefabs[
                UnityEngine.Random.Range(0, obstaculoPrefabs.Length)
            ];
            decoraciones[indice] = Instantiate(
                elegido, pos, Quaternion.identity, transform
            );
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
                    rotInicial = nuevoAgente.transform.rotation,
                    rotObjetivo = nuevoAgente.transform.rotation,
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
            av.rotInicial = av.objeto.transform.rotation;

            // Si de verdad se movio, giramos el prefab al angulo
            // que configuraste para esa direccion (arriba, en el
            // Inspector). Si no se movio (esta esperando, cargando
            // gasolina, etc), se queda mirando hacia donde ya
            // estaba mirando.
            Vector3 direccion = nuevaPos - av.posInicial;

            if (direccion.sqrMagnitude > 0.0001f)
            {
                float angulo;

                // Se mueve mas en X que en Z: fue un paso
                // arriba/abajo (fila). Si no, fue izquierda/
                // derecha (columna).
                if (Mathf.Abs(direccion.x) > Mathf.Abs(direccion.z))
                {
                    angulo = direccion.x > 0f ? anguloMovXPositivo : anguloMovXNegativo;
                }
                else
                {
                    angulo = direccion.z > 0f ? anguloMovZPositivo : anguloMovZNegativo;
                }

                av.rotObjetivo = Quaternion.Euler(0f, angulo, 0f);
            }

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

            av.objeto.transform.rotation = Quaternion.Slerp(
                av.rotInicial, av.rotObjetivo, t
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
        // Primero limpiamos el terreno que se ve ahorita, para
        // que desaparezca de inmediato al picarle al boton (sin
        // esperar a que llegue el campo nuevo). Cuando llegue el
        // siguiente mensaje con el terreno fresco, ReconstruirTerreno
        // lo vuelve a construir desde cero.
        LimpiarTerrenoVisual();

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

    void LimpiarTerrenoVisual()
    {
        if (decoraciones != null)
        {
            foreach (GameObject decoracion in decoraciones)
            {
                if (decoracion != null)
                {
                    Destroy(decoracion);
                }
            }
        }

        decoraciones = null;
        tamanoActual = -1;

        Debug.Log("[DIAGNOSTICO] Terreno visual limpiado por el boton de Reset");
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