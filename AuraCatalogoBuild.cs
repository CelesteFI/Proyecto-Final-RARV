using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Paso 2: convierte Panel_GuiaNutricional en el catalogo por TARJETAS (1/3 imagen, 2/3 texto)
/// y crea el sub-panel de detalle con Kcal/Macros y el boton "Servir en el plato".
/// Todo se crea en modo Editor: en runtime solo hay SetActive.
/// </summary>
public static class AuraCatalogoBuild
{
    private static readonly Color FondoTarjeta = new Color(0.078f, 0.098f, 0.176f, 1f);
    private static readonly Color FondoDetalle = new Color(0.03f, 0.04f, 0.07f, 0.985f);
    private static readonly Color TextoSuave = new Color(0.66f, 0.73f, 0.86f, 1f);
    private static readonly Color TextoBlanco = new Color(0.96f, 0.97f, 1f, 1f);
    private static readonly Color AzulPrimario = new Color(0.16f, 0.52f, 0.96f, 1f);

    public static void Execute()
    {
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("[AuraCatalogoBuild] No se encontro el objeto 'Canvas'.");
            return;
        }

        GameObject panelGuia = BuscarHijo(canvas, "Panel_GuiaNutricional");
        GameObject panelAR = BuscarHijo(canvas, "Panel_AR");

        if (panelGuia == null || panelAR == null)
        {
            Debug.LogError("[AuraCatalogoBuild] Faltan Panel_GuiaNutricional o Panel_AR.");
            return;
        }

        GameObject xrOrigin = GameObject.Find("XR Origin");
        ARUIManager arUI = xrOrigin.GetComponent<ARUIManager>();
        MenuController menu = xrOrigin.GetComponent<MenuController>();
        FoodCatalog catalog = xrOrigin.GetComponent<FoodCatalog>();

        TMP_Dropdown dropdown = BuscarComponente<TMP_Dropdown>(panelAR, "dropdownAlimentos");
        if (dropdown == null)
        {
            Debug.LogError("[AuraCatalogoBuild] No se encontro dropdownAlimentos.");
            return;
        }

        // El layout vertical generico del panel estorba a la rejilla de tarjetas.
        VerticalLayoutGroup layoutViejo = panelGuia.GetComponent<VerticalLayoutGroup>();
        if (layoutViejo != null)
        {
            UnityEngine.Object.DestroyImmediate(layoutViejo);
        }

        TMP_Text tituloGuia = BuscarComponente<TMP_Text>(panelGuia, "TituloGuia");
        TMP_Text textoGuia = BuscarComponente<TMP_Text>(panelGuia, "TextoGuia");
        Button btnVolver = BuscarComponente<Button>(panelGuia, "Btn_VolverGuia");

        if (tituloGuia == null)
        {
            Debug.LogError("[AuraCatalogoBuild] Falta TituloGuia como plantilla de texto.");
            return;
        }

