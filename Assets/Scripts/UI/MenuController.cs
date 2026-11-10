using UnityEngine;

/// <summary>
/// Router de pantallas de NutriAR. Los seis paneles existen en la jerarquia del Canvas:
/// solo se conmutan con SetActive (cero instanciacion en runtime) y, en el mismo punto unico
/// de conmutacion, se habilita o apaga la camara AR: solo la calibracion del plato y la vista
/// AR necesitan el mundo fisico; el resto de pantallas se dibujan sobre fondo estatico.
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
    [SerializeField] private GameObject panelGuia;          // Panel_GuiaNutricional

    [Header("Camara AR")]
    [SerializeField] private ARCameraController arCameraController;

    private void Start()
    {
        if (!ValidarReferencias())
        {
            enabled = false;
            return;
        }

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

    /// <summary>Guia nutricional / catalogo educativo (sin camara AR).</summary>
    public void MostrarGuia() => ActivarPanel(panelGuia);

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

    /// <summary>Enciende el panel indicado, apaga el resto y ajusta la camara AR.</summary>
    private void ActivarPanel(GameObject panelActivo)
    {
        Conmutar(panelInicio, panelActivo);
        Conmutar(panelMenuLateral, panelActivo);
        Conmutar(panelPerfil, panelActivo);
        Conmutar(panelAR, panelActivo);
        Conmutar(panelCalibracion, panelActivo);
        Conmutar(panelGuia, panelActivo);

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
