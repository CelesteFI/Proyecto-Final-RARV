using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Selector de forma del plato fisico (circular / cuadrado). Cambia el sprite del circulo guia de
/// la calibracion, conmuta los aros AR pre-creados (guia / fijado) y fuerza el regreso a la
/// pantalla de calibracion para reajustar el tamano. Cero instanciacion: solo cambia un sprite
/// ya serializado y activa o desactiva GameObjects que ya existen en la jerarquia.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlateShapeController : MonoBehaviour
{
    [Header("Guia en pantalla (calibracion)")]
    [SerializeField] private Image circuloGuia;
    [SerializeField] private Sprite spriteCircular;
    [SerializeField] private Sprite spriteCuadrado;
    [SerializeField] private TMP_Text textoForma;

    [Header("Referencias de escena")]
    [SerializeField] private PlateAnchorManager plateAnchor;
    [SerializeField] private MenuController menu;

    /// <summary>Forma elegida ahora mismo (lectura para auditoria).</summary>
    public PlateShape FormaActual => ARState.SelectedShape;

    private void Start()
    {
        // La escena arranca en circular: se deja la guia y los aros coherentes sin navegar.
        AplicarVisual(ARState.SelectedShape);
    }

    /// <summary>Plato circular: guia y aro fijado redondos.</summary>
    public void SeleccionarCircular()
    {
        AplicarForma(PlateShape.Circular);
    }

    /// <summary>Plato cuadrado: guia y aro fijado con cuatro lados rectos.</summary>
    public void SeleccionarCuadrado()
    {
        AplicarForma(PlateShape.Cuadrado);
    }

    /// <summary>Alterna entre circular y cuadrado desde un unico boton.</summary>
    public void AlternarForma()
    {
        AplicarForma(ARState.EsPlatoCuadrado ? PlateShape.Circular : PlateShape.Cuadrado);
    }

    /// <summary>
    /// Registra la forma, actualiza sprite y aros, reinicia el anclaje y devuelve al usuario a la
    /// calibracion para que reajuste el tamano sobre la mesa.
    /// </summary>
    public void AplicarForma(PlateShape forma)
    {
        ARState.SetPlateShape(forma);

        AplicarVisual(forma);

        if (plateAnchor != null)
        {
            plateAnchor.ReiniciarColocacion();
        }

        if (menu != null)
        {
            menu.MostrarCalibracion();
        }
    }

    /// <summary>Refresca sprite, etiqueta y aros sin navegar entre pantallas.</summary>
    private void AplicarVisual(PlateShape forma)
    {
        if (circuloGuia != null)
        {
            Sprite sprite = forma == PlateShape.Cuadrado ? spriteCuadrado : spriteCircular;

            if (sprite != null)
            {
                circuloGuia.sprite = sprite;
            }
            else
            {
                Debug.LogWarning("[PlateShapeController] Falta asignar el sprite de la forma " + forma + ".", this);
            }
        }

        if (textoForma != null)
        {
            textoForma.text = forma == PlateShape.Cuadrado ? "Forma: Cuadrado" : "Forma: Circular";
        }

        // Los cuatro aros viven en PlateAnchorManager: ahi se decide cual se enciende.
        if (plateAnchor != null)
        {
            plateAnchor.AplicarForma(forma);
        }
    }
}
