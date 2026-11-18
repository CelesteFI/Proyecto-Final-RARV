using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Fondo oscuro de la lista del dropdown para que las miniaturas y los nombres se lean.</summary>
public static class AuraFixListaDropdown
{
    private static readonly Color Fondo = new Color32(20, 25, 45, 250);
    private static readonly Color Fila = new Color32(34, 42, 72, 255);

    public static string Execute()
    {
        StringBuilder sb = new StringBuilder();

        GameObject canvasGo = GameObject.Find("Canvas");
        Transform dd = canvasGo.transform.Find("Panel_AR/PanelSuperior/dropdownAlimentos");
        Transform template = dd.Find("Template");

        if (template == null)
        {
            return "FALTA Template";
        }

        Image img = template.GetComponent<Image>();

        if (img != null)
        {
            img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            img.type = Image.Type.Sliced;
            img.color = Fondo;
        }

        Transform item = template.Find("Viewport/Content/Item");

        if (item != null)
        {
            Image itemImg = item.GetComponent<Image>();

            if (itemImg != null)
            {
                itemImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                itemImg.type = Image.Type.Sliced;
                itemImg.color = Fila;
            }
        }

        template.gameObject.SetActive(false);

        EditorUtility.SetDirty(canvasGo);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        sb.AppendLine("Lista del dropdown con fondo oscuro y filas azul marino");

        return sb.ToString();
    }
}
