using UnityEngine;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Enciende y apaga el renderizado del mundo fisico. La camara AR y el escaneo de planos
/// solo se habilitan en las pantallas que los necesitan (calibracion del plato y vista AR);
/// en el resto se desactivan por completo para ahorrar bateria y evitar colapsos.
/// No instancia objetos: solo conmuta el enabled de componentes ya existentes.
/// </summary>
[DisallowMultipleComponent]
public sealed class ARCameraController : MonoBehaviour
{
    [Header("Componentes de AR Foundation")]
    [SerializeField] private ARCameraManager cameraManager;
    [SerializeField] private ARCameraBackground cameraBackground;
    [SerializeField] private ARPlaneManager planeManager;
    [SerializeField] private ARRaycastManager raycastManager;

    [Header("Estado inicial")]
    [SerializeField] private bool arActivaAlIniciar;

    /// <summary>true mientras la camara AR y el escaneo de planos estan habilitados.</summary>
    public bool ARVisible { get; private set; }

    private void Awake()
    {
        if (cameraManager == null)
        {
            Debug.LogError("[ARCameraController] Falta asignar ARCameraManager: la camara AR no se puede gestionar.", this);
            enabled = false;
            return;
        }

        Configurar(arActivaAlIniciar);
    }

    public void HabilitarAR() => Configurar(true);

    public void DeshabilitarAR() => Configurar(false);

    /// <summary>Estado unico de la camara AR: los cuatro componentes siguen siempre al mismo valor.</summary>
    public void Configurar(bool activa)
    {
        ARVisible = activa;

        if (cameraManager != null)
        {
            cameraManager.enabled = activa;
        }

        if (cameraBackground != null)
        {
            cameraBackground.enabled = activa;
        }

        if (planeManager != null)
        {
            planeManager.enabled = activa;
        }

        if (raycastManager != null)
        {
            raycastManager.enabled = activa;
        }
    }
}
