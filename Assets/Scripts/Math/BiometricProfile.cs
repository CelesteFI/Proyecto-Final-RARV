using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Perfil biometrico: BMR (Mifflin-St Jeor) -> gasto total -> macronutrientes sugeridos.
/// Solo consume referencias serializadas de la escena; no instancia objetos en runtime.
/// </summary>
[DisallowMultipleComponent]
public sealed class BiometricProfile : MonoBehaviour
{
    public enum Sexo
    {
        Masculino = 0,
        Femenino = 1
    }

    private const float KcalPorGramoProteina = 4f;
    private const float KcalPorGramoCarbohidrato = 4f;
    private const float KcalPorGramoGrasa = 9f;

    private const float RepartoProteina = 0.30f;
    private const float RepartoCarbohidrato = 0.40f;
    private const float RepartoGrasa = 0.30f;

    [Header("Entradas (TMP_InputField)")]
    [SerializeField] private TMP_InputField InputEdad;
    [SerializeField] private TMP_InputField InputPeso;
    [SerializeField] private TMP_InputField InputAltura;
    [SerializeField] private TMP_InputField InputActividad;

    [Header("Interaccion y salida")]
    [SerializeField] private Button BtnCalcular;
    [SerializeField] private TMP_Text TextoResultados;

    [Header("Termino de sexo de la formula")]
    [Tooltip("Mifflin-St Jeor exige el termino +5 (masculino) o -161 (femenino).")]
    [SerializeField] private Sexo sexo = Sexo.Masculino;

    [Header("Rangos de validacion")]
    [SerializeField] private Vector2 rangoEdad = new Vector2(10f, 120f);
    [SerializeField] private Vector2 rangoPeso = new Vector2(20f, 400f);
    [SerializeField] private Vector2 rangoAltura = new Vector2(80f, 250f);
    [SerializeField] private Vector2 rangoActividad = new Vector2(1f, 2.5f);

    private readonly StringBuilder _buffer = new StringBuilder(192);

    private void Start()
    {
        if (!ValidarReferencias())
        {
            return;
        }

        BtnCalcular.onClick.AddListener(Calcular);
    }

    private void OnDestroy()
    {
        if (BtnCalcular != null)
        {
            BtnCalcular.onClick.RemoveListener(Calcular);
        }
    }

    /// <summary>Lee los cuatro inputs, valida y escribe el desglose en TextoResultados.</summary>
    public void Calcular()
    {
        if (!TryLeer(InputEdad, "edad", rangoEdad, out float edad) ||
            !TryLeer(InputPeso, "peso (kg)", rangoPeso, out float peso) ||
            !TryLeer(InputAltura, "altura (cm)", rangoAltura, out float altura) ||
            !TryLeer(InputActividad, "factor de actividad", rangoActividad, out float actividad))
        {
            return;
        }

        float tmb = CalcularTMB(peso, altura, edad, sexo);
        float gastoTotal = tmb * actividad;

        float gramosProteina = gastoTotal * RepartoProteina / KcalPorGramoProteina;
        float gramosCarbohidrato = gastoTotal * RepartoCarbohidrato / KcalPorGramoCarbohidrato;
        float gramosGrasa = gastoTotal * RepartoGrasa / KcalPorGramoGrasa;

        _buffer.Clear();
        _buffer.Append("TMB (Mifflin-St Jeor): ").Append(tmb.ToString("F0", CultureInfo.InvariantCulture)).Append(" kcal/dia\n");
        _buffer.Append("Gasto total (x").Append(actividad.ToString("F2", CultureInfo.InvariantCulture)).Append("): ")
               .Append(gastoTotal.ToString("F0", CultureInfo.InvariantCulture)).Append(" kcal/dia\n\n");
        _buffer.Append("Proteinas (30%): ").Append(gramosProteina.ToString("F0", CultureInfo.InvariantCulture)).Append(" g\n");
        _buffer.Append("Carbohidratos (40%): ").Append(gramosCarbohidrato.ToString("F0", CultureInfo.InvariantCulture)).Append(" g\n");
        _buffer.Append("Grasas (30%): ").Append(gramosGrasa.ToString("F0", CultureInfo.InvariantCulture)).Append(" g");

        TextoResultados.text = _buffer.ToString();
    }

    /// <summary>BMR = 10*peso + 6.25*altura - 5*edad + terminoSexo (kcal/dia).</summary>
    public static float CalcularTMB(float pesoKg, float alturaCm, float edad, Sexo sexo)
    {
        float terminoSexo = sexo == Sexo.Masculino ? 5f : -161f;
        return (10f * pesoKg) + (6.25f * alturaCm) - (5f * edad) + terminoSexo;
    }

    private bool TryLeer(TMP_InputField input, string etiqueta, Vector2 rango, out float valor)
    {
        valor = 0f;

        if (input == null)
        {
            Reportar($"Falta asignar en el Inspector el campo '{etiqueta}'.");
            return false;
        }

        string crudo = input.text;
        if (!TryParseSeguro(crudo, out valor))
        {
            Reportar($"Valor no numerico en {etiqueta}: '{crudo}'.");
            return false;
        }

        if (valor < rango.x || valor > rango.y)
        {
            Reportar($"{etiqueta} fuera de rango: {valor} (permitido {rango.x}-{rango.y}).");
            return false;
        }

        return true;
    }

    /// <summary>Parseo tolerante a 'coma' o 'punto' decimal, sin permitir separadores de millar.</summary>
    private static bool TryParseSeguro(string crudo, out float valor)
    {
        valor = 0f;

        if (string.IsNullOrWhiteSpace(crudo))
        {
            return false;
        }

        const NumberStyles estilo = NumberStyles.Float;

        return float.TryParse(crudo, estilo, CultureInfo.CurrentCulture, out valor)
            || float.TryParse(crudo, estilo, CultureInfo.InvariantCulture, out valor);
    }

    private bool ValidarReferencias()
    {
        bool valido = true;

        valido &= Validar(InputEdad, nameof(InputEdad));
        valido &= Validar(InputPeso, nameof(InputPeso));
        valido &= Validar(InputAltura, nameof(InputAltura));
        valido &= Validar(InputActividad, nameof(InputActividad));
        valido &= Validar(BtnCalcular, nameof(BtnCalcular));
        valido &= Validar(TextoResultados, nameof(TextoResultados));

        return valido;
    }

    private bool Validar(Object referencia, string nombreCampo)
    {
        if (referencia != null)
        {
            return true;
        }

        Debug.LogError($"[BiometricProfile] Referencia sin asignar: {nombreCampo}.", this);
        return false;
    }

    private void Reportar(string mensaje)
    {
        if (TextoResultados != null)
        {
            TextoResultados.text = "ERROR: " + mensaje;
        }

        Debug.LogWarning("[BiometricProfile] " + mensaje, this);
    }
}
