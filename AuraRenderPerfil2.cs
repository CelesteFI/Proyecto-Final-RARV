using System.IO;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Previsualizacion del Panel_Perfil con la misma proporcion que el Canvas (978x2119).</summary>
public static class AuraRenderPerfil2
{
    private const int Ancho = 800;
    private const int Alto = 1733;

    public static void Execute()
    {
        GameObject canvasGo = GameObject.Find("Canvas");
        Canvas canvas = canvasGo.GetComponent<Canvas>();

        GameObject camGo = new GameObject("AuraPreviewCamPerfil2");
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

        try
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 10f;

            Render(canvas, cam, "Panel_Perfil", "AuraPreview_Perfil2.png");
        }
        finally
        {
            canvas.renderMode = modoOriginal;
            canvas.worldCamera = camaraOriginal;
            canvas.planeDistance = distanciaOriginal;
            Object.DestroyImmediate(camGo);
            RestaurarEstado(canvasGo);
        }

        Debug.Log("[AuraRenderPerfil2] Previsualizacion generada.");
    }

    private static void Render(Canvas canvas, Camera cam, string panel, string archivo)
    {
        GameObject canvasGo = canvas.gameObject;

        for (int i = 0; i < canvasGo.transform.childCount; i++)
        {
            GameObject hijo = canvasGo.transform.GetChild(i).gameObject;
            bool activo = hijo.name == panel;

            if (hijo.activeSelf != activo)
            {
                hijo.SetActive(activo);
            }
        }

        Canvas.ForceUpdateCanvases();
        Canvas.ForceUpdateCanvases();

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

        Debug.Log("[AuraRenderPerfil2] " + archivo + " (" + panel + ")");
    }

    private static void RestaurarEstado(GameObject canvasGo)
    {
        for (int i = 0; i < canvasGo.transform.childCount; i++)
        {
            GameObject hijo = canvasGo.transform.GetChild(i).gameObject;
            bool activo = hijo.name == "Panel_Splash" || hijo.name == "Panel_Carga";

            if (hijo.activeSelf != activo)
            {
                hijo.SetActive(activo);
            }
        }
    }
}
