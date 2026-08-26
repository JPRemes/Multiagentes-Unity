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

    public int ContarCultivo()
    {
        int total = 0;
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                if (terreno[x, y] == TipoTerreno.Cultivo) total++;
        return total;
    }

    void Awake() => GenerarTerreno();

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