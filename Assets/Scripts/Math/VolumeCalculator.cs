using UnityEngine;

/// <summary>
/// Convierte masa (g) + alimento en la escala local de un holograma cilindrico
/// de altura constante. Volumen = Masa / Densidad; r = sqrt(V / (PI * h)).
/// No instancia objetos: solo devuelve la escala a aplicar al Transform existente.
/// </summary>
[DisallowMultipleComponent]
public sealed class VolumeCalculator : MonoBehaviour
{
    /// <summary>Altura fija del cilindro del holograma, en centimetros (eje Y).</summary>
    public const float CylinderHeightCm = 2.5f;

    [Header("Holograma (cilindro)")]
    [Tooltip("Radio del mesh del holograma a escala 1, en cm. Se toma del cilindro ya presente en la escena.")]
    [SerializeField, Min(0.0001f)] private float baseRadiusCm = 1f;

    public float BaseRadiusCm => baseRadiusCm;

    /// <summary>
    /// Devuelve la escala local (X y Z por radio, Y = 1). Si los datos no son validos
    /// registra una advertencia y devuelve Vector3.one.
    /// </summary>
    public Vector3 CalculateLocalScale(string foodName, bool isCooked, float grams)
    {
        return TryCalculateLocalScale(foodName, isCooked, grams, out Vector3 localScale)
            ? localScale
            : Vector3.one;
    }

    /// <summary>
    /// Intenta calcular la escala local del holograma. false si el alimento no existe,
    /// la densidad no es valida o la masa es menor o igual a cero.
    /// </summary>
    public bool TryCalculateLocalScale(string foodName, bool isCooked, float grams, out Vector3 localScale)
    {
        localScale = Vector3.one;

        if (grams <= 0f)
        {
            Debug.LogWarning($"[VolumeCalculator] Masa invalida ({grams} g) para '{foodName}'.", this);
            return false;
        }

        if (!FoodDensityData.TryGetDensity(foodName, isCooked, out float density))
        {
            Debug.LogWarning($"[VolumeCalculator] Alimento sin densidad registrada: '{foodName}'.", this);
            return false;
        }

        float radiusCm = RadiusFromVolume(MassToVolume(grams, density));
        float scaleXZ = radiusCm / baseRadiusCm;

        localScale = new Vector3(scaleXZ, 1f, scaleXZ);
        return true;
    }

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
