using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AuraWireAR
{
    public static void Execute()
    {
        GameObject xr = GameObject.Find("XR Origin");
        if (xr == null) { Debug.LogError("[WIRE] sin XR Origin"); return; }

        ARUIManager ui = xr.GetComponent<ARUIManager>();
        PlateAnchorManager anchor = xr.GetComponent<PlateAnchorManager>();

        if (ui == null || anchor == null) { Debug.LogError("[WIRE] faltan componentes en XR Origin"); return; }

        SerializedObject so = new SerializedObject(ui);
        SerializedProperty campo = so.FindProperty("plateAnchor");

        if (campo == null) { Debug.LogError("[WIRE] no existe el campo plateAnchor"); return; }

        campo.objectReferenceValue = anchor;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(ui);

        var escena = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);

        Debug.Log("[WIRE] plateAnchor asignado en ARUIManager = " + (campo.objectReferenceValue != null) +
                  " | escena guardada = " + escena.name);
    }
}
