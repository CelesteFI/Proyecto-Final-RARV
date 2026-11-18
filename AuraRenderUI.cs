using System.IO;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Previsualizacion: renderiza tres paneles de UI a PNG usando una camara temporal, sin tocar
/// el estado guardado de la escena.
/// </summary>
public static class AuraRenderUI
{
    private const int Ancho = 900;
    private const int Alto = 1780;

    public static void Execute()
    {
        GameObject canvasGo = GameObject.Find("Canvas");
        Canvas canvas = canvasGo.GetComponent<Canvas>();

        GameObject camGo = new GameObject("AuraPreviewCam");
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

            Render(canvas, cam, "Panel_GuiaNutricional", "AuraPreview_Catalogo.png");
            Render(canvas, cam, "Panel_DashboardMetas", "AuraPreview_Metas.png");
            Render(canvas, cam, "Panel_Tutorial", "AuraPreview_Tutorial.png");

            // El detalle vive dentro del catalogo: se captura con el sub-panel abierto.
            Transform detalle = canvas.transform.Find("Panel_GuiaNutricional/PanelDetalle");
            if (detalle != null)
            {
                detalle.gameObject.SetActive(true);
                Render(canvas, cam, "Panel_GuiaNutricional", "AuraPreview_Detalle.png");
                detalle.gameObject.SetActive(false);
            }
            else
            {
                Debug.LogWarning("[AuraRenderUI] No se encontro Panel_GuiaNutricional/PanelDetalle");
            }
        }
        finally
        {
            canvas.renderMode = modoOriginal;
            canvas.worldCamera = camaraOriginal;
            canvas.planeDistance = distanciaOriginal;
            Object.DestroyImmediate(camGo);
            RestaurarEstado(canvasGo);
        }

        Debug.Log("[AuraRenderUI] Previsualizaciones generadas en la raiz del proyecto.");
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

        Debug.Log("[AuraRenderUI] " + archivo + " (" + panel + ")");
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
