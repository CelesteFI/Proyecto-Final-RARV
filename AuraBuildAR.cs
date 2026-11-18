using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Reestructura el Panel_AR: dropdown con miniatura, boton "Servir en el plato" junto a el y
/// recuadro de metas con las Kcal del alimento elegido. Todo pre-creado en la jerarquia.
/// </summary>
public static class AuraBuildAR
{
    private static readonly Color Fondo = new Color32(20, 25, 45, 240);
    private static readonly Color Blanco = new Color32(245, 247, 255, 255);
    private static readonly Color Secundario = new Color32(168, 186, 219, 255);
    private static readonly Color Acento = new Color32(41, 133, 245, 255);
    private static readonly Color BotonGris = new Color32(51, 59, 76, 255);

    private static TMP_Text _plantilla;
    private static FoodCatalog _catalog;

    public static string Execute()
    {
        StringBuilder sb = new StringBuilder();

        GameObject canvasGo = GameObject.Find("Canvas");
        Transform canvas = canvasGo.transform;
        Transform panelAR = canvas.Find("Panel_AR");

        _plantilla = canvas.Find("Panel_GuiaNutricional/TituloGuia").GetComponent<TMP_Text>();
        _catalog = Object.FindAnyObjectByType<FoodCatalog>(FindObjectsInactive.Include);

        DropdownConMiniaturas(panelAR, sb);
        PanelSuperior(panelAR, sb);
        RecuadroMetas(panelAR, sb);
        QuitarServirDelDetalle(canvas, sb);
        Cablear(panelAR, sb);

        EditorUtility.SetDirty(canvasGo);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        return sb.ToString();
    }

    // ------------------------------------------------------------------ dropdown

    private static void DropdownConMiniaturas(Transform panelAR, StringBuilder sb)
    {
        Transform dd = panelAR.Find("PanelSuperior/dropdownAlimentos");
        if (dd == null)
        {
            sb.AppendLine("FALTA dropdownAlimentos");
            return;
        }

        TMP_Dropdown dropdown = dd.GetComponent<TMP_Dropdown>();

        // Imagen del valor seleccionado (caption) y de cada fila de la lista (item).
        Image captionIcon = HijoImagen(dd, "Caption Icon", new Vector2(12f, 0f), new Vector2(48f, 48f));
        captionIcon.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        captionIcon.type = Image.Type.Sliced;

        Transform item = dd.Find("Template/Viewport/Content/Item");
        Image itemIcon = item != null
            ? HijoImagen(item, "Item Icon", new Vector2(36f, 0f), new Vector2(28f, 28f))
            : null;

        if (itemIcon != null)
        {
            itemIcon.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            itemIcon.type = Image.Type.Sliced;
        }

        dropdown.captionImage = captionIcon;
        dropdown.itemImage = itemIcon;

        Estirar(dd.Find("Label"), 70f, -44f);
        Estirar(item != null ? item.Find("Item Label") : null, 72f, -12f);

        // Miniatura por opcion: sprite generado a partir del color del alimento en FoodCatalog.
        int conIcono = 0;

        for (int i = 0; i < dropdown.options.Count; i++)
        {
            string nombre = dropdown.options[i].text;
            Sprite icono = IconoDeAlimento(nombre);

            if (icono != null)
            {
                dropdown.options[i].image = icono;
                conIcono++;
            }
            else
            {
                sb.AppendLine("sin icono: " + nombre);
            }
        }

        dropdown.RefreshShownValue();
        sb.AppendLine("dropdown: " + conIcono + "/" + dropdown.options.Count + " opciones con miniatura");
    }

    private static void PanelSuperior(Transform panelAR, StringBuilder sb)
    {
        Transform superior = panelAR.Find("PanelSuperior");
        RectTransform rt = superior as RectTransform;
        rt.sizeDelta = new Vector2(0f, 200f);

        RectTransform dd = superior.Find("dropdownAlimentos") as RectTransform;
        Colocar(dd, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(24f, -16f), new Vector2(620f, 80f));

        RectTransform menuBtn = superior.Find("Btn_AbrirMenu") as RectTransform;
        Colocar(menuBtn, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-24f, -16f), new Vector2(150f, 80f));

        RectTransform toggle = superior.Find("toggleCocido") as RectTransform;
        Colocar(toggle, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(340f, -112f), new Vector2(260f, 76f));

