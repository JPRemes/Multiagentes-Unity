using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Cosechadora : MonoBehaviour
{
    public int grano, capacidad = 100, recolectado;
    public List<Vector2Int> ruta = new();
    public bool solicitandoTractor, regresandoAlSilo;
    public Vector2Int? objetivoActual;
    public float umbralTractor = 0.60f;
    public Tractor tractorAsignado;

    // NUEVO: ajusta esto en el Inspector (0, 90, 180 o 270) si el modelo
    // no queda mirando hacia donde se mueve.
    public float offsetRotacionY = 0f;

    public Vector2Int PosGrid => new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.z));

    // NUEVO: le dice al tractor dónde va a estar esta cosechadora dentro de N pasos,
    // leyendo directamente su propia ruta planeada (no es una predicción aproximada).
    public Vector2Int PosicionFutura(int pasosAdelante)
    {
        if (ruta == null || ruta.Count == 0) return PosGrid;
        int idx = Mathf.Min(pasosAdelante - 1, ruta.Count - 1);
        return ruta[idx];
    }

    public void Tick(SimulationManager sim)
    {
        if (sim.gridManager == null || sim.gridManager.terreno == null) return;
        var terreno = sim.gridManager.terreno;

        // --- Regresando al silo ---
        if (regresandoAlSilo)
        {
            if (ruta.Count > 0) { AvanzarUnPaso(sim); return; }
            if (PosGrid == sim.silo) { regresandoAlSilo = false; grano = 0; }
            return;
        }

        // --- Llena -> se detiene ---
        if (grano >= capacidad)
        {
            solicitandoTractor = true;
            ruta.Clear();
            LiberarReserva(sim);
            return;
        }

        if (grano >= capacidad * umbralTractor)
            solicitandoTractor = true;

        // --- Buscar cultivo si no tiene ruta ---
        if (ruta.Count == 0)
        {
            LiberarReserva(sim);
            ruta = BuscarCultivoMasCercano(sim) ?? new List<Vector2Int>();

            if (ruta.Count == 0)
            {
                ruta = AStar.BuscarRuta(terreno, PosGrid, objetivo: sim.silo) ?? new List<Vector2Int>();
                if (ruta.Count > 0)
                {
                    solicitandoTractor = false;
                    regresandoAlSilo = true;
                }
                return;
            }

            objetivoActual = ruta[ruta.Count - 1];
            sim.cultivosReservados.Add(objetivoActual.Value);
        }

        // --- Avanzar ---
        AvanzarUnPaso(sim, recolectarCultivo: true);
    }

    void AvanzarUnPaso(SimulationManager sim, bool recolectarCultivo = false)
    {
        var siguiente = ruta[0];
        ruta.RemoveAt(0);

        // NUEVO: rota el modelo para que mire hacia donde se está moviendo.
        var direccion = new Vector3(siguiente.x - transform.position.x, 0, siguiente.y - transform.position.z);
        if (direccion != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(direccion) * Quaternion.Euler(0, offsetRotacionY, 0);

        transform.position = new Vector3(siguiente.x, 0, siguiente.y);

        var terreno = sim.gridManager.terreno;
        var tipo = terreno[siguiente.x, siguiente.y];

        if (recolectarCultivo && tipo == TipoTerreno.Cultivo && grano < capacidad)
        {
            terreno[siguiente.x, siguiente.y] = TipoTerreno.Vacio;
            sim.gridManager.RemoverTile(siguiente.x, siguiente.y);

            recolectado++;
            grano++;

            if (objetivoActual == siguiente)
                LiberarReserva(sim);

            if (grano >= capacidad * umbralTractor)
                solicitandoTractor = true;

            if (grano >= capacidad)
                ruta.Clear();
        }
        else if (tipo == TipoTerreno.Lodo)
        {
            // pasos lentos si en el futuro agregas ese contador aquí también
        }
    }

    void LiberarReserva(SimulationManager sim)
    {
        if (objetivoActual.HasValue)
        {
            sim.cultivosReservados.Remove(objetivoActual.Value);
            objetivoActual = null;
        }
    }

    List<Vector2Int> BuscarCultivoMasCercano(SimulationManager sim)
    {
        var terreno = sim.gridManager.terreno;
        int size = sim.gridManager.size;
        var reservados = sim.cultivosReservados;

        var cultivos = new HashSet<Vector2Int>();
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                var pos = new Vector2Int(x, y);
                if (terreno[x, y] == TipoTerreno.Cultivo &&
                    (!reservados.Contains(pos) || pos == objetivoActual))
                    cultivos.Add(pos);
            }

        if (cultivos.Count == 0) return null;

        return AStar.BuscarRuta(terreno, PosGrid, objetivos: cultivos);
    }
}