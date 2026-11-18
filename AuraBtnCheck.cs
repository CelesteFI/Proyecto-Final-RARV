using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Compara el sprite de los botones para descartar rectangulos planos.</summary>
public static class AuraBtnCheck
{
    public static string Execute()
    {
        GameObject canvasGo = GameObject.Find("Canvas");
        Transform canvas = canvasGo.transform;

        StringBuilder sb = new StringBuilder();

        string[] rutas =
        {
            "Panel_GuiaNutricional/Btn_VolverGuia",
            "Panel_GuiaNutricional/PanelDetalle/TarjetaDetalle/Btn_Servir",
            "Panel_GuiaNutricional/PanelDetalle/TarjetaDetalle/Btn_CerrarDetalle",
            "Panel_DashboardMetas/Btn_VolverMetas",
            "Panel_Tutorial/TarjetaTutorial/Btn_Siguiente"
        };

        foreach (string ruta in rutas)
        {
            Transform t = canvas.Find(ruta);
            if (t == null)
            {
                sb.AppendLine(ruta + " -> no encontrado");
                continue;
            }

            Image img = t.GetComponent<Image>();
            if (img == null)
            {
                sb.AppendLine(ruta + " -> sin Image");
                continue;
            }

            Sprite sp = img.sprite;
            string borde = sp == null ? "n/a" : sp.border.ToString();

            sb.AppendLine(ruta + " sprite=" + (sp == null ? "NONE" : sp.name) +
                          " type=" + img.type + " borde=" + borde +
                          " ppu=" + img.pixelsPerUnitMultiplier);
        }

        return sb.ToString();
    }
}
