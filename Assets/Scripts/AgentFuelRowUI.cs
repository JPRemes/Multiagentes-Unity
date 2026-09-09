using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
// Fila individual de la grafica de barras: una por agente
// (cosechadora o tractor). La barra representa que tan lleno
// esta el tanque de combustible de ESE agente (combustible /
// combustible_maximo), asi que cada barra tiene su propia
// referencia de "llena", no una referencia compartida entre
// todos los agentes como en TeamRowUI (ahi el 100% era el
// maximo de wins de TODOS los equipos).
// ============================================================

public class AgentFuelRowUI : MonoBehaviour
{
    [Header("Referencias UI")]
    public TMP_Text agentNameText;
    public TMP_Text valueText;
    public RectTransform barFill;
    public Image barImage;

    [Header("Colores segun nivel de combustible")]
    public Color colorLleno = new Color(0.20f, 0.80f, 0.20f);   // verde
    public Color colorMedio = new Color(0.95f, 0.75f, 0.10f);   // amarillo
    public Color colorBajo  = new Color(0.85f, 0.20f, 0.20f);   // rojo

    [Range(0f, 1f)] public float umbralMedio = 0.6f;
    [Range(0f, 1f)] public float umbralBajo = 0.3f;

    private float maxBarWidth;

    // Se llama una sola vez, cuando la fila se crea por primera
    // vez para un agente nuevo.
    public void Configure(string nombreVisible, float maxBarWidth)
    {
        this.maxBarWidth = maxBarWidth;
        agentNameText.text = nombreVisible;
    }

    // Se llama cada vez que llega un estado nuevo desde Python,
    // para que la barra vaya decreciendo (o llenandose de nuevo,
    // si el agente recarga en el silo).
    public void UpdateValues(float combustible, float combustibleMaximo)
    {
        combustibleMaximo = Mathf.Max(combustibleMaximo, 0.0001f);
        float fraccion = Mathf.Clamp01(combustible / combustibleMaximo);

        valueText.text = $"{combustible:0}/{combustibleMaximo:0}";

        float barWidth = fraccion * maxBarWidth;
        barFill.sizeDelta = new Vector2(barWidth, barFill.sizeDelta.y);

        barImage.color = ColorSegunFraccion(fraccion);
    }

    private Color ColorSegunFraccion(float fraccion)
    {
        if (fraccion <= umbralBajo) return colorBajo;
        if (fraccion <= umbralMedio) return colorMedio;
        return colorLleno;
    }
}