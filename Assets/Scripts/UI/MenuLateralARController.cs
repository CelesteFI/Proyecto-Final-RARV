using TMPro;
using UnityEngine;

/// <summary>
/// Menu lateral flotante de la vista AR: se abre con el boton del panel superior y da acceso
/// directo a volver a fijar el plato, al selector de forma (circular / cuadrado) y al catalogo
/// de alimentos. Cero instanciacion: esconde y muestra un panel ya presente en la jerarquia.
/// </summary>
[DisallowMultipleComponent]
public sealed class MenuLateralARController : MonoBehaviour
{
    [Header("Referencias de escena")]
    [SerializeField] private PlateShapeController shapeController;
    [SerializeField] private PlateAnchorManager plateAnchor;
    [SerializeField] private MenuController menu;

    [Header("Etiqueta de estado")]
    [SerializeField] private TMP_Text textoFormaPlato;

    /// <summary>true mientras el menu flotante esta desplegado.</summary>
    public bool Abierto => gameObject.activeSelf;

    /// <summary>Abre el menu flotante sobre la vista AR.</summary>
    public void Abrir()
    {
        gameObject.SetActive(true);
        RefrescarEtiqueta();
    }

    /// <summary>Cierra el menu flotante.</summary>
    public void Cerrar()
    {
        gameObject.SetActive(false);
    }

    /// <summary>Alterna abierto / cerrado desde el boton del panel superior.</summary>
    public void Alternar()
    {
        if (gameObject.activeSelf)
        {
            Cerrar();
            return;
        }

        Abrir();
    }

    /// <summary>Regresa a la calibracion para reajustar el tamano del plato sobre la mesa.</summary>
    public void RefijarPlato()
    {
        Cerrar();

        if (plateAnchor != null)
        {
            plateAnchor.VolverACalibracion();
        }
    }

    /// <summary>Plato circular: actualiza sprite, aro y vuelve a la calibracion.</summary>
    public void SeleccionarFormaCircular()
    {
        Cerrar();

        if (shapeController != null)
        {
            shapeController.SeleccionarCircular();
        }
    }

    /// <summary>Plato cuadrado: actualiza sprite, aro y vuelve a la calibracion.</summary>
    public void SeleccionarFormaCuadrada()
    {
        Cerrar();

        if (shapeController != null)
        {
            shapeController.SeleccionarCuadrado();
        }
    }

    /// <summary>Catálogo de alimentos (guia nutricional del proyecto).</summary>
    public void AbrirCatalogo()
    {
        Cerrar();

        if (menu != null)
        {
            menu.MostrarGuia();
        }
    }

    /// <summary>Dashboard de metas: agua, energia y macronutrientes.</summary>
    public void AbrirMetas()
    {
        Cerrar();

        if (menu != null)
        {
            menu.MostrarMetas();
        }
    }

    /// <summary>Reabre el tutorial de 4 pasos sobre la pantalla actual.</summary>
    public void AbrirTutorial()
    {
        Cerrar();

        if (menu != null)
        {
            menu.AbrirTutorial();
        }
    }

    private void RefrescarEtiqueta()
    {
        if (textoFormaPlato != null)
        {
            textoFormaPlato.text = ARState.EsPlatoCuadrado ? "Forma del plato: Cuadrado" : "Forma del plato: Circular";
        }
    }
}
