using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Puente UI -> VolumeCalculator -> escala del molde. Los tres controles comparten
/// un unico punto de recalculo; no crea objetos ni modifica las opciones del Dropdown.
/// </summary>
[DisallowMultipleComponent]
public sealed class ARUIManager : MonoBehaviour
{
    [Header("Controles de UI")]
    [SerializeField] private Slider sliderGramos;
    [SerializeField] private TMP_Dropdown dropdownAlimentos;
    [SerializeField] private Toggle toggleCocido;

    [Header("Referencias de escena")]
    [SerializeField] private VolumeCalculator calculator;
    [SerializeField] private Transform moldeHolograma;

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
    private void OnAlimentoCambiado(int indice) => ActualizarMolde();
    private void OnCocidoCambiado(bool estado) => ActualizarMolde();

    /// <summary>Lee la UI, pide la escala y la aplica al molde existente.</summary>
    public void ActualizarMolde()
    {
        Vector3 escala = calculator.CalculateLocalScale(
            ObtenerAlimentoActual(),
            toggleCocido.isOn,
            sliderGramos.value);

        moldeHolograma.localScale = escala;
    }

    /// <summary>Nombre del alimento segun la opcion seleccionada (texto definido en el Inspector).</summary>
    private string ObtenerAlimentoActual()
    {
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

        return valido;
    }

    private bool Validar(Object referencia, string nombreCampo)
    {
        if (referencia != null)
        {
            return true;
        }

        Debug.LogError($"[ARUIManager] Referencia sin asignar: {nombreCampo}.", this);
        return false;
    }
}
