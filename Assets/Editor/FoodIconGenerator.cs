using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Herramienta temporal de Editor: genera los 14 iconos cenitales (512x512, fondo transparente)
/// a partir de los prefabs de Assets/3D y reemplaza los PNG de Assets/Sprites/IconosAlimentos
/// conservando el nombre de archivo, de modo que todas las referencias (dropdown del Panel_AR y
/// tarjetas de Panel_GuiaNutricional) siguen apuntando al mismo Sprite.
/// Menu: NutriAR / 7 Generar Iconos Cenitales.
/// </summary>
public static class FoodIconGenerator
{
    private const int Lado = 512;
    private const float Margen = 1.08f;
    private const int Capa = 31;
    private const string CarpetaIconos = "Assets/Sprites/IconosAlimentos";
    private const string ShaderOpaco = "Shader Graphs/Opaco";
    private const string RutaGrafoOpaco = "Assets/Shader/Opaco.shadergraph";

    /// <summary>Icono destino + prefab de origen (para los 10 ingredientes, el prefab COCIDO).</summary>
    private static readonly string[,] Iconos =
    {
        { "Icono_Pollo",              "Assets/3D/Pollo/Pollo_Cocido.prefab" },
        { "Icono_Res",                "Assets/3D/CarneRes/CarneRes_Cocida.prefab" },
        { "Icono_Cerdo",              "Assets/3D/CarneCerdo/CarneCerdo_Cocida.prefab" },
        { "Icono_FrijolNegro",        "Assets/3D/Frijol/Frijol_Cocido.prefab" },
        { "Icono_Lentejas",           "Assets/3D/Lentejas/Lentejas_Cocidas.prefab" },
        { "Icono_Garbanzos",          "Assets/3D/Garbanzo/Garbanzos_Cocidos.prefab" },
        { "Icono_Espinaca",           "Assets/3D/Espinaca/Espinaca_Cocida.prefab" },
        { "Icono_Zanahoria",          "Assets/3D/Zanahoria/Zanahoria_Cocida.prefab" },
        { "Icono_Papa",               "Assets/3D/Papa/Papa_Cocida.prefab" },
        { "Icono_Calabacita",         "Assets/3D/Calabaza/Calabaza_Cocida.prefab" },
        { "Icono_ArrozconFrijoles",   "Assets/3D/ArrozConFrijol/ArrozConFrijol.prefab" },
        { "Icono_PicadilloRes",       "Assets/3D/Picadillo/Picadillo.prefab" },
        { "Icono_GuisodePollo",       "Assets/3D/GuisadoPollo/GuisadoPollo.prefab" },
        { "Icono_LentejasconCerdo",   "Assets/3D/LentejasConCerdo/LentejasConCerdo.prefab" },
    };

    [MenuItem("NutriAR/7 Generar Iconos Cenitales")]
    public static void Execute()
    {
        var sb = new StringBuilder();
        var ci = CultureInfo.InvariantCulture;

        // --- snapshot de iluminacion de ambiente (se restaura al final) ---
        var modoAmbiente = RenderSettings.ambientMode;
        var luzAmbiente = RenderSettings.ambientLight;
        var cieloAmbiente = RenderSettings.ambientSkyColor;
        var ecuadorAmbiente = RenderSettings.ambientEquatorColor;
        var sueloAmbiente = RenderSettings.ambientGroundColor;
        var intensidadAmbiente = RenderSettings.ambientIntensity;

        // Luz neutra pareja: cenital + relleno, sin sombras.
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.62f, 0.62f, 0.62f, 1f);

        GameObject raiz = null;
        Camera camara = null;
        RenderTexture rt = null;
        Texture2D lienzo = null;
        Material materialOpaco = null;

