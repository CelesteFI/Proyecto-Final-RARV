using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sustituye los iconos placeholder por miniaturas reales de cada alimento y aclara el
/// dropdown del Panel_AR (campo oscuro, flecha nativa y textos claros) para que la miniatura
/// y el nombre se lean bien.
/// </summary>
public static class AuraFixIconos
{
    private static readonly string[,] Mapa =
    {
        { "icono_arroz", "Icono_Arroz" },
        { "icono_pasta", "Icono_Pasta" },
        { "icono_pollo", "Icono_Pollo" },
        { "icono_res", "Icono_Res" },
        { "icono_cerdo", "Icono_Cerdo" },
        { "icono_salmon", "Icono_Salmon" },
        { "icono_atun", "Icono_Atun" },
        { "icono_huevo", "Icono_Huevo" },
        { "icono_papa", "Icono_Papa" },
        { "icono_zanahoria", "Icono_Zanahoria" },
        { "icono_guisode_pollo", "Icono_GuisodePollo" },
        { "icono_sopa_verduras", "Icono_SopadeVerduras" },
        { "icono_arroz_con_pollo", "Icono_ArrozconPollo" },
        { "icono_ensalada_mixta", "Icono_EnsaladaMixta" }
    };

    private static readonly Color Campo = new Color32(20, 25, 45, 255);
    private static readonly Color Claro = new Color32(240, 244, 255, 255);

    public static string Execute()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine(CopiarIconos());
        sb.AppendLine(EstilizarDropdown());

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        return sb.ToString();
    }

    private static string CopiarIconos()
    {
        StringBuilder sb = new StringBuilder();
        int copiados = 0;

        for (int i = 0; i < Mapa.GetLength(0); i++)
        {
            string origen = "Assets/Generated_Images/" + Mapa[i, 0] + ".png";
            string destino = "Assets/Sprites/IconosAlimentos/" + Mapa[i, 1] + ".png";

            if (!File.Exists(origen))
            {
                sb.AppendLine("FALTA origen " + origen);
                continue;
            }

            if (!File.Exists(destino))
            {
                sb.AppendLine("FALTA destino " + destino);
                continue;
            }

            File.Copy(origen, destino, true);
            AssetDatabase.ImportAsset(destino, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(destino) as TextureImporter;

            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 64f;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.maxTextureSize = 256;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport();
            }

            copiados++;
        }

        sb.AppendLine("Miniaturas reales copiadas: " + copiados + "/" + Mapa.GetLength(0));
        return sb.ToString();
    }

    private static string EstilizarDropdown()
    {
        StringBuilder sb = new StringBuilder();

        GameObject canvasGo = GameObject.Find("Canvas");
        Transform dd = canvasGo.transform.Find("Panel_AR/PanelSuperior/dropdownAlimentos");

        if (dd == null)
        {
            return "FALTA dropdownAlimentos";
        }

        Image fondo = dd.GetComponent<Image>();

        if (fondo != null)
        {
            fondo.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            fondo.type = Image.Type.Sliced;
            fondo.color = Campo;
        }

        TMP_Text caption = dd.Find("Label") != null ? dd.Find("Label").GetComponent<TMP_Text>() : null;

        if (caption != null)
        {
            caption.color = Claro;
        }

        Transform flecha = dd.Find("Arrow");

        if (flecha != null)
        {
            Image img = flecha.GetComponent<Image>();
            img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd");
            img.color = Claro;
        }

        Transform item = dd.Find("Template/Viewport/Content/Item");

        if (item != null)
        {
            Transform itemFondo = item.Find("Item Background");

            if (itemFondo != null)
            {
                Image img = itemFondo.GetComponent<Image>();

                if (img != null)
                {
                    img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                    img.type = Image.Type.Sliced;
                    img.color = Campo;
                }
            }

            Transform itemLabel = item.Find("Item Label");

            if (itemLabel != null)
            {
                TMP_Text tmp = itemLabel.GetComponent<TMP_Text>();

                if (tmp != null)
                {
                    tmp.color = Claro;
                }
            }

            Transform itemCheck = item.Find("Item Checkmark");

            if (itemCheck != null)
            {
                Image img = itemCheck.GetComponent<Image>();

                if (img != null)
                {
                    img.color = new Color32(41, 133, 245, 255);
                }
            }
        }

        sb.AppendLine("Dropdown del Panel_AR: campo oscuro, flecha y textos claros");
        return sb.ToString();
    }
}
