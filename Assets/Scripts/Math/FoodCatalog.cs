using System;
using UnityEngine;

/// <summary>
/// Un modelo definitivo del catalogo: 10 ingredientes base en crudo y cocido (20 variantes)
/// mas 4 platos compuestos. El GameObject del modelo ya existe en la escena (inactivo):
/// cuando lleguen los modelos de Meshy solo hay que arrastrarlos a este slot.
/// </summary>
[Serializable]
public sealed class FoodModelSlot
{
    public string nombre = string.Empty;
    public string estado = FoodCatalog.EstadoCrudo;
    [Min(1f)] public float gramosReferencia = 100f;
    public GameObject modelo;

    [Header("Color del molde parametrico")]
    [Tooltip("Color del cilindro de prueba cuando el alimento esta crudo.")]
    public Color colorCrudo = Color.white;

    [Tooltip("Color del cilindro de prueba cuando el alimento esta cocido o es un plato compuesto.")]
    public Color colorCocido = Color.white;

    /// <summary>
    /// true solo si el slot contiene geometria renderizable. Los 24 GameObjects del catalogo ya
    /// existen en la jerarquia como marcadores vacios: mientras no se les arrastre el modelo de
    /// Meshy no tienen Renderer, asi que cuentan como "sin modelo" y la vista sigue usando el
    /// cilindro parametrico de prueba.
    /// </summary>
    public bool TieneModelo => modelo != null && modelo.GetComponentInChildren<Renderer>(true) != null;

    public bool EsPlatoCompuesto =>
        string.Equals(estado, FoodCatalog.EstadoPlato, StringComparison.OrdinalIgnoreCase);

    public string Etiqueta => string.IsNullOrEmpty(estado) ? nombre : nombre + " (" + estado + ")";
}

/// <summary>
/// Catalogo de los 24 modelos definitivos. Resuelve que slot corresponde al alimento y estado
/// elegidos en la UI y muestra u oculta los GameObjects ya presentes (cero instanciacion).
/// Las densidades siguen viviendo en FoodDensityData: el catalogo solo aporta el modelo.
/// </summary>
[DisallowMultipleComponent]
public sealed class FoodCatalog : MonoBehaviour
{
    public const string EstadoCrudo = "Crudo";
    public const string EstadoCocido = "Cocido";
    public const string EstadoPlato = "Plato";

    public const int TotalSlots = 24;

    [Header("Slots de modelos (10 ingredientes x crudo/cocido + 4 platos)")]
    [Tooltip("Arrastra aqui cada modelo exportado. Slots 1-20: ingredientes crudo/cocido. Slots 21-24: platos compuestos.")]
    [SerializeField] private FoodModelSlot[] slots = new FoodModelSlot[TotalSlots];

    public int Count => slots == null ? 0 : slots.Length;

    public FoodModelSlot GetSlot(int index)
    {
        return (slots != null && (uint)index < (uint)slots.Length) ? slots[index] : null;
    }

    /// <summary>
    /// Busca el slot del alimento en el estado pedido. Los platos compuestos no distinguen
    /// crudo/cocido, asi que se resuelven en la segunda pasada.
    /// </summary>
    public bool TryGetSlot(string nombre, bool cocido, out FoodModelSlot slot)
    {
        slot = null;

        if (slots == null || string.IsNullOrEmpty(nombre))
        {
            return false;
        }

        string estadoIngrediente = cocido ? EstadoCocido : EstadoCrudo;

        for (int i = 0; i < slots.Length; i++)
        {
            FoodModelSlot candidato = slots[i];

            if (candidato == null || string.IsNullOrEmpty(candidato.nombre))
            {
                continue;
            }

            if (!string.Equals(candidato.nombre, nombre, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (string.Equals(candidato.estado, estadoIngrediente, StringComparison.OrdinalIgnoreCase))
            {
                slot = candidato;
                return true;
            }
        }

        for (int i = 0; i < slots.Length; i++)
        {
            FoodModelSlot candidato = slots[i];

            if (candidato == null || string.IsNullOrEmpty(candidato.nombre))
            {
                continue;
            }

            if (!string.Equals(candidato.nombre, nombre, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (candidato.EsPlatoCompuesto)
            {
                slot = candidato;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// true si el alimento es un plato compuesto: por naturaleza siempre esta cocido, asi que
    /// la UI bloquea el Toggle de crudo/cocido cuando se elige uno de los cuatro guisados.
    /// </summary>
    public bool EsPlatoCompuesto(string nombre)
    {
        if (slots == null || string.IsNullOrEmpty(nombre))
        {
            return false;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            FoodModelSlot candidato = slots[i];

            if (candidato == null || string.IsNullOrEmpty(candidato.nombre))
            {
                continue;
            }

            if (!string.Equals(candidato.nombre, nombre, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (candidato.EsPlatoCompuesto)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Color base del molde para el alimento y el estado elegidos en la UI.</summary>
    public bool TryGetColor(string nombre, bool cocido, out Color color)
    {
        color = Color.white;

        if (!TryGetSlot(nombre, cocido, out FoodModelSlot slot) || slot == null)
        {
            return false;
        }

        color = (cocido || slot.EsPlatoCompuesto) ? slot.colorCocido : slot.colorCrudo;
        return true;
    }

    /// <summary>Deja visible solo el modelo del slot indicado.</summary>
    public void MostrarModelo(FoodModelSlot slotActivo)
    {
        if (slots == null)
        {
            return;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            FoodModelSlot candidato = slots[i];

            if (candidato == null || candidato.modelo == null)
            {
                continue;
            }

            bool debeEstarActivo = ReferenceEquals(candidato, slotActivo);

            if (candidato.modelo.activeSelf != debeEstarActivo)
            {
                candidato.modelo.SetActive(debeEstarActivo);
            }
        }
    }

    /// <summary>Oculta los 24 modelos: la vista vuelve al molde parametrico de prueba.</summary>
    public void OcultarTodosLosModelos()
    {
        if (slots == null)
        {
            return;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            FoodModelSlot candidato = slots[i];

            if (candidato != null && candidato.modelo != null && candidato.modelo.activeSelf)
            {
                candidato.modelo.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Solo lo usa la herramienta de Editor (NutriAR/6) para dejar el catalogo armado.
    /// En runtime los slots se leen, nunca se reescriben.
    /// </summary>
    public void ConfigurarSlots(FoodModelSlot[] nuevosSlots)
    {
        slots = nuevosSlots;
    }
}
