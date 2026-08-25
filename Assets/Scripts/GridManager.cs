using UnityEngine;

public enum TipoTerreno { Vacio, Cultivo, Obstaculo, Lodo }

public class GridManager : MonoBehaviour
{
    public int size = 25;
    public TipoTerreno[,] terreno;
    public GameObject[,] tilesInstanciados;
    public GameObject prefabCultivo, prefabObstaculo, prefabLodo;

    public int ContarCultivo()
    {
        int total = 0;
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                if (terreno[x, y] == TipoTerreno.Cultivo) total++;
        return total;
    }

    void Awake() => GenerarTerreno();

    private GameObject PrefabPara(TipoTerreno tipo)
    {
        switch (tipo)
        {
            case TipoTerreno.Cultivo: return prefabCultivo;
            case TipoTerreno.Obstaculo: return prefabObstaculo;
            case TipoTerreno.Lodo: return prefabLodo;
            default: return null;
        }
    }

    void GenerarTerreno()
    {
        terreno = new TipoTerreno[size, size];
        tilesInstanciados = new GameObject[size, size];
        var rng = new System.Random();

        // Manchas de lodo (random walk)
        int nManchasLodo = 5;
        for (int i = 0; i < nManchasLodo; i++)
        {
            int x = rng.Next(0, size);
            int y = rng.Next(0, size);
            int largo = rng.Next(5, 15);

            for (int j = 0; j < largo; j++)
            {
                if (terreno[x, y] == TipoTerreno.Vacio)
                    terreno[x, y] = TipoTerreno.Lodo;

                x = Mathf.Clamp(x + rng.Next(-1, 2), 0, size - 1);
                y = Mathf.Clamp(y + rng.Next(-1, 2), 0, size - 1);
            }
        }

        // Obstáculos
        float densidadObstaculo = 0.05f;
        int nObstaculos = Mathf.FloorToInt(size * size * densidadObstaculo);
        for (int i = 0; i < nObstaculos; i++)
        {
            int x = rng.Next(0, size);
            int y = rng.Next(0, size);
            terreno[x, y] = TipoTerreno.Obstaculo;
        }

        // Cultivo
        float densidadCultivo = 0.90f;
        int nCultivo = Mathf.FloorToInt(size * size * densidadCultivo);
        int colocados = 0, intentos = 0;
        while (colocados < nCultivo && intentos < nCultivo * 20)
        {
            int x = rng.Next(0, size);
            int y = rng.Next(0, size);
            if (terreno[x, y] == TipoTerreno.Vacio)
            {
                terreno[x, y] = TipoTerreno.Cultivo;
                colocados++;
            }
            intentos++;
        }

        // Instanciar
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                if (terreno[x, y] != TipoTerreno.Vacio)
                {
                    var tile = Instantiate(PrefabPara(terreno[x, y]), new Vector3(x, 0, y), Quaternion.identity, transform);
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