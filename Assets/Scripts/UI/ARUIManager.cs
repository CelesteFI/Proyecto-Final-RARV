using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Puente UI -> FoodCatalog -> VolumeCalculator -> escala del molde. Los tres controles comparten
/// un unico punto de recalculo; no crea objetos: activa el modelo ya presente en la escena o,
/// mientras los modelos de Meshy no esten asignados, usa el cilindro parametrico de prueba.
/// </summary>
[DisallowMultipleComponent]
public sealed class ARUIManager : MonoBehaviour
{
    /// <summary>Densidad usada si el alimento no aparece en FoodDensityData (agua).</summary>
    private const float DensidadPorDefecto = 1f;

    /// <summary>Propiedades de color de URP Lit y del shader Standard.</summary>
    private static readonly int IdColorBase = Shader.PropertyToID("_BaseColor");
    private static readonly int IdColor = Shader.PropertyToID("_Color");

    [Header("Controles de UI")]
    [SerializeField] private Slider sliderGramos;
    [SerializeField] private TMP_Dropdown dropdownAlimentos;
    [SerializeField] private Toggle toggleCocido;
    [SerializeField] private TMP_Text textoGramos;

    [Header("Referencias de escena")]
    [SerializeField] private VolumeCalculator calculator;
    [SerializeField] private Transform moldeHolograma;
    [SerializeField] private MeshRenderer moldeRenderer;
    [SerializeField] private FoodCatalog catalog;

    [Header("Color del molde parametrico")]
    [Tooltip("Color del cilindro cuando el alimento no tiene color propio en el catalogo.")]
    [SerializeField] private Color colorPorDefecto = new Color(0.85f, 0.85f, 0.85f, 1f);

    // Reutilizado en cada refresco: no asigna memoria por ajuste de UI.
    private MaterialPropertyBlock _bloqueColor;

    /// <summary>Nombre del alimento que se esta midiendo (lectura para auditoria).</summary>
    public string AlimentoActual { get; private set; }

    /// <summary>true si se esta mostrando un modelo del catalogo en lugar del cilindro de prueba.</summary>
    public bool UsandoModeloDelCatalogo { get; private set; }

    private void Start()
    {
        if (!ValidarReferencias())
        {
            enabled = false;
            return;
        }

        sliderGramos.onValueChanged.AddListener(OnGramosCambiados);
        dropdownAlimentos.onValueChanged.AddListener(OnAlimentoCambiado);
        toggleCocido.onValueChanged.AddListener(OnCocidoCambiado);

        // Estado de coccion que exige el alimento ya seleccionado (los guisados van bloqueados en cocido).
        ActualizarBloqueoCoccion();

        // Estado inicial: la escala debe corresponder a la UI ya al cargar la escena.
        ActualizarMolde();
    }

    private void OnDestroy()
    {
        if (sliderGramos != null)
        {
            sliderGramos.onValueChanged.RemoveListener(OnGramosCambiados);
        }

        if (dropdownAlimentos != null)
        {
            dropdownAlimentos.onValueChanged.RemoveListener(OnAlimentoCambiado);
        }

        if (toggleCocido != null)
        {
            toggleCocido.onValueChanged.RemoveListener(OnCocidoCambiado);
        }
    }

    private void OnGramosCambiados(float valor) => ActualizarMolde();

    private void OnAlimentoCambiado(int indice)
    {
        // Los platos compuestos solo existen cocidos: el Toggle se marca y se bloquea.
        ActualizarBloqueoCoccion();
        ActualizarMolde();
    }

    private void OnCocidoCambiado(bool estado) => ActualizarMolde();

