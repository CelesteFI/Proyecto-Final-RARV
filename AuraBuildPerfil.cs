using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel_Perfil: boton "Volver" en la esquina inferior izquierda y resultados divididos en varios
/// TextMeshProUGUI independientes (titulo / numero / sufijo por cada dato).
/// </summary>
public static class AuraBuildPerfil
{
    private static readonly Color Fondo = new Color32(20, 25, 45, 255);
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
        TarjetaResultados(perfil, sb);
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

        RectTransform rt = volver as RectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = Vector2.zero;
        rt.sizeDelta = new Vector2(300f, 90f);
        rt.anchoredPosition = new Vector2(40f, 40f);

        sb.AppendLine("Btn_Volver movido a la esquina inferior izquierda");
    }

    private static void TarjetaResultados(Transform perfil, StringBuilder sb)
    {
        // El bloque de un solo texto desaparece: cada dato tendra su propio TMP.
        Transform viejo = perfil.Find("TextoResultados");
        if (viejo != null)
        {
            Object.DestroyImmediate(viejo.gameObject);
        }

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

        RectTransform rt = cardT as RectTransform;
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(900f, 600f);
        rt.anchoredPosition = new Vector2(0f, -1000f);

        Limpiar(cardT, "TituloResultado", "ResultadoEstado", "KcalTitulo", "KcalNumero", "KcalSufijo",
                "ProtTitulo", "ProtNumero", "ProtSufijo", "CarboTitulo", "CarboNumero", "CarboSufijo",
                "GrasaTitulo", "GrasaNumero", "GrasaSufijo", "TmbTitulo", "TmbNumero", "TmbSufijo",
                "MetaPesoTexto", "AvisoTexto");

        TextoIzq(cardT, "TituloResultado", "Tu meta diaria sugerida es:", new Vector2(30f, -26f),
                 new Vector2(840f, 44f), 28f, Blanco);

        TextoIzq(cardT, "ResultadoEstado", "Completa tus datos y pulsa Calcular.", new Vector2(30f, -74f),
                 new Vector2(840f, 34f), 22f, Secundario);

        // Tarjeta destacada de energia: titulo, cifra y unidad en TMP separados.
        TextoIzq(cardT, "KcalTitulo", "Calorias al dia", new Vector2(30f, -132f), new Vector2(300f, 40f), 24f, Blanco);

        TextMeshProUGUI kcalNum = Texto(cardT, "KcalNumero", "--", new Vector2(1f, 1f), new Vector2(1f, 1f),
                                        new Vector2(-160f, -118f), new Vector2(220f, 60f), 46f, Blanco,
                                        TextAlignmentOptions.Right);

        Texto(cardT, "KcalSufijo", "kcal", new Vector2(1f, 1f), new Vector2(0f, 1f),
              new Vector2(-150f, -136f), new Vector2(130f, 34f), 22f, Secundario, TextAlignmentOptions.Left);

        EditorUtility.SetDirty(kcalNum);

        Fila(cardT, "Prot", "Proteinas", -200f);
        Fila(cardT, "Carbo", "Carbohidratos", -248f);
        Fila(cardT, "Grasa", "Grasas", -296f);

        TextoIzq(cardT, "TmbTitulo", "Gasto en reposo (TMB)", new Vector2(30f, -350f),
                 new Vector2(400f, 36f), 22f, Secundario);

        Texto(cardT, "TmbNumero", "--", new Vector2(1f, 1f), new Vector2(1f, 1f),
              new Vector2(-160f, -350f), new Vector2(160f, 36f), 26f, Blanco, TextAlignmentOptions.Right);

        Texto(cardT, "TmbSufijo", "kcal", new Vector2(1f, 1f), new Vector2(0f, 1f),
              new Vector2(-150f, -350f), new Vector2(110f, 36f), 20f, Secundario, TextAlignmentOptions.Left);

        TextoIzq(cardT, "MetaPesoTexto", string.Empty, new Vector2(30f, -410f), new Vector2(840f, 36f), 24f, Blanco);
        TextoIzq(cardT, "AvisoTexto", string.Empty, new Vector2(30f, -460f), new Vector2(840f, 70f), 22f, Aviso);

        sb.AppendLine("TarjetaResultados creada con 20 textos independientes");
    }

    private static void Fila(Transform card, string prefijo, string etiqueta, float y)
    {
        TextoIzq(card, prefijo + "Titulo", etiqueta, new Vector2(30f, y), new Vector2(340f, 36f), 24f, Secundario);

        Texto(card, prefijo + "Numero", "--", new Vector2(1f, 1f), new Vector2(1f, 1f),
              new Vector2(-160f, y), new Vector2(160f, 36f), 30f, Blanco, TextAlignmentOptions.Right);

        Texto(card, prefijo + "Sufijo", "g", new Vector2(1f, 1f), new Vector2(0f, 1f),
              new Vector2(-150f, y), new Vector2(100f, 36f), 20f, Secundario, TextAlignmentOptions.Left);
    }

    private static void Cablear(Transform perfil, StringBuilder sb)
    {
        GameObject xr = GameObject.Find("XR Origin");
        BiometricProfile perfilScript = xr.GetComponent<BiometricProfile>();
        Transform card = perfil.Find("TarjetaResultados");

        SerializedObject so = new SerializedObject(perfilScript);

        Asignar(so, "resultadoTitulo", card.Find("TituloResultado"));
        Asignar(so, "resultadoEstado", card.Find("ResultadoEstado"));
        Asignar(so, "kcalTitulo", card.Find("KcalTitulo"));
        Asignar(so, "kcalNumero", card.Find("KcalNumero"));
        Asignar(so, "kcalSufijo", card.Find("KcalSufijo"));

        foreach (string prefijo in new[] { "Prot", "Carbo", "Grasa" })
        {
            string campo = prefijo.ToLowerInvariant();

            if (campo == "prot")
            {
                campo = "proteina";
            }

            Asignar(so, campo + "Titulo", card.Find(prefijo + "Titulo"));
            Asignar(so, campo + "Numero", card.Find(prefijo + "Numero"));
            Asignar(so, campo + "Sufijo", card.Find(prefijo + "Sufijo"));
        }

        Asignar(so, "tmbTitulo", card.Find("TmbTitulo"));
        Asignar(so, "tmbNumero", card.Find("TmbNumero"));
        Asignar(so, "tmbSufijo", card.Find("TmbSufijo"));
        Asignar(so, "metaPesoTexto", card.Find("MetaPesoTexto"));
        Asignar(so, "avisoTexto", card.Find("AvisoTexto"));

        so.ApplyModifiedPropertiesWithoutUndo();

        sb.AppendLine("BiometricProfile cableado con los 20 textos");
    }

    private static void Asignar(SerializedObject so, string campo, Transform destino)
    {
        SerializedProperty prop = so.FindProperty(campo);

        if (prop == null)
        {
            Debug.LogWarning("[AuraBuildPerfil] Campo no encontrado: " + campo);
            return;
        }

        prop.objectReferenceValue = destino != null ? destino.GetComponent<TMP_Text>() : null;
    }

    private static TextMeshProUGUI TextoIzq(Transform padre, string nombre, string contenido, Vector2 pos,
                                           Vector2 tam, float fuente, Color color)
    {
        return Texto(padre, nombre, contenido, new Vector2(0f, 1f), new Vector2(0f, 1f), pos, tam,
                     fuente, color, TextAlignmentOptions.Left);
    }

    private static TextMeshProUGUI Texto(Transform padre, string nombre, string contenido, Vector2 ancMin,
                                         Vector2 ancMax, Vector2 pos, Vector2 tam, float fuente, Color color,
                                         TextAlignmentOptions alineacion)
    {
        TMP_Text clon = Object.Instantiate(_plantilla, padre);
        clon.gameObject.name = nombre;
        clon.text = contenido;
        clon.fontSize = fuente;
        clon.enableAutoSizing = false;
        clon.color = color;
        clon.alignment = alineacion;
        clon.raycastTarget = false;

        LayoutElement le = clon.GetComponent<LayoutElement>();
        if (le != null)
        {
            Object.DestroyImmediate(le);
        }

        TextMeshProUGUI tmp = clon as TextMeshProUGUI;
        RectTransform rt = clon.rectTransform;
        rt.anchorMin = ancMin;
        rt.anchorMax = ancMax;
        rt.pivot = new Vector2(ancMin.x, 1f);
        rt.sizeDelta = tam;
        rt.anchoredPosition = pos;

        return tmp;
    }

    private static void Limpiar(Transform padre, params string[] nombres)
    {
        foreach (string nombre in nombres)
        {
            Transform t = padre.Find(nombre);

            if (t != null)
            {
                Object.DestroyImmediate(t.gameObject);
            }
        }
    }
}
