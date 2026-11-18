using System.Globalization;
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

    [Header("Etiqueta de gramos modular (numero + sufijo)")]
    [Tooltip("Sufijo de la etiqueta de gramos (' g'). Si falta, el sufijo se compone en textoGramos.")]
    [SerializeField] private TMP_Text textoGramosSufijo;
    [SerializeField] private string sufijoGramos = " g";

    [Header("Referencias de escena")]
    [SerializeField] private VolumeCalculator calculator;
    [SerializeField] private Transform moldeHolograma;
    [SerializeField] private MeshRenderer moldeRenderer;
    [SerializeField] private FoodCatalog catalog;

    [Header("Color del molde parametrico")]
    [Tooltip("Color del cilindro cuando el alimento no tiene color propio en el catalogo.")]
    [SerializeField] private Color colorPorDefecto = new Color(0.85f, 0.85f, 0.85f, 1f);

    [Header("Catalogo por tarjetas y sub-panel de detalle")]
    [Tooltip("Router de pantallas: el detalle devuelve a la vista AR al servir el alimento.")]
    [SerializeField] private MenuController menu;
    [Tooltip("Sub-panel informativo que abren las tarjetas del catalogo.")]
    [SerializeField] private GameObject panelDetalle;
    [SerializeField] private Image detalleIcono;
    [SerializeField] private TMP_Text detalleTitulo;
    [SerializeField] private TMP_Text detalleEstado;
    [SerializeField] private TMP_Text detalleKcalNumero;
    [SerializeField] private TMP_Text detalleKcalSufijo;
    [SerializeField] private TMP_Text detalleProteinaTitulo;
    [SerializeField] private TMP_Text detalleProteinaNumero;
    [SerializeField] private TMP_Text detalleProteinaSufijo;
    [SerializeField] private TMP_Text detalleCarboTitulo;
    [SerializeField] private TMP_Text detalleCarboNumero;
    [SerializeField] private TMP_Text detalleCarboSufijo;
    [SerializeField] private TMP_Text detalleGrasaTitulo;
    [SerializeField] private TMP_Text detalleGrasaNumero;
    [SerializeField] private TMP_Text detalleGrasaSufijo;

    // Alimento abierto ahora mismo en el sub-panel de detalle.
    private string _alimentoDetalle = string.Empty;

    // Reutilizado en cada refresco: no asigna memoria por ajuste de UI.
    private MaterialPropertyBlock _bloqueColor;

    /// <summary>Nombre del alimento que se esta midiendo (lectura para auditoria).</summary>
    public string AlimentoActual { get; private set; }

    /// <summary>true si se esta mostrando un modelo del catalogo en lugar del cilindro de prueba.</summary>
    public bool UsandoModeloDelCatalogo { get; private set; }

    /// <summary>Estado de coccion elegido en la UI: lo leen el catalogo y el sub-panel de detalle.</summary>
    public bool EstadoCocidoActual => toggleCocido != null && toggleCocido.isOn;

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
        // Modularidad: el numero y el sufijo van en dos TextMeshProUGUI distintos.
        int gramosEnteros = Mathf.RoundToInt(gramos);

        if (textoGramos != null)
        {
            textoGramos.text = textoGramosSufijo != null
                ? gramosEnteros.ToString(CultureInfo.InvariantCulture)
                : gramosEnteros.ToString(CultureInfo.InvariantCulture) + sufijoGramos;
        }

        if (textoGramosSufijo != null)
        {
            textoGramosSufijo.text = sufijoGramos;
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

    /// <summary>
    /// Una tarjeta del catalogo: solo abre el sub-panel informativo. No sirve ni escala nada
    /// (eso lo hace el boton "Servir en el plato" del propio sub-panel).
    /// </summary>
    public void AbrirDetalleAlimento(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            return;
        }

        _alimentoDetalle = nombre.Trim();
        RellenarDetalle(_alimentoDetalle);

        if (panelDetalle != null && !panelDetalle.activeSelf)
        {
            panelDetalle.SetActive(true);
        }
    }

    /// <summary>Cierra el sub-panel de detalle y vuelve a la lista de tarjetas.</summary>
    public void CerrarDetalle()
    {
        _alimentoDetalle = string.Empty;

        if (panelDetalle != null && panelDetalle.activeSelf)
        {
            panelDetalle.SetActive(false);
        }
    }

    /// <summary>
    /// Boton "Servir en el plato" del detalle: selecciona el alimento en el dropdown (eso ya
    /// refresca escala y color), cierra el detalle y regresa a la vista AR.
    /// </summary>
    public void ServirEnElPlato()
    {
        if (string.IsNullOrEmpty(_alimentoDetalle))
        {
            return;
        }

        int indice = IndiceDeAlimento(_alimentoDetalle);

        if (indice >= 0 && dropdownAlimentos != null)
        {
            // Notifica: onValueChanged pone al dia el molde, el color y el bloqueo de coccion.
            dropdownAlimentos.value = indice;
        }

        CerrarDetalle();

        if (menu != null)
        {
            menu.MostrarAR();
        }
    }

    /// <summary>Escribe titulo, energia y macros del alimento en los TMP ya serializados.</summary>
    private void RellenarDetalle(string nombre)
    {
        bool cocido = EstadoCocidoActual || (catalog != null && catalog.EsPlatoCompuesto(nombre));

        if (detalleTitulo != null)
        {
            detalleTitulo.text = nombre;
        }

        if (detalleEstado != null)
        {
            detalleEstado.text = (cocido ? "Cocido" : "Crudo") + " · valores por 100 g";
        }

        if (detalleIcono != null)
        {
            detalleIcono.color = catalog != null && catalog.TryGetColor(nombre, cocido, out Color color)
                ? color
                : colorPorDefecto;
        }

        if (!FoodNutritionData.TryGetNutrition(nombre, out FoodNutrition info))
        {
            Escribir(detalleKcalNumero, "--");
            Escribir(detalleProteinaNumero, "--");
            Escribir(detalleCarboNumero, "--");
            Escribir(detalleGrasaNumero, "--");
            return;
        }

        Escribir(detalleKcalNumero, Mathf.RoundToInt(info.Kcal(cocido)).ToString(CultureInfo.InvariantCulture));
        Escribir(detalleKcalSufijo, "kcal / 100 g");

        Escribir(detalleProteinaTitulo, "Proteinas");
        Escribir(detalleProteinaNumero, info.Proteina(cocido).ToString("F1", CultureInfo.InvariantCulture));
        Escribir(detalleProteinaSufijo, "g");

        Escribir(detalleCarboTitulo, "Carbohidratos");
        Escribir(detalleCarboNumero, info.Carbohidrato(cocido).ToString("F1", CultureInfo.InvariantCulture));
        Escribir(detalleCarboSufijo, "g");

        Escribir(detalleGrasaTitulo, "Grasas");
        Escribir(detalleGrasaNumero, info.Grasa(cocido).ToString("F1", CultureInfo.InvariantCulture));
        Escribir(detalleGrasaSufijo, "g");
    }

    private static void Escribir(TMP_Text campo, string valor)
    {
        if (campo != null)
        {
            campo.text = valor;
        }
    }

    /// <summary>Indice del alimento dentro del dropdown (match por texto, sin mayusculas).</summary>
    private int IndiceDeAlimento(string nombre)
    {
        if (dropdownAlimentos == null)
        {
            return -1;
        }

        var opciones = dropdownAlimentos.options;

        for (int i = 0; i < opciones.Count; i++)
        {
            if (string.Equals(opciones[i].text.Trim(), nombre, System.StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
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
