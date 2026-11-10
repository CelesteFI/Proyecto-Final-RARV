using System.Collections;
using UnityEngine;

/// <summary>
/// Pantalla splash: espera a que el panel de carga termine de desvanecerse y de desactivarse,
/// entra con un fundido corto y, tras el tiempo configurado, cede el paso al menu principal.
/// No instancia objetos: solo modula el CanvasGroup ya presente y pide la transicion al
/// MenuController, que es el unico punto de conmutacion de paneles.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public sealed class SplashController : MonoBehaviour
{
    [Header("Destino")]
    [SerializeField] private MenuController menu;

    [Header("Panel de carga")]
    [Tooltip("Panel que debe cerrarse por completo (SetActive(false)) antes de mostrar el logotipo.")]
    [SerializeField] private GameObject panelCarga;

    [Header("Fundido y duracion")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField, Min(0f)] private float duracionEntrada = 0.35f;
    [SerializeField, Min(0f)] private float duracionSegundos = 1.5f;

    [Tooltip("Tope de seguridad: si la carga no se cierra sola, la splash aparece igualmente.")]
    [SerializeField, Min(0.1f)] private float esperaMaximaCarga = 5f;

    private Coroutine _rutina;

    private void OnEnable()
    {
        if (menu == null)
        {
            Debug.LogError("[SplashController] Falta asignar el MenuController: la splash no puede avanzar.", this);
            return;
        }

        // Oculto mientras la carga siga en pantalla: los dos paneles nunca se enciman.
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

        _rutina = StartCoroutine(EsperarCargaYEntrar());
    }

    private void OnDisable()
    {
        if (_rutina != null)
        {
            StopCoroutine(_rutina);
            _rutina = null;
        }
    }

    private IEnumerator EsperarCargaYEntrar()
    {
        // 1) El panel de carga se desactiva por completo antes de mostrar el logotipo.
        float espera = 0f;

        while (panelCarga != null && panelCarga.activeInHierarchy && espera < esperaMaximaCarga)
        {
            espera += Time.unscaledDeltaTime;
            yield return null;
        }

        // 2) Fundido de entrada del logotipo (tiempo real: no depende de Time.timeScale).
        if (canvasGroup != null && duracionEntrada > 0f)
        {
            float transcurrido = 0f;

            while (transcurrido < duracionEntrada)
            {
                transcurrido += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Clamp01(transcurrido / duracionEntrada);
                yield return null;
            }

            canvasGroup.alpha = 1f;
        }

        // 3) Tiempo en pantalla antes de ceder el paso al menu principal.
        if (duracionSegundos > 0f)
        {
            yield return new WaitForSecondsRealtime(duracionSegundos);
        }

        _rutina = null;
        menu.MostrarMenuPrincipal();
    }
}
