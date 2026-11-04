using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Pre-coloca el molde del plato con un raycast al centro de pantalla sobre planos
/// detectados (PlaneWithinPolygon) y lo deja estatico al confirmar.
/// No instancia objetos: solo mueve el Transform serializado.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlateAnchorManager : MonoBehaviour
{
    [Header("Referencias de escena")]
    [SerializeField] private Transform moldeHolograma;
    [SerializeField] private Button btnFijarPlato;
    [SerializeField] private ARRaycastManager raycastManager;

    // Buffer reutilizado: cero asignaciones por frame.
    private readonly List<ARRaycastHit> _impactos = new List<ARRaycastHit>(8);

    private bool _fijado;

    public bool Fijado => _fijado;

    private void Start()
    {
        if (!ValidarReferencias())
        {
            enabled = false;
            return;
        }

        _fijado = false;
        btnFijarPlato.onClick.AddListener(FijarPlato);
    }

    private void OnDestroy()
    {
        if (btnFijarPlato != null)
        {
            btnFijarPlato.onClick.RemoveListener(FijarPlato);
        }
    }

    private void Update()
    {
        if (_fijado)
        {
            return;
        }

        Vector2 centroPantalla = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        if (!raycastManager.Raycast(centroPantalla, _impactos, TrackableType.PlaneWithinPolygon))
        {
            return;
        }

        moldeHolograma.position = _impactos[0].pose.position;
    }

    /// <summary>Detiene el seguimiento: el molde queda fijo en la mesa.</summary>
    public void FijarPlato()
    {
        _fijado = true;
    }

    private bool ValidarReferencias()
    {
        bool valido = true;

        valido &= Validar(moldeHolograma, nameof(moldeHolograma));
        valido &= Validar(btnFijarPlato, nameof(btnFijarPlato));
        valido &= Validar(raycastManager, nameof(raycastManager));

        return valido;
    }

    private bool Validar(Object referencia, string nombreCampo)
    {
        if (referencia != null)
        {
            return true;
        }

        Debug.LogError($"[PlateAnchorManager] Referencia sin asignar: {nombreCampo}.", this);
        return false;
    }
}
