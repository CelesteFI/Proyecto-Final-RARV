using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Paso 4 del armado de NutriAR: agrega la pantalla de carga, rellena el menu lateral y
/// coloca el boton hamburguesa en PanelSuperior. Solo crea UI en Edit Time y cablea
/// listeners persistentes; cero instanciacion en runtime. Idempotente por nombre y por Undo.
/// </summary>
public static class NutriAR_FinalUISetup
{
    private const string MenuPath = "NutriAR/4. Refinar Interfaz (Carga y Menu Lateral)";
    private const string MenuPathEstado = "NutriAR/5. Estado de Arranque Seguro (sin bloqueo de UI)";
    private const string UndoLabel = "NutriAR: Refinar Interfaz";

    private const string ManagersName = "AR_Managers";
    private const string PanelInicioName = "Panel_PantallaInicio";
    private const string PanelCargaName = "Panel_Carga";
    private const string PanelMenuName = "Panel_MenuPrincipal";
    private const string PanelPerfilName = "Panel_Perfil";
    private const string PanelARName = "Panel_AR";
    private const string PanelSuperiorName = "PanelSuperior";
    private const string BotonMenuName = "Btn_AbrirMenu";

    private const float AltoBoton = 96f;

    private static readonly Color ColorFondoCarga = Color.black;
    private static readonly Color ColorFondoMenu = new Color(0.05f, 0.05f, 0.05f, 0.92f);
    private static readonly Color ColorBoton = new Color(0.20f, 0.55f, 0.95f, 1f);
    private static readonly Color ColorBotonSecundario = new Color(0.85f, 0.85f, 0.85f, 1f);

