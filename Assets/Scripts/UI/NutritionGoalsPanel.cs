using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// Panel flotante de metas nutricionales de la esquina superior derecha de la vista AR.
/// Solo se muestra cuando el perfil biometrico tiene un peso objetivo valido: sin meta no hay
/// nada que comparar. Cero instanciacion: conmuta el CanvasGroup de un panel ya serializado.
/// </summary>
[DisallowMultipleComponent]
public sealed class NutritionGoalsPanel : MonoBehaviour
{
    [Header("Presentacion")]
    [SerializeField] private CanvasGroup grupo;
    [SerializeField] private TMP_Text textoMetas;

    [Header("Referencias de escena")]
    [SerializeField] private BiometricProfile perfil;

    // Reutilizado en cada refresco: cero memoria por actualizacion.
    private readonly StringBuilder _buffer = new StringBuilder(192);

    private void OnEnable()
    {
        Actualizar();
    }

    private void Start()
    {
        Actualizar();
    }

    /// <summary>Muestra u oculta el panel y reescribe la meta segun el perfil biometrico.</summary>
    public void Actualizar()
    {
        bool hayMeta = perfil != null && perfil.TienePesoObjetivo;

        AplicarVisibilidad(hayMeta);

        if (textoMetas == null)
        {
            return;
        }

        if (!hayMeta)
        {
            textoMetas.text = string.Empty;
            return;
        }

        _buffer.Clear();
        _buffer.Append("<b>META DE PESO</b>\n");
        _buffer.Append("Objetivo: ").Append(perfil.PesoObjetivoKg.ToString("F1", CultureInfo.InvariantCulture)).Append(" kg\n");

        float diferencia = perfil.PesoObjetivoKg - perfil.PesoActualKg;

        if (Mathf.Abs(diferencia) < 0.05f)
        {
            _buffer.Append("Objetivo alcanzado\n");
        }
        else
        {
            _buffer.Append(diferencia > 0f ? "Faltan " : "Sobran ")
                   .Append(Mathf.Abs(diferencia).ToString("F1", CultureInfo.InvariantCulture))
                   .Append(" kg\n");
        }

        _buffer.Append("\n").Append(perfil.KcalObjetivo.ToString("F0", CultureInfo.InvariantCulture)).Append(" kcal/dia\n");
        _buffer.Append("P ").Append(perfil.GramosProteina.ToString("F0", CultureInfo.InvariantCulture))
               .Append(" g · C ").Append(perfil.GramosCarbohidrato.ToString("F0", CultureInfo.InvariantCulture))
               .Append(" g · G ").Append(perfil.GramosGrasa.ToString("F0", CultureInfo.InvariantCulture)).Append(" g");

        textoMetas.text = _buffer.ToString();
    }

    private void AplicarVisibilidad(bool visible)
    {
        if (grupo == null)
        {
            gameObject.SetActive(visible);
            return;
        }

        grupo.alpha = visible ? 1f : 0f;
        grupo.blocksRaycasts = visible;
        grupo.interactable = visible;
    }
}
