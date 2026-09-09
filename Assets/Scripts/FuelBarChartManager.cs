using System.Collections.Generic;
using UnityEngine;

// ============================================================
// Administra la grafica de barras de combustible: una fila por
// agente (cosechadora o tractor). Se alimenta directamente del
// mismo SimulationData que ya procesa WebSocketManager, asi que
// no necesita su propia conexion ni logica de parseo.
// ============================================================

public class FuelBarChartManager : MonoBehaviour
{
    [Header("Prefab de una fila ")]
    public AgentFuelRowUI filaPrefab;

    [Header("Contenedor de filas")]
    public RectTransform contenedor;

    [Header("Ancho de barra llena")]
    public float maxBarWidth = 200f;

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

        foreach (AgentData agente in data.agentes)
        {
            idsVistos.Add(agente.id);

            if (!filas.TryGetValue(agente.id, out AgentFuelRowUI fila))
            {
                fila = Instantiate(filaPrefab, contenedor);
                fila.Configure(NombreVisible(agente), maxBarWidth);
                filas.Add(agente.id, fila);
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