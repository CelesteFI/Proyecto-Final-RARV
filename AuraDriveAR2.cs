using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public static class AuraDriveAR2
{
    public static void Execute()
    {
        StringBuilder sb = new StringBuilder();

        PlateAnchorManager anchor = Object.FindFirstObjectByType<PlateAnchorManager>();
        MenuController menu = Object.FindFirstObjectByType<MenuController>();
        PlateCalibrationController cal = Object.FindFirstObjectByType<PlateCalibrationController>(FindObjectsInactive.Include);

        // Estado limpio como en el arranque de la escena.
        anchor.ReiniciarColocacion();

        // Pose simulada del raycast (en el editor no hay planos).
        typeof(PlateAnchorManager).GetField("_hayPose", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(anchor, true);
        typeof(PlateAnchorManager).GetField("_ultimaPose", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(anchor, new Pose(new Vector3(0f, 0f, 1.2f), Quaternion.identity));

        // Flujo real: abrir calibracion (dispara OnEnable) y pulsar el boton.
        menu.MostrarCalibracion();

        Button boton = Object.FindFirstObjectByType<PlateCalibrationController>(FindObjectsInactive.Include)
            .GetComponentInChildren<Button>(true);

        sb.AppendLine("[BTN] buscando el boton de fijar por nombre...");
        Button[] todos = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Button objetivo = null;
        for (int i = 0; i < todos.Length; i++)
        {
            if (todos[i].name == "Btn_FijarPlatoCalibracion") { objetivo = todos[i]; break; }
        }

        if (objetivo == null) { sb.AppendLine("[BTN] NO encontrado Btn_FijarPlatoCalibracion"); Debug.Log(sb.ToString()); return; }

        sb.AppendLine("[BTN] listeners persistentes=" + objetivo.onClick.GetPersistentEventCount() +
                      " | interactable=" + objetivo.interactable +
                      " | activoEnJerarquia=" + objetivo.gameObject.activeInHierarchy);
        for (int i = 0; i < objetivo.onClick.GetPersistentEventCount(); i++)
        {
            sb.AppendLine("  listener[" + i + "] target=" + objetivo.onClick.GetPersistentTarget(i) + " metodo=" + objetivo.onClick.GetPersistentMethodName(i));
        }

        objetivo.onClick.Invoke();

        sb.AppendLine("[BTN] tras invocar: Fijado=" + anchor.Fijado + " escala=" + anchor.EscalaAros.ToString("F4") +
                      " radio=" + ARState.SelectedPlateRadius + " cuadrado=" + ARState.EsPlatoCuadrado);

        Transform molde = GameObject.Find("MoldeHolograma") != null ? GameObject.Find("MoldeHolograma").transform : null;
        Transform aros = GameObject.Find("ArosAR") != null ? GameObject.Find("ArosAR").transform : null;
        sb.AppendLine("  molde=" + (molde == null ? "NULL" : "self=" + molde.gameObject.activeSelf + " pos=" + molde.position.ToString("F4") + " scale=" + molde.localScale.ToString("F5")));
        sb.AppendLine("  aros=" + (aros == null ? "NULL" : "self=" + aros.gameObject.activeSelf + " pos=" + aros.position.ToString("F4")));

        if (aros != null)
        {
            string[] nombres = new string[] { "spriteGuiaCircular", "spriteFijadoCircular", "spriteGuiaCuadrado", "spriteFijadoCuadrado" };
            for (int i = 0; i < nombres.Length; i++)
            {
                Transform t = aros.Find(nombres[i]);
                sb.AppendLine("    " + nombres[i] + " self=" + (t == null ? "NULL" : t.gameObject.activeSelf.ToString()));
            }
        }

        // Paneles activos tras el flujo
        string[] paneles = new string[] { "Panel_Splash", "Panel_MenuPrincipal", "Panel_Perfil", "Panel_AR", "Panel_CalibracionPlato", "Panel_GuiaNutricional", "Panel_DashboardMetas" };
        for (int i = 0; i < paneles.Length; i++)
        {
            GameObject p = GameObject.Find("Canvas/" + paneles[i]);
            sb.AppendLine("  panel " + paneles[i] + " = " + (p == null ? "NO EXISTE" : p.activeSelf.ToString()));
        }

        Debug.Log(sb.ToString());
    }
}
