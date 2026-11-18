using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public static class AuraVerifyAR
{
    public static void Execute()
    {
        StringBuilder sb = new StringBuilder();

        PlateAnchorManager anchor = Object.FindFirstObjectByType<PlateAnchorManager>();
        MenuController menu = Object.FindFirstObjectByType<MenuController>();
        ARUIManager ui = Object.FindFirstObjectByType<ARUIManager>();
        Camera cam = Camera.main;

        // Escenario del dispositivo: ARCore todavia NO ha detectado la mesa (raycast sin impacto).
        anchor.ReiniciarColocacion();
        typeof(PlateAnchorManager).GetField("_hayPose", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(anchor, false);

        menu.MostrarCalibracion();

        // Pulso real del boton de calibracion.
        Button objetivo = null;
        Button[] todos = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < todos.Length; i++)
        {
            if (todos[i].name == "Btn_FijarPlatoCalibracion") { objetivo = todos[i]; break; }
        }

        if (objetivo == null) { Debug.LogError("[VERIFY] falta Btn_FijarPlatoCalibracion"); return; }

        objetivo.onClick.Invoke();

        Transform molde = GameObject.Find("MoldeHolograma") != null ? GameObject.Find("MoldeHolograma").transform : null;
        Transform aros = GameObject.Find("ArosAR") != null ? GameObject.Find("ArosAR").transform : null;

        sb.AppendLine("[VERIFY] SIN plano detectado (respaldo delante de la camara)");
        sb.AppendLine("  Fijado=" + anchor.Fijado + " escala=" + anchor.EscalaAros.ToString("F4"));
        sb.AppendLine("  camara en " + cam.transform.position.ToString("F3") + " mirando " + cam.transform.forward.ToString("F3"));
        sb.AppendLine("  molde self=" + molde.gameObject.activeSelf + " hier=" + molde.gameObject.activeInHierarchy +
                      " pos=" + molde.position.ToString("F4") + " viewport=" + cam.WorldToViewportPoint(molde.position).ToString("F3"));
        sb.AppendLine("  aros self=" + aros.gameObject.activeSelf + " pos=" + aros.position.ToString("F4") +
                      " viewport=" + cam.WorldToViewportPoint(aros.position).ToString("F3"));

        string[] nombres = new string[] { "spriteGuiaCircular", "spriteFijadoCircular", "spriteGuiaCuadrado", "spriteFijadoCuadrado" };
        for (int i = 0; i < nombres.Length; i++)
        {
            Transform t = aros.Find(nombres[i]);
            sb.AppendLine("    " + nombres[i] + " self=" + (t == null ? "NULL" : t.gameObject.activeSelf + " scale=" + t.localScale.ToString("F4")));
        }

        // Servir en el plato: el molde debe quedar visible sobre el plato.
        molde.gameObject.SetActive(false);
        ui.ServirEnElPlato();
        sb.AppendLine("[VERIFY] tras ServirEnElPlato: molde self=" + molde.gameObject.activeSelf +
                      " pos=" + molde.position.ToString("F4") +
                      " viewport=" + cam.WorldToViewportPoint(molde.position).ToString("F3") +
                      " scale=" + molde.localScale.ToString("F5"));

        Debug.Log(sb.ToString());
    }
}
