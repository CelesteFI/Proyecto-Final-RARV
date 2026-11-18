using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Paso 1 del pase final: genera los 4 sprites AR (guia / fijado x circular / cuadrado),
/// crea la raiz "ArosAR" con los 4 Image pre-creados, elimina el antiguo aro de 32 esferas
/// y cablea PlateAnchorManager.
/// </summary>
public static class AuraArosBuild
{
    private const string Carpeta = "Assets/Sprites/ArosAR";
    private const int Lado = 512;
    private const float RadioPintado = 250f;

    public static void Execute()
    {
        AssetDatabase.Refresh();

        if (!AssetDatabase.IsValidFolder("Assets/Sprites"))
        {
            AssetDatabase.CreateFolder("Assets", "Sprites");
        }

        if (!AssetDatabase.IsValidFolder(Carpeta))
        {
            AssetDatabase.CreateFolder("Assets/Sprites", "ArosAR");
        }

        Sprite guiaCircular = GenerarSprite("AroGuiaCircular", false, false);
        Sprite fijadoCircular = GenerarSprite("AroFijadoCircular", true, false);
        Sprite guiaCuadrado = GenerarSprite("AroGuiaCuadrado", false, true);
        Sprite fijadoCuadrado = GenerarSprite("AroFijadoCuadrado", true, true);

        GameObject xrOrigin = GameObject.Find("XR Origin");
        if (xrOrigin == null)
        {
            Debug.LogError("[AuraArosBuild] No se encontro 'XR Origin'.");
            return;
        }

        // Se retira el aro de 32 esferas: lo sustituyen los 4 aros con sprite.
        Transform viejo = xrOrigin.transform.Find("AroReferencia");
        if (viejo != null)
        {
            Object.DestroyImmediate(viejo.gameObject);
            Debug.Log("[AuraArosBuild] AroReferencia (32 esferas) eliminado: lo sustituyen los 4 aros AR.");
        }

        // Raiz comun: Canvas en espacio de mundo de 1x1 unidad. Su escala fija la de los 4 aros.
        GameObject raizGo = new GameObject("ArosAR", typeof(RectTransform));
        raizGo.transform.SetParent(xrOrigin.transform, false);

        RectTransform raiz = (RectTransform)raizGo.transform;
        raiz.localPosition = Vector3.zero;
        raiz.localRotation = Quaternion.identity;
        raiz.localScale = Vector3.one;
        raiz.sizeDelta = new Vector2(1f, 1f);
        raiz.pivot = new Vector2(0.5f, 0.5f);

        Canvas canvas = raizGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 5;

        GameObject guiaCirc = CrearAro(raiz, "spriteGuiaCircular", guiaCircular);
        GameObject fijCirc = CrearAro(raiz, "spriteFijadoCircular", fijadoCircular);
        GameObject guiaCuad = CrearAro(raiz, "spriteGuiaCuadrado", guiaCuadrado);
        GameObject fijCuad = CrearAro(raiz, "spriteFijadoCuadrado", fijadoCuadrado);

        // PlateAnchorManager: los 4 aros, su raiz y el radio pintado (250/256).
        PlateAnchorManager ancla = xrOrigin.GetComponent<PlateAnchorManager>();
        if (ancla != null)
        {
            SerializedObject so = new SerializedObject(ancla);
            Asignar(so, "spriteGuiaCircular", guiaCirc);
            Asignar(so, "spriteFijadoCircular", fijCirc);
            Asignar(so, "spriteGuiaCuadrado", guiaCuad);
            Asignar(so, "spriteFijadoCuadrado", fijCuad);
            Asignar(so, "arosRaiz", raiz);
            Asignar(so, "radioPintadoAro", RadioPintado / (Lado * 0.5f));
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        else
        {
            Debug.LogError("[AuraArosBuild] 'XR Origin' no tiene PlateAnchorManager.");
        }

        EditorUtility.SetDirty(raizGo);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        Debug.Log("[AuraArosBuild] 4 aros AR creados bajo '/XR Origin/ArosAR' (inactivos) y cableados.");
    }

    private static GameObject CrearAro(RectTransform padre, string nombre, Sprite sprite)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);

        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Simple;
        img.preserveAspect = true;
        img.raycastTarget = false;
        img.color = Color.white;

        go.SetActive(false);
        return go;
    }

    private static Sprite GenerarSprite(string nombre, bool fijado, bool cuadrado)
    {
        string ruta = Carpeta + "/" + nombre + ".png";
        Texture2D tex = new Texture2D(Lado, Lado, TextureFormat.RGBA32, false);
        Color32[] pixeles = new Color32[Lado * Lado];

        float centro = Lado * 0.5f;
        float mediaAnchura = fijado ? 9f : 6.5f;

        // Fijado: verde menta solido. Guia dinamica: azul claro punteado.
        Color color = fijado ? new Color(0.24f, 0.92f, 0.62f, 1f) : new Color(0.40f, 0.78f, 1f, 0.95f);

        for (int y = 0; y < Lado; y++)
        {
            for (int x = 0; x < Lado; x++)
            {
                float dx = (x + 0.5f) - centro;
                float dy = (y + 0.5f) - centro;

                float distancia;
                float avance;

                if (cuadrado)
                {
                    float chebyshev = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));
                    distancia = Mathf.Abs(chebyshev - RadioPintado);
                    avance = (Mathf.Atan2(dy, dx) + Mathf.PI) / (2f * Mathf.PI);
                }
                else
                {
                    distancia = Mathf.Abs(Mathf.Sqrt((dx * dx) + (dy * dy)) - RadioPintado);
                    avance = (Mathf.Atan2(dy, dx) + Mathf.PI) / (2f * Mathf.PI);
                }

                // Borde suavizado de ~1.5 px.
                float alfa = Mathf.Clamp01(((mediaAnchura - distancia) / 1.5f) + 0.5f);

                if (!fijado)
                {
                    // Punteado: 24 tramos con 4 px de separacion proporcional.
                    float tramo = Mathf.Repeat(avance * 24f, 1f);
                    alfa *= tramo < 0.62f ? 1f : 0.08f;
                }

                pixeles[(y * Lado) + x] = new Color32(
                    (byte)(color.r * 255f),
                    (byte)(color.g * 255f),
                    (byte)(color.b * 255f),
                    (byte)(Mathf.Clamp01(alfa) * color.a * 255f));
            }
        }

        tex.SetPixels32(pixeles);
        tex.Apply();

        File.WriteAllBytes(ruta, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(ruta);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = Lado;      // 512 px = 1 unidad de mundo
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
        Debug.Log("[AuraArosBuild] Sprite generado: " + ruta);
        return sprite;
    }

    private static void Asignar(SerializedObject so, string campo, Object valor)
    {
        SerializedProperty prop = so.FindProperty(campo);
        if (prop == null)
        {
            Debug.LogError("[AuraArosBuild] Campo no encontrado: " + campo);
            return;
        }

        prop.objectReferenceValue = valor;
    }

    private static void Asignar(SerializedObject so, string campo, float valor)
    {
        SerializedProperty prop = so.FindProperty(campo);
        if (prop == null)
        {
            Debug.LogError("[AuraArosBuild] Campo no encontrado: " + campo);
            return;
        }

        prop.floatValue = valor;
    }
}
