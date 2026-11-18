using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ajuste fino del layout de los paneles nuevos, medido sobre las previsualizaciones renderizadas.
/// Solo mueve RectTransforms y tamanos de fuente: no crea ni elimina objetos ni toca el cableado.
/// </summary>
public static class AuraLayoutFix
{
    public static void Execute()
    {
        GameObject canvasGo = GameObject.Find("Canvas");
        Transform canvas = canvasGo.transform;

        Catalogo(canvas.Find("Panel_GuiaNutricional"));
        Detalle(canvas.Find("Panel_GuiaNutricional/PanelDetalle"));
        Metas(canvas.Find("Panel_DashboardMetas"));
        Tutorial(canvas.Find("Panel_Tutorial"));

        EditorUtility.SetDirty(canvasGo);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        Debug.Log("[AuraLayoutFix] Layout de catalogo, detalle, metas y tutorial ajustado y escena guardada.");
    }

    // ---------------------------------------------------------------- catalogo

    private static void Catalogo(Transform guia)
    {
        if (guia == null)
        {
            return;
        }

        // La rejilla ocupaba solo la mitad superior: celdas mas altas y bloque centrado.
        Transform gridT = guia.Find("GridTarjetas");
        if (gridT != null)
        {
            GridLayoutGroup grid = gridT.GetComponent<GridLayoutGroup>();
            if (grid != null)
            {
                grid.cellSize = new Vector2(425f, 170f);
                grid.spacing = new Vector2(24f, 22f);
                grid.childAlignment = TextAnchor.MiddleCenter;
            }
        }

        // El boton "Volver" era el unico rectangulo plano del panel.
        Transform volver = guia.Find("Btn_VolverGuia");
        if (volver != null)
        {
            Image img = volver.GetComponent<Image>();
            if (img != null && img.sprite == null)
            {
                img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                img.type = Image.Type.Sliced;
            }
        }

        if (gridT == null)
        {
            return;
        }

        for (int i = 0; i < gridT.childCount; i++)
        {
            Transform tarjeta = gridT.GetChild(i);

            // La imagen ocupa el tercio izquierdo; el texto empieza despues de ella (antes se solapaban).
            Colocar(Buscar(tarjeta, "Icono"), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(110f, 110f));

            Colocar(Buscar(tarjeta, "Txt_Nombre"), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f), new Vector2(146f, 32f), new Vector2(263f, 46f));

            Colocar(Buscar(tarjeta, "Txt_KcalNumero"), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f), new Vector2(146f, -28f), new Vector2(74f, 34f));

