using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Paso 2 del armado de NutriAR: anida la UI de AR dentro de Panel_AR y cablea los paneles
/// existentes del Canvas con MenuController. Todo pasa por Undo (un solo Ctrl+Z revierte).
/// No crea paneles: solo reorganiza y asigna referencias de escena.
/// </summary>
public static class NutriAR_MenuSetupTool
{
    private const string MenuPath = "NutriAR/2. Conectar Menús y Paneles";
    private const string UndoLabel = "NutriAR: Conectar Menus y Paneles";

    private const string ManagersName = "AR_Managers";

    private const string PanelInicioName = "Panel_PantallaInicio";
    private const string PanelMenuName = "Panel_MenuPrincipal";
    private const string PanelPerfilName = "Panel_Perfil";
    private const string PanelARName = "Panel_AR";
    private const string PanelSuperiorName = "PanelSuperior";
    private const string PanelInferiorName = "PanelInferior";

    [MenuItem(MenuPath)]
    public static void Conectar()
    {
        GameObject managers = FindInScene(ManagersName);
        if (managers == null)
        {
            Debug.LogError($"[NutriAR] No se encontro '{ManagersName}' en la escena. " +
                           "Ejecuta primero 'NutriAR/Generar Escena y UI'.");
            return;
        }

        Canvas canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas == null)
        {
            Debug.LogError("[NutriAR] No hay ningun Canvas en la escena. " +
                           "Ejecuta primero 'NutriAR/Generar Escena y UI'.");
            return;
        }

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(UndoLabel);

        GameObject inicio = BuscarPanel(canvas.transform, PanelInicioName);
        GameObject menuLateral = BuscarPanel(canvas.transform, PanelMenuName);
        GameObject perfil = BuscarPanel(canvas.transform, PanelPerfilName);
        GameObject panelAR = BuscarPanel(canvas.transform, PanelARName);

        MenuController controller = EnsureComponent<MenuController>(managers);

        AnidarUIAR(canvas.transform, panelAR);
        Cablear(controller, inicio, menuLateral, perfil, panelAR);
        AplicarEstadoInicial(inicio, new[] { inicio, menuLateral, perfil, panelAR });

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Undo.CollapseUndoOperations(undoGroup);

        Selection.activeGameObject = managers;
        Debug.Log("[NutriAR] MenuController cableado: Panel_PantallaInicio es el estado por defecto, " +
                  "PanelSuperior y PanelInferior quedaron dentro de Panel_AR. " +
                  "Ctrl+Z revierte todo el proceso en un solo paso.", managers);
    }

    // ------------------------------------------------------------ reorganizacion

    /// <summary>Mueve la UI de AR existente para que dependa de la visibilidad de Panel_AR.</summary>
    private static void AnidarUIAR(Transform canvas, GameObject panelAR)
    {
        if (panelAR == null)
        {
            Debug.LogWarning("[NutriAR] No hay 'Panel_AR': PanelSuperior y PanelInferior se quedan sueltos en el Canvas.");
            return;
        }

        MoverBajo(canvas, panelAR.transform, PanelSuperiorName);
        MoverBajo(canvas, panelAR.transform, PanelInferiorName);
    }

    private static void MoverBajo(Transform canvas, Transform panelPadre, string nombreHijo)
    {
        Transform hijo = FindDescendiente(canvas, nombreHijo);

        if (hijo == null)
        {
            Debug.LogWarning($"[NutriAR] No se encontro '{nombreHijo}' dentro del Canvas.");
            return;
        }

        if (hijo.parent == panelPadre)
        {
            return;
        }

        Undo.SetTransformParent(hijo, panelPadre, UndoLabel + ": " + nombreHijo);

        // Ambos paneles estan anclados en horizontal (anchorMin.x = 0, anchorMax.x = 1) sobre un
        // padre del mismo tamano: devolver el desplazamiento a cero conserva el layout exacto.
        var rect = hijo as RectTransform;
        if (rect != null)
        {
            Undo.RecordObject(rect, UndoLabel + ": anclas " + nombreHijo);
            rect.anchoredPosition = Vector2.zero;
        }
    }

    // -------------------------------------------------------------- auto-cableado

    private static void Cablear(MenuController controller, GameObject inicio, GameObject menuLateral,
                                GameObject perfil, GameObject panelAR)
    {
        var serialized = new SerializedObject(controller);
        Undo.RecordObject(controller, UndoLabel + ": MenuController");

        // Panel_MenuPrincipal es el menu lateral del MenuController.
        Asignar(serialized, "panelInicio", inicio);
        Asignar(serialized, "panelMenuLateral", menuLateral);
        Asignar(serialized, "panelPerfil", perfil);
        Asignar(serialized, "panelAR", panelAR);

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);
    }

    private static void Asignar(SerializedObject serialized, string nombreCampo, GameObject panel)
    {
        SerializedProperty property = serialized.FindProperty(nombreCampo);

        if (property == null)
        {
            Debug.LogWarning($"[NutriAR] {nameof(MenuController)} no expone el campo serializado '{nombreCampo}'.");
            return;
        }

        if (panel == null)
        {
            Debug.LogWarning($"[NutriAR] Panel no encontrado para '{nombreCampo}': la referencia queda sin asignar.");
            return;
        }

        property.objectReferenceValue = panel;
    }

    /// <summary>Estado de arranque: solo Panel_PantallaInicio encendido.</summary>
    private static void AplicarEstadoInicial(GameObject panelInicio, GameObject[] paneles)
    {
        for (int i = 0; i < paneles.Length; i++)
        {
            GameObject panel = paneles[i];

            if (panel == null)
            {
                continue;
            }

            bool debeEstarActivo = panel == panelInicio;

            if (panel.activeSelf == debeEstarActivo)
            {
                continue;
            }

            Undo.RecordObject(panel, UndoLabel + ": " + panel.name);
            panel.SetActive(debeEstarActivo);
            EditorUtility.SetDirty(panel);
        }
    }

    // ------------------------------------------------------------------ helpers

    private static T EnsureComponent<T>(GameObject target) where T : Component
    {
        T existing = target.GetComponent<T>();
        return existing != null ? existing : Undo.AddComponent<T>(target);
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
