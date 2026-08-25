using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class AStar
{
    public static List<Vector2Int> BuscarRuta(TipoTerreno[,] terreno, Vector2Int inicio, Vector2Int? objetivo = null, HashSet<Vector2Int> objetivos = null, bool tractor = false)
    {
        int Heuristica(Vector2Int p) =>
            objetivo.HasValue
                ? Mathf.Abs(p.x - objetivo.Value.x) + Mathf.Abs(p.y - objetivo.Value.y)
                : objetivos.Min(o => Mathf.Abs(p.x - o.x) + Mathf.Abs(p.y - o.y));

        var abiertos = new SortedSet<(int f, int g, Vector2Int pos)>(Comparer<(int,int,Vector2Int)>.Create(
            (a, b) => a.Item1 != b.Item1 ? a.Item1.CompareTo(b.Item1) : (a.Item3.x != b.Item3.x ? a.Item3.x.CompareTo(b.Item3.x) : a.Item3.y.CompareTo(b.Item3.y))));

        var costos = new Dictionary<Vector2Int, int> { [inicio] = 0 };
        var padres = new Dictionary<Vector2Int, Vector2Int?> { [inicio] = null };
        abiertos.Add((Heuristica(inicio), 0, inicio));

        Vector2Int[] vecinos = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        while (abiertos.Count > 0)
        {
            var (f, g, pos) = abiertos.Min;
            abiertos.Remove(abiertos.Min);

            if ((objetivo.HasValue && pos == objetivo.Value) ||
                (objetivos != null && objetivos.Contains(pos) && pos != inicio))
                return Reconstruir(padres, inicio, pos);

            foreach (var d in vecinos)
            {
                var vecino = pos + d;
                if (!EnRango(vecino, terreno.GetLength(0), terreno.GetLength(1))) continue;
                var tipo = terreno[vecino.x, vecino.y];
                if (tipo == TipoTerreno.Obstaculo || (tractor && tipo == TipoTerreno.Cultivo)) continue;

                int costoMov = tipo == TipoTerreno.Lodo ? 2 : 1;
                int nuevoG = g + costoMov;

                if (!costos.ContainsKey(vecino) || nuevoG < costos[vecino])
                {
                    costos[vecino] = nuevoG;
                    padres[vecino] = pos;
                    abiertos.Add((nuevoG + Heuristica(vecino), nuevoG, vecino));
                }
            }
        }
        return null;
    }

    private static bool EnRango(Vector2Int pos, int sizeX, int sizeY)
    {
        return pos.x >= 0 && pos.x < sizeX && pos.y >= 0 && pos.y < sizeY;
    }

    private static List<Vector2Int> Reconstruir(Dictionary<Vector2Int, Vector2Int?> padres, Vector2Int inicio, Vector2Int objetivo)
    {
        var ruta = new List<Vector2Int>();
        Vector2Int? actual = objetivo;

        while (actual != inicio)
        {
            ruta.Add(actual.Value);
            actual = padres[actual.Value];
        }

        ruta.Reverse();
        return ruta;
    }
}