using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Paso 3 del armado de NutriAR: rellena Panel_PantallaInicio y Panel_Perfil con TMP en Edit Time
/// y auto-cablea BiometricProfile (SerializedObject) y MenuController (persistent listeners).
/// Idempotente: los hijos se reutilizan por nombre. Todo queda registrado en Undo.
/// </summary>
public static class NutriAR_PopulatePanelsTool
{
    private const string MenuPath = "NutriAR/3. Construir Interfaz Interna";
    private const string UndoLabel = "NutriAR: Construir Interfaz Interna";

    private const string ManagersName = "AR_Managers";
    private const string PanelInicioName = "Panel_PantallaInicio";
    private const string PanelPerfilName = "Panel_Perfil";

    private const float AltoTitulo = 140f;
    private const float AltoBoton = 96f;
    private const float AltoCampo = 88f;
    private const float AltoResultados = 260f;

    private static readonly Color ColorBoton = new Color(0.20f, 0.55f, 0.95f, 1f);
    private static readonly Color ColorBotonSecundario = new Color(0.85f, 0.85f, 0.85f, 1f);
    private static readonly Color ColorCampo = new Color(1f, 1f, 1f, 0.92f);
    private static readonly Color ColorFondoPerfil = new Color(0.1f, 0.1f, 0.1f, 1f);
    private static readonly Color ColorTextoOscuro = new Color(0.05f, 0.05f, 0.05f, 1f);
    private static readonly Color ColorPlaceholder = new Color(0f, 0f, 0f, 0.35f);

    private static readonly string[] EtiquetasActividad =
    {
        "Sedentario", "Ligero", "Moderado", "Intenso", "Muy intenso"
    };

