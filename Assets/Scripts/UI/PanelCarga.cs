using System.Collections;
using UnityEngine;

/// <summary>
/// Oculta el panel de carga tras el primer frame con un desvanecido sin asignaciones por frame.
/// No instancia objetos: solo modula el CanvasGroup ya presente y se desactiva a si mismo.
/// El grupo se mantiene siempre sin interaccion: una pantalla de carga solo decora y no debe
/// poder capturar eventos, de modo que ni un fallo del desvanecido ni una reactivacion
/// accidental puedan dejar bloqueada la interfaz.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public sealed class PanelCarga : MonoBehaviour
{
    [Header("Desvanecido")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField, Min(0f)] private float retardoSegundos = 0.25f;
    [SerializeField, Min(0.01f)] private float duracionDesvanecido = 0.6f;

    /// <summary>
    /// Se ejecuta antes que cualquier otro ciclo de vida, incluso si Start() no llega a correr
    /// (comportamiento deshabilitado, objeto desactivado a mitad del desvanecido, etc.).
    /// </summary>
    private void Awake()
    {
        if (!ValidarReferencias())
        {
            gameObject.SetActive(false);
            return;
        }

        NeutralizarInteraccion();
    }

    private IEnumerator Start()
    {
        if (!ValidarReferencias())
        {
            gameObject.SetActive(false);
            yield break;
        }

        NeutralizarInteraccion();
        canvasGroup.alpha = 1f;

        if (retardoSegundos > 0f)
        {
            yield return new WaitForSecondsRealtime(retardoSegundos);
        }

        float transcurrido = 0f;

        while (transcurrido < duracionDesvanecido)
        {
            transcurrido += Time.unscaledDeltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(transcurrido / duracionDesvanecido);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        NeutralizarInteraccion();
        gameObject.SetActive(false);
    }

    /// <summary>Deja el panel visible pero transparente a los raycasts de la UI.</summary>
    private void NeutralizarInteraccion()
    {
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private bool ValidarReferencias()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (canvasGroup != null)
        {
            return true;
        }

        Debug.LogError("[PanelCarga] Falta el CanvasGroup: el panel de carga no puede desvanecerse.", this);
        return false;
    }
}
