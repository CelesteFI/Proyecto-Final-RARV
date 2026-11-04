using TMPro;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Genera la jerarquia base de NutriAR en Edit Time. Todo queda como objetos de escena
/// serializados (cero instanciacion en runtime) y cada alta se registra en Undo.
/// Idempotente: si un objeto ya existe se reutiliza en lugar de duplicarse.
/// </summary>
public static class NutriARSetupTool
{
    private const string MenuPath = "NutriAR/Generar Escena y UI";
    private const string UndoLabel = "NutriAR: Generar Escena y UI";

    private const float CameraYOffset = 1.1176f;

    // La primitiva Cylinder de Unity tiene radio 0.5 unidades (1 unidad = 1 m).
    private const float PrimitiveCylinderRadius = 0.5f;

    [MenuItem(MenuPath)]
    public static void Generar()
    {
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(UndoLabel);

        GameObject xrOrigin = EnsureXROrigin();
        EnsureARSession();
        GameObject managers = EnsureManagers();
        GameObject molde = EnsureMoldeHolograma(xrOrigin);

        Canvas canvas = EnsureCanvas();
        EnsureEventSystem();

        RectTransform panelSuperior = EnsurePanel(canvas.transform, "PanelSuperior", true);
        RectTransform panelInferior = EnsurePanel(canvas.transform, "PanelInferior", false);

        TMP_Dropdown dropdown = EnsureDropdown(panelSuperior);
        Toggle toggle = EnsureToggle(panelSuperior);
        Slider slider = EnsureSlider(panelInferior);
        Button button = EnsureButton(panelInferior);

        Cablear(managers, molde, dropdown, toggle, slider, button);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Undo.CollapseUndoOperations(undoGroup);

        Selection.activeGameObject = managers;
        Debug.Log("[NutriAR] Escena y UI generadas. Referencias auto-cableadas en AR_Managers. " +
                  "Ctrl+Z revierte todo el proceso en un solo paso.", managers);
    }

    // ---------------------------------------------------------------- AR base

    private static GameObject EnsureXROrigin()
    {
        XROrigin existing = Object.FindFirstObjectByType<XROrigin>(FindObjectsInactive.Include);
        if (existing != null)
        {
            return existing.gameObject;
        }

        var originGo = new GameObject("XR Origin (Mobile AR)");
        Undo.RegisterCreatedObjectUndo(originGo, UndoLabel + ": XR Origin");

        XROrigin origin = Undo.AddComponent<XROrigin>(originGo);

        var cameraOffsetGo = new GameObject("Camera Offset");
        Undo.RegisterCreatedObjectUndo(cameraOffsetGo, UndoLabel + ": Camera Offset");
        cameraOffsetGo.transform.SetParent(originGo.transform, false);

        var cameraGo = new GameObject("Main Camera",
            typeof(Camera),
            typeof(AudioListener),
            typeof(ARCameraManager),
            typeof(ARCameraBackground),
            typeof(TrackedPoseDriver));
        Undo.RegisterCreatedObjectUndo(cameraGo, UndoLabel + ": Main Camera");
        cameraGo.tag = "MainCamera";
        cameraGo.transform.SetParent(cameraOffsetGo.transform, false);

        origin.Camera = cameraGo.GetComponent<Camera>();
        origin.CameraFloorOffsetObject = cameraOffsetGo;
        origin.CameraYOffset = CameraYOffset;

        Debug.LogWarning("[NutriAR] No habia XR Origin: se creo el rig minimo (XROrigin + Camera Offset + Main Camera). " +
                         "Verifica los bindings del TrackedPoseDriver contra un rig AR funcionando.", originGo);

        return originGo;
    }

    private static GameObject EnsureARSession()
    {
        ARSession existing = Object.FindFirstObjectByType<ARSession>(FindObjectsInactive.Include);
        if (existing != null)
        {
            return existing.gameObject;
        }

        var sessionGo = new GameObject("AR Session");
        Undo.RegisterCreatedObjectUndo(sessionGo, UndoLabel + ": AR Session");
        Undo.AddComponent<ARSession>(sessionGo);
        Undo.AddComponent<ARInputManager>(sessionGo);

        return sessionGo;
    }

