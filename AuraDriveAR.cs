using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public static class AuraDriveAR
{
    public static void Execute()
    {
        StringBuilder sb = new StringBuilder();

        PlateAnchorManager anchor = Object.FindFirstObjectByType<PlateAnchorManager>();
        if (anchor == null) { Debug.Log("[DRIVE] sin PlateAnchorManager"); return; }

        // Pose controlada: simula el impacto del raycast (que en el editor no existe).
        FieldInfo hayPose = typeof(PlateAnchorManager).GetField("_hayPose", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo ultimaPose = typeof(PlateAnchorManager).GetField("_ultimaPose", BindingFlags.Instance | BindingFlags.NonPublic);
        hayPose.SetValue(anchor, true);
        ultimaPose.SetValue(anchor, new Pose(new Vector3(0f, 0f, 1.5f), Quaternion.identity));

        sb.AppendLine("[DRIVE] antes de fijar:");
        Estado(sb, anchor);

        ARState.SetPlateRadius(12f);
        anchor.EstablecerRadioVisual(12f);
        anchor.FijarPlato();

        sb.AppendLine("[DRIVE] despues de fijar: Fijado=" + anchor.Fijado + " escala=" + anchor.EscalaAros.ToString("F4"));

        Transform xr = GameObject.Find("XR Origin").transform;
        Transform molde = GameObject.Find("MoldeHolograma") != null ? GameObject.Find("MoldeHolograma").transform : null;
        Transform aros = GameObject.Find("ArosAR") != null ? GameObject.Find("ArosAR").transform : null;

        sb.AppendLine("  molde: " + (molde == null ? "NULL" : "parent=" + (molde.parent == null ? "<raiz>" : molde.parent.name) +
            " self=" + molde.gameObject.activeSelf + " hier=" + molde.gameObject.activeInHierarchy +
            " pos=" + molde.position.ToString("F4") + " scale=" + molde.localScale.ToString("F5")));
        sb.AppendLine("  aros: " + (aros == null ? "NULL" : "parent=" + (aros.parent == null ? "<raiz>" : aros.parent.name) +
            " self=" + aros.gameObject.activeSelf + " pos=" + aros.position.ToString("F4") + " rot=" + aros.eulerAngles.ToString("F2") + " scale=" + aros.localScale.ToString("F4")));

        if (aros != null)
        {
            string[] nombres = new string[] { "spriteGuiaCircular", "spriteFijadoCircular", "spriteGuiaCuadrado", "spriteFijadoCuadrado" };
            for (int i = 0; i < nombres.Length; i++)
            {
                Transform t = aros.Find(nombres[i]);
                if (t == null) { sb.AppendLine("    " + nombres[i] + " NULL"); continue; }
                sb.AppendLine("    " + nombres[i] + " self=" + t.gameObject.activeSelf + " hier=" + t.gameObject.activeInHierarchy +
                              " worldScale=" + t.lossyScale.ToString("F4") + " pos=" + t.position.ToString("F4"));
            }
        }

        var cam = Camera.main;
        if (cam != null && molde != null)
        {
            sb.AppendLine("  camara=" + cam.transform.position.ToString("F3") + " -> molde en pantalla: " +
                          cam.WorldToViewportPoint(molde.position).ToString("F3"));
            if (aros != null) sb.AppendLine("  -> aros en pantalla: " + cam.WorldToViewportPoint(aros.position).ToString("F3"));
        }

        Debug.Log(sb.ToString());
    }

    private static void Estado(StringBuilder sb, PlateAnchorManager anchor)
    {
        sb.AppendLine("  Fijado=" + anchor.Fijado + " escala=" + anchor.EscalaAros.ToString("F4") + " ARVisible=" + Object.FindFirstObjectByType<ARCameraController>().ARVisible);
    }
}
