using System.Collections;
using System.Linq;
using UnityEngine;

public class Tractor : MonoBehaviour
{
    public int grano = 0;
    public int capacidad = 60;
    public System.Collections.Generic.List<Vector2Int> ruta = new();
    public int pasosLentosRestantes = 0;
    public int entregado = 0;
    public string estado = "esperando"; // esperando | buscando_cosechadora | yendo_al_silo

    public Cosechadora cosechadoraObjetivo;

    // NUEVO: el tractor tiene su propio ritmo de tick, independiente
    // del intervaloTick del SimulationManager, para poder ir más rápido
    // que las cosechadoras.
    public float intervaloPropio = 0.15f;
    float acumuladoPropio;

    // NUEVO: recuerda hacia qué punto se calculó la última ruta,
    // para no recalcular A* en cada tick si el objetivo casi no se movió.
    Vector2Int? ultimoObjetivoCalculado;

    Vector2Int PosGrid => new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.z));

    public void Tick(SimulationManager sim, float deltaTime)
    {
        acumuladoPropio += deltaTime;
        if (acumuladoPropio < intervaloPropio) return;
        acumuladoPropio -= intervaloPropio;

        if (pasosLentosRestantes > 0)
        {
            pasosLentosRestantes--;
            return;
        }

        if (estado == "esperando")
        {
            var cosechadora = BuscarCosechadoraDisponible(sim);
            if (cosechadora == null) return;

            cosechadora.tractorAsignado = this;
            cosechadoraObjetivo = cosechadora;
            estado = "buscando_cosechadora";
            ultimoObjetivoCalculado = null; // NUEVO: fuerza recálculo con el nuevo objetivo
        }

        if (estado == "buscando_cosechadora")
        {
            var cosechadora = cosechadoraObjetivo;
            if (cosechadora == null) { estado = "esperando"; return; }

            int distancia = Mathf.Abs(PosGrid.x - cosechadora.PosGrid.x) + Mathf.Abs(PosGrid.y - cosechadora.PosGrid.y);

            if (distancia <= 1)
            {
                int espacio = capacidad - grano;
                int cantidad = Mathf.Min(cosechadora.grano, espacio);

                cosechadora.grano -= cantidad;
                grano += cantidad;

                if (cosechadora.grano < cosechadora.capacidad * cosechadora.umbralTractor)
                {
                    cosechadora.solicitandoTractor = false;
                    cosechadora.tractorAsignado = null;
                }

                if (grano >= capacidad)
                {
                    ruta = AStar.BuscarRuta(sim.gridManager.terreno, PosGrid, objetivo: sim.silo, tractor: true) ?? new System.Collections.Generic.List<Vector2Int>();
                    estado = "yendo_al_silo";
                    cosechadora.tractorAsignado = null;
                    cosechadoraObjetivo = null;
                    return;
                }

                if (cosechadora.grano > 0 && cosechadora.solicitandoTractor)
                    return; // sigue junto a ella esperando más grano

                cosechadora.tractorAsignado = null;
                cosechadoraObjetivo = null;
                estado = "esperando";
                ruta.Clear();
                return;
            }

            // ================================================
            // NUEVO: en vez de perseguir la posición actual de
            // la cosechadora, apunta a donde va a estar en 3
            // pasos (leído de su propia ruta planeada), y solo
            // recalcula A* si el objetivo se movió lo suficiente.
            // ================================================
            var objetivoFuturo = cosechadora.PosicionFutura(3);

            bool necesitaRecalcular = ruta.Count == 0 ||
                !ultimoObjetivoCalculado.HasValue ||
                (Mathf.Abs(ultimoObjetivoCalculado.Value.x - objetivoFuturo.x) + Mathf.Abs(ultimoObjetivoCalculado.Value.y - objetivoFuturo.y)) > 2;

            if (necesitaRecalcular)
            {
                ruta = AStar.BuscarRuta(sim.gridManager.terreno, PosGrid, objetivo: objetivoFuturo, tractor: true) ?? new System.Collections.Generic.List<Vector2Int>();
                ultimoObjetivoCalculado = objetivoFuturo;
            }

            if (ruta.Count == 0) return;

            AvanzarUnPaso(sim);
            return;
        }

        if (estado == "yendo_al_silo")
        {
            if (ruta != null && ruta.Count > 0)
            {
                AvanzarUnPaso(sim);
                return;
            }

            if (PosGrid == sim.silo)
            {
                entregado += grano;
                grano = 0;
                ruta.Clear();
                estado = "esperando";
            }
        }
    }

    void AvanzarUnPaso(SimulationManager sim)
    {
        var siguiente = ruta[0];
        ruta.RemoveAt(0);
        transform.position = new Vector3(siguiente.x, 0, siguiente.y);

        if (sim.gridManager.terreno[siguiente.x, siguiente.y] == TipoTerreno.Lodo)
            pasosLentosRestantes = 1;
    }

    Cosechadora BuscarCosechadoraDisponible(SimulationManager sim)
    {
        var candidatas = sim.cosechadoras.Where(c =>
            c.solicitandoTractor && (c.tractorAsignado == null || c.tractorAsignado == this)).ToList();

        if (candidatas.Count == 0) return null;

        return candidatas.OrderBy(c =>
            Mathf.Abs(PosGrid.x - c.PosGrid.x) + Mathf.Abs(PosGrid.y - c.PosGrid.y)).First();
    }
}