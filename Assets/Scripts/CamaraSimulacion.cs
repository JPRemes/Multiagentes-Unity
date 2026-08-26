using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Coloca la cámara automáticamente para ver todo el grid desde arriba en
/// ángulo (vista isométrica-ish), y permite ajustar la vista con el mouse
/// mientras corre la simulación:
///   - Scroll: zoom in/out
///   - Click derecho + arrastrar: rotar alrededor del centro del grid
///   - Click medio (rueda) + arrastrar: pan (mover el punto de enfoque)
/// </summary>
[RequireComponent(typeof(Camera))]
public class CamaraSimulacion : MonoBehaviour
{
    public GridManager gridManager;

    [Header("Encuadre inicial")]
    public float anguloInclinacion = 55f;   // qué tan "desde arriba" se ve (90 = totalmente cenital)
    public float margenExtra = 1.3f;        // qué tanto más lejos de lo justo-necesario se posiciona

    [Header("Controles en vivo")]
    public float velocidadZoom = 15f;
    public float velocidadRotacion = 100f;
    public float velocidadPan = 0.02f;
    public float distanciaMinima = 5f;
    public float distanciaMaxima = 200f;

    Vector3 puntoEnfoque;
    float distancia;
    float yaw;   // rotación horizontal alrededor del punto de enfoque

    void Start()
    {
        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();

        EncuadrarGrid();
    }

    void EncuadrarGrid()
    {
        int size = gridManager != null ? gridManager.size : 25;
        puntoEnfoque = new Vector3((size - 1) / 2f, 0f, (size - 1) / 2f);

        // Distancia necesaria para que quepa todo el grid en el campo de visión de la cámara.
        var cam = GetComponent<Camera>();
        float mitadCampoVision = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
        distancia = (size / 2f) / Mathf.Tan(mitadCampoVision) * margenExtra;

        yaw = 0f;
        AplicarTransform();
    }

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null) return; // no hay mouse conectado, no hace nada

        // Zoom con la rueda del mouse
        float scroll = mouse.scroll.ReadValue().y;
        if (scroll != 0f)
        {
            distancia -= scroll * velocidadZoom * Time.deltaTime;
            distancia = Mathf.Clamp(distancia, distanciaMinima, distanciaMaxima);
        }

        Vector2 delta = mouse.delta.ReadValue();

        // Rotar con click derecho
        if (mouse.rightButton.isPressed)
        {
            yaw += delta.x * velocidadRotacion * 0.01f * Time.deltaTime;
        }

        // Pan con click medio (rueda presionada)
        if (mouse.middleButton.isPressed)
        {
            var derecha = transform.right;
            var arriba = transform.up;
            puntoEnfoque -= derecha * delta.x * velocidadPan * distancia * 0.01f;
            puntoEnfoque -= arriba * delta.y * velocidadPan * distancia * 0.01f;
        }

        AplicarTransform();
    }

    void AplicarTransform()
    {
        var rotacion = Quaternion.Euler(anguloInclinacion, yaw, 0f);
        var offset = rotacion * new Vector3(0f, 0f, -distancia);
        transform.position = puntoEnfoque + offset;
        transform.rotation = rotacion;
    }

    // Recalcula el encuadre si cambias el tamaño del grid en tiempo de ejecución
    // (por ejemplo desde un botón de UI). Llama a esto manualmente si lo necesitas.
    public void ReEncuadrar() => EncuadrarGrid();
}