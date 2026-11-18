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
/// Repositorio estatico de densidades aparentes (g/cm3) de los 14 alimentos del catalogo:
/// 10 ingredientes en crudo y cocido mas 4 platos compuestos (solo cocidos).
/// </summary>
public static class FoodDensityData
{
    private const float MinDensity = 1e-4f;

    public static readonly IReadOnlyList<FoodDensity> Entries = new[]
    {
        // Carnes: la base de datos del panel de detalle da la misma densidad en ambos estados.
        new FoodDensity("Pollo",        1.05f, 1.05f),
        new FoodDensity("Res",          1.05f, 1.05f),
        new FoodDensity("Cerdo",        1.00f, 1.00f),

        // Leguminosas: secas (crudo) y ya remojadas / cocidas.
        new FoodDensity("Frijol Negro", 1.30f, 0.85f),
        new FoodDensity("Lentejas",     1.30f, 0.85f),
        new FoodDensity("Garbanzos",    1.30f, 0.85f),

        // Verduras.
        new FoodDensity("Espinaca",     0.15f, 0.90f),
        new FoodDensity("Zanahoria",    0.60f, 0.65f),
        new FoodDensity("Papa",         0.65f, 0.70f),
        new FoodDensity("Calabacita",   0.50f, 0.60f),

        // Platos compuestos: se sirven preparados, asi que crudo y cocido comparten densidad.
        new FoodDensity("Arroz con Frijoles", 0.85f, 0.85f),
        new FoodDensity("Picadillo de Res",   1.00f, 1.00f),
        new FoodDensity("Guisado de Pollo",   1.00f, 1.00f),
        new FoodDensity("Lentejas con Cerdo", 0.95f, 0.95f)
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

    /// <summary>Beneficio principal del alimento (texto del sub-panel de detalle).</summary>
    public readonly string Beneficio;

    /// <summary>Sugerencia de combinacion para mejorar la absorcion de sus nutrientes.</summary>
    public readonly string Sugerencia;

    public FoodNutrition(string name,
                         float kcalCrudo, float proteinaCrudo, float carbohidratoCrudo, float grasaCrudo,
                         float kcalCocido, float proteinaCocido, float carbohidratoCocido, float grasaCocido,
                         string beneficio = "", string sugerencia = "")
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
        Beneficio = beneficio ?? string.Empty;
        Sugerencia = sugerencia ?? string.Empty;
    }

    public float Kcal(bool cocido) => cocido ? KcalCocido : KcalCrudo;
    public float Proteina(bool cocido) => cocido ? ProteinaCocido : ProteinaCrudo;
    public float Carbohidrato(bool cocido) => cocido ? CarbohidratoCocido : CarbohidratoCrudo;
    public float Grasa(bool cocido) => cocido ? GrasaCocido : GrasaCrudo;
}