            Colocar(Buscar(tarjeta, "Txt_KcalSufijo"), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f), new Vector2(224f, -28f), new Vector2(185f, 34f));

            Fuente(Buscar(tarjeta, "Txt_Nombre"), 28f);
            Fuente(Buscar(tarjeta, "Txt_KcalNumero"), 26f);
            Fuente(Buscar(tarjeta, "Txt_KcalSufijo"), 20f);
        }
    }

    // ---------------------------------------------------------------- detalle

    private static void Detalle(Transform panelDetalle)
    {
        if (panelDetalle == null)
        {
            return;
        }

        Transform card = panelDetalle.Find("TarjetaDetalle");
        if (card == null)
        {
            return;
        }

        // La fila "Grasas" quedaba oculta bajo el boton "Servir en el plato": se comprime el bloque.
        Colocar(Buscar(card, "Icono"), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(220f, 220f));

        Colocar(Buscar(card, "Titulo"), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -300f), new Vector2(820f, 70f));

        Colocar(Buscar(card, "Estado"), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -378f), new Vector2(820f, 44f));

        Colocar(Buscar(card, "KcalNumero"), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -432f), new Vector2(820f, 110f));

        Colocar(Buscar(card, "KcalSufijo"), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -548f), new Vector2(820f, 42f));

        FilaDetalle(card, "Prot", -628f);
        FilaDetalle(card, "Carbo", -712f);
        FilaDetalle(card, "Grasa", -796f);

        Colocar(Buscar(card, "Btn_Servir"), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 180f), new Vector2(720f, 100f));

        Colocar(Buscar(card, "Btn_CerrarDetalle"), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 80f), new Vector2(720f, 80f));
    }

    /// <summary>Titulo a la izquierda y, a la derecha, numero con su sufijo ("g") detras.</summary>
    private static void FilaDetalle(Transform card, string prefijo, float y)
    {
        Colocar(Buscar(card, prefijo + "Titulo"), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(60f, y), new Vector2(380f, 50f));

        Colocar(Buscar(card, prefijo + "Numero"), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-150f, y), new Vector2(180f, 50f));

        Colocar(Buscar(card, prefijo + "Sufijo"), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, 1f), new Vector2(-140f, y), new Vector2(100f, 50f));
    }

    // ---------------------------------------------------------------- metas

    private static void Metas(Transform panel)
    {
        if (panel == null)
        {
            return;
        }

        Transform card = panel.Find("TarjetaMacros");
        if (card == null)
        {
            return;
        }

        // La barra de acento morada solo cubria la mitad alta de la tarjeta.
        Colocar(Buscar(card, "Acento"), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(0f, -20f), new Vector2(12f, 430f));

        FilaMetas(card, "Prot", -150f);
        FilaMetas(card, "Carbo", -250f);
        FilaMetas(card, "Grasa", -350f);
    }

    private static void FilaMetas(Transform card, string prefijo, float y)
    {
        Colocar(Buscar(card, prefijo + "Titulo"), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(60f, y), new Vector2(420f, 50f));

        // Antes el sufijo "g" caia a la izquierda del numero.
        Colocar(Buscar(card, prefijo + "Numero"), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-160f, y), new Vector2(200f, 50f));

        Colocar(Buscar(card, prefijo + "Sufijo"), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, 1f), new Vector2(-150f, y), new Vector2(100f, 50f));
    }

    // ---------------------------------------------------------------- tutorial

    private static void Tutorial(Transform panel)
    {
        if (panel == null)
        {
            return;
        }

        Transform card = panel.Find("TarjetaTutorial");
        if (card == null)
        {
            return;
        }

        Colocar(Buscar(card, "Indicador"), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -56f), new Vector2(300f, 44f));

        // El bloque de pasos dejaba un hueco muerto de 360 px antes de los botones.
        for (int paso = 1; paso <= 4; paso++)
        {
            Transform bloque = Buscar(card, "Paso_" + paso);
            if (bloque == null)
            {
                continue;
            }

            Colocar(bloque as RectTransform, Vector2.zero, Vector2.one,
                    new Vector2(0.5f, 0.5f), new Vector2(0f, 25f), new Vector2(-80f, -300f));

            Colocar(Buscar(bloque, "Icono"), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(320f, 320f));

            Colocar(Buscar(bloque, "Titulo"), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f), new Vector2(0f, -370f), new Vector2(760f, 70f));

            Colocar(Buscar(bloque, "Descripcion"), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f), new Vector2(0f, -450f), new Vector2(760f, 260f));
        }

        Colocar(Buscar(card, "Btn_Anterior"), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(-230f, 190f), new Vector2(360f, 96f));

        Colocar(Buscar(card, "Btn_Siguiente"), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(230f, 190f), new Vector2(360f, 96f));

        Colocar(Buscar(card, "Btn_CerrarTutorial"), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 80f), new Vector2(360f, 88f));
    }

    // ---------------------------------------------------------------- utilidades

    private static RectTransform Buscar(Transform raiz, string ruta)
    {
        if (raiz == null)
        {
            return null;
        }

        Transform t = raiz.Find(ruta);
        if (t == null)
        {
            Debug.LogWarning("[AuraLayoutFix] No se encontro " + raiz.name + "/" + ruta);
            return null;
        }

        return t as RectTransform;
    }

    private static void Colocar(RectTransform rt, Vector2 anclaMin, Vector2 anclaMax,
                                Vector2 pivote, Vector2 pos, Vector2 tam)
    {
        if (rt == null)
        {
            return;
        }

        rt.anchorMin = anclaMin;
        rt.anchorMax = anclaMax;
        rt.pivot = pivote;
        rt.sizeDelta = tam;
        rt.anchoredPosition = pos;
    }

    private static void Fuente(Transform t, float tam)
    {
        if (t == null)
        {
            return;
        }

        TextMeshProUGUI tmp = t.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.fontSize = tam;
        }
    }
}
