using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class AuraFinalAR
{
    public static void Execute()
    {
        StringBuilder sb = new StringBuilder();
        GameObject canvas = GameObject.Find("Canvas");
        GameObject xr = GameObject.Find("XR Origin");

        sb.AppendLine("[FINAL] escena activa=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);

        // Estado de paneles guardado
        for (int i = 0; i < canvas.transform.childCount; i++)
        {
            Transform hijo = canvas.transform.GetChild(i);
            if (hijo.name.StartsWith("Panel_"))
            {
                sb.AppendLine("  panel " + hijo.name + " activo=" + hijo.gameObject.activeSelf);
            }
        }

        // Cableado del nuevo campo
        ARUIManager ui = xr.GetComponent<ARUIManager>();
        SerializedObject so = new SerializedObject(ui);
        SerializedProperty campo = so.FindProperty("plateAnchor");
        sb.AppendLine("[FINAL] ARUIManager.plateAnchor = " +
            (campo != null && campo.objectReferenceValue != null ? campo.objectReferenceValue.name : "SIN ASIGNAR"));

        // Aros: referencia + sprite + estado guardado
        PlateAnchorManager anchor = xr.GetComponent<PlateAnchorManager>();
        SerializedObject soA = new SerializedObject(anchor);
        string[] campos = new string[] { "spriteGuiaCircular", "spriteFijadoCircular", "spriteGuiaCuadrado", "spriteFijadoCuadrado" };
        int asignados = 0;
        for (int i = 0; i < campos.Length; i++)
        {
            SerializedProperty p = soA.FindProperty(campos[i]);
            GameObject aro = p == null ? null : p.objectReferenceValue as GameObject;
            if (aro == null) { sb.AppendLine("  " + campos[i] + " = SIN ASIGNAR"); continue; }
            asignados++;
            Image img = aro.GetComponent<Image>();
            sb.AppendLine("  " + campos[i] + " = " + aro.name + " sprite=" + (img != null && img.sprite != null ? img.sprite.name : "NULL") +
                          " activo=" + aro.activeSelf);
        }
        sb.AppendLine("[FINAL] aros asignados = " + asignados + "/4");

        // Molde
        Transform molde = xr.transform.Find("MoldeHolograma");
        sb.AppendLine("[FINAL] MoldeHolograma hijo de XR Origin=" + (molde != null) +
                      (molde == null ? "" : " activo=" + molde.gameObject.activeSelf));

        Debug.Log(sb.ToString());
    }
}
