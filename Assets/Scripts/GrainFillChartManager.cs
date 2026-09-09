using System.Collections.Generic;
using UnityEngine;

public class GrainFillChartManager : MonoBehaviour
{
    [Header("Referencia a la grafica")]
    public GrainFillLineChartUI lineChart;

    [Header("Muestreo")]
    [Tooltip("Cada cuantos pasos de simulacion se agrega un punto nuevo a la grafica. Con maxPoints=11 en la grafica, define cuanto tiempo total cubre la ventana visible.")]
    public int pasosPorMuestra = 20;

    [Header("Colores por cosechadora (en orden de aparicion)")]
    public string[] paletaColores = new string[]
    {
        "#E74C3C", // rojo
        "#3498DB", // azul
        "#2ECC71", // verde
        "#F1C40F", // amarillo
        "#9B59B6", // morado
        "#E67E22", // naranja
    };

    private HashSet<string> seriesCreadas = new HashSet<string>();
    private int ultimoStepRegistrado = -1;

    public void ActualizarDesdeEstado(SimulationData data)
    {
        if (lineChart == null || data == null || data.agentes == null)
        {
            return;
        }

        // Solo tomamos una muestra cada "pasosPorMuestra" pasos.
        // Usamos data.step (no un contador propio de mensajes) para
        // que, si el servidor esta pausado y repite el mismo paso,
        // no dupliquemos el punto en la grafica.
        bool tocaMuestrear = data.step % pasosPorMuestra == 0;
        bool esStepNuevo = data.step != ultimoStepRegistrado;

        if (!tocaMuestrear || !esStepNuevo)
        {
            return;
        }

        ultimoStepRegistrado = data.step;

        foreach (AgentData agente in data.agentes)
        {
            if (agente.tipo != "cosechadora")
            {
                continue;
            }

            if (!seriesCreadas.Contains(agente.id))
            {
                string color = paletaColores[seriesCreadas.Count % paletaColores.Length];
                lineChart.AddSeries(agente.id, color);
                seriesCreadas.Add(agente.id);
            }

            float porcentajeLleno = agente.capacidad > 0
                ? (agente.grano / (float)agente.capacidad) * 100f
                : 0f;

            lineChart.AddValue(agente.id, porcentajeLleno);
        }
    }

    // Se llama al reiniciar la simulacion, para que la grafica
    // arranque vacia con las cosechadoras del nuevo campo.
    public void LimpiarTodo()
    {
        lineChart?.ClearAllSeries();
        seriesCreadas.Clear();
        ultimoStepRegistrado = -1;
    }
}