        try
        {
            raiz = new GameObject("AuraIconRig");

            var luzCenital = new GameObject("LuzCenital").AddComponent<Light>();
            luzCenital.transform.SetParent(raiz.transform, false);
            luzCenital.type = LightType.Directional;
            luzCenital.transform.rotation = Quaternion.Euler(65f, -25f, 0f);
            luzCenital.color = new Color(1f, 0.98f, 0.95f);
            luzCenital.intensity = 1.15f;
            luzCenital.shadows = LightShadows.None;
            luzCenital.cullingMask = 1 << Capa;

            var luzRelleno = new GameObject("LuzRelleno").AddComponent<Light>();
            luzRelleno.transform.SetParent(raiz.transform, false);
            luzRelleno.type = LightType.Directional;
            luzRelleno.transform.rotation = Quaternion.Euler(35f, 155f, 0f);
            luzRelleno.color = new Color(0.95f, 0.97f, 1f);
            luzRelleno.intensity = 0.45f;
            luzRelleno.shadows = LightShadows.None;
            luzRelleno.cullingMask = 1 << Capa;

            var goCamara = new GameObject("CamaraCenital");
            goCamara.transform.SetParent(raiz.transform, false);
            camara = goCamara.AddComponent<Camera>();
            camara.orthographic = true;
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = new Color(0f, 0f, 0f, 0f);
            camara.nearClipPlane = 0.01f;
            camara.farClipPlane = 50f;
            camara.cullingMask = 1 << Capa;
            camara.allowHDR = false;
            camara.allowMSAA = true;
            camara.depth = -100f;

            rt = new RenderTexture(Lado, Lado, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            rt.antiAliasing = 4;
            rt.Create();
            camara.targetTexture = rt;

            lienzo = new Texture2D(Lado, Lado, TextureFormat.RGBA32, false, false);

            // Un solo material temporal (Shader Graphs/Opaco) para las 14 capturas: el Shader Graph
            // Transparency deja el icono semitransparente, asi que solo durante la foto se sustituye
            // por una superficie opaca que conserva las 4 texturas del prefab. Los materiales de
            // Assets/3D nunca se modifican (solo se leen sus texturas).
            materialOpaco = CrearMaterialOpaco(materialOpaco);
            if (materialOpaco == null)
            {
                Debug.LogError("[FoodIconGenerator] No se encontro el shader '" + ShaderOpaco +
                               "'. Revisa " + RutaGrafoOpaco + " y vuelve a ejecutar el menu.");
                return;
            }

            int generados = 0;

            for (int i = 0; i < Iconos.GetLength(0); i++)
            {
                string nombreIcono = Iconos[i, 0];
                string rutaPrefab = Iconos[i, 1];

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(rutaPrefab);
                if (prefab == null)
                {
                    sb.AppendLine("ICONO FALTA PREFAB " + rutaPrefab);
                    continue;
                }

                var inst = PrefabUtility.InstantiatePrefab(prefab, raiz.transform) as GameObject;
                if (inst == null)
                {
                    sb.AppendLine("ICONO NO INSTANCIA " + rutaPrefab);
                    continue;
                }

                inst.transform.localPosition = Vector3.zero;
                PonerCapa(inst, Capa);

                // Sustituye los materiales de la instancia temporal por el material Opaco.
                AplicarMaterialOpaco(inst, materialOpaco);

                Bounds caja = CajaDe(inst);
                float ancho = Mathf.Max(caja.size.x, caja.size.z);

                // Vista cenital ortografica: la camara mira hacia abajo sobre el centro del modelo.
                camara.transform.position = new Vector3(caja.center.x, caja.max.y + 2f, caja.center.z);
                camara.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                camara.orthographicSize = Mathf.Max(ancho * 0.5f * Margen, 0.01f);

                camara.Render();

                RenderTexture.active = rt;
                lienzo.ReadPixels(new Rect(0f, 0f, Lado, Lado), 0, 0);
                lienzo.Apply();
                RenderTexture.active = null;

                byte[] png = lienzo.EncodeToPNG();
                string rutaPng = CarpetaIconos + "/" + nombreIcono + ".png";
                File.WriteAllBytes(Path.GetFullPath(rutaPng), png);

                // Caja de pixeles opacos: comprueba centrado, margen y fondo transparente.
                int x0 = Lado, y0 = Lado, x1 = -1, y1 = -1, opacos = 0;
                Color32[] pixeles = lienzo.GetPixels32();
                for (int p = 0; p < pixeles.Length; p++)
                {
                    if (pixeles[p].a < 8) continue;
                    opacos++;
                    int px = p % Lado;
                    int py = p / Lado;
                    if (px < x0) x0 = px;
                    if (px > x1) x1 = px;
                    if (py < y0) y0 = py;
                    if (py > y1) y1 = py;
                }

                sb.AppendLine(nombreIcono + " <- " + prefab.name +
                              " opacos=" + opacos +
                              " caja=(" + x0 + "," + y0 + ")-(" + x1 + "," + y1 + ")" +
                              " centro=(" + ((x0 + x1) / 2) + "," + ((y0 + y1) / 2) + ")" +
                              " esquinaA=" + pixeles[0].a +
                              " bytes=" + png.Length);

                UnityEngine.Object.DestroyImmediate(inst);
                generados++;
            }

            sb.AppendLine("ICONOS generados=" + generados + " de " + Iconos.GetLength(0));
            Debug.Log(sb.ToString());
        }
        finally
        {
            if (camara != null) camara.targetTexture = null;
            if (rt != null) { rt.Release(); UnityEngine.Object.DestroyImmediate(rt); }
            if (lienzo != null) UnityEngine.Object.DestroyImmediate(lienzo);
            if (raiz != null) UnityEngine.Object.DestroyImmediate(raiz);
            if (materialOpaco != null) UnityEngine.Object.DestroyImmediate(materialOpaco);

            RenderSettings.ambientMode = modoAmbiente;
            RenderSettings.ambientLight = luzAmbiente;
            RenderSettings.ambientSkyColor = cieloAmbiente;
            RenderSettings.ambientEquatorColor = ecuadorAmbiente;
            RenderSettings.ambientGroundColor = sueloAmbiente;
            RenderSettings.ambientIntensity = intensidadAmbiente;
        }

        AssetDatabase.Refresh();

        // Reimporta los 14 PNG como Sprite (2D and UI) con alpha is transparency.
        for (int i = 0; i < Iconos.GetLength(0); i++)
        {
            string rutaPng = CarpetaIconos + "/" + Iconos[i, 0] + ".png";
            var importer = AssetImporter.GetAtPath(rutaPng) as TextureImporter;
            if (importer == null) { Debug.LogWarning("[FoodIconGenerator] Sin importer: " + rutaPng); continue; }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = Lado;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        Debug.Log("[FoodIconGenerator] 14 iconos reemplazados en " + CarpetaIconos + " con import como Sprite.");
    }

    /// <summary>
    /// Crea el material temporal (Shader Graphs/Opaco). Si Shader.Find no lo resuelve todavia
    /// (grafo recien importado), lo toma del sub-asset del propio .shadergraph.
    /// </summary>
    private static Material CrearMaterialOpaco(Material existente)
    {
        if (existente != null) return existente;

        var shader = Shader.Find(ShaderOpaco);
        if (shader == null)
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(RutaGrafoOpaco))
            {
                if (asset is Shader encontrado) { shader = encontrado; break; }
            }
        }