        // Encabezado del catalogo.
        Colocar((RectTransform)tituloGuia.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -70f), new Vector2(900f, 80f));
        tituloGuia.text = "Catalogo de alimentos";
        tituloGuia.fontSize = 46f;
        tituloGuia.fontStyle = FontStyles.Bold;
        tituloGuia.alignment = TextAlignmentOptions.Center;
        tituloGuia.color = TextoBlanco;

        if (textoGuia != null)
        {
            Colocar((RectTransform)textoGuia.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -160f), new Vector2(880f, 70f));
            textoGuia.text = "Toca una tarjeta para ver su energia y sus macros.";
            textoGuia.fontSize = 26f;
            textoGuia.fontStyle = FontStyles.Normal;
            textoGuia.alignment = TextAlignmentOptions.Center;
            textoGuia.color = TextoSuave;
        }

        if (btnVolver != null)
        {
            Colocar((RectTransform)btnVolver.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0f, 60f), new Vector2(360f, 84f));
            LimpiarListeners(btnVolver.onClick);
            if (menu != null)
            {
                UnityEventTools.AddVoidPersistentListener(btnVolver.onClick, menu.MostrarMenuPrincipal);
            }
        }

        // Rejilla de tarjetas: 2 columnas x 7 filas, sin scroll (todas visibles de una vez).
        GameObject rejilla = CrearObjeto("GridTarjetas", panelGuia.transform);
        RectTransform rejillaRt = (RectTransform)rejilla.transform;
        rejillaRt.anchorMin = Vector2.zero;
        rejillaRt.anchorMax = Vector2.one;
        rejillaRt.offsetMin = new Vector2(40f, 170f);
        rejillaRt.offsetMax = new Vector2(-40f, -240f);

        GridLayoutGroup grid = rejilla.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(425f, 96f);
        grid.spacing = new Vector2(24f, 18f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperCenter;

        Sprite redondeado = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        List<string> alimentos = new List<string>();
        for (int i = 0; i < dropdown.options.Count; i++)
        {
            string texto = dropdown.options[i].text.Trim();
            if (!string.IsNullOrEmpty(texto))
            {
                alimentos.Add(texto);
            }
        }

        for (int i = 0; i < alimentos.Count; i++)
        {
            string nombre = alimentos[i];
            bool plato = catalog != null && catalog.EsPlatoCompuesto(nombre);
            bool cocido = plato;

            GameObject tarjeta = CrearTarjeta(rejilla.transform, "Tarjeta_" + (i + 1).ToString("00"),
                                             FondoTarjeta, redondeado);
            Button boton = tarjeta.GetComponent<Button>();

            // 1/3 izquierdo: imagen del alimento. 2/3 derecho: textos.
            GameObject icono = CrearObjeto("Icono", tarjeta.transform);
            RectTransform iconoRt = (RectTransform)icono.transform;
            Colocar(iconoRt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(72f, 0f), new Vector2(96f, 74f));
            Image iconoImg = icono.AddComponent<Image>();
            iconoImg.sprite = redondeado;
            iconoImg.type = Image.Type.Sliced;
            iconoImg.raycastTarget = false;
            iconoImg.color = catalog != null && catalog.TryGetColor(nombre, cocido, out Color color)
                ? color
                : Color.white;

            TMP_Text nombreTxt = ClonarTexto(tituloGuia, tarjeta.transform, "Txt_Nombre");
            Colocar((RectTransform)nombreTxt.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(150f, 18f), new Vector2(265f, 42f));
            nombreTxt.text = nombre;
            nombreTxt.fontSize = 30f;
            nombreTxt.fontStyle = FontStyles.Bold;
            nombreTxt.alignment = TextAlignmentOptions.Left;
            nombreTxt.color = TextoBlanco;
            nombreTxt.overflowMode = TextOverflowModes.Ellipsis;

            string kcal = "--";
            if (FoodNutritionData.TryGetNutrition(nombre, out FoodNutrition info))
            {
                kcal = Mathf.RoundToInt(info.Kcal(cocido)).ToString();
            }

            TMP_Text kcalNumero = ClonarTexto(tituloGuia, tarjeta.transform, "Txt_KcalNumero");
            Colocar((RectTransform)kcalNumero.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(150f, -20f), new Vector2(64f, 32f));
            kcalNumero.text = kcal;
            kcalNumero.fontSize = 26f;
            kcalNumero.fontStyle = FontStyles.Bold;
            kcalNumero.alignment = TextAlignmentOptions.Left;
            kcalNumero.color = TextoBlanco;

            TMP_Text kcalSufijo = ClonarTexto(tituloGuia, tarjeta.transform, "Txt_KcalSufijo");
            Colocar((RectTransform)kcalSufijo.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(218f, -20f), new Vector2(180f, 32f));
            kcalSufijo.text = "kcal / 100 g";
            kcalSufijo.fontSize = 21f;
            kcalSufijo.fontStyle = FontStyles.Normal;
            kcalSufijo.alignment = TextAlignmentOptions.Left;
            kcalSufijo.color = TextoSuave;

            if (arUI != null && boton != null)
            {
                UnityEventTools.AddStringPersistentListener(boton.onClick, arUI.AbrirDetalleAlimento, nombre);
            }
        }

        GameObject panelDetalle = CrearDetalle(panelGuia, tituloGuia, redondeado, arUI);
        panelDetalle.SetActive(false);

        // Cableado de ARUIManager con el sub-panel de detalle.
        if (arUI != null)
        {
            SerializedObject so = new SerializedObject(arUI);
            Asignar(so, "menu", menu);
            Asignar(so, "panelDetalle", panelDetalle);
            Asignar(so, "detalleIcono", BuscarComponente<Image>(panelDetalle, "Icono"));
            Asignar(so, "detalleTitulo", BuscarComponente<TMP_Text>(panelDetalle, "Titulo"));
            Asignar(so, "detalleEstado", BuscarComponente<TMP_Text>(panelDetalle, "Estado"));
            Asignar(so, "detalleKcalNumero", BuscarComponente<TMP_Text>(panelDetalle, "KcalNumero"));
            Asignar(so, "detalleKcalSufijo", BuscarComponente<TMP_Text>(panelDetalle, "KcalSufijo"));
            Asignar(so, "detalleProteinaTitulo", BuscarComponente<TMP_Text>(panelDetalle, "ProtTitulo"));
            Asignar(so, "detalleProteinaNumero", BuscarComponente<TMP_Text>(panelDetalle, "ProtNumero"));
            Asignar(so, "detalleProteinaSufijo", BuscarComponente<TMP_Text>(panelDetalle, "ProtSufijo"));
            Asignar(so, "detalleCarboTitulo", BuscarComponente<TMP_Text>(panelDetalle, "CarboTitulo"));
            Asignar(so, "detalleCarboNumero", BuscarComponente<TMP_Text>(panelDetalle, "CarboNumero"));
            Asignar(so, "detalleCarboSufijo", BuscarComponente<TMP_Text>(panelDetalle, "CarboSufijo"));
            Asignar(so, "detalleGrasaTitulo", BuscarComponente<TMP_Text>(panelDetalle, "GrasaTitulo"));
            Asignar(so, "detalleGrasaNumero", BuscarComponente<TMP_Text>(panelDetalle, "GrasaNumero"));
            Asignar(so, "detalleGrasaSufijo", BuscarComponente<TMP_Text>(panelDetalle, "GrasaSufijo"));
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // MenuController necesita conocer el catalogo y el ARUIManager.
        if (menu != null)
        {
            SerializedObject soMenu = new SerializedObject(menu);
            Asignar(soMenu, "arUI", arUI);
            soMenu.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorUtility.SetDirty(panelGuia);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("[AuraCatalogoBuild] Catalogo por tarjetas creado con " + alimentos.Count + " tarjetas y detalle.");
    }

    private static GameObject CrearDetalle(GameObject panelGuia, TMP_Text plantilla, Sprite redondeado, ARUIManager arUI)
    {
        GameObject panel = CrearObjeto("PanelDetalle", panelGuia.transform);
        RectTransform rt = (RectTransform)panel.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image fondo = panel.AddComponent<Image>();
        fondo.color = FondoDetalle;
        fondo.raycastTarget = true;

        GameObject tarjeta = CrearObjeto("TarjetaDetalle", panel.transform);
        RectTransform tarjetaRt = (RectTransform)tarjeta.transform;
        tarjetaRt.anchorMin = new Vector2(0.5f, 0.5f);
        tarjetaRt.anchorMax = new Vector2(0.5f, 0.5f);
        tarjetaRt.pivot = new Vector2(0.5f, 0.5f);
        tarjetaRt.anchoredPosition = Vector2.zero;
        tarjetaRt.sizeDelta = new Vector2(900f, 1180f);

        Image tarjetaImg = tarjeta.AddComponent<Image>();
        tarjetaImg.sprite = redondeado;
        tarjetaImg.type = Image.Type.Sliced;
        tarjetaImg.color = FondoTarjeta;

        GameObject icono = CrearObjeto("Icono", tarjeta.transform);
        Colocar((RectTransform)icono.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -80f), new Vector2(240f, 200f));
        Image iconoImg = icono.AddComponent<Image>();
        iconoImg.sprite = redondeado;
        iconoImg.type = Image.Type.Sliced;
        iconoImg.raycastTarget = false;
        iconoImg.color = Color.white;

        TMP_Text titulo = ClonarTexto(plantilla, tarjeta.transform, "Titulo");
        Colocar((RectTransform)titulo.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -310f), new Vector2(820f, 70f));
        titulo.text = "Alimento";
        titulo.fontSize = 52f;
        titulo.fontStyle = FontStyles.Bold;
        titulo.alignment = TextAlignmentOptions.Center;
        titulo.color = TextoBlanco;

        TMP_Text estado = ClonarTexto(plantilla, tarjeta.transform, "Estado");
        Colocar((RectTransform)estado.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -390f), new Vector2(820f, 48f));
        estado.text = "Crudo · valores por 100 g";
        estado.fontSize = 26f;
        estado.fontStyle = FontStyles.Normal;
        estado.alignment = TextAlignmentOptions.Center;
        estado.color = TextoSuave;

        TMP_Text kcalNumero = ClonarTexto(plantilla, tarjeta.transform, "KcalNumero");
        Colocar((RectTransform)kcalNumero.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -480f), new Vector2(820f, 110f));
        kcalNumero.text = "--";
        kcalNumero.fontSize = 84f;
        kcalNumero.fontStyle = FontStyles.Bold;
        kcalNumero.alignment = TextAlignmentOptions.Center;
        kcalNumero.color = TextoBlanco;

        TMP_Text kcalSufijo = ClonarTexto(plantilla, tarjeta.transform, "KcalSufijo");
        Colocar((RectTransform)kcalSufijo.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -585f), new Vector2(820f, 44f));
        kcalSufijo.text = "kcal / 100 g";
        kcalSufijo.fontSize = 26f;
        kcalSufijo.fontStyle = FontStyles.Normal;
        kcalSufijo.alignment = TextAlignmentOptions.Center;
        kcalSufijo.color = TextoSuave;

        CrearFilaMacro(tarjeta.transform, plantilla, "Prot", "Proteinas", -690f);
        CrearFilaMacro(tarjeta.transform, plantilla, "Carbo", "Carbohidratos", -790f);
        CrearFilaMacro(tarjeta.transform, plantilla, "Grasa", "Grasas", -890f);

        Button servir = CrearBoton(tarjeta.transform, plantilla, "Btn_Servir", "Servir en el plato",
                                   new Vector2(0f, 200f), new Vector2(720f, 100f), AzulPrimario, 32f);
        Button cerrar = CrearBoton(tarjeta.transform, plantilla, "Btn_CerrarDetalle", "Volver al catalogo",
                                   new Vector2(0f, 90f), new Vector2(720f, 80f),
                                   new Color(0.18f, 0.21f, 0.30f, 1f), 28f);

        if (arUI != null)
        {
            UnityEventTools.AddVoidPersistentListener(servir.onClick, arUI.ServirEnElPlato);
            UnityEventTools.AddVoidPersistentListener(cerrar.onClick, arUI.CerrarDetalle);
        }

        return panel;
    }

    private static void CrearFilaMacro(Transform padre, TMP_Text plantilla, string prefijo, string etiqueta, float y)
    {
        TMP_Text titulo = ClonarTexto(plantilla, padre, prefijo + "Titulo");
        Colocar((RectTransform)titulo.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(60f, y), new Vector2(380f, 50f));
        titulo.text = etiqueta;
        titulo.fontSize = 30f;
        titulo.fontStyle = FontStyles.Normal;
        titulo.alignment = TextAlignmentOptions.Left;
        titulo.color = TextoSuave;

        TMP_Text numero = ClonarTexto(plantilla, padre, prefijo + "Numero");
        Colocar((RectTransform)numero.transform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-140f, y), new Vector2(180f, 50f));
        numero.text = "--";
        numero.fontSize = 32f;
        numero.fontStyle = FontStyles.Bold;
        numero.alignment = TextAlignmentOptions.Right;
        numero.color = TextoBlanco;

        TMP_Text sufijo = ClonarTexto(plantilla, padre, prefijo + "Sufijo");
        Colocar((RectTransform)sufijo.transform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-120f, y), new Vector2(100f, 50f));
        sufijo.text = "g";
        sufijo.fontSize = 26f;
        sufijo.fontStyle = FontStyles.Normal;
        sufijo.alignment = TextAlignmentOptions.Left;
        sufijo.color = TextoSuave;
    }

    private static Button CrearBoton(Transform padre, TMP_Text plantilla, string nombre, string etiqueta,
                                     Vector2 posicion, Vector2 tam, Color fondo, float fuente)
    {
        GameObject go = CrearObjeto(nombre, padre);
        RectTransform rt = (RectTransform)go.transform;
        Colocar(rt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), posicion, tam);

        Image img = go.AddComponent<Image>();
        img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        img.type = Image.Type.Sliced;
        img.color = fondo;

        Button boton = go.AddComponent<Button>();
        boton.targetGraphic = img;

        TMP_Text texto = ClonarTexto(plantilla, go.transform, "Label");
        RectTransform textoRt = (RectTransform)texto.transform;
        textoRt.anchorMin = Vector2.zero;
        textoRt.anchorMax = Vector2.one;
        textoRt.offsetMin = Vector2.zero;
        textoRt.offsetMax = Vector2.zero;
        texto.text = etiqueta;
        texto.fontSize = fuente;
        texto.fontStyle = FontStyles.Bold;
        texto.alignment = TextAlignmentOptions.Center;
        texto.color = Color.white;
        texto.raycastTarget = false;

        return boton;
    }

    private static GameObject CrearTarjeta(Transform padre, string nombre, Color fondo, Sprite redondeado)
    {
        GameObject go = CrearObjeto(nombre, padre);

        Image img = go.AddComponent<Image>();
        img.sprite = redondeado;
        img.type = Image.Type.Sliced;
        img.color = fondo;

        Button boton = go.AddComponent<Button>();
        boton.targetGraphic = img;

        return go;
    }

    internal static GameObject CrearObjeto(string nombre, Transform padre)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        return go;
    }

    internal static void Colocar(RectTransform rt, Vector2 ancla, Vector2 pivote, Vector2 posicion, Vector2 tam)
    {
        rt.anchorMin = ancla;
        rt.anchorMax = ancla;
        rt.pivot = pivote;
        rt.anchoredPosition = posicion;
        rt.sizeDelta = tam;
    }

    internal static TMP_Text ClonarTexto(TMP_Text plantilla, Transform padre, string nombre)
    {
        GameObject go = UnityEngine.Object.Instantiate(plantilla.gameObject, padre, false);
        go.name = nombre;

        RectTransform rt = (RectTransform)go.transform;
        rt.localScale = Vector3.one;

        LayoutElement layout = go.GetComponent<LayoutElement>();
        if (layout != null)
        {
            UnityEngine.Object.DestroyImmediate(layout);
        }

        TMP_Text texto = go.GetComponent<TMP_Text>();
        texto.raycastTarget = false;
        return texto;
    }

    internal static T BuscarComponente<T>(GameObject raiz, string nombre) where T : Component
    {
        Transform[] todos = raiz.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < todos.Length; i++)
        {
            if (todos[i].gameObject.name != nombre)
            {
                continue;
            }

            T componente = todos[i].GetComponent<T>();
            if (componente != null)
            {
                return componente;
            }
        }

        Debug.LogWarning("[AuraCatalogoBuild] No se encontro " + typeof(T).Name + " llamado '" + nombre + "'.");
        return null;
    }

    internal static T BuscarComponente<T>(Transform raiz, string nombre) where T : Component
    {
        return BuscarComponente<T>(raiz.gameObject, nombre);
    }

    internal static GameObject BuscarHijo(GameObject raiz, string nombre)
    {
        Transform encontrado = raiz.transform.Find(nombre);
        return encontrado != null ? encontrado.gameObject : null;
    }

    internal static void LimpiarListeners(UnityEngine.Events.UnityEventBase evento)
    {
        for (int i = evento.GetPersistentEventCount() - 1; i >= 0; i--)
        {
            UnityEventTools.RemovePersistentListener(evento, i);
        }
    }

    internal static void Asignar(SerializedObject so, string campo, UnityEngine.Object valor)
    {
        SerializedProperty prop = so.FindProperty(campo);
        if (prop == null)
        {
            Debug.LogError("[AuraCatalogoBuild] Campo no encontrado: " + campo);
            return;
        }

        prop.objectReferenceValue = valor;
    }
}
