using UnityEngine;

/// <summary>
/// Convierte masa (g) + densidad en la escala local de un holograma cilindrico de altura
/// constante: Volumen = Masa / Densidad; r = sqrt(V / (PI * h)). El radio fisico nunca excede
/// el radio del plato calibrado, de modo que el molde siempre queda dentro del plato.
/// No instancia objetos: solo devuelve la escala a aplicar al Transform existente.
/// </summary>
[DisallowMultipleComponent]
public sealed class VolumeCalculator : MonoBehaviour
{
    /// <summary>Altura fija del cilindro del holograma, en centimetros (eje Y).</summary>
    public const float CylinderHeightCm = 2.5f;

    /// <summary>
    /// Escala local del eje Y. El mesh Cylinder de Unity mide 2 unidades de alto y 1 de diametro;
    /// con la convencion del proyecto (baseRadiusCm 50 sobre un radio de mesh 0.5, es decir
    /// 1 unidad = 100 cm) la altura constante de 2.5 cm exige 2 * 0.0125 = 0.025 unidades.
    /// Sin este valor el molde se renderiza 80 veces mas alto que el volumen calculado.
    /// </summary>
    public const float CylinderScaleY = 0.0125f;

    /// <summary>Escala de respaldo: mantiene la altura constante aunque el calculo falle.</summary>
    private static readonly Vector3 EscalaPorDefecto = new Vector3(1f, CylinderScaleY, 1f);

    [Header("Holograma (cilindro)")]
    [Tooltip("Radio del mesh del holograma a escala 1, en cm. Se toma del cilindro ya presente en la escena.")]
    [SerializeField, Min(0.0001f)] private float baseRadiusCm = 1f;

    public float BaseRadiusCm => baseRadiusCm;

    /// <summary>Radio en cm del ultimo calculo realizado (utilidad de auditoria).</summary>
    public float UltimoRadioCm { get; private set; }

    /// <summary>true si el ultimo calculo tuvo que recortarse por el radio del plato.</summary>
    public bool UltimoLimitadoPorPlato { get; private set; }

    // --------------------------------------------------------------- API actual

    /// <summary>
    /// Devuelve la escala local con densidad explicita, recortando el radio al del plato
    /// calibrado (ARState.SelectedPlateRadius). Si los datos no son validos devuelve la
    /// escala por defecto, que conserva la altura de 2.5 cm.
    /// </summary>
    public Vector3 CalculateLocalScale(float density, float grams, float plateRadiusCm)
    {
        return TryCalculateLocalScale(density, grams, plateRadiusCm, out Vector3 localScale, out _, out _)
            ? localScale
            : EscalaPorDefecto;
    }

    /// <summary>
    /// Calcula la escala local (X y Z por radio, Y = CylinderScaleY) y reporta el radio real
    /// usado. false si la densidad o la masa no son validas.
    /// </summary>
    public bool TryCalculateLocalScale(float density, float grams, float plateRadiusCm,
                                       out Vector3 localScale, out float radiusCm, out bool limitadoPorPlato)
    {
        localScale = EscalaPorDefecto;
        radiusCm = 0f;
        limitadoPorPlato = false;
        UltimoLimitadoPorPlato = false;

        if (grams <= 0f)
        {
            Debug.LogWarning("[VolumeCalculator] Masa invalida (" + grams + " g).", this);
            return false;
        }

        if (density <= 1e-4f)
        {
            Debug.LogWarning("[VolumeCalculator] Densidad invalida (" + density + " g/cm3).", this);
            return false;
        }

        float radioFisico = RadiusFromVolume(MassToVolume(grams, density));
        radiusCm = radioFisico;

        // El holograma no puede salirse del plato fisico calibrado.
        if (plateRadiusCm > 0f && radioFisico > plateRadiusCm)
        {
            radiusCm = plateRadiusCm;
            limitadoPorPlato = true;
        }

        float scaleXZ = ScaleFromRadius(radiusCm, baseRadiusCm);

        localScale = new Vector3(scaleXZ, CylinderScaleY, scaleXZ);

        UltimoRadioCm = radiusCm;
        UltimoLimitadoPorPlato = limitadoPorPlato;
        return true;
    }

    /// <summary>
    /// Modelos de Meshy: al ser mallas reales no se deforman, se escalan de forma uniforme
    /// conservando la relacion volumetrica (el volumen crece con el cubo de la escala).
    /// </summary>
    public static Vector3 EscalaUniformeParaGramos(float grams, float referenciaGramos)
    {
        float referencia = Mathf.Max(referenciaGramos, 1e-3f);
        float factor = Mathf.Pow(Mathf.Max(grams, 1e-3f) / referencia, 1f / 3f);

        return new Vector3(factor, factor, factor);
    }

    // ------------------------------------------------- API por nombre (legado)

    /// <summary>Variante por nombre de alimento; sin tope de plato (compatibilidad).</summary>
    public Vector3 CalculateLocalScale(string foodName, bool isCooked, float grams)
    {
        return TryCalculateLocalScale(foodName, isCooked, grams, out Vector3 localScale)
            ? localScale
            : Vector3.one;
    }

    /// <summary>Variante por nombre de alimento; sin tope de plato (compatibilidad).</summary>
    public bool TryCalculateLocalScale(string foodName, bool isCooked, float grams, out Vector3 localScale)
    {
        localScale = EscalaPorDefecto;

        if (!FoodDensityData.TryGetDensity(foodName, isCooked, out float density))
        {
            Debug.LogWarning("[VolumeCalculator] Alimento sin densidad registrada: '" + foodName + "'.", this);
            return false;
        }

        return TryCalculateLocalScale(density, grams, 0f, out localScale, out _, out _);
    }

    // ------------------------------------------------------------- matematicas

    /// <summary>Volumen en cm3 a partir de masa en gramos y densidad en g/cm3.</summary>
    public static float MassToVolume(float grams, float density)
    {
        return grams / Mathf.Max(density, 1e-4f);
    }

    /// <summary>Radio en cm de un cilindro de altura CylinderHeightCm con el volumen dado.</summary>
    public static float RadiusFromVolume(float volumeCm3)
    {
        return Mathf.Sqrt(Mathf.Max(volumeCm3, 0f) / (Mathf.PI * CylinderHeightCm));
    }

    /// <summary>Escala XZ necesaria para un radio objetivo (cm) dado el radio base del mesh.</summary>
    public static float ScaleFromRadius(float radiusCm, float baseRadius)
    {
        return radiusCm / Mathf.Max(baseRadius, 1e-4f);
    }

    private void OnValidate()
    {
        baseRadiusCm = Mathf.Max(baseRadiusCm, 1e-4f);
    }
}
