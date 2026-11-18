using UnityEngine;

/// <summary>Auxiliar de revision: enciende o apaga un panel del Canvas para capturarlo.</summary>
public static class AuraPeek
{
    private const string Panel = "Panel_GuiaNutricional";
    private const bool Activo = true;

    public static void Execute()
    {
        GameObject canvas = GameObject.Find("Canvas");
        Transform panel = canvas.transform.Find(Panel);

        if (panel == null)
        {
            Debug.LogError("[AuraPeek] No existe " + Panel);
            return;
        }

        if (Activo)
        {
            for (int i = 0; i < canvas.transform.childCount; i++)
            {
                GameObject hijo = canvas.transform.GetChild(i).gameObject;

                if (hijo == panel.gameObject)
                {
                    continue;
                }

                if (hijo.activeSelf)
                {
                    hijo.SetActive(false);
                }
            }
        }

        panel.gameObject.SetActive(Activo);
        Canvas.ForceUpdateCanvases();
        Debug.Log("[AuraPeek] " + Panel + " activo=" + Activo);
    }
}
