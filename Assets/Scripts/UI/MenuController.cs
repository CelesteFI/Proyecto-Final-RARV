using UnityEngine;

/// <summary>
/// Router de paneles de la UI. Los cuatro paneles ya existen como objetos de la jerarquia
/// del Canvas: solo se conmutan con SetActive (cero instanciacion en runtime).
/// </summary>
[DisallowMultipleComponent]
public sealed class MenuController : MonoBehaviour
{
    [Header("Paneles del Canvas")]
    [SerializeField] private GameObject panelInicio;        // Panel_PantallaInicio
    [SerializeField] private GameObject panelMenuLateral;   // Panel_MenuPrincipal
    [SerializeField] private GameObject panelPerfil;        // Panel_Perfil
    [SerializeField] private GameObject panelAR;            // Panel_AR (contiene PanelSuperior y PanelInferior)

    private void Start()
    {
        if (!ValidarReferencias())
        {
            enabled = false;
        }
    }

    /// <summary>Vuelve a la pantalla inicial (estado por defecto de la escena).</summary>
    public void MostrarInicio() => ActivarPanel(panelInicio);

    /// <summary>Muestra el panel de perfil biometrico.</summary>
    public void MostrarPerfil() => ActivarPanel(panelPerfil);

    /// <summary>Muestra el panel de AR junto con su UI de alimentos.</summary>
    public void MostrarAR() => ActivarPanel(panelAR);

    /// <summary>Alterna el menu lateral: si ya esta visible, vuelve al inicio.</summary>
    public void ToggleMenuLateral()
    {
        if (panelMenuLateral != null && panelMenuLateral.activeSelf)
        {
            MostrarInicio();
            return;
        }

        ActivarPanel(panelMenuLateral);
    }

    /// <summary>Enciende el panel indicado y apaga el resto. Punto unico de conmutacion.</summary>
    private void ActivarPanel(GameObject panelActivo)
    {
        Conmutar(panelInicio, panelActivo);
        Conmutar(panelMenuLateral, panelActivo);
        Conmutar(panelPerfil, panelActivo);
        Conmutar(panelAR, panelActivo);
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

        return valido;
    }

    private bool Validar(Object referencia, string nombreCampo)
    {
        if (referencia != null)
        {
            return true;
        }

        Debug.LogError($"[MenuController] Referencia sin asignar: {nombreCampo}.", this);
        return false;
    }
}
