using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>Lista los campos de referencia serializados y marca los que quedaron en None.</summary>
public static class AuraCheck
{
    public static string Execute()
    {
        StringBuilder sb = new StringBuilder();

        foreach (MonoBehaviour mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            string tipo = mb.GetType().Name;

            if (tipo != "BiometricProfile" && tipo != "ARUIManager" && tipo != "MenuController" &&
                tipo != "PlateAnchorManager" && tipo != "PlateShapeController" && tipo != "MenuLateralARController")
            {
                continue;
            }

            sb.AppendLine("== " + tipo + " en '" + mb.gameObject.name + "'");

            SerializedObject so = new SerializedObject(mb);
            SerializedProperty it = so.GetIterator();

            while (it.NextVisible(true))
            {
                if (it.propertyType != SerializedPropertyType.ObjectReference)
                {
                    continue;
                }

                string valor = it.objectReferenceValue == null ? "<<< NONE >>>" : it.objectReferenceValue.name;
                sb.AppendLine("  " + it.propertyPath + " = " + valor);
            }
        }

        return sb.ToString();
    }
}
