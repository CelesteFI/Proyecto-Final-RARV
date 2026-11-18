using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Comprobacion final del estado de arranque del Canvas y guardado de la escena.</summary>
public static class AuraFinal
{
    public static string Execute()
    {
        GameObject canvasGo = GameObject.Find("Canvas");
        Transform canvas = canvasGo.transform;

        StringBuilder sb = new StringBuilder();
        bool correcto = canvas.childCount > 0;

        for (int i = 0; i < canvas.childCount; i++)
        {
            Transform hijo = canvas.GetChild(i);
            sb.AppendLine(i + " " + hijo.name + " activo=" + hijo.gameObject.activeSelf);

            bool deberiaEstarActivo = hijo.name == "Panel_Splash" || hijo.name == "Panel_Carga";

            if (hijo.gameObject.activeSelf != deberiaEstarActivo)
            {
                correcto = false;
            }
        }

        Transform carga = canvas.Find("Panel_Carga");
        bool cargaUltima = carga != null && carga.GetSiblingIndex() == canvas.childCount - 1;

        CanvasGroup grupo = carga != null ? carga.GetComponent<CanvasGroup>() : null;
        sb.AppendLine("Panel_Carga ultimo=" + cargaUltima +
                      " alpha=" + (grupo == null ? "sin CanvasGroup" : grupo.alpha.ToString("F2")) +
                      " blocksRaycasts=" + (grupo == null ? "n/a" : grupo.blocksRaycasts.ToString()));

        bool estadoGuardadoOk = correcto && cargaUltima && grupo != null && grupo.alpha <= 0.001f && !grupo.blocksRaycasts;

        if (estadoGuardadoOk)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            sb.AppendLine("ESTADO DE ARRANQUE OK: escena guardada.");
        }
        else
        {
            sb.AppendLine("REVISAR: el estado de arranque no coincide con el esperado, no se guardo.");
        }

        return sb.ToString();
    }
}
