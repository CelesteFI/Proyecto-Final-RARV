using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Remate visual: tarjeta del tutorial mas compacta, boton "Volver" con sprite y dashboard sin cifras de muestra.</summary>
public static class AuraPolish
{
    public static string Execute()
    {
        GameObject canvasGo = GameObject.Find("Canvas");
        Transform canvas = canvasGo.transform;

        StringBuilder sb = new StringBuilder();

        // ---- tutorial: la tarjeta era 130 px mas alta que su contenido
        Transform panelTutorial = canvas.Find("Panel_Tutorial");
        Transform card = panelTutorial != null ? panelTutorial.Find("TarjetaTutorial") : null;

        if (card != null)
        {
            RectTransform rt = card as RectTransform;
            rt.sizeDelta = new Vector2(880f, 1100f);

            for (int paso = 1; paso <= 4; paso++)
            {
                Transform bloque = card.Find("Paso_" + paso);
                if (bloque == null)
                {
                    sb.AppendLine("falta Paso_" + paso);
                    continue;
                }

                RectTransform brt = bloque as RectTransform;
                brt.sizeDelta = new Vector2(-80f, -280f);
                brt.anchoredPosition = new Vector2(0f, 0f);

                Colocar(bloque.Find("Icono"), new Vector2(0f, 0f), new Vector2(280f, 280f));
                Colocar(bloque.Find("Titulo"), new Vector2(0f, -310f), new Vector2(760f, 70f));
                Colocar(bloque.Find("Descripcion"), new Vector2(0f, -400f), new Vector2(760f, 260f));
            }

            ColocarAbajo(card.Find("Btn_Anterior"), new Vector2(-230f, 170f), new Vector2(360f, 96f));
            ColocarAbajo(card.Find("Btn_Siguiente"), new Vector2(230f, 170f), new Vector2(360f, 96f));
            ColocarAbajo(card.Find("Btn_CerrarTutorial"), new Vector2(0f, 60f), new Vector2(360f, 88f));
        }
        else
        {
            sb.AppendLine("falta TarjetaTutorial");
        }

        // ---- boton "Volver" del catalogo: unico rectangulo plano del panel
        Transform volver = canvas.Find("Panel_GuiaNutricional/Btn_VolverGuia");
        if (volver != null)
        {
            Image img = volver.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                img.type = Image.Type.Sliced;
                sb.AppendLine("Btn_VolverGuia sprite=" + (img.sprite == null ? "NONE" : img.sprite.name) + " type=" + img.type);
            }
        }
        else
        {
            sb.AppendLine("falta Btn_VolverGuia");
        }

        // ---- dashboard: cifras de muestra guardadas en la escena
        Transform metas = canvas.Find("Panel_DashboardMetas");
        if (metas != null)
        {
            Escribir(metas, "TarjetaAgua/AguaNumero", "--");
            Escribir(metas, "TarjetaKcal/KcalNumero", "--");
        }

        EditorUtility.SetDirty(canvasGo);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        return sb.ToString();
    }

    private static void Colocar(Transform t, Vector2 pos, Vector2 tam)
    {
        RectTransform rt = t as RectTransform;
        if (rt == null)
        {
            return;
        }

        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = tam;
        rt.anchoredPosition = pos;
    }

    private static void ColocarAbajo(Transform t, Vector2 pos, Vector2 tam)
    {
        RectTransform rt = t as RectTransform;
        if (rt == null)
        {
            return;
        }

        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = tam;
        rt.anchoredPosition = pos;
    }

    private static void Escribir(Transform raiz, string ruta, string texto)
    {
        Transform t = raiz.Find(ruta);
        if (t == null)
        {
            return;
        }

        TextMeshProUGUI tmp = t.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.text = texto;
        }
    }
}
