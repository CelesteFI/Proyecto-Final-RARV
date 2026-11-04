using System.Collections;
using UnityEngine;

/// <summary>
/// Oculta el panel de carga tras el primer frame con un desvanecido sin asignaciones por frame.
/// No instancia objetos: solo modula el CanvasGroup ya presente y se desactiva a si mismo.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public sealed class PanelCarga : MonoBehaviour
{
    [Header("Desvanecido")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField, Min(0f)] private float retardoSegundos = 0.25f;
    [SerializeField, Min(0.01f)] private float duracionDesvanecido = 0.6f;

    private IEnumerator Start()
    {
        if (!ValidarReferencias())
        {
            enabled = false;
            yield break;
        }

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

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
        canvasGroup.blocksRaycasts = false;
        gameObject.SetActive(false);
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

        Debug.LogError("[PanelCarga] Falta el CanvasGroup: la pantalla de carga no puede desvanecerse.", this);
        return false;
    }
}
