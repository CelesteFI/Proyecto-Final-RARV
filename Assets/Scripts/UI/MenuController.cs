using TMPro;
using UnityEngine;

/// <summary>
/// Router de pantallas de NutriAR. Los paneles existen en la jerarquia del Canvas: solo se
/// conmutan con SetActive (cero instanciacion en runtime) y, en el mismo punto unico de
/// conmutacion, se habilita o apaga la camara AR: solo la calibracion del plato y la vista
/// AR necesitan el mundo fisico; el resto de pantallas se dibujan sobre fondo estatico.
/// Tambien gobierna el tutorial de 4 pasos, que es una capa modal que no participa del router.
/// </summary>
[DisallowMultipleComponent]
public sealed class MenuController : MonoBehaviour
{
    [Header("Paneles del Canvas")]
    [SerializeField] private GameObject panelInicio;        // Panel_Splash
    [SerializeField] private GameObject panelMenuLateral;   // Panel_MenuPrincipal
    [SerializeField] private GameObject panelPerfil;        // Panel_Perfil
    [SerializeField] private GameObject panelAR;            // Panel_AR (contiene PanelSuperior y PanelInferior)
    [SerializeField] private GameObject panelCalibracion;   // Panel_CalibracionPlato
    [SerializeField] private GameObject panelGuia;          // Panel_GuiaNutricional (catalogo por tarjetas)
    [SerializeField] private GameObject panelDashboard;     // Panel_DashboardMetas

    [Header("Camara AR")]
    [SerializeField] private ARCameraController arCameraController;

    [Header("Catalogo por tarjetas")]
    [Tooltip("Se avisa al abrir el catalogo para que cierre el sub-panel de detalle anterior.")]
    [SerializeField] private ARUIManager arUI;

    [Header("Tutorial de inicio (capa modal)")]
    [Tooltip("Panel_Tutorial: se abre y se cierra con SetActive, sin tocar el router de pantallas.")]
    [SerializeField] private GameObject panelTutorial;
    [Tooltip("Los 4 pasos creados en la jerarquia: Forma > Tamano > Fijar > Servir.")]
    [SerializeField] private GameObject[] pasosTutorial = new GameObject[4];
    [SerializeField] private TMP_Text indicadorTutorial;

    private int _pasoTutorial;

    // Pantalla desde la que se abrio el dashboard de metas: decide adonde vuelve su boton Volver.
    private GameObject _origenMetas;

    private void Start()
    {
        if (!ValidarReferencias())
        {
            enabled = false;
            return;
        }

        // El tutorial arranca cerrado y en su primer paso.
        CerrarTutorial();

        // El panel que este visible al cargar la escena decide el estado inicial de la camara.
        ActualizarCamaraAR(PanelActivo());
    }

    /// <summary>Splash: estado de arranque de la escena.</summary>
    public void MostrarInicio() => ActivarPanel(panelInicio);

    /// <summary>Menu principal: punto de partida de la navegacion.</summary>
    public void MostrarMenuPrincipal() => ActivarPanel(panelMenuLateral);

    /// <summary>Pantalla de calibracion del plato (camara AR activa).</summary>
    public void MostrarCalibracion() => ActivarPanel(panelCalibracion);

    /// <summary>Perfil biometrico (sin camara AR).</summary>
    public void MostrarPerfil() => ActivarPanel(panelPerfil);

    /// <summary>Catalogo por tarjetas (sin camara AR). Cierra cualquier detalle abierto antes.</summary>
    public void MostrarGuia()
    {
        if (arUI != null)
        {
            arUI.CerrarDetalle();
        }

        ActivarPanel(panelGuia);
    }

    /// <summary>
    /// Dashboard de metas: agua, energia y macronutrientes (sin camara AR). Antes de abrirlo se
    /// recuerda la pantalla de origen para que su boton Volver sea contextual.
    /// </summary>
    public void MostrarMetas()
    {
        GameObject actual = PanelActivo();

        if (actual != panelDashboard)
        {
            _origenMetas = actual;
        }

        ActivarPanel(panelDashboard);
    }

    /// <summary>
    /// Boton Volver del dashboard: regresa a la pantalla desde la que se abrio. Abierto desde la
    /// vista AR con el plato fijado, se vuelve al Panel_AR con el plato y la comida intactos (no
    /// se reinicia el anclaje ni la calibracion); abierto desde el menu principal, se vuelve a el.
    /// </summary>
    public void VolverDesdeMetas()
    {
        GameObject destino = _origenMetas;

        if (destino == null || destino == panelDashboard)
        {
            destino = panelMenuLateral;
        }

        ActivarPanel(destino);
    }

    /// <summary>Vista AR de seleccion y porciones (camara AR activa).</summary>
    public void MostrarAR() => ActivarPanel(panelAR);

    /// <summary>Alterna entre el menu principal y la vista AR.</summary>
    public void ToggleMenuLateral()
    {
        if (panelMenuLateral != null && panelMenuLateral.activeSelf)
        {
            MostrarAR();
            return;
        }

        MostrarMenuPrincipal();
    }

