using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Previsualizacion del Panel_AR con el dropdown desplegado (miniatura + nombre por opcion).</summary>
public static class AuraRenderAR
{
    private const int Ancho = 800;
    private const int Alto = 1733;

    public static void Execute()
    {
        GameObject canvasGo = GameObject.Find("Canvas");
        Canvas canvas = canvasGo.GetComponent<Canvas>();

        GameObject camGo = new GameObject("AuraPreviewCamAR");
        Camera cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.03f, 0.035f, 0.06f, 1f);
        cam.orthographic = true;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 100f;
        cam.cullingMask = ~0;
        cam.enabled = false;

        RenderMode modoOriginal = canvas.renderMode;
        Camera camaraOriginal = canvas.worldCamera;
        float distanciaOriginal = canvas.planeDistance;

        TMP_Dropdown dropdown = canvasGo.transform
            .Find("Panel_AR/PanelSuperior/dropdownAlimentos").GetComponent<TMP_Dropdown>();

        try
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 10f;

            MostrarSolo(canvasGo, "Panel_AR");
            Capturar(canvas, cam, "AuraPreview_AR2.png");
        }
        finally
        {
            canvas.renderMode = modoOriginal;
            canvas.worldCamera = camaraOriginal;
            canvas.planeDistance = distanciaOriginal;
            Object.DestroyImmediate(camGo);

            GameObject lista = GameObject.Find("Dropdown List");

            if (lista != null)
            {
                Object.DestroyImmediate(lista);
            }

            MostrarSolo(canvasGo, "Panel_Splash");
            canvasGo.transform.Find("Panel_Carga").gameObject.SetActive(true);
        }

        Debug.Log("[AuraRenderAR] Previsualizaciones generadas.");
    }

    private static void MostrarSolo(GameObject canvasGo, string panel)
    {
        for (int i = 0; i < canvasGo.transform.childCount; i++)
        {
            GameObject hijo = canvasGo.transform.GetChild(i).gameObject;
            bool activo = hijo.name == panel || hijo.name == "Panel_Carga";

            if (hijo.activeSelf != activo)
            {
                hijo.SetActive(activo);
            }
        }

        Canvas.ForceUpdateCanvases();
    }

    private static void Capturar(Canvas canvas, Camera cam, string archivo)
    {
        RenderTexture rt = new RenderTexture(Ancho, Alto, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(Ancho, Alto, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0f, 0f, Ancho, Alto), 0, 0);
        tex.Apply();
        RenderTexture.active = null;

        cam.targetTexture = null;
        rt.Release();
        Object.DestroyImmediate(rt);

        File.WriteAllBytes(archivo, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        Debug.Log("[AuraRenderAR] " + archivo);
    }
}