    private static GameObject EnsureManagers()
    {
        GameObject managers = FindInScene("AR_Managers");

        if (managers == null)
        {
            managers = new GameObject("AR_Managers");
            Undo.RegisterCreatedObjectUndo(managers, UndoLabel + ": AR_Managers");
        }

        EnsureComponent<ARRaycastManager>(managers);
        EnsureComponent<ARPlaneManager>(managers);   // sin planos detectados PlaneWithinPolygon nunca devuelve hit
        EnsureComponent<VolumeCalculator>(managers);
        EnsureComponent<PlateAnchorManager>(managers);
        EnsureComponent<ARUIManager>(managers);

        return managers;
    }

    private static GameObject EnsureMoldeHolograma(GameObject xrOrigin)
    {
        GameObject molde = FindInScene("MoldeHolograma");
        if (molde != null)
        {
            return molde;
        }

        molde = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        molde.name = "MoldeHolograma";
        Undo.RegisterCreatedObjectUndo(molde, UndoLabel + ": MoldeHolograma");

        Collider collider = molde.GetComponent<Collider>();
        if (collider != null)
        {
            Undo.DestroyObjectImmediate(collider);
        }

        molde.transform.SetParent(xrOrigin != null ? xrOrigin.transform : null, false);
        molde.transform.localPosition = new Vector3(0f, 1f, 1f);
        molde.transform.localRotation = Quaternion.identity;

        return molde;
    }

    // -------------------------------------------------------------------- UI

