using UnityEngine;

public enum TipoTerreno { Vacio, Cultivo, Obstaculo, Lodo }

public class GridManager : MonoBehaviour
{
    public int size = 25;
    public TipoTerreno[,] terreno;
    public GameObject[,] tilesInstanciados;
    public GameObject prefabCultivo;
    public GameObject[] prefabsObstaculo;
    public GameObject prefabLodo;

    // NUEVO: arrastra aquí tu propio GameObject Plane (ya en la escena, con su
    // material asignado). GridManager lo escala y centra automáticamente
    // según 'size', sin importar el tamaño real de su mesh.
    public GameObject planoSuelo;
    public float margenSuelo = 2f; // cuánto se extiende más allá del borde del grid

    public int ContarCultivo()
    {
        int total = 0;
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                if (terreno[x, y] == TipoTerreno.Cultivo) total++;
        return total;
    }

    void Awake()
    {
        AjustarSuelo();
        GenerarTerreno();
    }

    void AjustarSuelo()
    {
        if (planoSuelo == null) return;

        // Instancia el prefab (tratándolo igual que prefabCultivo/prefabsObstaculo),
        // en vez de esperar que ya exista un GameObject puesto a mano en la escena.
        var instancia = Instantiate(planoSuelo, transform);

        var renderer = instancia.GetComponent<Renderer>();
        if (renderer == null) return;

        // Mide el tamaño real del plano (sin importar si es el Plane default
        // de Unity, un mesh distinto, o ya tiene una escala previa) usando sus
        // bounds actuales, y calcula el factor para llegar al tamaño deseado.
        float anchoActual = renderer.bounds.size.x;
        float largoActual = renderer.bounds.size.z;

        float ladoDeseado = size + margenSuelo * 2f;

        var escala = instancia.transform.localScale;
        escala.x *= ladoDeseado / anchoActual;
        escala.z *= ladoDeseado / largoActual;
        instancia.transform.localScale = escala;

        // Centra el plano sobre el grid (las celdas van de 0 a size-1),
        // conservando la altura Y que ya traiga el prefab.
        float centro = (size - 1) / 2f;
        var pos = instancia.transform.position;
        instancia.transform.position = new Vector3(centro, pos.y, centro);
    }

    private GameObject PrefabPara(TipoTerreno tipo, System.Random rng)
    {
        switch (tipo)
        {
            case TipoTerreno.Cultivo: return prefabCultivo;
            case TipoTerreno.Obstaculo:
                if (prefabsObstaculo == null || prefabsObstaculo.Length == 0) return null;
                return prefabsObstaculo[rng.Next(0, prefabsObstaculo.Length)];
            case TipoTerreno.Lodo: return prefabLodo;
            default: return null;
        }
    }

    void GenerarTerreno()
    {
        terreno = new TipoTerreno[size, size];
        tilesInstanciados = new GameObject[size, size];
        var rng = new System.Random();

        // Lodo removido: el terreno ahora solo tiene Cultivo y Obstaculo.

        // Obstáculos
        float densidadObstaculo = 0.05f;
        int nObstaculos = Mathf.FloorToInt(size * size * densidadObstaculo);
        for (int i = 0; i < nObstaculos; i++)
        {
            int x = rng.Next(0, size);
            int y = rng.Next(0, size);
            terreno[x, y] = TipoTerreno.Obstaculo;
        }

        // Cultivo: llena TODO lo que no sea obstáculo, sin dejar celdas vacías.
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                if (terreno[x, y] == TipoTerreno.Vacio)
                    terreno[x, y] = TipoTerreno.Cultivo;

        // Instanciar
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                if (terreno[x, y] != TipoTerreno.Vacio)
                {
                    var tile = Instantiate(PrefabPara(terreno[x, y], rng), new Vector3(x, 0, y), Quaternion.identity, transform);
                    tilesInstanciados[x, y] = tile;
                }
    }

    public void RemoverTile(int x, int y)
    {
        if (tilesInstanciados[x, y] != null)
        {
            Destroy(tilesInstanciados[x, y]);
            tilesInstanciados[x, y] = null;
        }
    }
}