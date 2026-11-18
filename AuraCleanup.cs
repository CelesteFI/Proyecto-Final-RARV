using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Limpia restos de la lista del dropdown y del marcador temporal de previsualizacion.</summary>
public static class AuraCleanup
{
    public static string Execute()
    {
        StringBuilder sb = new StringBuilder();

        foreach (GameObject go in Object.FindObjectsOfType<GameObject>(true))
        {
            if (go.name == "Dropdown List" || go.name == "AuraPreviewCamAR" || go.name == "AuraPreviewCamPerfil" ||
                go.name == "AuraPreviewCamPerfil2" || go.name == "AuraPreviewCam")
            {
                sb.AppendLine("Destruido: " + go.name + " (padre " + (go.transform.parent != null ? go.transform.parent.name : "raiz") + ")");
                Object.DestroyImmediate(go);
            }
        }

        GameObject canvasGo = GameObject.Find("Canvas");
        sb.AppendLine("Hijos del Canvas:");

        foreach (Transform t in canvasGo.transform)
        {
            sb.AppendLine("   " + t.name + " activo " + t.gameObject.activeSelf);
        }

        CanvasGroup carga = canvasGo.transform.Find("Panel_Carga").GetComponent<CanvasGroup>();
        sb.AppendLine("Panel_Carga alpha " + carga.alpha + " interactable " + carga.interactable +
                      " blocksRaycasts " + carga.blocksRaycasts + " childIndex " + carga.transform.GetSiblingIndex());

        TMP_Dropdown dropdown = canvasGo.transform
            .Find("Panel_AR/PanelSuperior/dropdownAlimentos").GetComponent<TMP_Dropdown>();
        sb.AppendLine("dropdown valor " + dropdown.value + " caption '" + dropdown.captionText.text + "'");

        return sb.ToString();
    }
}