        // Boton "Servir en el plato": debajo del dropdown, en el mismo panel superior.
        Transform existente = superior.Find("Btn_ServirPlato");
        if (existente != null)
        {
            Object.DestroyImmediate(existente.gameObject);
        }

        Button servir = Boton(superior, "Btn_ServirPlato", "Servir en el plato", Acento);
        Colocar(servir.transform as RectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(24f, -112f), new Vector2(300f, 76f));

        sb.AppendLine("PanelSuperior listo con Btn_ServirPlato");
    }

    private static void RecuadroMetas(Transform panelAR, StringBuilder sb)
    {
        Transform metas = panelAR.Find("PanelMetas");
        if (metas == null)
        {
            sb.AppendLine("FALTA PanelMetas");
            return;
        }

        RectTransform rt = metas as RectTransform;
        Colocar(rt, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-24f, -224f), new Vector2(460f, 330f));

        Image fondo = metas.GetComponent<Image>();
        if (fondo != null)
        {
            fondo.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            fondo.type = Image.Type.Sliced;
            fondo.color = Fondo;
            fondo.raycastTarget = false;
        }

        CanvasGroup grupo = metas.GetComponent<CanvasGroup>();
        if (grupo != null)
        {
            grupo.alpha = 1f;
            grupo.blocksRaycasts = false;
            grupo.interactable = false;
        }

        // El texto unico de muestra desaparece: ahora cada dato tiene su propio TMP.
        Transform viejo = metas.Find("TextoMetas");
        if (viejo != null)
        {
            Object.DestroyImmediate(viejo.gameObject);
        }

        Limpiar(metas, "TituloBox", "NombreAlimento", "IconoMetas", "KcalNumero", "KcalSufijo",
                "ProtTitulo", "ProtNumero", "ProtSufijo", "CarboTitulo", "CarboNumero", "CarboSufijo",
                "GrasaTitulo", "GrasaNumero", "GrasaSufijo");

        // Miniatura del alimento elegido.
        Transform icono = metas.Find("IconoMetas");
        Image iconoImg = icono == null
            ? HijoImagen(metas, "IconoMetas", Vector2.zero, new Vector2(56f, 56f))
            : icono.GetComponent<Image>();
        iconoImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        iconoImg.type = Image.Type.Sliced;
        iconoImg.raycastTarget = false;
        Colocar(iconoImg.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-18f, -14f), new Vector2(56f, 56f));

        TMP_Text titulo = Texto(metas, "TituloBox", "Kcal del alimento", 22f, Secundario, TextAlignmentOptions.Left);
        Colocar(titulo.rectTransform, ArribaIzq, ArribaIzq, ArribaIzq, new Vector2(20f, -12f), new Vector2(340f, 28f));

        TMP_Text nombre = Texto(metas, "NombreAlimento", "Arroz", 30f, Blanco, TextAlignmentOptions.Left);
        Colocar(nombre.rectTransform, ArribaIzq, ArribaIzq, ArribaIzq, new Vector2(20f, -44f), new Vector2(340f, 40f));

        TMP_Text kcalNum = Texto(metas, "KcalNumero", "358", 48f, Blanco, TextAlignmentOptions.Left);
        Colocar(kcalNum.rectTransform, ArribaIzq, ArribaIzq, ArribaIzq, new Vector2(20f, -96f), new Vector2(150f, 64f));

        TMP_Text kcalSuf = Texto(metas, "KcalSufijo", "kcal / 100 g", 22f, Secundario, TextAlignmentOptions.Left);
        Colocar(kcalSuf.rectTransform, ArribaIzq, ArribaIzq, ArribaIzq, new Vector2(178f, -122f), new Vector2(240f, 32f));

        FilaMetas(metas, "Prot", "Proteinas", -182f);
        FilaMetas(metas, "Carbo", "Carbohidratos", -228f);
        FilaMetas(metas, "Grasa", "Grasas", -274f);

        sb.AppendLine("PanelMetas reconstruido como recuadro de Kcal del alimento");
    }

    private static void FilaMetas(Transform metas, string prefijo, string etiqueta, float y)
    {
        TMP_Text titulo = Texto(metas, prefijo + "Titulo", etiqueta, 20f, Secundario, TextAlignmentOptions.Left);
        Colocar(titulo.rectTransform, ArribaIzq, ArribaIzq, ArribaIzq, new Vector2(20f, y), new Vector2(200f, 30f));

        TMP_Text numero = Texto(metas, prefijo + "Numero", "--", 28f, Blanco, TextAlignmentOptions.Right);
        Colocar(numero.rectTransform, ArribaDer, ArribaDer, ArribaDer, new Vector2(-110f, y), new Vector2(120f, 30f));

        TMP_Text sufijo = Texto(metas, prefijo + "Sufijo", "g", 20f, Secundario, TextAlignmentOptions.Left);
        Colocar(sufijo.rectTransform, ArribaDer, ArribaDer, new Vector2(0f, 1f), new Vector2(-100f, y), new Vector2(60f, 30f));
    }

    // ------------------------------------------------------------------ catalogo de guia

    private static void QuitarServirDelDetalle(Transform canvas, StringBuilder sb)
    {
        Transform card = canvas.Find("Panel_GuiaNutricional/PanelDetalle/TarjetaDetalle");
        if (card == null)
        {
            sb.AppendLine("FALTA TarjetaDetalle");
            return;
        }

        Transform servir = card.Find("Btn_Servir");
        if (servir != null)
        {
            Object.DestroyImmediate(servir.gameObject);
            sb.AppendLine("Btn_Servir eliminado del panel Guia (es solo informativo)");
        }

        Transform cerrar = card.Find("Btn_CerrarDetalle");
        if (cerrar != null)
        {
            Colocar(cerrar as RectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0f, 110f), new Vector2(720f, 100f));
        }
    }

    // ------------------------------------------------------------------ cableado

    private static void Cablear(Transform panelAR, StringBuilder sb)
    {
        GameObject xr = GameObject.Find("XR Origin");
        ARUIManager arUI = xr.GetComponent<ARUIManager>();
        MenuLateralARController lateral = panelAR.Find("MenuLateralAR").GetComponent<MenuLateralARController>();

        Transform metas = panelAR.Find("PanelMetas");

        SerializedObject so = new SerializedObject(arUI);
        Asignar(so, "menuLateral", lateral);
        Asignar(so, "metasIcono", metas.Find("IconoMetas").GetComponent<Image>());
        Asignar(so, "metasNombre", metas.Find("NombreAlimento").GetComponent<TMP_Text>());
        Asignar(so, "metasKcalNumero", metas.Find("KcalNumero").GetComponent<TMP_Text>());
        Asignar(so, "metasKcalSufijo", metas.Find("KcalSufijo").GetComponent<TMP_Text>());
        Asignar(so, "metasProteinaNumero", metas.Find("ProtNumero").GetComponent<TMP_Text>());
        Asignar(so, "metasProteinaSufijo", metas.Find("ProtSufijo").GetComponent<TMP_Text>());
        Asignar(so, "metasCarboNumero", metas.Find("CarboNumero").GetComponent<TMP_Text>());
        Asignar(so, "metasCarboSufijo", metas.Find("CarboSufijo").GetComponent<TMP_Text>());
        Asignar(so, "metasGrasaNumero", metas.Find("GrasaNumero").GetComponent<TMP_Text>());
        Asignar(so, "metasGrasaSufijo", metas.Find("GrasaSufijo").GetComponent<TMP_Text>());
        so.ApplyModifiedPropertiesWithoutUndo();

        Button servir = panelAR.Find("PanelSuperior/Btn_ServirPlato").GetComponent<Button>();
        for (int i = servir.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
        {
            UnityEventTools.RemovePersistentListener(servir.onClick, i);
        }

        UnityEventTools.AddVoidPersistentListener(servir.onClick, arUI.ServirEnElPlato);
        EditorUtility.SetDirty(servir);

        sb.AppendLine("ARUIManager cableado (11 campos) y Btn_ServirPlato enlazado");
    }

    // ------------------------------------------------------------------ iconos

    private static Sprite IconoDeAlimento(string nombre)
    {
        Color color = new Color32(150, 150, 150, 255);

        if (_catalog != null)
        {
            if (!_catalog.TryGetColor(nombre, true, out color) &&
                !_catalog.TryGetColor(nombre, false, out color))
            {
                color = new Color32(150, 150, 150, 255);
            }
        }

        return CrearIcono(nombre, color);
    }

    private static Sprite CrearIcono(string nombre, Color color)
    {
        const int lado = 64;
        const float radio = 14f;

        if (!AssetDatabase.IsValidFolder("Assets/Sprites/IconosAlimentos"))
        {
            AssetDatabase.CreateFolder("Assets/Sprites", "IconosAlimentos");
        }

        string ruta = "Assets/Sprites/IconosAlimentos/Icono_" + Sanear(nombre) + ".png";

        if (!File.Exists(ruta))
        {
            Texture2D tex = new Texture2D(lado, lado, TextureFormat.RGBA32, false);
            Color32[] pixeles = new Color32[lado * lado];

            for (int y = 0; y < lado; y++)
            {
                for (int x = 0; x < lado; x++)
                {
                    float cx = Mathf.Clamp(x, radio, lado - 1 - radio);
                    float cy = Mathf.Clamp(y, radio, lado - 1 - radio);
                    float distancia = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));

                    pixeles[(y * lado) + x] = distancia <= radio
                        ? (Color32)color
                        : new Color32(0, 0, 0, 0);
                }
            }

            tex.SetPixels32(pixeles);
            tex.Apply();

            File.WriteAllBytes(ruta, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);
        }

        TextureImporter imp = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (imp != null)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = lado;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.filterMode = FilterMode.Bilinear;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
    }

    private static string Sanear(string nombre)
    {
        return nombre.Replace(" ", string.Empty).Replace("á", "a").Replace("é", "e")
                     .Replace("í", "i").Replace("ó", "o").Replace("ú", "u");
    }

    // ------------------------------------------------------------------ utilidades UI

    private static readonly Vector2 ArribaIzq = new Vector2(0f, 1f);
    private static readonly Vector2 ArribaDer = new Vector2(1f, 1f);

    private static void Asignar(SerializedObject so, string campo, Object valor)
    {
        SerializedProperty prop = so.FindProperty(campo);

        if (prop == null)
        {
            Debug.LogWarning("[AuraBuildAR] Campo no encontrado: " + campo);
            return;
        }

        prop.objectReferenceValue = valor;
    }

    private static Image HijoImagen(Transform padre, string nombre, Vector2 pos, Vector2 tam)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(padre, false);

        Image img = go.GetComponent<Image>();
        img.raycastTarget = false;

        Colocar(img.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), pos, tam);
        return img;
    }

    private static TMP_Text Texto(Transform padre, string nombre, string contenido, float tam, Color color,
                                  TextAlignmentOptions alineacion)
    {
        TMP_Text clon = Object.Instantiate(_plantilla, padre);
        clon.gameObject.name = nombre;
        clon.text = contenido;
        clon.fontSize = tam;
        clon.enableAutoSizing = false;
        clon.color = color;
        clon.alignment = alineacion;
        clon.raycastTarget = false;

        LayoutElement le = clon.GetComponent<LayoutElement>();
        if (le != null)
        {
            Object.DestroyImmediate(le);
        }

        return clon;
    }

    private static Button Boton(Transform padre, string nombre, string etiqueta, Color fondo)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(padre, false);

        Image img = go.GetComponent<Image>();
        img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        img.type = Image.Type.Sliced;
        img.color = fondo;

        Button btn = go.GetComponent<Button>();
        btn.targetGraphic = img;

        TMP_Text label = Object.Instantiate(_plantilla, go.transform);
        label.gameObject.name = "Label";
        label.text = etiqueta;
        label.fontSize = 26f;
        label.enableAutoSizing = false;
        label.color = Blanco;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;

        LayoutElement le = label.GetComponent<LayoutElement>();
        if (le != null)
        {
            Object.DestroyImmediate(le);
        }

        RectTransform lrt = label.rectTransform;
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.pivot = new Vector2(0.5f, 0.5f);
        lrt.offsetMin = new Vector2(12f, 0f);
        lrt.offsetMax = new Vector2(-12f, 0f);

        return btn;
    }

    private static void Estirar(Transform t, float izquierda, float derecha)
    {
        RectTransform rt = t as RectTransform;

        if (rt == null)
        {
            return;
        }

        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(izquierda, 0f);
        rt.offsetMax = new Vector2(derecha, 0f);
    }

    private static void Colocar(RectTransform rt, Vector2 ancMin, Vector2 ancMax, Vector2 pivote,
                                Vector2 pos, Vector2 tam)
    {
        if (rt == null)
        {
            return;
        }

        rt.anchorMin = ancMin;
        rt.anchorMax = ancMax;
        rt.pivot = pivote;
        rt.sizeDelta = tam;
        rt.anchoredPosition = pos;
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