        if (shader == null) return null;
        return new Material(shader) { name = "AuraIconOpaco_Temporal", hideFlags = HideFlags.DontSave };
    }

    /// <summary>
    /// Copia las 4 texturas (_BaseTex, _NormalTex, _MetallicTex, _RoughTex) del material original de la
    /// instancia al material temporal Opaco (null si el prefab no la tiene, para no arrastrar texturas
    /// del alimento anterior) y asigna ese material a todos los Renderer de la instancia temporal.
    /// </summary>
    private static void AplicarMaterialOpaco(GameObject inst, Material materialOpaco)
    {
        var renderers = inst.GetComponentsInChildren<Renderer>(true);

        // Material donante: el primero con _BaseTex (todos los prefabs usan Transparency).
        Material original = null;
        foreach (var r in renderers)
        {
            foreach (var m in r.sharedMaterials)
            {
                if (m != null && m.HasProperty("_BaseTex")) { original = m; break; }
            }
            if (original != null) break;
        }

        materialOpaco.SetTexture("_BaseTex", original != null ? original.GetTexture("_BaseTex") : null);
        materialOpaco.SetTexture("_NormalTex", original != null ? original.GetTexture("_NormalTex") : null);
        materialOpaco.SetTexture("_MetallicTex", original != null ? original.GetTexture("_MetallicTex") : null);
        materialOpaco.SetTexture("_RoughTex", original != null ? original.GetTexture("_RoughTex") : null);

        foreach (var r in renderers)
        {
            int ranuras = Mathf.Max(1, r.sharedMaterials.Length);
            var relleno = new Material[ranuras];
            for (int s = 0; s < ranuras; s++) relleno[s] = materialOpaco;
            r.sharedMaterials = relleno;
        }
    }

    private static void PonerCapa(GameObject go, int capa)
    {
        go.layer = capa;
        foreach (Transform hijo in go.transform) PonerCapa(hijo.gameObject, capa);
    }

    private static Bounds CajaDe(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.one * 0.1f);

        Bounds total = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) total.Encapsulate(renderers[i].bounds);
        return total;
    }
}
