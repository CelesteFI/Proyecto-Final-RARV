using System.Reflection;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public static class AuraDiagPlanes
{
    public static void Execute()
    {
        ARPlaneManager pm = Object.FindFirstObjectByType<ARPlaneManager>(FindObjectsInactive.Include);
        if (pm == null) { Debug.Log("[PLANES] no hay ARPlaneManager"); return; }

        FieldInfo campo = typeof(ARPlaneManager).GetField("m_DetectionMode", BindingFlags.Instance | BindingFlags.NonPublic);
        object valor = campo == null ? "campo no encontrado" : campo.GetValue(pm);

        ARRaycastManager rm = Object.FindFirstObjectByType<ARRaycastManager>(FindObjectsInactive.Include);

        Debug.Log("[PLANES] enabled=" + pm.enabled +
                  " m_DetectionMode=" + valor +
                  " requested=" + pm.requestedPlaneDetectionMode +
                  " current=" + pm.currentDetectionMode +
                  " | raycastExiste=" + (rm != null) + " raycastEnabled=" + (rm != null && rm.enabled) +
                  " | planePrefab=" + (pm.planePrefab == null ? "NULL" : pm.planePrefab.name));
    }
}
