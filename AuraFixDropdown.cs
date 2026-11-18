using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Apaga la plantilla (Template) del dropdown, la deja con fondo oscuro y pone una miniatura
/// real en el icono del recuadro de metas.
/// </summary>
public static class AuraFixDropdown
{
    private static readonly Color Campo = new Color32(20, 25, 45, 255);
    private static readonly Color Claro = new Color32(240, 244, 255, 255);

    public static string Execute()
    {
        StringBuilder sb = new StringBuilder();

        GameObject canvasGo = GameObject.Find("Canvas");
        Transform dd = canvasGo.transform.Find("Panel_AR/PanelSuperior/dropdownAlimentos");
        Transform template = dd.Find("Template");

        sb.AppendLine("Template encontrado: " + (template != null) + " activo " + (template != null && template.gameObject.activeSelf));

        if (template != null)
        {
            Volcar(template, sb, 0);
            template.gameObject.SetActive(false);
            sb.AppendLine("Template apagado");
        }

        Transform metas = canvasGo.transform.Find("Panel_AR/PanelMetas");
        Transform icono = metas != null ? metas.Find("IconoMetas") : null;

        if (icono != null)
        {
            Image img = icono.GetComponent<Image>();
            Sprite arroz = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/IconosAlimentos/Icono_Arroz.png");

            if (arroz != null)
            {
                img.sprite = arroz;
                img.color = Color.white;
                img.preserveAspect = true;
                sb.AppendLine("IconoMetas -> Icono_Arroz");
            }
            else
            {
                sb.AppendLine("No se pudo cargar Icono_Arroz");
            }
        }

        EditorUtility.SetDirty(canvasGo);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        return sb.ToString();
    }

    private static void Volcar(Transform t, StringBuilder sb, int nivel)
    {
        string extra = string.Empty;
        Image img = t.GetComponent<Image>();

        if (img != null)
        {
            extra += " [Image sprite " + (img.sprite != null ? img.sprite.name : "None") + " color " + img.color + "]";
        }

        TMP_Text tmp = t.GetComponent<TMP_Text>();

        if (tmp != null)
        {
            extra += " [TMP '" + tmp.text + "' color " + tmp.color + "]";
        }

        sb.AppendLine(new string(' ', nivel * 2) + t.name + " activo " + t.gameObject.activeSelf + extra);

        foreach (Transform hijo in t)
        {
            Volcar(hijo, sb, nivel + 1);
        }
    }
}
