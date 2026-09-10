using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PageManager : MonoBehaviour
{
    [Header("Paginas (objetos vacios con el contenido de cada una)")]
    public GameObject[] paginas;

    [Header("Botones (opcional, tambien puedes llamar los metodos desde el Inspector)")]
    public Button botonIzquierda;
    public Button botonDerecha;

    [Header("Texto de pagina")]
    public TMP_Text textoPagina;

    [Tooltip("Si esta activo, muestra 'Pagina X / Y'. Si no, solo 'X / Y'")]
    public bool incluirPalabraPagina = true;

    [Header("Ciclico")]
    [Tooltip("Si esta activo, al llegar a la ultima pagina regresa a la primera (y viceversa)")]
    public bool ciclico = true;

    private int paginaActual = 0;

    void Start()
    {
        OcultarTodas();
    }

    // Apaga todas las paginas. Se usa antes de que arranque la
    // simulacion, para que no se vea ninguna pagina de datos.
    public void OcultarTodas()
    {
        if (paginas == null)
        {
            return;
        }

        foreach (GameObject pagina in paginas)
        {
            if (pagina != null)
            {
                pagina.SetActive(false);
            }
        }
    }

    public void PaginaSiguiente()
    {
        int nuevaPagina = paginaActual + 1;

        if (nuevaPagina >= paginas.Length)
        {
            nuevaPagina = ciclico ? 0 : paginas.Length - 1;
        }

        MostrarPagina(nuevaPagina);
    }

    public void PaginaAnterior()
    {
        int nuevaPagina = paginaActual - 1;

        if (nuevaPagina < 0)
        {
            nuevaPagina = ciclico ? paginas.Length - 1 : 0;
        }

        MostrarPagina(nuevaPagina);
    }

    void MostrarPagina(int indice)
    {
        if (paginas == null || paginas.Length == 0)
        {
            return;
        }

        for (int i = 0; i < paginas.Length; i++)
        {
            if (paginas[i] != null)
            {
                paginas[i].SetActive(i == indice);
            }
        }

        paginaActual = indice;
        ActualizarBotones();
        ActualizarTexto();
    }

    void ActualizarBotones()
    {
        // Si no es ciclico, deshabilita los botones en los extremos
        // para que se note visualmente que no hay mas paginas en esa direccion.
        if (!ciclico)
        {
            if (botonIzquierda != null) botonIzquierda.interactable = paginaActual > 0;
            if (botonDerecha != null) botonDerecha.interactable = paginaActual < paginas.Length - 1;
        }
    }

    void ActualizarTexto()
    {
        if (textoPagina == null)
        {
            return;
        }

        string prefijo = incluirPalabraPagina ? "p " : "";
        textoPagina.text = $"{prefijo}{paginaActual + 1} / {paginas.Length}";
    }

    public void MostrarPrimeraPagina()
    {
        MostrarPagina(0);
        paginaActual = 0;
    }
}