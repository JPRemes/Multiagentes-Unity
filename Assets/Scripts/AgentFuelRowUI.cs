using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(LayoutElement))]
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
    private LayoutElement layoutElement;

    private void Awake()
    {
        // Se toma del mismo objeto donde vive este script (el
        // objeto raiz de la fila, "FilaAgente" en el prefab).
        layoutElement = GetComponent<LayoutElement>();
    }

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

    // Llamado por el FuelBarChartManager cada vez que cambia la
    // cantidad de filas, para topar la altura entre un minimo
    // (legible) y un maximo (que no se vea gigante si hay pocos
    // agentes). Al fijar preferredHeight y apagar flexibleHeight,
    // el Vertical Layout Group deja de "estirar" la fila.
    public void SetAlturaPreferida(float altura)
    {
        if (layoutElement == null)
        {
            layoutElement = GetComponent<LayoutElement>();
        }

        layoutElement.preferredHeight = altura;
        layoutElement.flexibleHeight = 0f;
    }

    private Color ColorSegunFraccion(float fraccion)
    {
        if (fraccion <= umbralBajo) return colorBajo;
        if (fraccion <= umbralMedio) return colorMedio;
        return colorLleno;
    }
}