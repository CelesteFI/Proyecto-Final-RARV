using System.Text;
using UnityEngine;

public static class AuraPing
{
    public static string Execute()
    {
        StringBuilder sb = new StringBuilder();

        GameObject guia = Buscar("Panel_GuiaNutricional");
        Transform card = guia.transform.Find("PanelDetalle/TarjetaDetalle");

        // Marcadores neutros: el panel se rellena solo al abrirse desde una tarjeta.
        Escribir(card, "Titulo", "Alimento", sb);
        Escribir(card, "Estado", "Crudo \u00b7 valores por 100 g", sb);
        Escribir(card, "KcalNumero", "--", sb);
        Escribir(card, "ProtNumero", "--", sb);
        Escribir(card, "CarboNumero", "--", sb);
        Escribir(card, "GrasaNumero", "--", sb);
        Escribir(card, "BeneficioTexto", "--", sb);
        Escribir(card, "SugerenciaTexto", "--", sb);

        return sb.ToString();
    }

    private static void Escribir(Transform card, string nombre, string texto, StringBuilder sb)
    {
        Transform t = card.Find(nombre);
        if (t == null) { sb.AppendLine("FALTA " + nombre); return; }

        TMPro.TMP_Text tmp = t.GetComponent<TMPro.TMP_Text>();
        if (tmp == null) { sb.AppendLine("SIN TMP " + nombre); return; }

        tmp.text = texto;
        UnityEditor.EditorUtility.SetDirty(t);
        sb.AppendLine(nombre + " = '" + texto + "'");
    }

    private static GameObject Buscar(string nombre)
    {
        GameObject[] todos = Resources.FindObjectsOfTypeAll<GameObject>();

        for (int i = 0; i < todos.Length; i++)
        {
            if (todos[i].name == nombre && todos[i].scene.IsValid())
            {
                return todos[i];
            }
        }

        return null;
    }
}