    [MenuItem(MenuPath)]
    public static void Construir()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas == null)
        {
            Debug.LogError("[NutriAR] No hay ningun Canvas en la escena. Ejecuta primero 'NutriAR/Generar Escena y UI'.");
            return;
        }

        GameObject managers = FindInScene(ManagersName);
        MenuController menu = managers != null ? managers.GetComponent<MenuController>() : null;

        if (menu == null)
        {
            Debug.LogError($"[NutriAR] '{ManagersName}' no tiene MenuController. " +
                           "Ejecuta 'NutriAR/2. Conectar Menús y Paneles' antes que este paso.");
            return;
        }

        Transform panelMenu = FindDescendiente(canvas.transform, PanelMenuName);
        Transform panelSuperior = FindDescendiente(canvas.transform, PanelSuperiorName);

        if (panelMenu == null || panelSuperior == null)
        {
            Debug.LogError($"[NutriAR] Faltan '{PanelMenuName}' o '{PanelSuperiorName}' dentro del Canvas. " +
                           "Ejecuta los pasos 1 a 3 antes que este.");
            return;
        }

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(UndoLabel);

        ConstruirPanelCarga(canvas.transform);
        ConstruirMenuLateral(panelMenu.gameObject, menu);
        ConstruirBotonHamburguesa(panelSuperior, menu);
        AplicarEstadoInicial(canvas.transform);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Undo.CollapseUndoOperations(undoGroup);

        Selection.activeGameObject = canvas.gameObject;
        Debug.Log("[NutriAR] Interfaz refinada: Panel_Carga (ultimo hijo, fade automatico), " +
                  "Panel_MenuPrincipal (Btn_VolverInicio + Btn_IrPerfil) y Btn_AbrirMenu en PanelSuperior. " +
                  "Ctrl+Z revierte todo el proceso en un solo paso.", canvas);
    }

    /// <summary>
    /// Reaplica el estado de arranque seguro sin tocar la jerarquia: Panel_PantallaInicio visible,
    /// el resto de paneles apagados, Panel_Carga activo (para que su desvanecido llegue a ejecutarse)
    /// y su CanvasGroup sin interaccion ni raycasts. Idempotente y en un unico grupo de Undo.
    /// </summary>
    [MenuItem(MenuPathEstado)]
    public static void AplicarEstadoArranqueSeguro()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas == null)
        {
            Debug.LogError("[NutriAR] No hay ningun Canvas en la escena.");
            return;
        }

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(UndoLabel + ": estado de arranque");

        AplicarEstadoInicial(canvas.transform);

        Transform carga = FindDescendiente(canvas.transform, PanelCargaName);
        if (carga == null)
        {
            Debug.LogWarning($"[NutriAR] No existe '{PanelCargaName}' en el Canvas: se omite su estado.");
        }
        else
        {
            CanvasGroup grupo = carga.GetComponent<CanvasGroup>();
            if (grupo == null)
            {
                grupo = EnsureComponent<CanvasGroup>(carga.gameObject);
            }

            Undo.RecordObject(grupo, UndoLabel + ": CanvasGroup de carga");
            // alpha 0 en la escena guardada: el panel es negro opaco a pantalla completa y,
            // como ultimo hijo del Canvas, taparia TODA la interfaz en Edit Mode. PanelCarga.Start()
            // lo sube a 1 en el primer frame de Play, antes de desvanecerse.
            grupo.alpha = 0f;
            grupo.interactable = false;
            grupo.blocksRaycasts = false;
            EditorUtility.SetDirty(grupo);

            if (!carga.gameObject.activeSelf)
            {
                Undo.RecordObject(carga.gameObject, UndoLabel + ": activar " + PanelCargaName);
                carga.gameObject.SetActive(true);
                EditorUtility.SetDirty(carga.gameObject);
            }
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Undo.CollapseUndoOperations(undoGroup);

        Selection.activeGameObject = canvas.gameObject;
        Debug.Log("[NutriAR] Estado de arranque seguro aplicado: Panel_PantallaInicio visible, " +
                  "Panel_Carga activo con CanvasGroup sin raycasts (la UI nunca queda bloqueada).", canvas);
    }

    // ------------------------------------------------------------ Panel_Carga

    /// <summary>
    /// Pantalla de arranque: fondo negro opaco con el rotulo "NutriAR". Se coloca como ultimo
    /// hijo para dibujarse por encima de los paneles existentes y se auto-oculta con PanelCarga.
    /// </summary>
    private static void ConstruirPanelCarga(Transform canvas)
    {
        RectTransform panel = AsegurarRect(canvas, PanelCargaName);

        Undo.RecordObject(panel, UndoLabel + ": layout " + PanelCargaName);
        Stretch(panel, 0f, 0f, 0f, 0f);
        panel.SetAsLastSibling();

        AsegurarImagen(panel.gameObject, ColorFondoCarga, true);

        TextMeshProUGUI titulo = AsegurarTexto(panel, "TextoNutriAR", "NutriAR", 120f, TextAlignmentOptions.Center);
        RectTransform tituloRt = titulo.rectTransform;
        Undo.RecordObject(tituloRt, UndoLabel + ": layout del rotulo de carga");
        Stretch(tituloRt, 0f, 0f, 0f, 0f);

        CanvasGroup grupo = EnsureComponent<CanvasGroup>(panel.gameObject);
        Undo.RecordObject(grupo, UndoLabel + ": CanvasGroup de carga");
        // alpha 0: la escena guardada nunca debe quedar tapada por este negro opaco.
        // PanelCarga.Start() fuerza alpha = 1 en el primer frame de Play.
        grupo.alpha = 0f;
        // Una pantalla de carga solo decora: si capturara raycasts, cualquier fallo del
        // desvanecido dejaria la interfaz completa sin respuesta.
        grupo.interactable = false;
        grupo.blocksRaycasts = false;
        EditorUtility.SetDirty(grupo);

        PanelCarga carga = EnsureComponent<PanelCarga>(panel.gameObject);
        CablearCanvasGroup(carga, grupo);

        if (!panel.gameObject.activeSelf)
        {
            Undo.RecordObject(panel.gameObject, UndoLabel + ": activar " + PanelCargaName);
            panel.gameObject.SetActive(true);
            EditorUtility.SetDirty(panel.gameObject);
        }
    }

    private static void CablearCanvasGroup(PanelCarga carga, CanvasGroup grupo)
    {
        var serialized = new SerializedObject(carga);
        Undo.RecordObject(carga, UndoLabel + ": PanelCarga");

        SerializedProperty property = serialized.FindProperty("canvasGroup");
        if (property == null)
        {
            Debug.LogWarning("[NutriAR] PanelCarga no expone el campo serializado 'canvasGroup'.");
            return;
        }

        property.objectReferenceValue = grupo;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(carga);
    }

    // ------------------------------------------------------- Panel_MenuPrincipal

    /// <summary>
    /// Menu lateral: Panel_MenuPrincipal sigue siendo hermano de los paneles del Canvas para que
    /// la conmutacion exclusiva de MenuController no apague Panel_AR al abrirlo.
    /// </summary>
    private static void ConstruirMenuLateral(GameObject panelMenu, MenuController menu)
    {
        Undo.RecordObject(panelMenu, UndoLabel + ": " + PanelMenuName);

        AsegurarImagen(panelMenu, ColorFondoMenu, true);
        AsegurarLayoutVertical(panelMenu, 300, 300);

        Button volverInicio = AsegurarBoton(panelMenu.transform, "Btn_VolverInicio",
                                            "Volver al inicio", ColorBotonSecundario);
        Button irPerfil = AsegurarBoton(panelMenu.transform, "Btn_IrPerfil",
                                        "Mi Perfil", ColorBoton);

        Enlazar(volverInicio, "MostrarInicio", menu.MostrarInicio);
        Enlazar(irPerfil, "MostrarPerfil", menu.MostrarPerfil);
    }

    // -------------------------------------------------------- Boton hamburguesa

    /// <summary>
    /// Boton "Menu" en la banda superior de AR. Se ancla arriba a la derecha y se desplaza para
    /// no solaparse con toggleCocido (que ocupa los ultimos 240 px de esa banda).
    /// </summary>
    private static void ConstruirBotonHamburguesa(Transform panelSuperior, MenuController menu)
    {
        RectTransform botonRt = AsegurarRect(panelSuperior, BotonMenuName);

        Undo.RecordObject(botonRt, UndoLabel + ": layout " + BotonMenuName);
        botonRt.anchorMin = new Vector2(1f, 1f);
        botonRt.anchorMax = new Vector2(1f, 1f);
        botonRt.pivot = new Vector2(1f, 1f);
        botonRt.anchoredPosition = new Vector2(-272f, -20f);
        botonRt.sizeDelta = new Vector2(88f, 60f);

        Image fondo = AsegurarImagen(botonRt.gameObject, ColorBoton, true);

        Button boton = EnsureComponent<Button>(botonRt.gameObject);
        Undo.RecordObject(boton, UndoLabel + ": " + BotonMenuName);
        boton.targetGraphic = fondo;
        boton.transition = Selectable.Transition.ColorTint;

        TextMeshProUGUI label = AsegurarTexto(botonRt, "Label", "Menú", 24f, TextAlignmentOptions.Center);
        RectTransform labelRt = label.rectTransform;
        Undo.RecordObject(labelRt, UndoLabel + ": label " + BotonMenuName);
        Stretch(labelRt, 6f, 6f, 0f, 0f);
        label.color = Color.white;

        Enlazar(boton, "ToggleMenuLateral", menu.ToggleMenuLateral);
    }

    // -------------------------------------------------------------- estructura

    /// <summary>
    /// Estado de arranque: Panel_PantallaInicio visible y el resto de paneles apagados.
    /// Panel_Carga queda encima de todo y se auto-oculta con su propio desvanecido.
    /// </summary>
    private static void AplicarEstadoInicial(Transform canvas)
    {
        string[] paneles = { PanelInicioName, PanelMenuName, PanelPerfilName, PanelARName };

        for (int i = 0; i < paneles.Length; i++)
        {
            Transform panel = FindDescendiente(canvas, paneles[i]);
            if (panel == null)
            {
                continue;
            }

            bool debeEstarActivo = paneles[i] == PanelInicioName;
            if (panel.gameObject.activeSelf == debeEstarActivo)
            {
                continue;
            }

            Undo.RecordObject(panel.gameObject, UndoLabel + ": estado " + panel.name);
            panel.gameObject.SetActive(debeEstarActivo);
            EditorUtility.SetDirty(panel.gameObject);
        }
    }

    private static VerticalLayoutGroup AsegurarLayoutVertical(GameObject panel, int paddingHorizontal, int paddingVertical)
    {
        VerticalLayoutGroup grupo = EnsureComponent<VerticalLayoutGroup>(panel);

        Undo.RecordObject(grupo, UndoLabel + ": layout " + panel.name);
        grupo.padding = new RectOffset(paddingHorizontal, paddingHorizontal, paddingVertical, paddingVertical);
        grupo.spacing = 32f;
        grupo.childAlignment = TextAnchor.UpperCenter;
        grupo.childControlWidth = true;
        grupo.childControlHeight = true;
        grupo.childForceExpandWidth = true;
        grupo.childForceExpandHeight = false;

        EditorUtility.SetDirty(grupo);
        return grupo;
    }

    private static Button AsegurarBoton(Transform padre, string nombre, string etiqueta, Color color)
    {
        Color colorTexto = ColorDeTextoPara(color);

        Transform existente = padre.Find(nombre);
        if (existente != null)
        {
            Button previo = existente.GetComponent<Button>();
            if (previo != null)
            {
                AsegurarContraste(existente, nombre, colorTexto);
                return previo;
            }
        }

        RectTransform root = AsegurarRect(padre, nombre);
        Image fondo = AsegurarImagen(root.gameObject, color, true);

        Button boton = EnsureComponent<Button>(root.gameObject);
        boton.targetGraphic = fondo;
        boton.transition = Selectable.Transition.ColorTint;

        TextMeshProUGUI label = AsegurarTexto(root, "Label", etiqueta, 28f, TextAlignmentOptions.Center);
        RectTransform labelRt = label.rectTransform;
        Undo.RecordObject(labelRt, UndoLabel + ": label " + nombre);
        Stretch(labelRt, 8f, 8f, 0f, 0f);
        label.color = colorTexto;

        LayoutElement elemento = EnsureComponent<LayoutElement>(root.gameObject);
        Undo.RecordObject(elemento, UndoLabel + ": layout element " + nombre);
        elemento.minHeight = AltoBoton;
        elemento.preferredHeight = AltoBoton;
        elemento.flexibleHeight = 0f;

        return boton;
    }

    /// <summary>Blanco sobre fondos oscuros, casi negro sobre fondos claros: el TMP siempre se lee.</summary>
    private static Color ColorDeTextoPara(Color fondo)
    {
        float luminancia = (fondo.r + fondo.g + fondo.b) / 3f;
        return luminancia > 0.6f ? new Color(0.08f, 0.08f, 0.08f, 1f) : Color.white;
    }

    private static void AsegurarContraste(Transform boton, string nombre, Color colorTexto)
    {
        Transform label = boton.Find("Label");
        TextMeshProUGUI texto = label != null ? label.GetComponent<TextMeshProUGUI>() : null;

        if (texto == null || texto.color == colorTexto)
        {
            return;
        }

        Undo.RecordObject(texto, UndoLabel + ": color de texto " + nombre);
        texto.color = colorTexto;
        EditorUtility.SetDirty(texto);
    }

    private static TextMeshProUGUI AsegurarTexto(Transform padre, string nombre, string texto, float tamano,
                                                 TextAlignmentOptions alineacion)
    {
        Transform existente = padre.Find(nombre);
        if (existente != null)
        {
            TextMeshProUGUI previo = existente.GetComponent<TextMeshProUGUI>();
            if (previo != null)
            {
                return previo;
            }
        }

        RectTransform root = AsegurarRect(padre, nombre);
        TextMeshProUGUI label = Undo.AddComponent<TextMeshProUGUI>(root.gameObject);

        if (TMP_Settings.defaultFontAsset != null)
        {
            label.font = TMP_Settings.defaultFontAsset;
        }

        label.text = texto;
        label.fontSize = tamano;
        label.alignment = alineacion;
        label.color = Color.white;
        label.raycastTarget = false;

        return label;
    }

    private static RectTransform AsegurarRect(Transform padre, string nombre)
    {
        Transform existente = padre.Find(nombre);
        if (existente != null)
        {
            Undo.RecordObject(existente, UndoLabel + ": " + nombre);
            return (RectTransform)existente;
        }

        var go = new GameObject(nombre, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, UndoLabel + ": " + nombre);

        var rect = (RectTransform)go.transform;
        rect.SetParent(padre, false);
        rect.localScale = Vector3.one;

        return rect;
    }

    private static Image AsegurarImagen(GameObject target, Color color, bool raycastTarget)
    {
        Image imagen = target.GetComponent<Image>();

        if (imagen == null)
        {
            imagen = Undo.AddComponent<Image>(target);
        }
        else
        {
            Undo.RecordObject(imagen, UndoLabel + ": fondo " + target.name);
        }

        imagen.color = color;
        imagen.raycastTarget = raycastTarget;
        EditorUtility.SetDirty(imagen);

        return imagen;
    }

    // -------------------------------------------------------------- cableado

    private static void Enlazar(Button boton, string nombreMetodo, UnityAction accion)
    {
        if (boton == null)
        {
            Debug.LogWarning($"[NutriAR] Boton no encontrado para '{nombreMetodo}': ruteo del click manual.");
            return;
        }

        if (TieneListener(boton.onClick, nombreMetodo))
        {
            return; // idempotente: no duplicar el listener al re-ejecutar la herramienta
        }

        Undo.RecordObject(boton, UndoLabel + ": " + boton.name + "." + nombreMetodo);
        UnityEventTools.AddVoidPersistentListener(boton.onClick, accion);
        EditorUtility.SetDirty(boton);
    }

    private static bool TieneListener(UnityEventBase evento, string nombreMetodo)
    {
        for (int i = 0; i < evento.GetPersistentEventCount(); i++)
        {
            if (evento.GetPersistentMethodName(i) == nombreMetodo)
            {
                return true;
            }
        }

        return false;
    }

    // ------------------------------------------------------------------ helpers

    private static T EnsureComponent<T>(GameObject target) where T : Component
    {
        T existing = target.GetComponent<T>();
        return existing != null ? existing : Undo.AddComponent<T>(target);
    }

    private static RectTransform Stretch(RectTransform rect, float left, float right, float top, float bottom)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);

        return rect;
    }

    private static Transform FindDescendiente(Transform raiz, string nombre)
    {
        for (int i = 0; i < raiz.childCount; i++)
        {
            Transform hijo = raiz.GetChild(i);

            if (hijo.name == nombre)
            {
                return hijo;
            }

            Transform encontrado = FindDescendiente(hijo, nombre);
            if (encontrado != null)
            {
                return encontrado;
            }
        }

        return null;
    }

    private static GameObject FindInScene(string nombre)
    {
        GameObject[] raices = SceneManager.GetActiveScene().GetRootGameObjects();

        for (int i = 0; i < raices.Length; i++)
        {
            if (raices[i].name == nombre)
            {
                return raices[i];
            }
        }

        return null;
    }
}
