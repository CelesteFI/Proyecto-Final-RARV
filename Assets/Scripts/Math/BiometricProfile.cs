using System.Collections.Generic;
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

    /// <summary>Factores de actividad en el mismo orden que las opciones de DropdownActividad.</summary>
    public static readonly IReadOnlyList<float> FactoresActividad = new[] { 1.2f, 1.375f, 1.55f, 1.725f, 1.9f };

    [Header("Entradas (TMP_InputField)")]
    [SerializeField] private TMP_InputField InputEdad;
    [SerializeField] private TMP_InputField InputPeso;
    [SerializeField] private TMP_InputField InputAltura;

    [Header("Selecciones (TMP_Dropdown)")]
    [SerializeField] private TMP_Dropdown DropdownSexo;      // 0 = Masculino, 1 = Femenino
    [SerializeField] private TMP_Dropdown DropdownActividad; // indice dentro de FactoresActividad

    [Header("Interaccion y salida")]
    [SerializeField] private Button BtnCalcular;
    [SerializeField] private TMP_Text TextoResultados;

    [Header("Meta de peso (opcional)")]
    [Tooltip("Si se rellena, el panel de metas de la vista AR se activa y compara con el peso actual.")]
    [SerializeField] private TMP_InputField InputPesoObjetivo;
    [SerializeField] private Vector2 rangoPesoObjetivo = new Vector2(20f, 400f);

    [Header("Dashboard de metas (textos modulares)")]
    [Tooltip("Tarjeta de agua: numero y sufijo van en TextMeshProUGUI distintos.")]
    [SerializeField] private TMP_Text dashAguaNumero;
    [SerializeField] private TMP_Text dashAguaSufijo;
    [Tooltip("Tarjeta de energia diaria.")]
    [SerializeField] private TMP_Text dashKcalNumero;
    [SerializeField] private TMP_Text dashKcalSufijo;
    [Tooltip("Tarjeta de macronutrientes: una fila por macro (titulo, numero, sufijo).")]
    [SerializeField] private TMP_Text dashProteinaNumero;
    [SerializeField] private TMP_Text dashProteinaSufijo;
    [SerializeField] private TMP_Text dashCarboNumero;
    [SerializeField] private TMP_Text dashCarboSufijo;
    [SerializeField] private TMP_Text dashGrasaNumero;
    [SerializeField] private TMP_Text dashGrasaSufijo;
    [Tooltip("Estado del dashboard: avisa cuando aun no hay perfil calculado.")]
    [SerializeField] private TMP_Text dashEstado;

    [Header("Recomendacion de agua")]
    [Tooltip("Mililitros de agua por kilo de peso actual (referencia: 35 ml/kg/dia).")]
    [SerializeField, Min(0f)] private float mlAguaPorKilo = 35f;

    [Header("Rangos de validacion")]
    [SerializeField] private Vector2 rangoEdad = new Vector2(10f, 120f);
    [SerializeField] private Vector2 rangoPeso = new Vector2(20f, 400f);
    [SerializeField] private Vector2 rangoAltura = new Vector2(80f, 250f);
    [SerializeField] private Vector2 rangoActividad = new Vector2(1f, 2.5f);

    private readonly StringBuilder _buffer = new StringBuilder(192);

    /// <summary>true cuando el perfil tiene un peso objetivo valido (activa el panel de metas).</summary>
    public bool TienePesoObjetivo { get; private set; }

    /// <summary>Peso actual introducido por el usuario, en kg.</summary>
    public float PesoActualKg { get; private set; }

    /// <summary>Peso objetivo introducido por el usuario, en kg. 0 si no hay meta.</summary>
    public float PesoObjetivoKg { get; private set; }

    /// <summary>Gasto energetico total calculado, en kcal/dia.</summary>
    public float KcalObjetivo { get; private set; }

    public float GramosProteina { get; private set; }
    public float GramosCarbohidrato { get; private set; }
    public float GramosGrasa { get; private set; }

    /// <summary>Meta diaria de agua en litros: 35 ml por kilo de peso actual.</summary>
    public float AguaLitros => PesoActualKg > 0f ? PesoActualKg * mlAguaPorKilo / 1000f : 0f;

    private void Start()
    {
        if (!ValidarReferencias())
        {
            return;
        }

        BtnCalcular.onClick.AddListener(Calcular);

        // El dashboard existe desde el arranque: se escribe vacio hasta que haya perfil.
        RefrescarDashboard();
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
            !TryLeerSelectores(out Sexo sexoActual, out float actividad))
        {
            return;
        }

        float tmb = CalcularTMB(peso, altura, edad, sexoActual);
        float gastoTotal = tmb * actividad;

        float gramosProteina = gastoTotal * RepartoProteina / KcalPorGramoProteina;
        float gramosCarbohidrato = gastoTotal * RepartoCarbohidrato / KcalPorGramoCarbohidrato;
        float gramosGrasa = gastoTotal * RepartoGrasa / KcalPorGramoGrasa;

        // Se guarda el desglose: el panel de metas de la vista AR lo lee sin recalcular nada.
        PesoActualKg = peso;
        KcalObjetivo = gastoTotal;
        GramosProteina = gramosProteina;
        GramosCarbohidrato = gramosCarbohidrato;
        GramosGrasa = gramosGrasa;
        TienePesoObjetivo = TryLeerPesoObjetivo(out float pesoObjetivo);
        PesoObjetivoKg = TienePesoObjetivo ? pesoObjetivo : 0f;

        _buffer.Clear();
        _buffer.Append("TMB (Mifflin-St Jeor): ").Append(tmb.ToString("F0", CultureInfo.InvariantCulture)).Append(" kcal/dia\n");
        _buffer.Append("Gasto total (x").Append(actividad.ToString("F2", CultureInfo.InvariantCulture)).Append("): ")
               .Append(gastoTotal.ToString("F0", CultureInfo.InvariantCulture)).Append(" kcal/dia\n\n");
        _buffer.Append("Proteinas (30%): ").Append(gramosProteina.ToString("F0", CultureInfo.InvariantCulture)).Append(" g\n");
        _buffer.Append("Carbohidratos (40%): ").Append(gramosCarbohidrato.ToString("F0", CultureInfo.InvariantCulture)).Append(" g\n");
        _buffer.Append("Grasas (30%): ").Append(gramosGrasa.ToString("F0", CultureInfo.InvariantCulture)).Append(" g");

        AnexarMeta(peso, pesoObjetivo);

        TextoResultados.text = _buffer.ToString();

        // Las tres sub-tarjetas del dashboard de metas leen estos valores ya calculados.
        RefrescarDashboard();
    }

    /// <summary>
    /// Escribe el dashboard de metas: agua, energia y macros. Cada cifra y su unidad viven en
    /// TextMeshProUGUI distintos para que se puedan reestilizar desde el Inspector.
    /// </summary>
    public void RefrescarDashboard()
    {
        bool hayPerfil = PesoActualKg > 0f;

        Escribir(dashAguaNumero, hayPerfil ? AguaLitros.ToString("F1", CultureInfo.InvariantCulture) : "--");
        Escribir(dashAguaSufijo, "L / dia");

        Escribir(dashKcalNumero, hayPerfil ? KcalObjetivo.ToString("F0", CultureInfo.InvariantCulture) : "--");
        Escribir(dashKcalSufijo, "kcal / dia");

        Escribir(dashProteinaNumero, hayPerfil ? GramosProteina.ToString("F0", CultureInfo.InvariantCulture) : "--");
        Escribir(dashProteinaSufijo, "g");
        Escribir(dashCarboNumero, hayPerfil ? GramosCarbohidrato.ToString("F0", CultureInfo.InvariantCulture) : "--");
        Escribir(dashCarboSufijo, "g");
        Escribir(dashGrasaNumero, hayPerfil ? GramosGrasa.ToString("F0", CultureInfo.InvariantCulture) : "--");
        Escribir(dashGrasaSufijo, "g");

        if (dashEstado != null)
        {
            dashEstado.text = hayPerfil
                ? PesoActualKg.ToString("F1", CultureInfo.InvariantCulture) + " kg registrados" +
                  (TienePesoObjetivo
                      ? " · meta " + PesoObjetivoKg.ToString("F1", CultureInfo.InvariantCulture) + " kg"
                      : string.Empty)
                : "Calcula tu perfil para ver tus metas";
        }
    }

    private static void Escribir(TMP_Text campo, string valor)
    {
        if (campo != null)
        {
            campo.text = valor;
        }
    }

    /// <summary>Anade al informe la meta de peso y la distancia que falta por recorrer.</summary>
    private void AnexarMeta(float pesoActual, float pesoObjetivo)
    {
        if (!TienePesoObjetivo)
        {
            return;
        }

        float diferencia = pesoObjetivo - pesoActual;

        _buffer.Append("\n\nMeta de peso: ").Append(pesoObjetivo.ToString("F1", CultureInfo.InvariantCulture)).Append(" kg");

        if (Mathf.Abs(diferencia) < 0.05f)
        {
            _buffer.Append("\nObjetivo alcanzado con el peso actual");
            return;
        }

        _buffer.Append(diferencia > 0f ? "\nFaltan " : "\nSobran ")
               .Append(Mathf.Abs(diferencia).ToString("F1", CultureInfo.InvariantCulture))
               .Append(" kg para la meta");
    }

    /// <summary>Peso objetivo opcional: vacio o fuera de rango deja el panel de metas oculto.</summary>
    private bool TryLeerPesoObjetivo(out float valor)
    {
        valor = 0f;

        if (InputPesoObjetivo == null)
        {
            return false;
        }

        string crudo = InputPesoObjetivo.text;

        if (string.IsNullOrWhiteSpace(crudo))
        {
            return false;
        }

        if (!TryParseSeguro(crudo, out valor) || valor < rangoPesoObjetivo.x || valor > rangoPesoObjetivo.y)
        {
            Debug.LogWarning("[BiometricProfile] Peso objetivo ignorado: '" + crudo +
                             "' (permitido " + rangoPesoObjetivo.x + "-" + rangoPesoObjetivo.y + " kg).", this);
            valor = 0f;
            return false;
        }

        return true;
    }

    /// <summary>BMR = 10*peso + 6.25*altura - 5*edad + terminoSexo (kcal/dia).</summary>
    public static float CalcularTMB(float pesoKg, float alturaCm, float edad, Sexo sexo)
    {
        float terminoSexo = sexo == Sexo.Masculino ? 5f : -161f;
        return (10f * pesoKg) + (6.25f * alturaCm) - (5f * edad) + terminoSexo;
    }

    /// <summary>Sexo y factor de actividad salen de los dropdowns (valor 0 por defecto).</summary>
    private bool TryLeerSelectores(out Sexo sexoActual, out float actividad)
    {
        sexoActual = Sexo.Masculino;
        actividad = 0f;

        if (DropdownSexo == null || DropdownActividad == null)
        {
            Reportar("Falta asignar en el Inspector DropdownSexo o DropdownActividad.");
            return false;
        }

        sexoActual = DropdownSexo.value == (int)Sexo.Femenino ? Sexo.Femenino : Sexo.Masculino;

        int indice = DropdownActividad.value;
        if ((uint)indice >= (uint)FactoresActividad.Count)
        {
            Reportar("Selecciona un factor de actividad valido.");
            return false;
        }

        actividad = FactoresActividad[indice];

        if (actividad < rangoActividad.x || actividad > rangoActividad.y)
        {
            Reportar($"factor de actividad fuera de rango: {actividad} (permitido {rangoActividad.x}-{rangoActividad.y}).");
            return false;
        }

        return true;
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
        valido &= Validar(DropdownSexo, nameof(DropdownSexo));
        valido &= Validar(DropdownActividad, nameof(DropdownActividad));
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
