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
    [Tooltip("Anclaje del plato: garantiza que el molde servido quede visible apoyado en la mesa.")]
    [SerializeField] private PlateAnchorManager plateAnchor;

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

    [Header("Sub-panel de detalle: beneficio y sugerencia (base de datos nutricional)")]
    [Tooltip("Titulo y texto del beneficio principal del alimento.")]
    [SerializeField] private TMP_Text detalleBeneficioTitulo;
    [SerializeField] private TMP_Text detalleBeneficio;
    [Tooltip("Titulo y texto de la sugerencia de combinacion del alimento.")]
    [SerializeField] private TMP_Text detalleSugerenciaTitulo;
    [SerializeField] private TMP_Text detalleSugerencia;

    [Header("Recuadro de metas del Panel AR (Kcal del alimento elegido)")]
    [Tooltip("Miniatura del alimento que muestra el recuadro superior derecho del Panel_AR.")]
    [SerializeField] private Image metasIcono;
    [SerializeField] private TMP_Text metasNombre;
    [SerializeField] private TMP_Text metasKcalNumero;
    [SerializeField] private TMP_Text metasKcalSufijo;
    [SerializeField] private TMP_Text metasProteinaNumero;
    [SerializeField] private TMP_Text metasProteinaSufijo;
    [SerializeField] private TMP_Text metasCarboNumero;
    [SerializeField] private TMP_Text metasCarboSufijo;
    [SerializeField] private TMP_Text metasGrasaNumero;
    [SerializeField] private TMP_Text metasGrasaSufijo;

    [Tooltip("Menu lateral flotante: se cierra al servir para dejar ver el plato.")]
    [SerializeField] private MenuLateralARController menuLateral;

    [Header("Metas del Panel AR: porcion ideal personalizada")]
    [Tooltip("Linea que recomienda la porcion ideal de este alimento segun el perfil biometrico.")]
    [SerializeField] private TMP_Text metasIdeal;
    [Tooltip("Perfil biometrico: aporta peso actual, peso objetivo y kcal diarias.")]
    [SerializeField] private BiometricProfile perfil;

    // Alimento abierto ahora mismo en el sub-panel de detalle.
    private string _alimentoDetalle = string.Empty;

    // true cuando la comida ya se sirvio: el molde 3D no cambia de alimento hasta el proximo
    // "Servir en el plato" (cambiar de alimento en el dropdown no toca el modelo en pantalla).
    private bool _servidoEnPlato;

    // Alimento y estado que estan realmente servidos en el plato. Pueden diferir del dropdown: la
    // seleccion no toca el modelo hasta volver a pulsar "Servir en el plato".
    private string _alimentoServido = string.Empty;
    private bool _cocidoServido = true;

    // Firma del ultimo calculo de la porcion ideal: solo se reescribe si cambia el perfil o el
    // alimento elegido (el texto no se recalcula en cada ajuste del slider).
    private string _claveIdeal = string.Empty;

    // Reutilizado en cada refresco: no asigna memoria por ajuste de UI.
    private MaterialPropertyBlock _bloqueColor;

    /// <summary>Nombre del alimento que se esta midiendo (lectura para auditoria).</summary>
    public string AlimentoActual { get; private set; }

    /// <summary>true si se esta mostrando un modelo del catalogo en lugar del cilindro de prueba.</summary>
    public bool UsandoModeloDelCatalogo { get; private set; }

    /// <summary>Alimento elegido ahora mismo en el dropdown del Panel_AR.</summary>
    public string AlimentoSeleccionado => ObtenerAlimentoActual();

    /// <summary>Estado de coccion elegido en la UI: lo leen el catalogo y el sub-panel de detalle.</summary>
    public bool EstadoCocidoActual => toggleCocido != null && toggleCocido.isOn;

    /// <summary>Alimento que esta servido ahora mismo en el plato (lectura para auditoria).</summary>
    public string AlimentoServido => _alimentoServido;

    /// <summary>
    /// true solo si hay un modelo servido y visible: el Slider y el Toggle refrescan en vivo ese
    /// modelo; el resto del tiempo el molde permanece apagado y no se toca.
    /// </summary>
    public bool HayComidaServida =>
        _servidoEnPlato && moldeHolograma != null && moldeHolograma.gameObject.activeSelf;

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

    /// <summary>
    /// Slider de porcion. Con comida ya servida reescala el modelo en pantalla en el acto usando
    /// la densidad del alimento servido; sin comida servida el molde sigue apagado.
    /// </summary>
    private void OnGramosCambiados(float valor)
    {
        if (_servidoEnPlato)
        {
            Refrescar(false);
            ActualizarMoldeServido(false);
            return;
        }

        ActualizarMolde();
    }

    /// <summary>
    /// Dropdown: nunca toca el modelo 3D del plato. Solo refresca el recuadro de metas y el estado
    /// de coccion que exige el alimento elegido; el modelo cambia al pulsar "Servir en el plato".
    /// </summary>
    private void OnAlimentoCambiado(int indice)
    {
        // Los platos compuestos solo existen cocidos: el Toggle se marca y se bloquea.
        ActualizarBloqueoCoccion();
        ActualizarMolde();
    }

    /// <summary>
    /// Toggle crudo/cocido. Con comida servida cambia en el acto el slot/color del modelo servido
    /// (los guisados lo bloquean) y recalcula su escala; sin comida servida solo refresca la UI.
    /// </summary>
    private void OnCocidoCambiado(bool estado)
    {
        if (!_servidoEnPlato)
        {
            ActualizarMolde();
            return;
        }

        bool servidoEsPlato = catalog != null && catalog.EsPlatoCompuesto(_alimentoServido);

        if (!servidoEsPlato)
        {
            _cocidoServido = estado;
        }

        Refrescar(false);
        ActualizarMoldeServido(!servidoEsPlato);
    }

    /// <summary>
    /// Recalculo de toda la UI. Sin comida servida el molde esta apagado y no se toca (el Slider y
    /// el Toggle solo refrescan sus cifras); el modelo 3D cambia unicamente al pulsar "Servir en el
    /// plato" o, ya servido, cuando el Slider o el Toggle lo refrescan en vivo.
    /// </summary>
    public void ActualizarMolde()
    {
        Refrescar(false);
    }

    /// <summary>
    /// Lee la UI, calcula los gramos reales de la porcion y refresca el recuadro de metas. Solo
    /// escribe en el molde 3D cuando se pide expresamente (servir en el plato).
    /// </summary>
    private void Refrescar(bool aplicarAlMolde)
    {
        AlimentoActual = ObtenerAlimentoActual();

        bool cocido = toggleCocido != null && toggleCocido.isOn;

        // El slider fija el VOLUMEN de la porcion (cm3) y la masa real depende de la densidad del
        // alimento en el estado elegido: G = volumen x densidad. De ahi salen los valores del
        // recuadro de metas (porcion real, no 100 g) y la escala del molde.
        float volumen = sliderGramos != null ? sliderGramos.value : 0f;
        float densidad = ResolverDensidad(AlimentoActual, cocido);
        float gramos = volumen * densidad;

        RefrescarMetasDelAlimento(cocido, gramos);

        // La etiqueta refleja siempre los gramos reales, con modelo del catalogo o con el cilindro.
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

        if (aplicarAlMolde)
        {
            AplicarModeloYEscala(AlimentoActual, cocido, gramos);
        }
    }

    /// <summary>
    /// Escribe en el molde el alimento, el estado y la porcion indicados: elige el slot del catalogo
    /// (o el cilindro parametrico) y aplica la escala por MASA servida. Es el unico punto que cambia
    /// el alimento del modelo 3D, y solo lo llaman "Servir en el plato" y el Toggle crudo/cocido.
    /// </summary>
    private void AplicarModeloYEscala(string nombre, bool cocido, float gramos)
    {
        FoodModelSlot slot = null;
        bool haySlot = catalog != null && catalog.TryGetSlot(nombre, cocido, out slot);

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
            AplicarColorMolde(ObtenerColorDe(nombre, cocido));
        }

        float radioPlato = ARState.HasPlate ? ARState.SelectedPlateRadius : ARState.DefaultPlateRadiusCm;

        moldeHolograma.localScale = calculator.CalculateLocalScaleDeMasa(gramos, radioPlato);
    }

    /// <summary>
    /// Refresco en vivo del modelo ya servido: el volumen sale del slider y la densidad del alimento
    /// servido (no del dropdown). Con cambiarSlot tambien cambia el modelo y el color del catalogo
    /// (Toggle crudo/cocido); sin el, solo se reescala la misma pieza que ya esta en el plato.
    /// </summary>
    private void ActualizarMoldeServido(bool cambiarSlot)
    {
        if (!HayComidaServida || string.IsNullOrEmpty(_alimentoServido))
        {
            return;
        }

        float volumen = sliderGramos != null ? sliderGramos.value : 0f;
        float densidad = ResolverDensidad(_alimentoServido, _cocidoServido);
        float gramos = volumen * densidad;

        if (cambiarSlot)
        {
            AplicarModeloYEscala(_alimentoServido, _cocidoServido, gramos);
            return;
        }

        FoodModelSlot servido = null;
        bool conModelo = catalog != null &&
                         catalog.TryGetSlot(_alimentoServido, _cocidoServido, out servido) &&
                         servido != null && servido.TieneModelo;

        if (conModelo)
        {
            moldeHolograma.localScale = VolumeCalculator.EscalaUniformeParaGramos(gramos, servido.gramosReferencia);
            return;
        }

        float radioPlato = ARState.HasPlate ? ARState.SelectedPlateRadius : ARState.DefaultPlateRadiusCm;

        moldeHolograma.localScale = calculator.CalculateLocalScaleDeMasa(gramos, radioPlato);
    }

    /// <summary>Densidad aparente (g/cm3) del alimento en el estado elegido; 1 g/cm3 si falta.</summary>
    private float ResolverDensidad(string nombre, bool cocido)
    {
        if (FoodDensityData.TryGetDensity(nombre, cocido, out float densidad))
        {
            return densidad;
        }

        Debug.LogWarning("[ARUIManager] Sin densidad registrada para '" + nombre +
                         "' (cocido: " + cocido + "). Se usa " + DensidadPorDefecto + " g/cm3.", this);

        return DensidadPorDefecto;
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

    /// <summary>Color base del catalogo para el alimento y el estado indicados.</summary>
    private Color ObtenerColorDe(string nombre, bool cocido)
    {
        if (catalog != null && catalog.TryGetColor(nombre, cocido, out Color color))
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
    /// Boton "Servir en el plato" del Panel_AR: aplica el alimento y los gramos elegidos al molde,
    /// lo deja visible sobre el plato anclado y cierra el menu lateral para dejar el plato a la vista.
    /// </summary>
    public void ServirEnElPlato()
    {
        // Alimento y estado que quedan servidos: el modelo en pantalla los conserva hasta el proximo
        // "Servir en el plato", aunque despues cambie el Dropdown o el Toggle.
        _alimentoServido = ObtenerAlimentoActual();
        _cocidoServido = EstadoCocidoActual ||
                         (catalog != null && catalog.EsPlatoCompuesto(_alimentoServido));

        // Aplica al molde los valores vigentes de la UI (alimento, estado, volumen) justo ahora: es
        // el unico momento en que el modelo 3D en pantalla cambia de alimento.
        Refrescar(true);
        _servidoEnPlato = true;

        // El molde ya escalado se coloca en el plato fijado y se enciende: servirlo nunca deja la
        // vista vacia aunque el anclaje se hubiera reiniciado al pasar por la calibracion.
        if (plateAnchor != null)
        {
            plateAnchor.MostrarComidaServida();
        }

        if (menuLateral != null && menuLateral.Abierto)
        {
            menuLateral.Cerrar();
        }

        Debug.Log("[ARUIManager] Servido en el plato: '" + AlimentoActual + "'.", this);
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
            // El icono del catalogo es una imagen real del alimento: se muestra tal cual. El color
            // del catalogo solo se usa mientras el alimento no tenga sprite asignado.
            Sprite miniatura = MiniaturaDeAlimento(nombre);

            if (miniatura != null)
            {
                detalleIcono.sprite = miniatura;
                detalleIcono.color = Color.white;
            }
            else
            {
                detalleIcono.color = catalog != null && catalog.TryGetColor(nombre, cocido, out Color color)
                    ? color
                    : colorPorDefecto;
            }
        }

        if (!FoodNutritionData.TryGetNutrition(nombre, out FoodNutrition info))
        {
            Escribir(detalleKcalNumero, "--");
            Escribir(detalleProteinaNumero, "--");
            Escribir(detalleCarboNumero, "--");
            Escribir(detalleGrasaNumero, "--");
            Escribir(detalleBeneficio, "--");
            Escribir(detalleSugerencia, "--");
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

        // Beneficio y sugerencia: texto fijo de la base de datos nutricional, nunca se calcula aqui.
        Escribir(detalleBeneficioTitulo, "Beneficio");
        Escribir(detalleBeneficio, TextoDeBaseDeDatos(info.Beneficio));

        Escribir(detalleSugerenciaTitulo, "Sugerencia de combinacion");
        Escribir(detalleSugerencia, TextoDeBaseDeDatos(info.Sugerencia));
    }

    /// <summary>Texto largo de la base de datos nutricional: "--" cuando el alimento no lo tiene cargado.</summary>
    private static string TextoDeBaseDeDatos(string valor)
    {
        return string.IsNullOrWhiteSpace(valor) ? "--" : valor;
    }

    /// <summary>
    /// Recuadro de metas del Panel_AR: nombre, miniatura y los valores de la porcion elegida. Los
    /// gramos reales llegan ya calculados (volumen del slider x densidad) y cada cifra se obtiene
    /// con la regla de tres de la base de datos por 100 g.
    /// </summary>
    private void RefrescarMetasDelAlimento(bool cocido, float gramos)
    {
        string nombre = AlimentoActual;

        Escribir(metasNombre, string.IsNullOrEmpty(nombre) ? "--" : nombre);

        if (metasIcono != null)
        {
            Sprite miniatura = MiniaturaDeAlimento(nombre);

            if (miniatura != null)
            {
                metasIcono.sprite = miniatura;
                metasIcono.color = Color.white;
            }
            else
            {
                metasIcono.color = catalog != null && catalog.TryGetColor(nombre, cocido, out Color color)
                    ? color
                    : colorPorDefecto;
            }
        }

        if (!FoodNutritionData.TryGetNutrition(nombre, out FoodNutrition info))
        {
            Escribir(metasKcalNumero, "--");
            Escribir(metasProteinaNumero, "--");
            Escribir(metasCarboNumero, "--");
            Escribir(metasGrasaNumero, "--");
            Escribir(metasIdeal, "--");
            _claveIdeal = string.Empty;
            return;
        }

        // Valor de la porcion = (valor por 100 g / 100) x gramos reales.
        float factor = Mathf.Max(gramos, 0f) / 100f;

        Escribir(metasKcalNumero, Mathf.RoundToInt(info.Kcal(cocido) * factor).ToString(CultureInfo.InvariantCulture));
        Escribir(metasKcalSufijo, "kcal");
        Escribir(metasProteinaNumero, (info.Proteina(cocido) * factor).ToString("F1", CultureInfo.InvariantCulture));
        Escribir(metasProteinaSufijo, "g");
        Escribir(metasCarboNumero, (info.Carbohidrato(cocido) * factor).ToString("F1", CultureInfo.InvariantCulture));
        Escribir(metasCarboSufijo, "g");
        Escribir(metasGrasaNumero, (info.Grasa(cocido) * factor).ToString("F1", CultureInfo.InvariantCulture));
        Escribir(metasGrasaSufijo, "g");

        RefrescarPorcionIdeal(nombre, cocido, info);
    }

    /// <summary>
    /// Porcion ideal para este alimento segun el perfil biometrico: toma la fraccion de las kcal
    /// diarias que corresponde a una comida principal (mas si el usuario quiere subir de peso,
    /// menos si quiere bajar) y la convierte a gramos con la densidad energetica del alimento.
    /// El texto solo se reescribe cuando cambia el perfil o el alimento/estado elegido.
    /// </summary>
    private void RefrescarPorcionIdeal(string nombre, bool cocido, FoodNutrition info)
    {
        if (metasIdeal == null)
        {
            return;
        }

        if (perfil == null || perfil.KcalObjetivo <= 0f)
        {
            if (_claveIdeal.Length > 0)
            {
                _claveIdeal = string.Empty;
            }

            Escribir(metasIdeal, "Ideal: calcula tu perfil biometrico");
            return;
        }

        // Firma del perfil + alimento: mientras no cambie, el valor se da por bueno.
        string clave = nombre + "|" + cocido + "|" +
                       perfil.KcalObjetivo.ToString("F1", CultureInfo.InvariantCulture) + "|" +
                       perfil.PesoActualKg.ToString("F1", CultureInfo.InvariantCulture) + "|" +
                       perfil.PesoObjetivoKg.ToString("F1", CultureInfo.InvariantCulture);

        if (clave == _claveIdeal)
        {
            return;
        }

        _claveIdeal = clave;

        float kcalPor100 = info.Kcal(cocido);

        if (kcalPor100 <= 0f)
        {
            Escribir(metasIdeal, "Ideal: --");
            return;
        }

        // Una comida principal cubre el 25 % de la energia diaria; subir de peso pide un plato mas
        // energetico y bajar de peso uno mas ligero.
        float fraccion = 0.25f;

        if (perfil.TienePesoObjetivo)
        {
            float diferencia = perfil.PesoObjetivoKg - perfil.PesoActualKg;

            if (diferencia > 0.5f)
            {
                fraccion = 0.30f;
            }
            else if (diferencia < -0.5f)
            {
                fraccion = 0.20f;
            }
        }

        float gramosIdeales = Mathf.Clamp(fraccion * perfil.KcalObjetivo * 100f / kcalPor100, 30f, 600f);
        gramosIdeales = Mathf.Round(gramosIdeales / 5f) * 5f;

        int kcalIdeales = Mathf.RoundToInt(gramosIdeales * kcalPor100 / 100f);

        Escribir(metasIdeal, "Ideal para ti: " + gramosIdeales.ToString("F0", CultureInfo.InvariantCulture) +
                              " g · " + kcalIdeales.ToString(CultureInfo.InvariantCulture) + " kcal");
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

    /// <summary>Miniatura del alimento elegido: la misma imagen que muestra el dropdown.</summary>
    private Sprite MiniaturaDeAlimento(string nombre)
    {
        int indice = IndiceDeAlimento(nombre);

        if (indice < 0)
        {
            return null;
        }

        return dropdownAlimentos.options[indice].image;
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

        if (plateAnchor == null)
        {
            Debug.LogWarning("[ARUIManager] Sin PlateAnchorManager: servir en el plato no puede colocar el molde " +
                             "sobre la mesa fijada.", this);
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
