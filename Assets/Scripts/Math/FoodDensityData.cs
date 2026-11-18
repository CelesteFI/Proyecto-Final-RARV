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

/// <summary>
/// Valores nutricionales de referencia por 100 g de un alimento, en crudo y en cocido.
/// Es la fuente unica de las tarjetas del catalogo y del sub-panel de detalle (Kcal y macros):
/// la UI nunca inventa numeros, solo los formatea.
/// </summary>
public readonly struct FoodNutrition
{
    public readonly string Name;

    public readonly float KcalCrudo;
    public readonly float ProteinaCrudo;
    public readonly float CarbohidratoCrudo;
    public readonly float GrasaCrudo;

    public readonly float KcalCocido;
    public readonly float ProteinaCocido;
    public readonly float CarbohidratoCocido;
    public readonly float GrasaCocido;

    public FoodNutrition(string name,
                         float kcalCrudo, float proteinaCrudo, float carbohidratoCrudo, float grasaCrudo,
                         float kcalCocido, float proteinaCocido, float carbohidratoCocido, float grasaCocido)
    {
        Name = name;
        KcalCrudo = kcalCrudo;
        ProteinaCrudo = proteinaCrudo;
        CarbohidratoCrudo = carbohidratoCrudo;
        GrasaCrudo = grasaCrudo;
        KcalCocido = kcalCocido;
        ProteinaCocido = proteinaCocido;
        CarbohidratoCocido = carbohidratoCocido;
        GrasaCocido = grasaCocido;
    }

    public float Kcal(bool cocido) => cocido ? KcalCocido : KcalCrudo;
    public float Proteina(bool cocido) => cocido ? ProteinaCocido : ProteinaCrudo;
    public float Carbohidrato(bool cocido) => cocido ? CarbohidratoCocido : CarbohidratoCrudo;
    public float Grasa(bool cocido) => cocido ? GrasaCocido : GrasaCrudo;
}

/// <summary>
/// Repositorio estatico de energia y macronutrientes (por 100 g) de los mismos alimentos del
/// catalogo y de FoodDensityData. Los platos compuestos solo existen cocidos, asi que crudo y
/// cocido comparten valor.
/// </summary>
public static class FoodNutritionData
{
    public static readonly IReadOnlyList<FoodNutrition> Entries = new[]
    {
        new FoodNutrition("Arroz",           358f, 7.1f, 78.0f, 0.7f,   130f, 2.7f, 28.0f, 0.3f),
        new FoodNutrition("Pasta",           371f, 13.0f, 75.0f, 1.5f,  158f, 5.8f, 31.0f, 0.9f),
        new FoodNutrition("Pollo",           165f, 31.0f, 0.0f, 3.6f,   190f, 29.0f, 0.0f, 7.0f),
        new FoodNutrition("Res",             250f, 26.0f, 0.0f, 15.0f,  244f, 27.0f, 0.0f, 14.0f),
        new FoodNutrition("Cerdo",           242f, 27.0f, 0.0f, 14.0f,  290f, 25.0f, 0.0f, 21.0f),
        new FoodNutrition("Salmon",          208f, 20.0f, 0.0f, 13.0f,  206f, 22.0f, 0.0f, 12.0f),
        new FoodNutrition("Atun",            132f, 28.0f, 0.0f, 1.0f,   184f, 30.0f, 0.0f, 6.0f),
        new FoodNutrition("Huevo",           143f, 13.0f, 1.1f, 9.5f,   155f, 13.0f, 1.1f, 11.0f),
        new FoodNutrition("Papa",             77f, 2.0f, 17.0f, 0.1f,    87f, 2.0f, 20.0f, 0.1f),
        new FoodNutrition("Zanahoria",        41f, 0.9f, 9.6f, 0.2f,    35f, 0.8f, 8.2f, 0.2f),
        new FoodNutrition("Brocoli",          34f, 2.8f, 6.6f, 0.4f,    35f, 2.4f, 7.2f, 0.4f),
        new FoodNutrition("Frijol",          333f, 23.0f, 60.0f, 1.2f,  127f, 9.0f, 23.0f, 0.5f),

        new FoodNutrition("Guiso de Pollo",  145f, 11.0f, 9.0f, 7.0f,   145f, 11.0f, 9.0f, 7.0f),
        new FoodNutrition("Sopa de Verduras", 45f, 1.5f, 6.0f, 1.5f,     45f, 1.5f, 6.0f, 1.5f),
        new FoodNutrition("Arroz con Pollo", 175f, 8.0f, 22.0f, 6.0f,   175f, 8.0f, 22.0f, 6.0f),
        new FoodNutrition("Ensalada Mixta",  60f, 1.5f, 5.0f, 4.0f,     60f, 1.5f, 5.0f, 4.0f)
    };

    private static readonly Dictionary<string, FoodNutrition> Lookup = BuildNutritionLookup();

    /// <summary>Busca los valores del alimento pedido (sin distinguir mayusculas).</summary>
    public static bool TryGetNutrition(string name, out FoodNutrition entry)
    {
        entry = default;

        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        return Lookup.TryGetValue(name.Trim(), out entry);
    }

    private static Dictionary<string, FoodNutrition> BuildNutritionLookup()
    {
        var lookup = new Dictionary<string, FoodNutrition>(Entries.Count, StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < Entries.Count; i++)
        {
            lookup[Entries[i].Name] = Entries[i];
        }

        return lookup;
    }
}
