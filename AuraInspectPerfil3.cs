using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Volcado completo del arbol del Panel_Perfil y de los objetos llamados Btn_Volver.</summary>
public static class AuraInspectPerfil3
{
    public static string Execute()
    {
        StringBuilder sb = new StringBuilder();

        GameObject canvasGo = GameObject.Find("Canvas");
        sb.AppendLine("Hijos del Canvas:");
        foreach (Transform t in canvasGo.transform)
        {
            sb.AppendLine("   " + t.name + " activo " + t.gameObject.activeSelf + " id " + t.GetInstanceID());
        }

        Transform[] todos = Object.FindObjectsOfType<Transform>(true);

        sb.AppendLine("Objetos 'Btn_Volver' en la escena:");
        foreach (Transform t in todos)
        {
            if (t.name == "Btn_Volver")
            {
                RectTransform rt = t as RectTransform;
                sb.AppendLine("   " + Ruta(t) + " id " + t.GetInstanceID() +
                              " anchor " + rt.anchorMin + "/" + rt.anchorMax + " pivot " + rt.pivot +
                              " pos " + rt.anchoredPosition + " size " + rt.sizeDelta + " " + rt.rect +
                              " activeSelf " + t.gameObject.activeSelf);
            }
        }

        sb.AppendLine("Objetos 'Panel_Perfil' en la escena:");

        foreach (Transform t in todos)
        {
            if (t.name == "Panel_Perfil")
            {
                RectTransform rt = t as RectTransform;
                sb.AppendLine("   " + Ruta(t) + " id " + t.GetInstanceID() + " activeSelf " + t.gameObject.activeSelf +
                              " rect " + rt.rect + " localScale " + rt.localScale);

                foreach (Component c in t.GetComponents<Component>())
                {
                    sb.AppendLine("      comp " + c.GetType().Name);
                }
            }
        }

        Transform perfil = canvasGo.transform.Find("Panel_Perfil");
        sb.AppendLine("Hijos de Canvas/Panel_Perfil:");

        foreach (Transform hijo in perfil)
        {
            RectTransform rt = hijo as RectTransform;
            sb.AppendLine("   " + hijo.name + " id " + hijo.GetInstanceID() +
                          " anchor " + rt.anchorMin + "/" + rt.anchorMax + " pivot " + rt.pivot +
                          " pos " + rt.anchoredPosition + " size " + rt.sizeDelta + " rect " + rt.rect +
                          " activeSelf " + hijo.gameObject.activeSelf);
        }

        return sb.ToString();
    }

    private static string Ruta(Transform t)
    {
        string ruta = t.name;

        while (t.parent != null)
        {
            t = t.parent;
            ruta = t.name + "/" + ruta;
        }

        return ruta;
    }
}
