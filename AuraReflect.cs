using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;

/// <summary>Comprueba si TMP_Dropdown expone captionImage/itemImage y si OptionData lleva sprite.</summary>
public static class AuraReflect
{
    public static string Execute()
    {
        StringBuilder sb = new StringBuilder();

        foreach (FieldInfo f in typeof(TMP_Dropdown).GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            sb.AppendLine("campo " + f.FieldType.Name + " " + f.Name);
        }

        foreach (PropertyInfo p in typeof(TMP_Dropdown).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            sb.AppendLine("prop " + p.PropertyType.Name + " " + p.Name);
        }

        sb.AppendLine("--- OptionData");
        foreach (FieldInfo f in typeof(TMP_Dropdown.OptionData).GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            sb.AppendLine("campo " + f.FieldType.Name + " " + f.Name);
        }

        foreach (PropertyInfo p in typeof(TMP_Dropdown.OptionData).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            sb.AppendLine("prop " + p.PropertyType.Name + " " + p.Name);
        }

        return sb.ToString();
    }
}
