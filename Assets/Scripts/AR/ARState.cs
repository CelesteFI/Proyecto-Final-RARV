using UnityEngine;

/// <summary>
/// Forma del plato fisico elegida por el usuario. El aro punteado de referencia sobre la mesa
/// y el circulo guia de la pantalla de calibracion adoptan la misma forma.
/// </summary>
public enum PlateShape
{
    Circular = 0,
    Cuadrado = 1
}

/// <summary>
/// Estado global de la sesion AR (cero instanciacion: es una clase estatica, no un objeto).
/// Guarda el radio del plato fisico calibrado y la forma elegida para que la vista AR ajuste
/// el holograma a las coordenadas reales del plato y no a un valor arbitrario.
/// </summary>
public static class ARState
{
    public const float MinPlateRadiusCm = 5f;
    public const float MaxPlateRadiusCm = 30f;
    public const float DefaultPlateRadiusCm = 12f;

    /// <summary>Radio del plato calibrado, en centimetros.</summary>
    public static float SelectedPlateRadius { get; private set; } = DefaultPlateRadiusCm;

    /// <summary>true cuando el usuario ya confirmo la calibracion del plato.</summary>
    public static bool HasPlate { get; private set; }

    /// <summary>Forma del plato fisico: aro circular punteado o plato cuadrado.</summary>
    public static PlateShape SelectedShape { get; private set; } = PlateShape.Circular;

    /// <summary>Atajo de lectura: true cuando el plato calibrado es cuadrado.</summary>
    public static bool EsPlatoCuadrado => SelectedShape == PlateShape.Cuadrado;

    /// <summary>
    /// Vuelve a los valores de fabrica en cada sesion de Play. Sin esto, con la recarga de
    /// dominio desactivada, el radio calibrado sobreviviria de una partida a la siguiente.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReiniciarEstado()
    {
        SelectedPlateRadius = DefaultPlateRadiusCm;
        HasPlate = false;
        SelectedShape = PlateShape.Circular;
    }

    public static float ClampRadius(float radiusCm)
    {
        return Mathf.Clamp(radiusCm, MinPlateRadiusCm, MaxPlateRadiusCm);
    }

    /// <summary>Guarda el radio calibrado y marca el plato como fijado.</summary>
    public static void SetPlateRadius(float radiusCm)
    {
        SelectedPlateRadius = ClampRadius(radiusCm);
        HasPlate = true;
    }

    /// <summary>Guarda la forma elegida; el aro y el circulo guia se redibujan con ella.</summary>
    public static void SetPlateShape(PlateShape forma)
    {
        SelectedShape = forma;
    }
}