    private static Canvas EnsureCanvas()
    {
        Canvas existing = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (existing != null)
        {
            return existing;
        }

        var canvasGo = new GameObject("Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGo, UndoLabel + ": Canvas");

        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private static GameObject EnsureEventSystem()
    {
        EventSystem existing = Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
        GameObject eventSystemGo;

        if (existing != null)
        {
            eventSystemGo = existing.gameObject;
        }
        else
        {
            eventSystemGo = new GameObject("EventSystem", typeof(EventSystem));
            Undo.RegisterCreatedObjectUndo(eventSystemGo, UndoLabel + ": EventSystem");
        }

        // activeInputHandler = 1 (Input System puro): StandaloneInputModule lanzaria excepcion en runtime.
        if (eventSystemGo.GetComponent<InputSystemUIInputModule>() == null &&
            eventSystemGo.GetComponent<StandaloneInputModule>() == null)
        {
            Undo.AddComponent<InputSystemUIInputModule>(eventSystemGo);
        }

        return eventSystemGo;
    }

    private static RectTransform EnsurePanel(Transform canvas, string name, bool top)
    {
        Transform existing = canvas.Find(name);
        if (existing != null)
        {
            return (RectTransform)existing;
        }

        RectTransform panel = CreateUIElement(name, canvas);

        if (top)
        {
            panel.anchorMin = new Vector2(0f, 1f);
            panel.anchorMax = new Vector2(1f, 1f);
            panel.pivot = new Vector2(0.5f, 1f);
            panel.sizeDelta = new Vector2(0f, 140f);
        }
        else
        {
            panel.anchorMin = new Vector2(0f, 0f);
            panel.anchorMax = new Vector2(1f, 0f);
            panel.pivot = new Vector2(0.5f, 0f);
            panel.sizeDelta = new Vector2(0f, 180f);
        }

        panel.anchoredPosition = Vector2.zero;

        Image background = AddImage(panel.gameObject, new Color(0f, 0f, 0f, 0.35f), false);
        background.raycastTarget = false;

        return panel;
    }

    private static TMP_Dropdown EnsureDropdown(RectTransform panel)
    {
        Transform existing = panel.Find("dropdownAlimentos");
        if (existing != null)
        {
            return existing.GetComponent<TMP_Dropdown>();
        }

        RectTransform root = CreateUIElement("dropdownAlimentos", panel);
        root.anchorMin = new Vector2(0f, 1f);
        root.anchorMax = new Vector2(0f, 1f);
        root.pivot = new Vector2(0f, 1f);
        root.anchoredPosition = new Vector2(24f, -20f);
        root.sizeDelta = new Vector2(360f, 60f);

        Image background = AddImage(root.gameObject, new Color(1f, 1f, 1f, 0.9f));

        TMP_Dropdown dropdown = Undo.AddComponent<TMP_Dropdown>(root.gameObject);
        dropdown.targetGraphic = background;

        // Caption
        RectTransform labelRt = CreateUIElement("Label", root);
        Stretch(labelRt, 16f, 40f, 0f, 0f);
        TextMeshProUGUI caption = AddTMPText(labelRt.gameObject, "Arroz", 22f, TextAlignmentOptions.MidlineLeft);

        // Arrow
        RectTransform arrowRt = CreateUIElement("Arrow", root);
        arrowRt.anchorMin = new Vector2(1f, 0.5f);
        arrowRt.anchorMax = new Vector2(1f, 0.5f);
        arrowRt.pivot = new Vector2(0.5f, 0.5f);
        arrowRt.anchoredPosition = new Vector2(-20f, 0f);
        arrowRt.sizeDelta = new Vector2(16f, 16f);
        AddImage(arrowRt.gameObject, Color.white, false);

        // Template (TMP_Dropdown valida: template con Toggle hijo y el texto del item dentro del Toggle)
        RectTransform template = CreateUIElement("Template", root);
        template.anchorMin = new Vector2(0f, 0f);
        template.anchorMax = new Vector2(1f, 0f);
        template.pivot = new Vector2(0.5f, 1f);
        template.anchoredPosition = new Vector2(0f, 2f);
        template.sizeDelta = new Vector2(0f, 240f);
        AddImage(template.gameObject, new Color(1f, 1f, 1f, 0.95f), false);

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
        content.sizeDelta = new Vector2(0f, 44f);

        RectTransform item = CreateUIElement("Item", content);
        item.anchorMin = new Vector2(0f, 0.5f);
        item.anchorMax = new Vector2(1f, 0.5f);
        item.pivot = new Vector2(0.5f, 0.5f);
        item.anchoredPosition = Vector2.zero;
        item.sizeDelta = new Vector2(0f, 44f);

        Image itemBackground = AddImage(item.gameObject, new Color(1f, 1f, 1f, 0.05f));

        Toggle itemToggle = Undo.AddComponent<Toggle>(item.gameObject);
        itemToggle.targetGraphic = itemBackground;

        RectTransform checkRt = CreateUIElement("Item Checkmark", item);
        checkRt.anchorMin = new Vector2(0f, 0.5f);
        checkRt.anchorMax = new Vector2(0f, 0.5f);
        checkRt.pivot = new Vector2(0f, 0.5f);
        checkRt.anchoredPosition = new Vector2(12f, 0f);
        checkRt.sizeDelta = new Vector2(20f, 20f);
        Image checkmark = AddImage(checkRt.gameObject, Color.white, false);

        RectTransform itemLabelRt = CreateUIElement("Item Label", item);
        Stretch(itemLabelRt, 44f, 12f, 0f, 0f);
        TextMeshProUGUI itemLabel = AddTMPText(itemLabelRt.gameObject, "Opcion", 20f, TextAlignmentOptions.MidlineLeft);

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

        // Opciones desde la unica fuente de verdad (FoodDensityData). Edit Time, no runtime.
        Undo.RecordObject(dropdown, UndoLabel + ": opciones dropdownAlimentos");
        var options = new System.Collections.Generic.List<TMP_Dropdown.OptionData>(FoodDensityData.Count);
        for (int i = 0; i < FoodDensityData.Count; i++)
        {
            options.Add(new TMP_Dropdown.OptionData(FoodDensityData.Entries[i].Name));
        }

        dropdown.options = options;
        dropdown.value = 0;
        dropdown.RefreshShownValue();

        template.gameObject.SetActive(false);
        EditorUtility.SetDirty(dropdown);

        return dropdown;
    }

    private static Toggle EnsureToggle(RectTransform panel)
    {
        Transform existing = panel.Find("toggleCocido");
        if (existing != null)
        {
            return existing.GetComponent<Toggle>();
        }

        RectTransform root = CreateUIElement("toggleCocido", panel);
        root.anchorMin = new Vector2(1f, 1f);
        root.anchorMax = new Vector2(1f, 1f);
        root.pivot = new Vector2(1f, 1f);
        root.anchoredPosition = new Vector2(-24f, -20f);
        root.sizeDelta = new Vector2(240f, 60f);

        Image background = AddImage(root.gameObject, new Color(1f, 1f, 1f, 0.9f));

        Toggle toggle = Undo.AddComponent<Toggle>(root.gameObject);
        toggle.targetGraphic = background;

        RectTransform checkRt = CreateUIElement("Checkmark", root);
        checkRt.anchorMin = new Vector2(0f, 0.5f);
        checkRt.anchorMax = new Vector2(0f, 0.5f);
        checkRt.pivot = new Vector2(0f, 0.5f);
        checkRt.anchoredPosition = new Vector2(10f, 0f);
        checkRt.sizeDelta = new Vector2(28f, 28f);
        Image checkmark = AddImage(checkRt.gameObject, new Color(0.1f, 0.8f, 0.3f, 1f), false);

        RectTransform labelRt = CreateUIElement("Label", root);
        Stretch(labelRt, 48f, 8f, 0f, 0f);
        AddTMPText(labelRt.gameObject, "Cocido", 22f, TextAlignmentOptions.MidlineLeft);

        toggle.graphic = checkmark;
        toggle.isOn = false;

        return toggle;
    }

    private static Slider EnsureSlider(RectTransform panel)
    {
        Transform existing = panel.Find("sliderGramos");
        if (existing != null)
        {
            return existing.GetComponent<Slider>();
        }

        RectTransform root = CreateUIElement("sliderGramos", panel);
        root.anchorMin = new Vector2(0f, 0f);
        root.anchorMax = new Vector2(1f, 0f);
        root.pivot = new Vector2(0.5f, 0f);
        root.anchoredPosition = new Vector2(0f, 100f);
        root.sizeDelta = new Vector2(-48f, 60f);

        RectTransform backgroundRt = CreateUIElement("Background", root);
        Stretch(backgroundRt, 0f, 0f, 18f, 18f);
        AddImage(backgroundRt.gameObject, new Color(1f, 1f, 1f, 0.25f));

        RectTransform fillAreaRt = CreateUIElement("Fill Area", root);
        Stretch(fillAreaRt, 0f, 0f, 18f, 18f);

        RectTransform fillRt = CreateUIElement("Fill", fillAreaRt);
        fillRt.anchorMin = new Vector2(0f, 0f);
        fillRt.anchorMax = new Vector2(0f, 1f);
        fillRt.pivot = new Vector2(0.5f, 0.5f);
        fillRt.sizeDelta = new Vector2(10f, 0f);
        fillRt.anchoredPosition = Vector2.zero;
        AddImage(fillRt.gameObject, new Color(0.2f, 0.7f, 1f, 1f));

        RectTransform handleAreaRt = CreateUIElement("Handle Slide Area", root);
        Stretch(handleAreaRt, 0f, 0f, 14f, 14f);

        RectTransform handleRt = CreateUIElement("Handle", handleAreaRt);
        handleRt.anchorMin = new Vector2(0f, 0f);
        handleRt.anchorMax = new Vector2(0f, 1f);
        handleRt.pivot = new Vector2(0.5f, 0.5f);
        handleRt.sizeDelta = new Vector2(32f, 0f);
        handleRt.anchoredPosition = Vector2.zero;
        Image handle = AddImage(handleRt.gameObject, Color.white);

        Slider slider = Undo.AddComponent<Slider>(root.gameObject);
        slider.targetGraphic = handle;
        slider.fillRect = fillRt;
        slider.handleRect = handleRt;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 10f;
        slider.maxValue = 500f;
        slider.wholeNumbers = true;
        slider.value = 100f;

        return slider;
    }

    private static Button EnsureButton(RectTransform panel)
    {
        Transform existing = panel.Find("btnFijarPlato");
        if (existing != null)
        {
            return existing.GetComponent<Button>();
        }

        RectTransform root = CreateUIElement("btnFijarPlato", panel);
        root.anchorMin = new Vector2(0f, 0f);
        root.anchorMax = new Vector2(1f, 0f);
        root.pivot = new Vector2(0.5f, 0f);
        root.anchoredPosition = new Vector2(0f, 20f);
        root.sizeDelta = new Vector2(-48f, 64f);

        Image background = AddImage(root.gameObject, new Color(0.2f, 0.6f, 1f, 1f));

        Button button = Undo.AddComponent<Button>(root.gameObject);
        button.targetGraphic = background;

        RectTransform labelRt = CreateUIElement("Label", root);
        Stretch(labelRt, 8f, 8f, 0f, 0f);
        AddTMPText(labelRt.gameObject, "Fijar Plato", 24f, TextAlignmentOptions.Center);

        return button;
    }

    // ------------------------------------------------------------- auto-cableado

    private static void Cablear(GameObject managers, GameObject molde, TMP_Dropdown dropdown, Toggle toggle, Slider slider, Button button)
    {
        var volume = managers.GetComponent<VolumeCalculator>();
        var plate = managers.GetComponent<PlateAnchorManager>();
        var uiManager = managers.GetComponent<ARUIManager>();
        var raycastManager = managers.GetComponent<ARRaycastManager>();

        AssignReference(plate, "moldeHolograma", molde.transform);
        AssignReference(plate, "btnFijarPlato", button);
        AssignReference(plate, "raycastManager", raycastManager);

        AssignReference(uiManager, "sliderGramos", slider);
        AssignReference(uiManager, "dropdownAlimentos", dropdown);
        AssignReference(uiManager, "toggleCocido", toggle);
        AssignReference(uiManager, "calculator", volume);
        AssignReference(uiManager, "moldeHolograma", molde.transform);

        SetBaseRadiusFromMolde(volume, molde);
    }

    private static void AssignReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
    {
        var serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);

        if (property == null)
        {
            Debug.LogWarning($"[NutriAR] No existe el campo serializado '{propertyName}' en {target.GetType().Name}.", target);
            return;
        }

        Undo.RecordObject(target, UndoLabel + ": " + propertyName);
        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetBaseRadiusFromMolde(VolumeCalculator volume, GameObject molde)
    {
        float radiusCm = PrimitiveCylinderRadius * molde.transform.lossyScale.x * 100f;

        var serialized = new SerializedObject(volume);
        SerializedProperty property = serialized.FindProperty("baseRadiusCm");

        if (property == null)
        {
            Debug.LogWarning("[NutriAR] VolumeCalculator no expone 'baseRadiusCm'.", volume);
            return;
        }

        Undo.RecordObject(volume, UndoLabel + ": radio base");
        property.floatValue = Mathf.Max(radiusCm, 0.01f);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log($"[NutriAR] baseRadiusCm = {property.floatValue} cm (radio del MoldeHolograma a escala 1).", volume);
    }

    // ------------------------------------------------------------------ helpers

    private static T EnsureComponent<T>(GameObject target) where T : Component
    {
        T existing = target.GetComponent<T>();
        return existing != null ? existing : Undo.AddComponent<T>(target);
    }

    private static RectTransform CreateUIElement(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, UndoLabel + ": " + name);

        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
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

    private static TextMeshProUGUI AddTMPText(GameObject target, string text, float size, TextAlignmentOptions alignment)
    {
        TextMeshProUGUI label = Undo.AddComponent<TextMeshProUGUI>(target);

        if (TMP_Settings.defaultFontAsset != null)
        {
            label.font = TMP_Settings.defaultFontAsset;
        }

        label.text = text;
        label.fontSize = size;
        label.alignment = alignment;
        label.color = Color.white;
        label.raycastTarget = false;

        return label;
    }

    private static GameObject FindInScene(string name)
    {
        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == name)
            {
                return roots[i];
            }

            Transform found = FindRecursive(roots[i].transform, name);
            if (found != null)
            {
                return found.gameObject;
            }
        }

        return null;
    }

    private static Transform FindRecursive(Transform parent, string name)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (child.name == name)
            {
                return child;
            }

            Transform found = FindRecursive(child, name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
