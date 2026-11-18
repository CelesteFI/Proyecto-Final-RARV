using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel_Perfil (v2): el VerticalLayoutGroup del panel mantiene la columna de inputs, pero
/// Btn_Volver y la tarjeta de resultados salen del flujo con LayoutElement.ignoreLayout para
/// colocarse a mano (boton en la esquina inferior izquierda, tarjeta bajo el boton Calcular).
/// Cada dato de la tarjeta es un TextMeshProUGUI independiente.
/// </summary>
public static class AuraFixPerfil
{
    private const float AnchoTarjeta = 738f;
    private const float AltoTarjeta = 600f;

    private static readonly Color Fondo = new Color32(22, 28, 50, 255);
    private static readonly Color Blanco = new Color32(245, 247, 255, 255);
    private static readonly Color Secundario = new Color32(168, 186, 219, 255);
    private static readonly Color Aviso = new Color32(242, 160, 61, 255);

    private static TMP_Text _plantilla;

    public static string Execute()
    {
        StringBuilder sb = new StringBuilder();

        GameObject canvasGo = GameObject.Find("Canvas");
        Transform perfil = canvasGo.transform.Find("Panel_Perfil");

        if (perfil == null)
        {
            return "FALTA Panel_Perfil";
        }

        _plantilla = canvasGo.transform.Find("Panel_GuiaNutricional/TituloGuia").GetComponent<TMP_Text>();

        BotonVolver(perfil, sb);
        Tarjeta(perfil, sb);
        Cablear(perfil, sb);

        EditorUtility.SetDirty(canvasGo);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        return sb.ToString();
    }

    private static void BotonVolver(Transform perfil, StringBuilder sb)
    {
        Transform volver = perfil.Find("Btn_Volver");

        if (volver == null)
        {
            sb.AppendLine("FALTA Btn_Volver");
            return;
        }

        LayoutElement le = volver.GetComponent<LayoutElement>();

        if (le == null)
        {
            le = volver.gameObject.AddComponent<LayoutElement>();
        }

        le.ignoreLayout = true;

        RectTransform rt = volver as RectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = Vector2.zero;
        rt.sizeDelta = new Vector2(300f, 90f);
        rt.anchoredPosition = new Vector2(40f, 40f);

        sb.AppendLine("Btn_Volver fuera del layout, en la esquina inferior izquierda (40,40)");
    }