/// <summary>
/// Repositorio estatico de energia y macronutrientes (por 100 g) de los 14 alimentos del
/// catalogo y de FoodDensityData. Los cuatro platos compuestos solo existen cocidos, asi que
/// crudo y cocido comparten valor.
/// </summary>
public static class FoodNutritionData
{
    public static readonly IReadOnlyList<FoodNutrition> Entries = new[]
    {
        new FoodNutrition("Pollo",            110f, 23.0f, 0.0f, 1.2f,   165f, 31.0f, 0.0f, 3.6f,
                          "Alta biodisponibilidad de proteína magra y rico en Niacina (B3) para el metabolismo energético.",
                          "Combínalo con verduras ricas en Vitamina C (espinaca o pimiento) para mejorar la absorción de nutrientes."),

        new FoodNutrition("Res",              143f, 22.0f, 0.0f, 5.0f,   250f, 26.0f, 0.0f, 15.0f,
                          "Fuente principal de Hierro hemo y Vitamina B12, clave para prevenir la fatiga y mantener la masa muscular.",
                          "Evita combinarla con lácteos en la misma comida, ya que el calcio reduce la absorción del hierro."),

        new FoodNutrition("Cerdo",            143f, 21.0f, 0.0f, 6.0f,   242f, 27.0f, 0.0f, 14.0f,
                          "Rico en Tiamina (Vitamina B1), esencial para el sistema nervioso y la recuperación muscular.",
                          "Ideal después de entrenar; acompáñalo con carbohidratos complejos como papa o frijoles."),

        new FoodNutrition("Frijol Negro",     341f, 21.0f, 62.0f, 1.5f,  132f, 8.8f, 23.0f, 0.5f,
                          "Alto en fibra soluble que estabiliza la glucosa en sangre y mejora la digestión.",
                          "Combínalos con un cereal (arroz o tortilla de maíz) para obtener una proteína completa."),

        new FoodNutrition("Lentejas",         353f, 25.0f, 60.0f, 1.0f,  116f, 9.0f, 20.0f, 0.4f,
                          "Aportan ácido fólico, hierro vegetal y magnesio para la salud cardiovascular.",
                          "Agrega unas gotas de limón al servirlas; la vitamina C multiplica la absorción de su hierro."),

        new FoodNutrition("Garbanzos",        364f, 19.0f, 61.0f, 6.0f,  164f, 8.8f, 27.0f, 2.5f,
                          "Generan alta saciedad por su mezcla de fibra y proteína vegetal, ayudando a controlar el apetito.",
                          "Excelentes con verduras frescas o en guisos ligeros para una comida equilibrada."),

        new FoodNutrition("Espinaca",          23f, 2.8f, 3.6f, 0.3f,    23f, 2.9f, 3.7f, 0.3f,
                          "Muy rica en Vitamina K (salud ósea) y antioxidantes que protegen la vista.",
                          "Cocerla brevemente al vapor reduce el ácido oxálico y facilita la absorción de sus minerales."),

        new FoodNutrition("Zanahoria",         41f, 0.9f, 9.5f, 0.2f,    35f, 0.7f, 8.0f, 0.1f,
                          "Altísima en betacarotenos (Vitamina A), clave para el sistema inmune y la piel.",
                          "Consúmela junto a una fuente de grasa saludable o carne para absorber mejor la Vitamina A."),

        new FoodNutrition("Papa",              77f, 2.0f, 17.0f, 0.1f,   87f, 1.8f, 20.0f, 0.1f,
                          "Carbohidrato de energía limpia con más potasio que el plátano, ideal para recuperación.",
                          "Si la hierves y la dejas enfriar, forma almidón resistente que beneficia tu flora intestinal."),

        new FoodNutrition("Calabacita",        17f, 1.2f, 3.1f, 0.3f,    15f, 1.1f, 2.7f, 0.3f,
                          "Casi 90% agua, hidrata y tiene un índice glucémico muy bajo.",
                          "Úsala para dar volumen a tu plato sin sumar calorías extra; combínala con proteína magra."),

        // Platos compuestos: solo se sirven cocidos, asi que crudo y cocido comparten cifra.
        new FoodNutrition("Arroz con Frijoles", 140f, 5.0f, 25.0f, 1.0f,  140f, 5.0f, 25.0f, 1.0f,
                          "Combinación clásica que forma una proteína vegetal completa con energía sostenida.",
                          "Acompáñalo con verduras frescas o una porción de proteína magra para balancear el plato."),

        new FoodNutrition("Picadillo de Res",   220f, 18.0f, 10.0f, 12.0f,  220f, 18.0f, 10.0f, 12.0f,
                          "Aporta proteína completa, hierro y carbohidratos moderados de la papa y zanahoria.",
                          "Sírvelo con verduras verdes al vapor para sumar fibra y equilibrar las grasas."),

        new FoodNutrition("Guisado de Pollo",   150f, 15.0f, 12.0f, 5.0f,   150f, 15.0f, 12.0f, 5.0f,
                          "Platillo equilibrado, alto en proteína y moderado en calorías con aporte de vegetales.",
                          "Perfecto como plato fuerte; regula la porción de salsa si buscas déficit calórico."),

        new FoodNutrition("Lentejas con Cerdo", 180f, 15.0f, 15.0f, 6.0f,   180f, 15.0f, 15.0f, 6.0f,
                          "Combina proteína animal y vegetal con alto aporte de fibra, hierro y vitaminas del complejo B.",
                          "Añade un toque de limón o verdura fresca para potenciar la absorción del hierro.")
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
