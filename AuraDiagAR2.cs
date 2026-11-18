using System.Text;
using UnityEngine;
using UnityEngine.UI;

public static class AuraDiagAR2
{
    public static void Execute()
    {
        StringBuilder sb = new StringBuilder();

        Transform xr = GameObject.Find("XR Origin").transform;
        Transform molde = xr.Find("MoldeHolograma");
        Transform arosRaiz = xr.Find("ArosAR");

        sb.AppendLine("[MOLDE] self=" + molde.gameObject.activeSelf + " hier=" + molde.gameObject.activeInHierarchy);
        sb.AppendLine("  worldPos=" + molde.position.ToString("F4") + " localScale=" + molde.localScale.ToString("F5"));

        MeshFilter mf = molde.GetComponent<MeshFilter>();
        MeshRenderer mr = molde.GetComponent<MeshRenderer>();
        sb.AppendLine("  MeshFilter=" + (mf == null ? "NULL" : (mf.sharedMesh == null ? "mesh NULL" : mf.sharedMesh.name + " verts=" + mf.sharedMesh.vertexCount)));

        if (mr != null)
        {
            sb.AppendLine("  MeshRenderer.enabled=" + mr.enabled + " isVisible=" + mr.isVisible);
            Material m = mr.sharedMaterial;
            sb.AppendLine("  mat=" + (m == null ? "NULL" : m.name + " shader=" + m.shader.name));
            if (m != null)
            {
                if (m.HasProperty("_BaseColor")) sb.AppendLine("    _BaseColor=" + m.GetColor("_BaseColor").ToString());
                if (m.HasProperty("_Color")) sb.AppendLine("    _Color=" + m.GetColor("_Color").ToString());
                sb.AppendLine("    renderQueue=" + m.renderQueue);
            }
            sb.AppendLine("  bounds center=" + mr.bounds.center.ToString("F4") + " size=" + mr.bounds.size.ToString("F5"));
        }

        string[] nombresAros = new string[] { "spriteGuiaCircular", "spriteFijadoCircular", "spriteGuiaCuadrado", "spriteFijadoCuadrado" };
        for (int i = 0; i < nombresAros.Length; i++)
        {
            Transform t = arosRaiz.Find(nombresAros[i]);
            if (t == null) { sb.AppendLine("[ARO] " + nombresAros[i] + " NO ENCONTRADO"); continue; }

            RectTransform rt = t.GetComponent<RectTransform>();
            Image img = t.GetComponent<Image>();
            sb.AppendLine("[ARO] " + nombresAros[i] + " self=" + t.gameObject.activeSelf + " worldPos=" + t.position.ToString("F4") + " localScale=" + t.localScale.ToString("F5"));
            if (rt != null) sb.AppendLine("  rect=" + rt.rect.width.ToString("F4") + "x" + rt.rect.height.ToString("F4") + " sizeDelta=" + rt.sizeDelta.ToString() + " anchors=" + rt.anchorMin.ToString() + rt.anchorMax.ToString());
            if (img != null) sb.AppendLine("  sprite=" + (img.sprite == null ? "NULL" : img.sprite.name) + " color=" + img.color.ToString() + " enabled=" + img.enabled + " preserveAspect=" + img.preserveAspect);
        }

        Canvas canvas = arosRaiz.GetComponent<Canvas>();
        RectTransform rtAros = arosRaiz.GetComponent<RectTransform>();
        sb.AppendLine("[AROSAR] self=" + arosRaiz.gameObject.activeSelf + " worldPos=" + arosRaiz.position.ToString("F4") + " localScale=" + arosRaiz.localScale.ToString("F4"));
        sb.AppendLine("  rect=" + rtAros.rect.width.ToString("F4") + "x" + rtAros.rect.height.ToString("F4") + " sizeDelta=" + rtAros.sizeDelta.ToString());
        if (canvas != null)
        {
            sb.AppendLine("  renderMode=" + canvas.renderMode + " worldCamera=" + (canvas.worldCamera == null ? "NULL" : canvas.worldCamera.name) +
                          " sortingOrder=" + canvas.sortingOrder + " overrideSorting=" + canvas.overrideSorting +
                          " scaleFactor=" + canvas.scaleFactor + " enabled=" + canvas.enabled);
            sb.AppendLine("  rootCanvas=" + (canvas.rootCanvas == null ? "NULL" : canvas.rootCanvas.name));
        }

        Debug.Log(sb.ToString());
    }
}
