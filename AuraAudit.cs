using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Auditoria del pase final: comprueba el cableado de los scripts, la estructura de la UI nueva
/// y deja la escena en su estado de arranque (solo splash visible).
/// </summary>
public static class AuraAudit
{
    public static void Execute()
    {
        StringBuilder sb = new StringBuilder(4096);
        GameObject canvas = GameObject.Find("Canvas");
        GameObject xrOrigin = GameObject.Find("XR Origin");

        sb.AppendLine("== Canvas ==");
        for (int i = 0; i < canvas.transform.childCount; i++)
        {
            GameObject hijo = canvas.transform.GetChild(i).gameObject;
            sb.AppendLine("  [" + i + "] " + hijo.name + " activo=" + hijo.activeSelf);
        }

        sb.AppendLine("== MenuController ==");
        AuditarComponente<MenuController>(sb, xrOrigin, new[]
        {
            "panelInicio", "panelMenuLateral", "panelPerfil", "panelAR", "panelCalibracion",
            "panelGuia", "panelDashboard", "panelTutorial", "indicadorTutorial", "arUI", "arCameraController"
        });

        MenuController menu = xrOrigin.GetComponent<MenuController>();
        SerializedObject soMenu = new SerializedObject(menu);
        SerializedProperty pasos = soMenu.FindProperty("pasosTutorial");
        sb.AppendLine("  pasosTutorial: " + pasos.arraySize);
        for (int i = 0; i < pasos.arraySize; i++)
        {
            Object paso = pasos.GetArrayElementAtIndex(i).objectReferenceValue;
            sb.AppendLine("    [" + i + "] " + (paso != null ? paso.name : "NULL"));
        }

        sb.AppendLine("== BiometricProfile ==");
        AuditarComponente<BiometricProfile>(sb, xrOrigin, new[]
        {
            "dashAguaNumero", "dashAguaSufijo", "dashKcalNumero", "dashKcalSufijo",
            "dashProteinaNumero", "dashProteinaSufijo", "dashCarboNumero", "dashCarboSufijo",
            "dashGrasaNumero", "dashGrasaSufijo", "dashEstado"
        });

        sb.AppendLine("== ARUIManager ==");
        AuditarComponente<ARUIManager>(sb, xrOrigin, new[]
        {
            "menu", "panelDetalle", "detalleIcono", "detalleTitulo", "detalleEstado",
            "detalleKcalNumero", "detalleKcalSufijo", "detalleProteinaTitulo", "detalleProteinaNumero",
            "detalleProteinaSufijo", "detalleCarboTitulo", "detalleCarboNumero", "detalleCarboSufijo",
            "detalleGrasaTitulo", "detalleGrasaNumero", "detalleGrasaSufijo"
        });

        sb.AppendLine("== PlateAnchorManager ==");
        AuditarComponente<PlateAnchorManager>(sb, xrOrigin, new[]
        {
            "moldeHolograma", "arosRaiz", "spriteGuiaCircular", "spriteFijadoCircular",
            "spriteGuiaCuadrado", "spriteFijadoCuadrado", "raycastManager", "camaraAR", "btnFijarPlato", "menu"
        });

        SerializedObject soAncla = new SerializedObject(xrOrigin.GetComponent<PlateAnchorManager>());
        sb.AppendLine("  radioPintadoAro=" + soAncla.FindProperty("radioPintadoAro").floatValue +
                      " cosenoMaxInclinacion=" + soAncla.FindProperty("cosenoMaxInclinacion").floatValue);

        sb.AppendLine("== PlateShapeController ==");
        AuditarComponente<PlateShapeController>(sb, xrOrigin, new[] { "plateAnchor", "menu", "circuloGuia" });

        GameObject panelGuia = canvas.transform.Find("Panel_GuiaNutricional").gameObject;
        GameObject grid = panelGuia.transform.Find("GridTarjetas").gameObject;
        sb.AppendLine("== Catalogo ==");
        sb.AppendLine("  tarjetas=" + grid.transform.childCount);
        sb.AppendLine("  GridCell=" + grid.GetComponent<GridLayoutGroup>().cellSize);
        for (int i = 0; i < Mathf.Min(2, grid.transform.childCount); i++)
        {
            Transform t = grid.transform.GetChild(i);
            sb.AppendLine("    " + t.name + " size=" + ((RectTransform)t).sizeDelta +
                          " sizeDeltaOK=" + (((RectTransform)t).sizeDelta.x > 1f));
            Button b = t.GetComponent<Button>();
            sb.AppendLine("      listeners=" + (b != null ? b.onClick.GetPersistentEventCount() : -1) +
                          " target=" + (b != null && b.onClick.GetPersistentEventCount() > 0
                              ? b.onClick.GetPersistentMethodName(0) + "('" + b.onClick.GetPersistentTarget(0).name + "')"
                              : "-"));
        }

        GameObject detalle = panelGuia.transform.Find("PanelDetalle").gameObject;
        sb.AppendLine("  PanelDetalle activo=" + detalle.activeSelf);

        sb.AppendLine("== Tutorial ==");
        GameObject tutorial = canvas.transform.Find("Panel_Tutorial").gameObject;
        sb.AppendLine("  Panel_Tutorial activo=" + tutorial.activeSelf);
        Transform tarjeta = tutorial.transform.Find("TarjetaTutorial");
        for (int i = 0; i < tarjeta.childCount; i++)
        {
            Transform hijo = tarjeta.GetChild(i);
            sb.AppendLine("    " + hijo.name + " activo=" + hijo.gameObject.activeSelf);
        }

        GameObject menuPrincipal = canvas.transform.Find("Panel_MenuPrincipal").gameObject;
        sb.AppendLine("== Botones menu principal ==");
        foreach (Transform t in menuPrincipal.transform)
        {
            Button b = t.GetComponent<Button>();
            sb.AppendLine("    " + t.name + (b != null && b.onClick.GetPersistentEventCount() > 0
                ? " -> " + b.onClick.GetPersistentMethodName(0) : string.Empty));
        }

        GameObject lateral = canvas.transform.Find("Panel_AR/MenuLateralAR").gameObject;
        sb.AppendLine("== MenuLateralAR ==");
        foreach (Transform t in lateral.transform)
        {
            Button b = t.GetComponent<Button>();
            sb.AppendLine("    " + t.name + " y=" + ((RectTransform)t).anchoredPosition.y +
                          (b != null && b.onClick.GetPersistentEventCount() > 0
                              ? " -> " + b.onClick.GetPersistentMethodName(0) : string.Empty));
        }

        // Estado de arranque: solo la splash visible y el panel de carga transparente.
        for (int i = 0; i < canvas.transform.childCount; i++)
        {
            GameObject hijo = canvas.transform.GetChild(i).gameObject;
            bool debeEstarActivo = hijo.name == "Panel_Splash" || hijo.name == "Panel_Carga";

            if (hijo.activeSelf != debeEstarActivo)
            {
                hijo.SetActive(debeEstarActivo);
            }
        }

        CanvasGroup grupoCarga = canvas.transform.Find("Panel_Carga").GetComponent<CanvasGroup>();
        grupoCarga.alpha = 0f;
        grupoCarga.interactable = false;
        grupoCarga.blocksRaycasts = false;

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("[AuraAudit]\n" + sb.ToString());
    }

    private static void AuditarComponente<T>(StringBuilder sb, GameObject host, string[] campos) where T : Component
    {
        T componente = host.GetComponent<T>();
        if (componente == null)
        {
            sb.AppendLine("  FALTA el componente " + typeof(T).Name);
            return;
        }

        SerializedObject so = new SerializedObject(componente);
        List<string> faltan = new List<string>();

        for (int i = 0; i < campos.Length; i++)
        {
            SerializedProperty prop = so.FindProperty(campos[i]);

            if (prop == null)
            {
                faltan.Add(campos[i] + "(campo?)");
                continue;
            }

            if (prop.objectReferenceValue == null)
            {
                faltan.Add(campos[i]);
            }
        }

        sb.AppendLine("  " + typeof(T).Name + (faltan.Count == 0
            ? ": ok (" + campos.Length + " referencias)"
            : ": FALTAN " + string.Join(", ", faltan)));
    }
}