    /// <summary>Abre el tutorial de bienvenida por el primer paso.</summary>
    public void AbrirTutorial()
    {
        if (panelTutorial == null)
        {
            return;
        }

        _pasoTutorial = 0;
        panelTutorial.SetActive(true);
        RefrescarTutorial();
    }

    /// <summary>Cierra el tutorial (el boton de ayuda lo reabre cuando haga falta).</summary>
    public void CerrarTutorial()
    {
        if (panelTutorial != null && panelTutorial.activeSelf)
        {
            panelTutorial.SetActive(false);
        }
    }

    /// <summary>Paso siguiente del tutorial.</summary>
    public void TutorialSiguiente()
    {
        if (pasosTutorial == null || pasosTutorial.Length == 0)
        {
            return;
        }

        _pasoTutorial = Mathf.Min(_pasoTutorial + 1, pasosTutorial.Length - 1);
        RefrescarTutorial();
    }

    /// <summary>Paso anterior del tutorial.</summary>
    public void TutorialAnterior()
    {
        if (pasosTutorial == null || pasosTutorial.Length == 0)
        {
            return;
        }

        _pasoTutorial = Mathf.Max(_pasoTutorial - 1, 0);
        RefrescarTutorial();
    }

    /// <summary>Enciende solo el paso actual y actualiza el indicador "1 / 4".</summary>
    private void RefrescarTutorial()
    {
        if (pasosTutorial != null)
        {
            for (int i = 0; i < pasosTutorial.Length; i++)
            {
                GameObject paso = pasosTutorial[i];

                if (paso == null)
                {
                    continue;
                }

                bool debeEstarActivo = i == _pasoTutorial;

                if (paso.activeSelf != debeEstarActivo)
                {
                    paso.SetActive(debeEstarActivo);
                }
            }

            if (indicadorTutorial != null && pasosTutorial.Length > 0)
            {
                indicadorTutorial.text = (_pasoTutorial + 1) + " / " + pasosTutorial.Length;
            }
        }
    }

    /// <summary>Enciende el panel indicado, apaga el resto y ajusta la camara AR.</summary>
    private void ActivarPanel(GameObject panelActivo)
    {
        Conmutar(panelInicio, panelActivo);
        Conmutar(panelMenuLateral, panelActivo);
        Conmutar(panelPerfil, panelActivo);
        Conmutar(panelAR, panelActivo);
        Conmutar(panelCalibracion, panelActivo);
        Conmutar(panelGuia, panelActivo);
        Conmutar(panelDashboard, panelActivo);

        ActualizarCamaraAR(panelActivo);
    }

    /// <summary>La camara y el escaneo de planos solo viven en calibracion y en la vista AR.</summary>
    private void ActualizarCamaraAR(GameObject panelActivo)
    {
        if (arCameraController == null)
        {
            return;
        }

        bool usaAR = panelActivo == panelAR || panelActivo == panelCalibracion;

        if (usaAR)
        {
            arCameraController.HabilitarAR();
        }
        else
        {
            arCameraController.DeshabilitarAR();
        }
    }

    private GameObject PanelActivo()
    {
        if (panelInicio != null && panelInicio.activeSelf)
        {
            return panelInicio;
        }

        if (panelMenuLateral != null && panelMenuLateral.activeSelf)
        {
            return panelMenuLateral;
        }

        if (panelCalibracion != null && panelCalibracion.activeSelf)
        {
            return panelCalibracion;
        }

        if (panelPerfil != null && panelPerfil.activeSelf)
        {
            return panelPerfil;
        }

        if (panelGuia != null && panelGuia.activeSelf)
        {
            return panelGuia;
        }

        if (panelDashboard != null && panelDashboard.activeSelf)
        {
            return panelDashboard;
        }

        if (panelAR != null && panelAR.activeSelf)
        {
            return panelAR;
        }

        return panelInicio;
    }

    private static void Conmutar(GameObject panel, GameObject panelActivo)
    {
        if (panel == null)
        {
            return;
        }

        bool debeEstarActivo = panel == panelActivo;

        if (panel.activeSelf != debeEstarActivo)
        {
            panel.SetActive(debeEstarActivo);
        }
    }

    private bool ValidarReferencias()
    {
        bool valido = true;

        valido &= Validar(panelInicio, nameof(panelInicio));
        valido &= Validar(panelMenuLateral, nameof(panelMenuLateral));
        valido &= Validar(panelPerfil, nameof(panelPerfil));
        valido &= Validar(panelAR, nameof(panelAR));
        valido &= Validar(panelCalibracion, nameof(panelCalibracion));
        valido &= Validar(panelGuia, nameof(panelGuia));
        valido &= Validar(panelDashboard, nameof(panelDashboard));

        if (arCameraController == null)
        {
            Debug.LogWarning("[MenuController] Sin ARCameraController: la camara AR no se apagara fuera de " +
                             "las pantallas de AR.", this);
        }

        return valido;
    }

    private bool Validar(Object referencia, string nombreCampo)
    {
        if (referencia != null)
        {
            return true;
        }

        Debug.LogError("[MenuController] Referencia sin asignar: " + nombreCampo + ".", this);
        return false;
    }
}
