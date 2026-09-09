using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GrainFillLineChartUI : MonoBehaviour
{
    [Header("Chart Settings")]
    public RectTransform plotArea;
    public float minValue = 0f;
    public float maxValue = 100f;
    public int maxPoints = 11;

    [Header("Leyenda")]
    [Tooltip("Contenedor donde se dibuja la leyenda (nombre + color de cada cosechadora). Debe ser un RectTransform APARTE del plotArea, para que no quede recortado por el RectMask2D")]
    public RectTransform legendArea;
    [Tooltip("Layout vertical u horizontal para acomodar las entradas de la leyenda")]
    public bool leyendaHorizontal = true;
    public float legendSwatchSize = 14f;
    public float legendFontSize = 14f;
    public Color legendTextColor = new Color(0.15f, 0.15f, 0.18f, 1f);
    public float legendItemSpacing = 90f; 
    public float legendLineHeight = 22f;

    [Header("Contencion")]
    [Tooltip("Margen interno en pixeles para que los puntos de los bordes no sobresalgan del panel")]
    public float padding = 12f;

    private Vector2 tamanoAnteriorPlotArea;

    [Header("Line Appearance")]
    public float pointSize = 12f;
    public float lineThickness = 4f;

    [Header("Axes")]
    public float axisThickness = 2f;
    public Color axisColor = new Color(0.15f, 0.15f, 0.18f, 1f);

    [Header("Ticks")]
    public TMP_FontAsset tickFont;
    public float tickFontSize = 16f;
    public float tickLength = 8f;
    public Color tickTextColor = new Color(0.15f, 0.15f, 0.18f, 1f);

    private Dictionary<string, LineSeries> series = new Dictionary<string, LineSeries>();

    private class LineSeries
    {
        public Color color;
        public List<Vector2> points = new List<Vector2>();
        public List<GameObject> objetosCreados = new List<GameObject>();
        public GameObject entradaLeyenda;

        public LineSeries(Color color)
        {
            this.color = color;
        }
    }

    void Start()
    {
        // Asegura que cualquier contenido que se salga del area se
        // recorte visualmente en vez de desbordar el panel.
        if (plotArea.GetComponent<RectMask2D>() == null)
        {
            plotArea.gameObject.AddComponent<RectMask2D>();
        }

        tamanoAnteriorPlotArea = plotArea.rect.size;

        CreateAxes();
        CreateYTicks();
        CreateXTicks();
    }

    void Update()
    {
        // Si el panel cambio de tamano (resolucion distinta, Canvas
        // Scaler, el usuario redimensiono la ventana, etc), volvemos
        // a dibujar ejes y ticks para que sigan alineados. Las series
        // ya dibujadas se mantienen donde estaban (se recalculan solo
        // cuando se agrega un punto nuevo).
        if (plotArea.rect.size != tamanoAnteriorPlotArea)
        {
            tamanoAnteriorPlotArea = plotArea.rect.size;
            RedibujarEjes();
        }
    }

    void RedibujarEjes()
    {
        // Borra los ejes/ticks viejos (quedaron con el tamano
        // anterior) y los vuelve a crear con las medidas actuales.
        foreach (Transform hijo in plotArea)
        {
            if (hijo.name == "XAxis" || hijo.name == "YAxis" ||
                hijo.name.StartsWith("XTick_") || hijo.name.StartsWith("YTick_") ||
                hijo.name.StartsWith("XLabel_") || hijo.name.StartsWith("YLabel_"))
            {
                Destroy(hijo.gameObject);
            }
        }

        CreateAxes();
        CreateYTicks();
        CreateXTicks();
    }

    void CreateAxes()
    {
        float width = plotArea.rect.width - padding * 2f;
        float height = plotArea.rect.height - padding * 2f;

        CreateRectangle("XAxis", new Vector2(padding, padding), new Vector2(width, axisThickness), new Vector2(0, 0.5f), axisColor);
        CreateRectangle("YAxis", new Vector2(padding, padding), new Vector2(axisThickness, height), new Vector2(0.5f, 0), axisColor);
    }

    void CreateYTicks()
    {
        float height = plotArea.rect.height - padding * 2f;

        for (float value = minValue; value <= maxValue; value += 25f)
        {
            float normalized = Mathf.InverseLerp(minValue, maxValue, value);
            float y = padding + normalized * height;

            CreateRectangle("YTick_" + value, new Vector2(padding - tickLength / 2f, y), new Vector2(tickLength, axisThickness), new Vector2(0.5f, 0.5f), axisColor);
            CreateTickLabel("YLabel_" + value, Mathf.RoundToInt(value) + "%", new Vector2(padding - 42f, y), new Vector2(60f, 25f));
        }
    }

    void CreateXTicks()
    {
        float width = plotArea.rect.width - padding * 2f;
        float xSpacing = width / (maxPoints - 1);

        for (int i = 0; i < maxPoints; i++)
        {
            float x = padding + i * xSpacing;

            CreateRectangle("XTick_" + i, new Vector2(x, padding - tickLength / 2f), new Vector2(axisThickness, tickLength), new Vector2(0.5f, 0.5f), axisColor);
            CreateTickLabel("XLabel_" + i, i.ToString(), new Vector2(x, padding - 22f), new Vector2(40f, 25f));
        }
    }

    void CreateRectangle(string objectName, Vector2 position, Vector2 size, Vector2 pivot, Color color)
    {
        GameObject newObject = new GameObject(objectName);
        newObject.transform.SetParent(plotArea, false);

        Image image = newObject.AddComponent<Image>();
        image.color = color;

        RectTransform rect = newObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(0, 0);
        rect.pivot = pivot;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    void CreateTickLabel(string objectName, string text, Vector2 position, Vector2 size)
    {
        GameObject labelObject = new GameObject(objectName);
        labelObject.transform.SetParent(plotArea, false);

        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = tickFontSize;
        label.color = tickTextColor; 
        label.alignment = TextAlignmentOptions.Center;

        if (tickFont != null)
        {
            label.font = tickFont;
        }

        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(0, 0);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    public void AddSeries(string seriesName, string colorHex)
    {
        if (series.ContainsKey(seriesName))
            return;

        Color color = Color.white;
        ColorUtility.TryParseHtmlString(colorHex, out color);

        LineSeries nuevaSerie = new LineSeries(color);
        series.Add(seriesName, nuevaSerie);

        if (legendArea != null)
        {
            nuevaSerie.entradaLeyenda = CreateLegendEntry(seriesName, color, series.Count - 1);
        }
    }

    public void AddValue(string seriesName, float value)
    {
        if (!series.ContainsKey(seriesName))
            return;

        LineSeries currentSeries = series[seriesName];

        if (currentSeries.points.Count >= maxPoints)
            return;

        float width = plotArea.rect.width - padding * 2f;
        float height = plotArea.rect.height - padding * 2f;
        float xSpacing = width / (maxPoints - 1);
        float x = padding + currentSeries.points.Count * xSpacing;
        float normalizedValue = Mathf.InverseLerp(minValue, maxValue, value);
        float y = padding + normalizedValue * height;
        Vector2 newPoint = new Vector2(x, y);

        if (currentSeries.points.Count > 0)
        {
            Vector2 previousPoint = currentSeries.points[currentSeries.points.Count - 1];

            GameObject lineObj = CreateLine(seriesName, previousPoint, newPoint, currentSeries.color, currentSeries.points.Count);
            currentSeries.objetosCreados.Add(lineObj);
        }

        GameObject pointObj = CreatePoint(seriesName, newPoint, currentSeries.color, currentSeries.points.Count);
        currentSeries.objetosCreados.Add(pointObj);

        currentSeries.points.Add(newPoint);
    }

    GameObject CreatePoint(string seriesName, Vector2 position, Color color, int index)
    {
        GameObject pointObject = new GameObject(seriesName + "_Point_" + index);
        pointObject.transform.SetParent(plotArea, false);

        Image image = pointObject.AddComponent<Image>();
        image.color = color;

        RectTransform rect = pointObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(0, 0);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(pointSize, pointSize);
        rect.anchoredPosition = position;

        return pointObject;
    }

    GameObject CreateLine(string seriesName, Vector2 start, Vector2 end, Color color, int index)
    {
        GameObject lineObject = new GameObject(seriesName + "_Line_" + index);
        lineObject.transform.SetParent(plotArea, false);

        Image image = lineObject.AddComponent<Image>();
        image.color = color;

        RectTransform rect = lineObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(0, 0);
        rect.pivot = new Vector2(0, 0.5f);

        Vector2 direction = end - start;
        float distance = direction.magnitude;
        rect.sizeDelta = new Vector2(distance, lineThickness);
        rect.anchoredPosition = start;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        rect.localRotation = Quaternion.Euler(0, 0, angle);

        return lineObject;
    }

    GameObject CreateLegendEntry(string seriesName, Color color, int index)
    {
        GameObject entrada = new GameObject("Legend_" + seriesName);
        entrada.transform.SetParent(legendArea, false);

        RectTransform rectEntrada = entrada.AddComponent<RectTransform>();
        rectEntrada.anchorMin = new Vector2(0, 1);
        rectEntrada.anchorMax = new Vector2(0, 1);
        rectEntrada.pivot = new Vector2(0, 1);

        Vector2 posicion = leyendaHorizontal
            ? new Vector2(index * legendItemSpacing, 0f)
            : new Vector2(0f, -index * legendLineHeight);

        rectEntrada.anchoredPosition = posicion;
        rectEntrada.sizeDelta = new Vector2(legendItemSpacing, legendLineHeight);

        // Cuadrito de color
        GameObject swatch = new GameObject("Swatch");
        swatch.transform.SetParent(entrada.transform, false);
        Image imagenSwatch = swatch.AddComponent<Image>();
        imagenSwatch.color = color;
        RectTransform rectSwatch = swatch.GetComponent<RectTransform>();
        rectSwatch.anchorMin = new Vector2(0, 0.5f);
        rectSwatch.anchorMax = new Vector2(0, 0.5f);
        rectSwatch.pivot = new Vector2(0, 0.5f);
        rectSwatch.sizeDelta = new Vector2(legendSwatchSize, legendSwatchSize);
        rectSwatch.anchoredPosition = new Vector2(0, 0);

        // Texto con el nombre de la cosechadora (ej "C1")
        GameObject texto = new GameObject("Label");
        texto.transform.SetParent(entrada.transform, false);
        TextMeshProUGUI labelTexto = texto.AddComponent<TextMeshProUGUI>();
        labelTexto.text = seriesName;
        labelTexto.fontSize = legendFontSize;
        labelTexto.color = legendTextColor;
        labelTexto.alignment = TextAlignmentOptions.MidlineLeft;

        if (tickFont != null)
        {
            labelTexto.font = tickFont;
        }

        RectTransform rectTexto = texto.GetComponent<RectTransform>();
        rectTexto.anchorMin = new Vector2(0, 0.5f);
        rectTexto.anchorMax = new Vector2(0, 0.5f);
        rectTexto.pivot = new Vector2(0, 0.5f);
        rectTexto.sizeDelta = new Vector2(legendItemSpacing - legendSwatchSize - 6f, legendLineHeight);
        rectTexto.anchoredPosition = new Vector2(legendSwatchSize + 6f, 0);

        return entrada;
    }

    // Borra todas las series (puntos y lineas) para poder arrancar
    // de cero en un reset de la simulacion. Ejes y ticks no se
    // tocan: se crean una sola vez en Start() y siguen siendo los
    // mismos siempre.
    public void ClearAllSeries()
    {
        foreach (var serieActual in series.Values)
        {
            foreach (GameObject objeto in serieActual.objetosCreados)
            {
                if (objeto != null)
                {
                    Destroy(objeto);
                }
            }

            if (serieActual.entradaLeyenda != null)   // <-- NUEVO
            {
                Destroy(serieActual.entradaLeyenda);
            }
        }

        series.Clear();
    }
}