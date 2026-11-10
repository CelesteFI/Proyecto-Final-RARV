using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pantalla de calibracion del plato: el Slider define el radio real del plato (5 a 30 cm),
/// el circulo guia se redimensiona en tiempo real para que el usuario lo haga coincidir con
/// su plato fisico y "Fijar Plato" guarda el radio en ARState antes de pasar a la vista AR.
/// No instancia objetos: solo mueve el RectTransform y escribe el estado global.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlateCalibrationController : MonoBehaviour
{
    [Header("Controles de la pantalla")]
    [SerializeField] private Slider sliderRadioPlato;
    [SerializeField] private Button btnFijarPlatoCalibracion;
    [SerializeField] private TMP_Text textoRadioPlato;
    [SerializeField] private RectTransform circuloGuia;

    [Header("Referencias de escena")]
    [SerializeField] private MenuController menu;
    [SerializeField] private PlateAnchorManager plateAnchor;

    [Header("Escala del circulo guia")]
    [Tooltip("Pixeles de UI por cada centimetro de plato. Con 14, un plato de 24 cm de diametro ocupa 336 px.")]
    [SerializeField, Min(0.01f)] private float pixelsPorCentimetro = 14f;

    [Header("Rango del plato")]
    [SerializeField] private float radioMinimoCm = ARState.MinPlateRadiusCm;
    [SerializeField] private float radioMaximoCm = ARState.MaxPlateRadiusCm;

    /// <summary>Radio que el usuario tiene seleccionado ahora mismo, en centimetros.</summary>
    public float RadioSeleccionadoCm =>
        sliderRadioPlato != null ? sliderRadioPlato.value : ARState.DefaultPlateRadiusCm;

    private void OnEnable()
    {
        if (!ValidarReferencias())
        {
            return;
        }

        sliderRadioPlato.minValue = radioMinimoCm;
        sliderRadioPlato.maxValue = radioMaximoCm;
        sliderRadioPlato.wholeNumbers = false;

        // Primera visita: radio por defecto. Visitas siguientes: el ultimo radio confirmado.
        sliderRadioPlato.value = ARState.HasPlate ? ARState.SelectedPlateRadius : ARState.DefaultPlateRadiusCm;

        sliderRadioPlato.onValueChanged.AddListener(OnRadioCambiado);
        btnFijarPlatoCalibracion.onClick.AddListener(FijarPlato);

        // Cada vez que se entra a calibrar, el molde vuelve a seguir el plano detectado.
        if (plateAnchor != null)
        {
            plateAnchor.ReiniciarColocacion();
        }

        ActualizarVisual(RadioSeleccionadoCm);
    }

    private void OnDisable()
    {
        if (sliderRadioPlato != null)
        {
            sliderRadioPlato.onValueChanged.RemoveListener(OnRadioCambiado);
        }

        if (btnFijarPlatoCalibracion != null)
        {
            btnFijarPlatoCalibracion.onClick.RemoveListener(FijarPlato);
        }
    }

    private void OnRadioCambiado(float valor) => ActualizarVisual(valor);

    /// <summary>Redimensiona el circulo guia y actualiza la etiqueta con el radio y el diametro.</summary>
    public void ActualizarVisual(float radioCm)
    {
        float diametroPx = 2f * radioCm * pixelsPorCentimetro;

        if (circuloGuia != null)
        {
            circuloGuia.sizeDelta = new Vector2(diametroPx, diametroPx);
        }

        if (textoRadioPlato != null)
        {
            textoRadioPlato.text = "Radio del plato: " + radioCm.ToString("F1") +
                                   " cm   ·   Diametro: " + (2f * radioCm).ToString("F1") + " cm";
        }
    }

    /// <summary>Guarda el radio calibrado y transiciona a la vista AR.</summary>
    public void FijarPlato()
    {
        ARState.SetPlateRadius(RadioSeleccionadoCm);

        // El molde y el aro punteado solo aparecen al fijar el plato, ya dentro de la vista AR.
        if (plateAnchor != null)
        {
            plateAnchor.FijarPlato();
        }

        if (menu != null)
        {
            menu.MostrarAR();
        }
    }

    private bool ValidarReferencias()
    {
        bool valido = true;

        valido &= Validar(sliderRadioPlato, nameof(sliderRadioPlato));
        valido &= Validar(btnFijarPlatoCalibracion, nameof(btnFijarPlatoCalibracion));
        valido &= Validar(textoRadioPlato, nameof(textoRadioPlato));
        valido &= Validar(circuloGuia, nameof(circuloGuia));
        valido &= Validar(menu, nameof(menu));

        return valido;
    }

    private bool Validar(Object referencia, string nombreCampo)
    {
        if (referencia != null)
        {
            return true;
        }

        Debug.LogError("[PlateCalibrationController] Referencia sin asignar: " + nombreCampo + ".", this);
        return false;
    }
}