    [MenuItem(MenuPath)]
    public static void Construir()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas == null)
        {
            Debug.LogError("[NutriAR] No hay ningun Canvas en la escena. Ejecuta primero 'NutriAR/Generar Escena y UI'.");
            return;
        }

        GameObject panelInicio = BuscarPanel(canvas.transform, PanelInicioName);
        GameObject panelPerfil = BuscarPanel(canvas.transform, PanelPerfilName);
        GameObject managers = FindInScene(ManagersName);

        if (panelInicio == null || panelPerfil == null || managers == null)
        {
            Debug.LogError("[NutriAR] Faltan piezas base (Panel_PantallaInicio, Panel_Perfil o AR_Managers). " +
                           "Ejecuta los pasos 1 y 2 antes que este.");
            return;
        }

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(UndoLabel);

        // ------------------------------------------------ Panel_PantallaInicio
        AsegurarLayoutVertical(panelInicio, 160, 160);

        TextMeshProUGUI titulo = AsegurarTitulo(panelInicio.transform, "TituloNutriAR", "NutriAR");
        Button btnIrMedicion = AsegurarBoton(panelInicio.transform, "Btn_IrMedicion", "Ir a Medición", ColorBoton);
        Button btnIrPerfil = AsegurarBoton(panelInicio.transform, "Btn_IrPerfil", "Mi Perfil", ColorBoton);

        // --------------------------------------------------------- Panel_Perfil
        // Fondo opaco: sin el, los TMP se mezclan con el feed de la camara AR.
        AsegurarFondo(panelPerfil, ColorFondoPerfil);
        AsegurarLayoutVertical(panelPerfil, 120, 120);

        Button btnVolver = AsegurarBoton(panelPerfil.transform, "Btn_Volver", "Volver", ColorBotonSecundario);
        TMP_InputField inputEdad = AsegurarInput(panelPerfil.transform, "InputEdad", "Edad (años)");
        TMP_InputField inputPeso = AsegurarInput(panelPerfil.transform, "InputPeso", "Peso (kg)");
        TMP_InputField inputAltura = AsegurarInput(panelPerfil.transform, "InputAltura", "Altura (cm)");
        TMP_Dropdown dropdownSexo = AsegurarDropdown(panelPerfil.transform, "DropdownSexo", OpcionesSexo());
        TMP_Dropdown dropdownActividad = AsegurarDropdown(panelPerfil.transform, "DropdownActividad", OpcionesActividad());
        Button btnCalcular = AsegurarBoton(panelPerfil.transform, "Btn_Calcular", "Calcular", ColorBoton);
        TextMeshProUGUI textoResultados = AsegurarTextoResultados(panelPerfil.transform, "TextoResultados");

        // ------------------------------------------------------- auto-cableado
        BiometricProfile perfil = EnsureComponent<BiometricProfile>(managers);
        CablearPerfil(perfil, inputEdad, inputPeso, inputAltura, dropdownSexo, dropdownActividad,
                      btnCalcular, textoResultados);

        MenuController menu = managers.GetComponent<MenuController>();
        if (menu == null)
        {
            Debug.LogWarning("[NutriAR] AR_Managers no tiene MenuController: los clicks de navegacion quedan sin ruteo. " +
                             "Ejecuta 'NutriAR/2. Conectar Menús y Paneles'.");
        }
        else
        {
            CablearMenu(menu, btnIrMedicion, btnIrPerfil, btnVolver);
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Undo.CollapseUndoOperations(undoGroup);

        Selection.activeGameObject = panelInicio;
        Debug.Log("[NutriAR] Panel_PantallaInicio y Panel_Perfil construidos y cableados. " +
                  "Ctrl+Z revierte todo el proceso en un solo paso.", panelInicio);
    }

    // ------------------------------------------------------------- estructura

    /// <summary>Garantiza un Image de fondo en el panel y fuerza el color opaco pedido.</summary>
    private static Image AsegurarFondo(GameObject panel, Color color)
    {
        Image fondo = panel.GetComponent<Image>();

        if (fondo == null)
        {
            fondo = Undo.AddComponent<Image>(panel);
        }
        else if (fondo.color != color)
        {
            Undo.RecordObject(fondo, UndoLabel + ": fondo " + panel.name);
        }

        fondo.color = color;
        fondo.raycastTarget = true;
        EditorUtility.SetDirty(fondo);

        return fondo;
    }

    private static VerticalLayoutGroup AsegurarLayoutVertical(GameObject panel, int paddingVertical, int paddingHorizontal)
    {
        VerticalLayoutGroup grupo = EnsureComponent<VerticalLayoutGroup>(panel);

        Undo.RecordObject(grupo, UndoLabel + ": layout " + panel.name);
        grupo.padding = new RectOffset(paddingHorizontal, paddingHorizontal, paddingVertical, paddingVertical);
        grupo.spacing = 24f;
        grupo.childAlignment = TextAnchor.UpperCenter;
        grupo.childControlWidth = true;
        grupo.childControlHeight = true;
        grupo.childForceExpandWidth = true;
        grupo.childForceExpandHeight = false;

        EditorUtility.SetDirty(grupo);
        return grupo;
    }

    private static TextMeshProUGUI AsegurarTitulo(Transform padre, string nombre, string texto)
    {
        Transform existente = padre.Find(nombre);
        if (existente != null)
        {
            return existente.GetComponent<TextMeshProUGUI>();
        }

        RectTransform root = CreateUIElement(nombre, padre);
        TextMeshProUGUI titulo = AddTMPText(root.gameObject, texto, 64f, TextAlignmentOptions.Center);

        AñadirLayoutElement(root.gameObject, AltoTitulo, 0f);
        return titulo;
    }

    private static TextMeshProUGUI AsegurarTextoResultados(Transform padre, string nombre)
    {
        Transform existente = padre.Find(nombre);
        if (existente != null)
        {
            return existente.GetComponent<TextMeshProUGUI>();
        }

        RectTransform root = CreateUIElement(nombre, padre);
        TextMeshProUGUI texto = AddTMPText(root.gameObject, "Resultados del calculo", 26f, TextAlignmentOptions.TopLeft);

        // El texto es el unico elemento que puede crecer: absorbe la altura sobrante del panel.
        LayoutElement elemento = AñadirLayoutElement(root.gameObject, AltoResultados, 1f);
        elemento.minHeight = 0f;

        return texto;
    }

    private static Button AsegurarBoton(Transform padre, string nombre, string etiqueta, Color color)
    {
        Transform existente = padre.Find(nombre);
        if (existente != null)
        {
            return existente.GetComponent<Button>();
        }

        RectTransform root = CreateUIElement(nombre, padre);
        Image fondo = AddImage(root.gameObject, color);

        Button boton = Undo.AddComponent<Button>(root.gameObject);
        boton.targetGraphic = fondo;
        boton.transition = Selectable.Transition.ColorTint;

        RectTransform labelRt = CreateUIElement("Label", root);
        Stretch(labelRt, 8f, 8f, 0f, 0f);
        TextMeshProUGUI label = AddTMPText(labelRt.gameObject, etiqueta, 28f, TextAlignmentOptions.Center);
        label.color = Color.white;

        AñadirLayoutElement(root.gameObject, AltoBoton, 0f);
        return boton;
    }

    private static TMP_InputField AsegurarInput(Transform padre, string nombre, string textoPlaceholder)
    {
        Transform existente = padre.Find(nombre);
        if (existente != null)
        {
            return existente.GetComponent<TMP_InputField>();
        }

        RectTransform root = CreateUIElement(nombre, padre);
        Image fondo = AddImage(root.gameObject, ColorCampo);

        TMP_InputField input = Undo.AddComponent<TMP_InputField>(root.gameObject);
        input.targetGraphic = fondo;

        // TMP_InputField exige un viewport con RectMask2D y un TMP_Text hijo para dibujar el caret.
        RectTransform area = CreateUIElement("Text Area", root);
        Stretch(area, 16f, 16f, 8f, 8f);
        Undo.AddComponent<RectMask2D>(area.gameObject);

        RectTransform placeholderRt = CreateUIElement("Placeholder", area);
        Stretch(placeholderRt, 0f, 0f, 0f, 0f);
        TextMeshProUGUI placeholder = AddTMPText(placeholderRt.gameObject, textoPlaceholder, 26f, TextAlignmentOptions.MidlineLeft);
        placeholder.color = ColorPlaceholder;

        RectTransform textoRt = CreateUIElement("Text", area);
        Stretch(textoRt, 0f, 0f, 0f, 0f);
        TextMeshProUGUI texto = AddTMPText(textoRt.gameObject, string.Empty, 26f, TextAlignmentOptions.MidlineLeft);
        texto.color = ColorTextoOscuro;

        input.textViewport = area;
        input.textComponent = texto;
        input.placeholder = placeholder;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.contentType = TMP_InputField.ContentType.DecimalNumber;
        input.characterValidation = TMP_InputField.CharacterValidation.Decimal;
        input.text = string.Empty;

        AñadirLayoutElement(root.gameObject, AltoCampo, 0f);
        EditorUtility.SetDirty(input);

        return input;
    }

    private static TMP_Dropdown AsegurarDropdown(Transform padre, string nombre, List<string> opciones)
    {
        Transform existente = padre.Find(nombre);
        if (existente != null)
        {
            return existente.GetComponent<TMP_Dropdown>();
        }

        RectTransform root = CreateUIElement(nombre, padre);
        Image fondo = AddImage(root.gameObject, ColorCampo);

        TMP_Dropdown dropdown = Undo.AddComponent<TMP_Dropdown>(root.gameObject);
        dropdown.targetGraphic = fondo;

        RectTransform labelRt = CreateUIElement("Label", root);
        Stretch(labelRt, 16f, 40f, 0f, 0f);
        TextMeshProUGUI caption = AddTMPText(labelRt.gameObject, opciones.Count > 0 ? opciones[0] : string.Empty,
                                             26f, TextAlignmentOptions.MidlineLeft);
        caption.color = ColorTextoOscuro;

        RectTransform arrowRt = CreateUIElement("Arrow", root);
        arrowRt.anchorMin = new Vector2(1f, 0.5f);
        arrowRt.anchorMax = new Vector2(1f, 0.5f);
        arrowRt.pivot = new Vector2(0.5f, 0.5f);
        arrowRt.anchoredPosition = new Vector2(-20f, 0f);
        arrowRt.sizeDelta = new Vector2(18f, 18f);
        AddImage(arrowRt.gameObject, ColorTextoOscuro, false);

        // TMP_Dropdown.SetupTemplate valida: Template con un Toggle hijo y el texto del item dentro del Toggle.
        RectTransform template = CreateUIElement("Template", root);
        template.anchorMin = new Vector2(0f, 0f);
        template.anchorMax = new Vector2(1f, 0f);
        template.pivot = new Vector2(0.5f, 1f);
        template.anchoredPosition = new Vector2(0f, 2f);
        template.sizeDelta = new Vector2(0f, 320f);
        AddImage(template.gameObject, new Color(1f, 1f, 1f, 0.98f), false);

        RectTransform viewport = CreateUIElement("Viewport", template);
        Stretch(viewport, 0f, 0f, 0f, 0f);
        AddImage(viewport.gameObject, new Color(1f, 1f, 1f, 0.02f), false);
        Mask mask = Undo.AddComponent<Mask>(viewport.gameObject);
        mask.showMaskGraphic = false;

        RectTransform content = CreateUIElement("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, 56f);

        RectTransform item = CreateUIElement("Item", content);
        item.anchorMin = new Vector2(0f, 0.5f);
        item.anchorMax = new Vector2(1f, 0.5f);
        item.pivot = new Vector2(0.5f, 0.5f);
        item.anchoredPosition = Vector2.zero;
        item.sizeDelta = new Vector2(0f, 56f);

        Image itemFondo = AddImage(item.gameObject, new Color(0f, 0f, 0f, 0.06f));
        Toggle itemToggle = Undo.AddComponent<Toggle>(item.gameObject);
        itemToggle.targetGraphic = itemFondo;

        RectTransform checkRt = CreateUIElement("Item Checkmark", item);
        checkRt.anchorMin = new Vector2(0f, 0.5f);
        checkRt.anchorMax = new Vector2(0f, 0.5f);
        checkRt.pivot = new Vector2(0f, 0.5f);
        checkRt.anchoredPosition = new Vector2(12f, 0f);
        checkRt.sizeDelta = new Vector2(20f, 20f);
        Image checkmark = AddImage(checkRt.gameObject, ColorBoton, false);

        RectTransform itemLabelRt = CreateUIElement("Item Label", item);
        Stretch(itemLabelRt, 44f, 12f, 0f, 0f);
        TextMeshProUGUI itemLabel = AddTMPText(itemLabelRt.gameObject, "Opción", 24f, TextAlignmentOptions.MidlineLeft);
        itemLabel.color = ColorTextoOscuro;

        itemToggle.graphic = checkmark;

        ScrollRect scroll = Undo.AddComponent<ScrollRect>(template.gameObject);
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        dropdown.template = template;
        dropdown.captionText = caption;
        dropdown.itemText = itemLabel;

        Undo.RecordObject(dropdown, UndoLabel + ": opciones " + nombre);
        var listaOpciones = new List<TMP_Dropdown.OptionData>(opciones.Count);
        for (int i = 0; i < opciones.Count; i++)
        {
            listaOpciones.Add(new TMP_Dropdown.OptionData(opciones[i]));
        }

        dropdown.options = listaOpciones;
        dropdown.value = 0;
        dropdown.RefreshShownValue();

        template.gameObject.SetActive(false);

        AñadirLayoutElement(root.gameObject, AltoCampo, 0f);
        EditorUtility.SetDirty(dropdown);

        return dropdown;
    }

    private static List<string> OpcionesSexo()
    {
        // El indice debe coincidir con BiometricProfile.Sexo (0 = Masculino, 1 = Femenino).
        return new List<string> { "Masculino", "Femenino" };
    }

    private static List<string> OpcionesActividad()
    {
        int total = BiometricProfile.FactoresActividad.Count;
        var opciones = new List<string>(total);

        for (int i = 0; i < total; i++)
        {
            string etiqueta = i < EtiquetasActividad.Length ? EtiquetasActividad[i] : $"Actividad {i + 1}";
            string factor = BiometricProfile.FactoresActividad[i].ToString("F3", CultureInfo.InvariantCulture);

            opciones.Add($"{etiqueta} (x{factor})");
        }

        return opciones;
    }

    // -------------------------------------------------------------- cableado

    private static void CablearPerfil(BiometricProfile perfil, TMP_InputField edad, TMP_InputField peso,
                                      TMP_InputField altura, TMP_Dropdown sexo, TMP_Dropdown actividad,
                                      Button btnCalcular, TMP_Text resultados)
    {
        var serialized = new SerializedObject(perfil);
        Undo.RecordObject(perfil, UndoLabel + ": BiometricProfile");

        Asignar(serialized, "InputEdad", edad);
        Asignar(serialized, "InputPeso", peso);
        Asignar(serialized, "InputAltura", altura);
        Asignar(serialized, "DropdownSexo", sexo);
        Asignar(serialized, "DropdownActividad", actividad);
        Asignar(serialized, "BtnCalcular", btnCalcular);
        Asignar(serialized, "TextoResultados", resultados);

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(perfil);
    }

    private static void Asignar(SerializedObject serialized, string nombreCampo, Object valor)
    {
        SerializedProperty property = serialized.FindProperty(nombreCampo);

        if (property == null)
        {
            Debug.LogWarning($"[NutriAR] {nameof(BiometricProfile)} no expone el campo serializado '{nombreCampo}'.");
            return;
        }

        if (valor == null)
        {
            Debug.LogWarning($"[NutriAR] Elemento no encontrado para '{nombreCampo}': la referencia queda sin asignar.");
            return;
        }

        property.objectReferenceValue = valor;
    }

    private static void CablearMenu(MenuController menu, Button btnIrMedicion, Button btnIrPerfil, Button btnVolver)
    {
        Enlazar(btnIrMedicion, "MostrarAR", menu.MostrarAR);
        Enlazar(btnIrPerfil, "MostrarPerfil", menu.MostrarPerfil);
        Enlazar(btnVolver, "MostrarInicio", menu.MostrarInicio);
    }

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

    private static LayoutElement AñadirLayoutElement(GameObject target, float alto, float flexible)
    {
        LayoutElement elemento = EnsureComponent<LayoutElement>(target);

        Undo.RecordObject(elemento, UndoLabel + ": layout element " + target.name);
        elemento.minHeight = alto;
        elemento.preferredHeight = alto;
        elemento.flexibleHeight = flexible;

        return elemento;
    }

    private static RectTransform CreateUIElement(string nombre, Transform padre)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, UndoLabel + ": " + nombre);

        var rect = (RectTransform)go.transform;
        rect.SetParent(padre, false);
        rect.localScale = Vector3.one;

        return rect;
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

    private static Image AddImage(GameObject target, Color color, bool raycastTarget = true)
    {
        Image image = Undo.AddComponent<Image>(target);
        image.color = color;
        image.raycastTarget = raycastTarget;

        return image;
    }

    private static TextMeshProUGUI AddTMPText(GameObject target, string texto, float tamano, TextAlignmentOptions alineacion)
    {
        TextMeshProUGUI label = Undo.AddComponent<TextMeshProUGUI>(target);

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

    private static GameObject BuscarPanel(Transform canvas, string nombre)
    {
        Transform panel = FindDescendiente(canvas, nombre);

        if (panel == null)
        {
            Debug.LogWarning($"[NutriAR] No se encontro el panel '{nombre}' dentro del Canvas.");
            return null;
        }

        return panel.gameObject;
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