    /// <summary>Lee la UI, resuelve el modelo y aplica la escala al Transform existente.</summary>
    public void ActualizarMolde()
    {
        AlimentoActual = ObtenerAlimentoActual();

        bool cocido = toggleCocido != null && toggleCocido.isOn;
        float gramos = sliderGramos != null ? sliderGramos.value : 0f;

        // La etiqueta refleja siempre los gramos pedidos, con modelo del catalogo o con el cilindro.
        if (textoGramos != null)
        {
            textoGramos.text = Mathf.RoundToInt(gramos).ToString() + " g";
        }

        FoodModelSlot slot = null;
        bool haySlot = catalog != null && catalog.TryGetSlot(AlimentoActual, cocido, out slot);

        if (haySlot && slot != null && slot.TieneModelo)
        {
            // Modelo real (Meshy): escala uniforme sobre su tamano de referencia.
            catalog.MostrarModelo(slot);
            UsandoModeloDelCatalogo = true;

            if (moldeRenderer != null)
            {
                moldeRenderer.enabled = false;
            }

            moldeHolograma.localScale = VolumeCalculator.EscalaUniformeParaGramos(gramos, slot.gramosReferencia);
            return;
        }

        // Sin modelo asignado: cilindro parametrico de prueba.
        UsandoModeloDelCatalogo = false;

        if (catalog != null)
        {
            catalog.OcultarTodosLosModelos();
        }

        if (moldeRenderer != null)
        {
            moldeRenderer.enabled = true;
            AplicarColorMolde(ObtenerColorBase(cocido));
        }

        if (!FoodDensityData.TryGetDensity(AlimentoActual, cocido, out float densidad))
        {
            Debug.LogWarning("[ARUIManager] Sin densidad registrada para '" + AlimentoActual +
                             "' (cocido: " + cocido + "). Se usa " + DensidadPorDefecto + " g/cm3.", this);
            densidad = DensidadPorDefecto;
        }

        float radioPlato = ARState.HasPlate ? ARState.SelectedPlateRadius : ARState.DefaultPlateRadiusCm;

        moldeHolograma.localScale = calculator.CalculateLocalScale(densidad, gramos, radioPlato);
    }

    /// <summary>
    /// Los cuatro guisados son exclusivamente cocidos: al elegirlos el Toggle aparece marcado
    /// en "Cocido" y bloqueado (interactable = false) para que no se pueda alterar por error.
    /// </summary>
    private void ActualizarBloqueoCoccion()
    {
        if (toggleCocido == null)
        {
            return;
        }

        bool soloCocido = catalog != null && catalog.EsPlatoCompuesto(ObtenerAlimentoActual());
        toggleCocido.interactable = !soloCocido;

        if (soloCocido && !toggleCocido.isOn)
        {
            // Sin notificar: el refresco del molde ya ocurre en el mismo paso.
            toggleCocido.SetIsOnWithoutNotify(true);
        }
    }

    /// <summary>Color base que corresponde al alimento y estado actuales.</summary>
    private Color ObtenerColorBase(bool cocido)
    {
        if (catalog != null && catalog.TryGetColor(AlimentoActual, cocido, out Color color))
        {
            return color;
        }

        return colorPorDefecto;
    }

    /// <summary>Pinta el material del cilindro sin instanciar materiales ni objetos nuevos.</summary>
    private void AplicarColorMolde(Color color)
    {
        if (moldeRenderer == null)
        {
            return;
        }

        if (_bloqueColor == null)
        {
            _bloqueColor = new MaterialPropertyBlock();
        }

        moldeRenderer.GetPropertyBlock(_bloqueColor);
        _bloqueColor.SetColor(IdColorBase, color);
        _bloqueColor.SetColor(IdColor, color);
        moldeRenderer.SetPropertyBlock(_bloqueColor);
    }

    /// <summary>Nombre del alimento segun la opcion seleccionada (texto definido en el Inspector).</summary>
    private string ObtenerAlimentoActual()
    {
        if (dropdownAlimentos == null)
        {
            return string.Empty;
        }

        int indice = dropdownAlimentos.value;
        var opciones = dropdownAlimentos.options;

        if ((uint)indice >= (uint)opciones.Count)
        {
            return string.Empty;
        }

        return opciones[indice].text.Trim();
    }

    private bool ValidarReferencias()
    {
        bool valido = true;

        valido &= Validar(sliderGramos, nameof(sliderGramos));
        valido &= Validar(dropdownAlimentos, nameof(dropdownAlimentos));
        valido &= Validar(toggleCocido, nameof(toggleCocido));
        valido &= Validar(calculator, nameof(calculator));
        valido &= Validar(moldeHolograma, nameof(moldeHolograma));

        if (catalog == null)
        {
            Debug.LogWarning("[ARUIManager] Sin FoodCatalog: se usara siempre el cilindro parametrico de prueba.", this);
        }

        return valido;
    }

    private bool Validar(Object referencia, string nombreCampo)
    {
        if (referencia != null)
        {
            return true;
        }

        Debug.LogError("[ARUIManager] Referencia sin asignar: " + nombreCampo + ".", this);
        return false;
    }
}
