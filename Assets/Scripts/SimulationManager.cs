using System.Collections.Generic;
using UnityEngine;

public class SimulationManager : MonoBehaviour
{
    public List<Cosechadora> cosechadoras;
    public List<Tractor> tractores;
    public GridManager gridManager;
    public Vector2Int silo = Vector2Int.zero;
    public HashSet<Vector2Int> cultivosReservados = new();
    public float intervaloTick = 0.3f; // ritmo de las cosechadoras
    float acumulado;
    public int totalCultivo;

    void Start()
    {
        totalCultivo = gridManager.ContarCultivo();
    }

    void Update()
    {
        // El tractor tiene su propio reloj interno (ver Tractor.intervaloPropio),
        // así que se le pasa deltaTime en cada frame y él decide cuándo tickear.
        foreach (var t in tractores) t.Tick(this, Time.deltaTime);

        acumulado += Time.deltaTime;
        if (acumulado < intervaloTick) return;
        acumulado = 0;

        foreach (var c in cosechadoras) c.Tick(this);

        if (SimulacionTerminada()) enabled = false;
    }

    bool SimulacionTerminada()
    {
        int totalRecolectado = 0;
        foreach (var c in cosechadoras) totalRecolectado += c.recolectado;

        bool todoRecolectado = totalRecolectado >= totalCultivo;

        bool sinGranoPendiente = true;
        foreach (var c in cosechadoras) if (c.grano != 0) { sinGranoPendiente = false; break; }
        if (sinGranoPendiente)
            foreach (var t in tractores) if (t.grano != 0) { sinGranoPendiente = false; break; }

        return todoRecolectado && sinGranoPendiente;
    }
}