    private static void Tarjeta(Transform perfil, StringBuilder sb)
    {
        Transform cardT = perfil.Find("TarjetaResultados");

        if (cardT == null)
        {
            GameObject go = new GameObject("TarjetaResultados", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(perfil, false);

            Image img = go.GetComponent<Image>();
            img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            img.type = Image.Type.Sliced;
            img.color = Fondo;
            img.raycastTarget = false;

            cardT = go.transform;
        }

        LayoutElement le = cardT.GetComponent<LayoutElement>();

        if (le == null)
        {
            le = cardT.gameObject.AddComponent<LayoutElement>();
        }

        le.ignoreLayout = true;

        RectTransform rt = cardT as RectTransform;
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(AnchoTarjeta, AltoTarjeta);
        rt.anchoredPosition = new Vector2(0f, -912f);

        // Se reconstruye entero: las posiciones se recalculan para el ancho real de la tarjeta.
        while (cardT.childCount > 0)
        {
            Object.DestroyImmediate(cardT.GetChild(0).gameObject);
        }

        TextoIzq(cardT, "TituloResultado", "Tu meta diaria sugerida es:", 24f, -24f, 420f, 44f, 26f, Blanco);
        TextoIzq(cardT, "ResultadoEstado", "Completa tus datos y pulsa Calcular.", 24f, -72f, 620f, 32f, 20f, Secundario);

        TextoIzq(cardT, "KcalTitulo", "Calorias al dia", 24f, -136f, 300f, 40f, 22f, Blanco);
        TextoDer(cardT, "KcalNumero", "--", -110f, -122f, 180f, 64f, 46f, Blanco);
        TextoSufijo(cardT, "KcalSufijo", "kcal", -100f, -142f, 80f, 34f, 20f, Secundario);

        Fila(cardT, "Prot", "Proteinas", -206f);
        Fila(cardT, "Carbo", "Carbohidratos", -252f);
        Fila(cardT, "Grasa", "Grasas", -298f);

        TextoIzq(cardT, "TmbTitulo", "Gasto en reposo (TMB)", 24f, -352f, 400f, 36f, 22f, Secundario);
        TextoDer(cardT, "TmbNumero", "--", -110f, -352f, 120f, 36f, 24f, Blanco);
        TextoSufijo(cardT, "TmbSufijo", "kcal", -100f, -352f, 80f, 36f, 20f, Secundario);

        TextoIzq(cardT, "MetaPesoTexto", string.Empty, 24f, -418f, 690f, 34f, 22f, Blanco);
        TextoIzq(cardT, "AvisoTexto", string.Empty, 24f, -462f, 690f, 64f, 20f, Aviso);

        sb.AppendLine("TarjetaResultados 738x600 fuera del layout, con 19 textos independientes");
    }

    private static void Fila(Transform card, string prefijo, string etiqueta, float y)
    {
        TextoIzq(card, prefijo + "Titulo", etiqueta, 24f, y, 340f, 36f, 22f, Secundario);
        TextoDer(card, prefijo + "Numero", "--", -110f, y, 120f, 36f, 28f, Blanco);
        TextoSufijo(card, prefijo + "Sufijo", "g", -100f, y, 80f, 36f, 20f, Secundario);
    }

    private static void Cablear(Transform perfil, StringBuilder sb)
    {
        BiometricProfile perfilScript = GameObject.Find("XR Origin").GetComponent<BiometricProfile>();
        Transform card = perfil.Find("TarjetaResultados");

        SerializedObject so = new SerializedObject(perfilScript);

        Asignar(so, "resultadoTitulo", card.Find("TituloResultado"));
        Asignar(so, "resultadoEstado", card.Find("ResultadoEstado"));
        Asignar(so, "kcalTitulo", card.Find("KcalTitulo"));
        Asignar(so, "kcalNumero", card.Find("KcalNumero"));
        Asignar(so, "kcalSufijo", card.Find("KcalSufijo"));
        Asignar(so, "proteinaTitulo", card.Find("ProtTitulo"));
        Asignar(so, "proteinaNumero", card.Find("ProtNumero"));
        Asignar(so, "proteinaSufijo", card.Find("ProtSufijo"));
        Asignar(so, "carboTitulo", card.Find("CarboTitulo"));
        Asignar(so, "carboNumero", card.Find("CarboNumero"));
        Asignar(so, "carboSufijo", card.Find("CarboSufijo"));
        Asignar(so, "grasaTitulo", card.Find("GrasaTitulo"));
        Asignar(so, "grasaNumero", card.Find("GrasaNumero"));
        Asignar(so, "grasaSufijo", card.Find("GrasaSufijo"));
        Asignar(so, "tmbTitulo", card.Find("TmbTitulo"));
        Asignar(so, "tmbNumero", card.Find("TmbNumero"));
        Asignar(so, "tmbSufijo", card.Find("TmbSufijo"));
        Asignar(so, "metaPesoTexto", card.Find("MetaPesoTexto"));
        Asignar(so, "avisoTexto", card.Find("AvisoTexto"));

        so.ApplyModifiedPropertiesWithoutUndo();

        sb.AppendLine("BiometricProfile recableado a los 19 textos nuevos");
    }

    private static void Asignar(SerializedObject so, string campo, Transform destino)
    {
        SerializedProperty prop = so.FindProperty(campo);

        if (prop == null)
        {
            Debug.LogWarning("[AuraFixPerfil] Campo no encontrado: " + campo);
            return;
        }

        prop.objectReferenceValue = destino != null ? destino.GetComponent<TMP_Text>() : null;
    }

    private static TextMeshProUGUI TextoIzq(Transform padre, string nombre, string contenido, float x, float y,
                                            float ancho, float alto, float fuente, Color color)
    {
        return Crear(padre, nombre, contenido, new Vector2(0f, 1f), new Vector2(0f, 1f),
                     new Vector2(x, y), new Vector2(ancho, alto), fuente, color, TextAlignmentOptions.Left);
    }

    private static TextMeshProUGUI TextoDer(Transform padre, string nombre, string contenido, float x, float y,
                                            float ancho, float alto, float fuente, Color color)
    {
        return Crear(padre, nombre, contenido, new Vector2(1f, 1f), new Vector2(1f, 1f),
                     new Vector2(x, y), new Vector2(ancho, alto), fuente, color, TextAlignmentOptions.Right);
    }

    private static TextMeshProUGUI TextoSufijo(Transform padre, string nombre, string contenido, float x, float y,
                                               float ancho, float alto, float fuente, Color color)
    {
        // Pivote izquierdo anclado al borde derecho: el sufijo arranca donde termina la cifra.
        return Crear(padre, nombre, contenido, new Vector2(1f, 1f), new Vector2(1f, 1f),
                     new Vector2(x, y), new Vector2(ancho, alto), fuente, color, TextAlignmentOptions.Left,
                     new Vector2(0f, 1f));
    }

    private static TextMeshProUGUI Crear(Transform padre, string nombre, string contenido, Vector2 ancMin,
                                         Vector2 ancMax, Vector2 pos, Vector2 tam, float fuente, Color color,
                                         TextAlignmentOptions alineacion, Vector2? pivote = null)
    {
        TMP_Text clon = Object.Instantiate(_plantilla, padre);
        clon.gameObject.name = nombre;
        clon.text = contenido;
        clon.fontSize = fuente;
        clon.enableAutoSizing = false;
        clon.color = color;
        clon.alignment = alineacion;
        clon.raycastTarget = false;
        clon.overflowMode = TextOverflowModes.Overflow;

        LayoutElement le = clon.GetComponent<LayoutElement>();
        if (le != null)
        {
            Object.DestroyImmediate(le);
        }

        RectTransform rt = clon.rectTransform;
        rt.anchorMin = ancMin;
        rt.anchorMax = ancMax;
        rt.pivot = pivote ?? new Vector2(ancMin.x, 1f);
        rt.sizeDelta = tam;
        rt.anchoredPosition = pos;

        return clon as TextMeshProUGUI;
    }
}
