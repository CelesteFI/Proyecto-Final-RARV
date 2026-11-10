using System;
using System.Collections.Generic;

/// <summary>
/// Densidad aparente (g/cm3) de un alimento en estado crudo y cocido.
/// </summary>
public readonly struct FoodDensity
{
    public readonly string Name;
    public readonly float RawDensity;
    public readonly float CookedDensity;

    public FoodDensity(string name, float rawDensity, float cookedDensity)
    {
        Name = name;
        RawDensity = rawDensity;
        CookedDensity = cookedDensity;
    }

    public float GetDensity(bool cooked) => cooked ? CookedDensity : RawDensity;
}

/// <summary>
/// Repositorio estatico de densidades de los 12 alimentos de prueba.
/// </summary>
public static class FoodDensityData
{
    private const float MinDensity = 1e-4f;

    public static readonly IReadOnlyList<FoodDensity> Entries = new[]
    {
        new FoodDensity("Arroz",       0.85f, 0.72f),
        new FoodDensity("Pasta",       0.62f, 0.85f),
        new FoodDensity("Pollo",       1.05f, 0.90f),
        new FoodDensity("Res",         1.06f, 0.95f),
        new FoodDensity("Cerdo",       1.03f, 0.92f),
        new FoodDensity("Salmon",      1.05f, 0.94f),
        new FoodDensity("Atun",        1.08f, 0.96f),
        new FoodDensity("Huevo",       1.03f, 0.98f),
        new FoodDensity("Papa",        1.09f, 0.92f),
        new FoodDensity("Zanahoria",   1.04f, 0.96f),
        new FoodDensity("Brocoli",     0.38f, 0.34f),
        new FoodDensity("Frijol",      0.80f, 1.00f),

        // Slots 21-24 del catalogo: platos compuestos. No cambian de densidad al "cocinarse"
        // porque ya se sirven preparados (crudo y cocido comparten valor).
        new FoodDensity("Guiso de Pollo",    0.95f, 0.95f),
        new FoodDensity("Sopa de Verduras",  0.90f, 0.90f),
        new FoodDensity("Arroz con Pollo",   0.88f, 0.88f),
        new FoodDensity("Ensalada Mixta",    0.45f, 0.45f)
    };

    private static readonly Dictionary<string, FoodDensity> Lookup = BuildLookup();

    public static int Count => Entries.Count;

    public static bool TryGet(string name, out FoodDensity entry)
    {
        if (string.IsNullOrEmpty(name))
        {
            entry = default;
            return false;
        }

        return Lookup.TryGetValue(name, out entry);
    }

    public static bool TryGetDensity(string name, bool cooked, out float density)
    {
        if (TryGet(name, out FoodDensity entry))
        {
            float value = entry.GetDensity(cooked);
            if (value > MinDensity)
            {
                density = value;
                return true;
            }
        }

        density = 0f;
        return false;
    }

    private static Dictionary<string, FoodDensity> BuildLookup()
    {
        var lookup = new Dictionary<string, FoodDensity>(Entries.Count, StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < Entries.Count; i++)
        {
            lookup[Entries[i].Name] = Entries[i];
        }

        return lookup;
    }
}
