using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FuelBarChartManager : MonoBehaviour
{
    [Header("Prefab de una fila (debe traer el componente AgentFuelRowUI)")]
    public AgentFuelRowUI filaPrefab;

    [Header("Contenedor donde se instancian las filas (con un Vertical Layout Group)")]
    public RectTransform contenedor;

    [Header("Ancho en pixeles que representa el tanque lleno (100%)")]
    public float maxBarWidth = 200f;

    [Header("Altura de cada fila")]
    public float alturaMinima = 24f;
    public float alturaMaxima = 48f;

    private readonly Dictionary<string, AgentFuelRowUI> filas =
        new Dictionary<string, AgentFuelRowUI>();

    // Llamar desde WebSocketManager cada vez que llega un
    // estado nuevo (mismo objeto SimulationData que usa
    // UpdateAgents).
    public void ActualizarDesdeEstado(SimulationData data)
    {
        if (data == null || data.agentes == null)
        {
            return;
        }

        HashSet<string> idsVistos = new HashSet<string>();
        bool cambioLaCantidad = false;

        foreach (AgentData agente in data.agentes)
        {
            idsVistos.Add(agente.id);

            if (!filas.TryGetValue(agente.id, out AgentFuelRowUI fila))
            {
                fila = Instantiate(filaPrefab, contenedor);
                fila.Configure(NombreVisible(agente), maxBarWidth);
                filas.Add(agente.id, fila);
                cambioLaCantidad = true;
            }

            fila.UpdateValues(agente.combustible, agente.combustible_maximo);
        }

        // Si un reset cambio la cantidad de agentes, quitamos las
        // filas de los que ya no existen en el estado actual.
        List<string> idsAEliminar = new List<string>();

        foreach (string id in filas.Keys)
        {
            if (!idsVistos.Contains(id))
            {
                idsAEliminar.Add(id);
            }
        }

        foreach (string id in idsAEliminar)
        {
            Destroy(filas[id].gameObject);
            filas.Remove(id);
            cambioLaCantidad = true;
        }

        if (cambioLaCantidad)
        {
            RecalcularAlturas();
        }
    }

    // Calcula cuanto le toca de alto a cada fila para llenar el
    // contenedor SIN pasarse de alturaMaxima ni bajar de
    // alturaMinima, tomando en cuenta el spacing y el padding
    // que tenga configurado el Vertical Layout Group.
    private void RecalcularAlturas()
    {
        if (filas.Count == 0)
        {
            return;
        }

        float spacing = 0f;
        float paddingVertical = 0f;

        VerticalLayoutGroup vlg = contenedor.GetComponent<VerticalLayoutGroup>();

        if (vlg != null)
        {
            spacing = vlg.spacing;
            paddingVertical = vlg.padding.top + vlg.padding.bottom;
        }

        float alturaDisponible =
            contenedor.rect.height
            - paddingVertical
            - spacing * (filas.Count - 1);

        float alturaPorFila = alturaDisponible / filas.Count;
        alturaPorFila = Mathf.Clamp(alturaPorFila, alturaMinima, alturaMaxima);

        foreach (AgentFuelRowUI fila in filas.Values)
        {
            fila.SetAlturaPreferida(alturaPorFila);
        }
    }

    // Util para llamarlo justo cuando se le pica al boton de
    // Reset, y que la grafica se vacie al instante en vez de
    // esperar al primer mensaje del modelo nuevo.
    public void LimpiarTodo()
    {
        foreach (AgentFuelRowUI fila in filas.Values)
        {
            if (fila != null)
            {
                Destroy(fila.gameObject);
            }
        }

        filas.Clear();
    }

    private string NombreVisible(AgentData agente)
    {
        string etiqueta = agente.tipo == "tractor" ? "Tractor" : "Cosechadora";
        return $"{etiqueta} {agente.id}";
    }
}