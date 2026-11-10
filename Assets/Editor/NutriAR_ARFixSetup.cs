using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Paso 7 del armado de NutriAR: puesta a punto de la realidad aumentada para el APK Android.
/// - Feed de la camara: anade el ARBackgroundRendererFeature que URP exige a los renderers del
///   proyecto (sin el no se dibuja el video de ARCore) y deja la camara sin tinte ni post-proceso.
/// - Catalogo: 24 slots con modelo + color base crudo/cocido del cilindro de prueba.
/// - Aro punteado de referencia en la mesa, oculto hasta fijar el plato.
/// - Etiqueta de gramos con fondo oscuro semitransparente y texto en negritas.
/// - Transicion Carga -> Splash sin encimarse.
/// Todo se crea en Edit Time (cero instanciacion en runtime), es idempotente por nombre y
/// queda protegido con Undo en un unico grupo.
/// </summary>
public static class NutriAR_ARFixSetup
{
    private const string MenuPath = "NutriAR/7. Correcciones AR (Camara, Aro, Colores)";
    private const string UndoLabel = "NutriAR: Correcciones AR";

    private const string ManagersName = "AR_Managers";
    private const string MoldeName = "MoldeHolograma";
    private const string ModelosName = "ModelosHolograma";
    private const string AroName = "AroReferencia";
    private const string PanelARName = "Panel_AR";
    private const string PanelInferiorName = "PanelInferior";
    private const string PanelSplashName = "Panel_Splash";
    private const string PanelCargaName = "Panel_Carga";
    private const string BtnFijarARName = "btnFijarPlato";

    private const string CarpetaMateriales = "Assets/Materials";
    private const string RutaMaterialMolde = CarpetaMateriales + "/MoldeHolograma.mat";
    private const string RutaMaterialAro = CarpetaMateriales + "/AroReferencia.mat";

    private const string ShaderLit = "Universal Render Pipeline/Lit";
    private const string ShaderUnlit = "Universal Render Pipeline/Unlit";

    private const string MobileRenderer = "Assets/Settings/Mobile_Renderer.asset";
    private const string PCRenderer = "Assets/Settings/PC_Renderer.asset";

    private const int PuntosAro = 32;
    private const float TamanoPuntoAro = 0.04f;
    private const float AltoPanelInferior = 260f;

    private static readonly Color ColorAro = new Color(0.20f, 0.90f, 0.95f, 1f);
    private static readonly Color FondoEtiqueta = new Color(0f, 0f, 0f, 0.82f);
    private static readonly Color TextoBlanco = Color.white;

    private static readonly string[] AlimentosBase =
    {
        "Arroz", "Pasta", "Pollo", "Res", "Cerdo", "Salmon", "Atun", "Huevo", "Papa", "Zanahoria"
    };

    private static readonly Color[] ColoresCrudo =
    {
        new Color(0.93f, 0.90f, 0.82f, 1f), // Arroz
        new Color(0.90f, 0.78f, 0.45f, 1f), // Pasta
        new Color(0.20f, 0.62f, 0.30f, 1f), // Pollo: verde intenso en crudo
        new Color(0.65f, 0.20f, 0.22f, 1f), // Res
        new Color(0.85f, 0.55f, 0.55f, 1f), // Cerdo
        new Color(0.95f, 0.50f, 0.40f, 1f), // Salmon
        new Color(0.80f, 0.30f, 0.32f, 1f), // Atun
        new Color(0.97f, 0.93f, 0.85f, 1f), // Huevo
        new Color(0.85f, 0.75f, 0.55f, 1f), // Papa
        new Color(0.90f, 0.50f, 0.18f, 1f)  // Zanahoria
    };

    private static readonly Color[] ColoresCocido =
    {
        new Color(0.98f, 0.97f, 0.94f, 1f), // Arroz
        new Color(0.96f, 0.88f, 0.62f, 1f), // Pasta
        new Color(0.55f, 0.80f, 0.55f, 1f), // Pollo: verde pastel en cocido
        new Color(0.45f, 0.24f, 0.18f, 1f), // Res
        new Color(0.72f, 0.48f, 0.40f, 1f), // Cerdo
        new Color(0.90f, 0.62f, 0.50f, 1f), // Salmon
        new Color(0.65f, 0.42f, 0.40f, 1f), // Atun
        new Color(0.99f, 0.96f, 0.80f, 1f), // Huevo
        new Color(0.95f, 0.88f, 0.65f, 1f), // Papa
        new Color(0.95f, 0.62f, 0.30f, 1f)  // Zanahoria
    };

    private static readonly string[] PlatosCompuestos =
    {
        "Guiso de Pollo", "Sopa de Verduras", "Arroz con Pollo", "Ensalada Mixta"
    };

    private static readonly Color[] ColoresPlatos =
    {
        new Color(0.80f, 0.62f, 0.32f, 1f),
        new Color(0.55f, 0.68f, 0.35f, 1f),
        new Color(0.88f, 0.76f, 0.45f, 1f),
        new Color(0.35f, 0.70f, 0.40f, 1f)
    };

    [MenuItem(MenuPath)]
    public static void Ejecutar()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        GameObject managers = FindInScene(ManagersName);
        Transform molde = FindDescendienteGlobal(MoldeName);

        if (canvas == null || managers == null || molde == null)
        {
            Debug.LogError("[NutriAR] Faltan el Canvas, '" + ManagersName + "' o '" + MoldeName +
                           "'. Ejecuta el paso 6 antes.");
            return;
        }

        MenuController menu = managers.GetComponent<MenuController>();

        if (menu == null)
        {
            Debug.LogError("[NutriAR] '" + ManagersName + "' no tiene MenuController. Ejecuta el paso 6.");
            return;
        }

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(UndoLabel);

        int featuresAnadidas = AsegurarRenderFeatureAR(MobileRenderer) + AsegurarRenderFeatureAR(PCRenderer);
        AsegurarCamaraAR();

        Material materialMolde = AsegurarMaterial(RutaMaterialMolde, ShaderLit, new Color(0.90f, 0.90f, 0.90f, 1f), 0.25f);
        Material materialAro = AsegurarMaterial(RutaMaterialAro, ShaderUnlit, ColorAro, 0f);

        AsignarMaterialMolde(molde, materialMolde);

        Transform xrOrigin = molde.parent != null ? molde.parent : molde;
        Transform aro = AsegurarAro(xrOrigin, materialAro);
        OcultarMolde(molde);

        TextMeshProUGUI textoGramos = ConstruirEtiquetaGramos(canvas.transform);
        FoodCatalog catalogo = ConstruirCatalogo(managers, molde);

        CablearOpciones(canvas.transform);
        CablearAnclaje(managers, molde, aro, menu);
        CablearEscala(managers, molde, catalogo, textoGramos);
        CablearSplash(canvas.transform);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

        Debug.Log("[NutriAR] Correcciones AR aplicadas: ARBackgroundRendererFeature en " + featuresAnadidas +
                  " renderer(s), camara sin post-proceso, aro punteado de 32 puntos, catalogo de 24 slots con " +
                  "color crudo/cocido, etiqueta de gramos con fondo oscuro y transicion Carga -> Splash sin " +
                  "encimarse. Ctrl+Z revierte todo en un solo paso.", canvas);
    }

    // ------------------------------------------------- renderer feature de AR (feed de camara)

    /// <summary>
    /// Sin el ARBackgroundRendererFeature en el renderer de URP no se dibuja la textura de ARCore:
    /// la camara muestra un plano de color solido en lugar del video real. Devuelve 1 si lo anadio.
    /// </summary>
    private static int AsegurarRenderFeatureAR(string rutaRenderer)
    {
        ScriptableRendererData datos = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(rutaRenderer);

        if (datos == null)
        {
            Debug.LogWarning("[NutriAR] No se encontro el renderer '" + rutaRenderer + "'.");
            return 0;
        }

        List<ScriptableRendererFeature> features = datos.rendererFeatures;

        if (features != null)
        {
            for (int i = 0; i < features.Count; i++)
            {
                if (features[i] is ARBackgroundRendererFeature)
                {
                    return 0;
                }
            }
        }

        ARBackgroundRendererFeature feature = ScriptableObject.CreateInstance<ARBackgroundRendererFeature>();
        feature.name = "AR Background Renderer Feature";

        AssetDatabase.AddObjectToAsset(feature, datos);
        datos.rendererFeatures.Add(feature);
        datos.SetDirty();

        // ValidateRendererFeatures() reconstruye el mapa interno (local file ids) que URP usa
        // para reconectar los renderer features tras cada recarga de scripts.
        MethodInfo validar = typeof(ScriptableRendererData).GetMethod("ValidateRendererFeatures",
                                                                    BindingFlags.Instance | BindingFlags.NonPublic);

        if (validar != null)
        {
            validar.Invoke(datos, null);
        }

        EditorUtility.SetDirty(datos);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(rutaRenderer, ImportAssetOptions.ForceUpdate);

        Debug.Log("[NutriAR] AR Background Renderer Feature anadido a '" + rutaRenderer + "'.");
        return 1;
    }

    /// <summary>Deja la camara AR sin tinte: material nativo de AR Foundation, fondo negro y sin post-proceso.</summary>
    private static void AsegurarCamaraAR()
    {
        Transform camara = FindDescendienteGlobal("Main Camera");

        if (camara == null)
        {
            Debug.LogWarning("[NutriAR] No se encontro 'Main Camera': revisa la camara AR a mano.");
            return;
        }

        Camera camaraComponente = camara.GetComponent<Camera>();

        if (camaraComponente != null)
        {
            Undo.RecordObject(camaraComponente, UndoLabel + ": camara");
            camaraComponente.clearFlags = CameraClearFlags.SolidColor;
            camaraComponente.backgroundColor = Color.black;
            EditorUtility.SetDirty(camaraComponente);
        }

        ARCameraBackground fondo = camara.GetComponent<ARCameraBackground>();

        if (fondo != null)
        {
            Undo.RecordObject(fondo, UndoLabel + ": fondo AR");
            // Material por defecto del proveedor: reproduce el video con sus colores reales.
            fondo.useCustomMaterial = false;
            EditorUtility.SetDirty(fondo);
        }

        UniversalAdditionalCameraData datosURP = camara.GetComponent<UniversalAdditionalCameraData>();

        if (datosURP != null)
        {
            Undo.RecordObject(datosURP, UndoLabel + ": datos URP de la camara");
            // Ningun volumen global puede tintar el video de la camara AR.
            datosURP.renderPostProcessing = false;
            EditorUtility.SetDirty(datosURP);
        }
    }

    // ------------------------------------------------------------------- materiales

    private static Material AsegurarMaterial(string ruta, string nombreShader, Color color, float suavizado)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(ruta);

        if (material != null)
        {
            return material;
        }

        Shader shader = Shader.Find(nombreShader);

        if (shader == null)
        {
            Debug.LogError("[NutriAR] No se encontro el shader '" + nombreShader + "'.");
            return null;
        }

        if (!AssetDatabase.IsValidFolder(CarpetaMateriales))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }

        material = new Material(shader);
        material.name = System.IO.Path.GetFileNameWithoutExtension(ruta);

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", suavizado);
        }

        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", 0f);
        }

        AssetDatabase.CreateAsset(material, ruta);
        AssetDatabase.SaveAssets();

        return material;
    }

    private static void AsignarMaterialMolde(Transform molde, Material material)
    {
        if (material == null)
        {
            return;
        }

        MeshRenderer renderer = molde.GetComponent<MeshRenderer>();

        if (renderer == null)
        {
            return;
        }

        Undo.RecordObject(renderer, UndoLabel + ": material del molde");
        renderer.sharedMaterial = material;
        EditorUtility.SetDirty(renderer);
    }

    // ------------------------------------------------------------------------ aro

    private static Transform AsegurarAro(Transform padre, Material materialAro)
    {
        Transform aro = padre.Find(AroName);

        if (aro == null)
        {
            var go = new GameObject(AroName);
            Undo.RegisterCreatedObjectUndo(go, UndoLabel + ": " + AroName);
            go.transform.SetParent(padre, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            aro = go.transform;
        }

        for (int i = 0; i < PuntosAro; i++)
        {
            string nombre = "Punto_" + i.ToString("00");
            Transform punto = aro.Find(nombre);

            if (punto == null)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = nombre;
                Undo.RegisterCreatedObjectUndo(go, UndoLabel + ": " + nombre);
                go.transform.SetParent(aro, false);

                Collider colisionador = go.GetComponent<Collider>();

                if (colisionador != null)
                {
                    Undo.DestroyObjectImmediate(colisionador);
                }

                punto = go.transform;
            }

            float angulo = (Mathf.PI * 2f * i) / PuntosAro;
            punto.localPosition = new Vector3(Mathf.Cos(angulo), 0f, Mathf.Sin(angulo));
            punto.localRotation = Quaternion.identity;
            punto.localScale = Vector3.one * TamanoPuntoAro;

            MeshRenderer renderer = punto.GetComponent<MeshRenderer>();

            if (renderer == null)
            {
                continue;
            }

            Undo.RecordObject(renderer, UndoLabel + ": material del punto");
            renderer.sharedMaterial = materialAro;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            EditorUtility.SetDirty(renderer);
        }

        if (aro.gameObject.activeSelf)
        {
            Undo.RecordObject(aro.gameObject, UndoLabel + ": ocultar aro");
            aro.gameObject.SetActive(false);
            EditorUtility.SetDirty(aro.gameObject);
        }

        return aro;
    }

    /// <summary>En la escena el molde arranca oculto: solo se muestra al fijar el plato.</summary>
    private static void OcultarMolde(Transform molde)
    {
        if (!molde.gameObject.activeSelf)
        {
            return;
        }

        Undo.RecordObject(molde.gameObject, UndoLabel + ": ocultar molde");
        molde.gameObject.SetActive(false);
        EditorUtility.SetDirty(molde.gameObject);
    }

    // ------------------------------------------------------------ etiqueta de gramos

    private static TextMeshProUGUI ConstruirEtiquetaGramos(Transform canvas)
    {
        Transform panelAR = FindDescendiente(canvas, PanelARName);

        if (panelAR == null)
        {
            Debug.LogWarning("[NutriAR] No se encontro '" + PanelARName + "': la etiqueta de gramos queda sin crear.");
            return null;
        }

        Transform inferior = FindDescendiente(panelAR, PanelInferiorName);

        if (inferior == null)
        {
            Debug.LogWarning("[NutriAR] No se encontro '" + PanelInferiorName + "'.");
            return null;
        }

        var rectInferior = (RectTransform)inferior;

        if (!Mathf.Approximately(rectInferior.sizeDelta.y, AltoPanelInferior))
        {
            Undo.RecordObject(rectInferior, UndoLabel + ": alto de la banda inferior");
            rectInferior.sizeDelta = new Vector2(rectInferior.sizeDelta.x, AltoPanelInferior);
            EditorUtility.SetDirty(rectInferior);
        }

        RectTransform etiqueta = AsegurarRect(inferior, "PanelTextGramos");
        Undo.RecordObject(etiqueta, UndoLabel + ": panel de gramos");
        etiqueta.anchorMin = new Vector2(0.5f, 0f);
        etiqueta.anchorMax = new Vector2(0.5f, 0f);
        etiqueta.pivot = new Vector2(0.5f, 0f);
        etiqueta.anchoredPosition = new Vector2(0f, 182f);
        etiqueta.sizeDelta = new Vector2(520f, 68f);
        EditorUtility.SetDirty(etiqueta);

        AsegurarImagen(etiqueta.gameObject, FondoEtiqueta, false);

        TextMeshProUGUI texto = AsegurarTexto(etiqueta, "TextoGramos", "100 g", 40f, TextAlignmentOptions.Center);
        Undo.RecordObject(texto, UndoLabel + ": texto de gramos");
        texto.fontStyle = FontStyles.Bold;
        texto.color = TextoBlanco;
        Stretch(texto.rectTransform, 8f, 8f, 0f, 0f);
        EditorUtility.SetDirty(texto);

        etiqueta.SetAsLastSibling();
        return texto;
    }

    // ------------------------------------------------------------------- catalogo

    private static FoodCatalog ConstruirCatalogo(GameObject managers, Transform molde)
    {
        Transform contenedor = AsegurarHijoTransform(molde, ModelosName);
        FoodCatalog catalogo = EnsureComponent<FoodCatalog>(managers);

        var slots = new FoodModelSlot[FoodCatalog.TotalSlots];
        int indice = 0;

        for (int i = 0; i < AlimentosBase.Length; i++)
        {
            indice = CrearSlot(contenedor, slots, indice, AlimentosBase[i], FoodCatalog.EstadoCrudo, ColoresCrudo[i]);
            indice = CrearSlot(contenedor, slots, indice, AlimentosBase[i], FoodCatalog.EstadoCocido, ColoresCocido[i]);
        }

        for (int i = 0; i < PlatosCompuestos.Length; i++)
        {
            indice = CrearSlot(contenedor, slots, indice, PlatosCompuestos[i], FoodCatalog.EstadoPlato, ColoresPlatos[i]);
        }

        Undo.RecordObject(catalogo, UndoLabel + ": catalogo de 24 modelos");
        catalogo.ConfigurarSlots(slots);
        EditorUtility.SetDirty(catalogo);

        return catalogo;
    }

    private static int CrearSlot(Transform contenedor, FoodModelSlot[] slots, int indice, string nombre,
                                 string estado, Color color)
    {
        if (indice >= slots.Length)
        {
            return indice;
        }

        string nombreModelo = "Modelo_" + (indice + 1).ToString("00") + "_" + LimpiarNombre(nombre) + "_" + estado;
        GameObject modelo = AsegurarHijoInactivo(contenedor, nombreModelo);

        slots[indice] = new FoodModelSlot
        {
            nombre = nombre,
            estado = estado,
            gramosReferencia = 100f,
            modelo = modelo,
            colorCrudo = color,
            colorCocido = color
        };

        return indice + 1;
    }

    /// <summary>10 ingredientes + 4 platos compuestos: cada opcion del selector tiene su slot.</summary>
    private static void CablearOpciones(Transform canvas)
    {
        Transform panelAR = FindDescendiente(canvas, PanelARName);

        if (panelAR == null)
        {
            return;
        }

        Transform encontrado = FindDescendiente(panelAR, "dropdownAlimentos");
        TMP_Dropdown dropdown = encontrado != null ? encontrado.GetComponent<TMP_Dropdown>() : null;

        if (dropdown != null)
        {
            var opciones = new List<TMP_Dropdown.OptionData>(AlimentosBase.Length + PlatosCompuestos.Length);

            for (int i = 0; i < AlimentosBase.Length; i++)
            {
                opciones.Add(new TMP_Dropdown.OptionData(AlimentosBase[i]));
            }

            for (int i = 0; i < PlatosCompuestos.Length; i++)
            {
                opciones.Add(new TMP_Dropdown.OptionData(PlatosCompuestos[i]));
            }

            Undo.RecordObject(dropdown, UndoLabel + ": opciones de alimentos");
            dropdown.options = opciones;
            dropdown.value = Mathf.Clamp(dropdown.value, 0, opciones.Count - 1);
            dropdown.RefreshShownValue();
            EditorUtility.SetDirty(dropdown);
        }

        Transform encontradoToggle = FindDescendiente(panelAR, "toggleCocido");
        Toggle toggle = encontradoToggle != null ? encontradoToggle.GetComponent<Toggle>() : null;

        if (toggle != null)
        {
            // El estado lo gobierna ARUIManager en runtime; aqui solo queda un punto de partida sano.
            Undo.RecordObject(toggle, UndoLabel + ": toggle de coccion");
            toggle.interactable = true;
            toggle.isOn = false;
            EditorUtility.SetDirty(toggle);
        }
    }

    // -------------------------------------------------------------------- cableado

    private static void CablearAnclaje(GameObject managers, Transform molde, Transform aro, MenuController menu)
    {
        PlateAnchorManager anclaje = managers.GetComponent<PlateAnchorManager>();

        if (anclaje == null)
        {
            Debug.LogWarning("[NutriAR] 'AR_Managers' no tiene PlateAnchorManager.");
            return;
        }

        Button boton = null;
        Transform encontrado = FindDescendienteGlobal(BtnFijarARName);

        if (encontrado != null)
        {
            boton = encontrado.GetComponent<Button>();
        }

        Cablear(anclaje, serialized =>
        {
            Asignar(serialized, "moldeHolograma", molde);
            Asignar(serialized, "aroReferencia", aro);
            Asignar(serialized, "menu", menu);
            Asignar(serialized, "btnFijarPlato", boton);
        });

        QuitarListenersPersistentes(boton);
    }

    private static void CablearEscala(GameObject managers, Transform molde, FoodCatalog catalogo, TMP_Text textoGramos)
    {
        ARUIManager escala = managers.GetComponent<ARUIManager>();

        if (escala == null)
        {
            Debug.LogWarning("[NutriAR] 'AR_Managers' no tiene ARUIManager.");
            return;
        }

        MeshRenderer rendererMolde = molde.GetComponent<MeshRenderer>();

        Cablear(escala, serialized =>
        {
            Asignar(serialized, "catalog", catalogo);
            Asignar(serialized, "moldeRenderer", rendererMolde);
            Asignar(serialized, "textoGramos", textoGramos);
        });
    }

    private static void CablearSplash(Transform canvas)
    {
        Transform splash = FindDescendiente(canvas, PanelSplashName);
        Transform carga = FindDescendiente(canvas, PanelCargaName);

        if (splash == null)
        {
            return;
        }

        SplashController controlador = splash.GetComponent<SplashController>();

        if (controlador == null)
        {
            Debug.LogWarning("[NutriAR] '" + PanelSplashName + "' no tiene SplashController.");
            return;
        }

        CanvasGroup grupo = EnsureComponent<CanvasGroup>(splash.gameObject);

        if (grupo.alpha < 1f)
        {
            Undo.RecordObject(grupo, UndoLabel + ": alpha de la splash");
            grupo.alpha = 1f;
            EditorUtility.SetDirty(grupo);
        }

        GameObject panelCarga = carga != null ? carga.gameObject : null;

        Cablear(controlador, serialized =>
        {
            Asignar(serialized, "canvasGroup", grupo);
            Asignar(serialized, "panelCarga", panelCarga);
        });
    }

    private static void QuitarListenersPersistentes(Button boton)
    {
        if (boton == null)
        {
            return;
        }

        Undo.RecordObject(boton, UndoLabel + ": " + boton.name);

        for (int i = boton.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
        {
            UnityEventTools.RemovePersistentListener(boton.onClick, i);
        }

        EditorUtility.SetDirty(boton);
    }

    // -------------------------------------------------------------- helpers comunes

    private static void Cablear(Object objetivo, System.Action<SerializedObject> asignaciones)
    {
        Undo.RecordObject(objetivo, UndoLabel + ": " + objetivo.name);
        var serialized = new SerializedObject(objetivo);
        asignaciones(serialized);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(objetivo);
    }

    private static void Asignar(SerializedObject serialized, string nombreCampo, Object valor)
    {
        SerializedProperty property = serialized.FindProperty(nombreCampo);

        if (property == null)
        {
            Debug.LogWarning("[NutriAR] " + serialized.targetObject.GetType().Name +
                             " no expone el campo serializado '" + nombreCampo + "'.");
            return;
        }

        if (valor == null)
        {
            Debug.LogWarning("[NutriAR] Referencia nula para '" + nombreCampo + "': queda sin asignar.");
            return;
        }

        property.objectReferenceValue = valor;
    }

    private static T EnsureComponent<T>(GameObject target) where T : Component
    {
        T existing = target.GetComponent<T>();
        return existing != null ? existing : Undo.AddComponent<T>(target);
    }

    private static RectTransform AsegurarRect(Transform padre, string nombre)
    {
        Transform existente = padre.Find(nombre);

        if (existente != null)
        {
            Undo.RecordObject(existente, UndoLabel + ": " + nombre);
            return (RectTransform)existente;
        }

        var go = new GameObject(nombre, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, UndoLabel + ": " + nombre);

        var rect = (RectTransform)go.transform;
        rect.SetParent(padre, false);
        rect.localScale = Vector3.one;
        rect.localPosition = Vector3.zero;

        return rect;
    }

    private static Image AsegurarImagen(GameObject target, Color color, bool raycastTarget)
    {
        Image imagen = target.GetComponent<Image>();

        if (imagen == null)
        {
            imagen = Undo.AddComponent<Image>(target);
        }
        else
        {
            Undo.RecordObject(imagen, UndoLabel + ": fondo " + target.name);
        }

        imagen.color = color;
        imagen.raycastTarget = raycastTarget;
        EditorUtility.SetDirty(imagen);

        return imagen;
    }

    private static TextMeshProUGUI AsegurarTexto(Transform padre, string nombre, string texto, float tamano,
                                                 TextAlignmentOptions alineacion)
    {
        Transform existente = padre.Find(nombre);
        TextMeshProUGUI label = existente != null ? existente.GetComponent<TextMeshProUGUI>() : null;

        if (label == null)
        {
            RectTransform root = AsegurarRect(padre, nombre);
            label = Undo.AddComponent<TextMeshProUGUI>(root.gameObject);

            if (TMP_Settings.defaultFontAsset != null)
            {
                label.font = TMP_Settings.defaultFontAsset;
            }

            label.raycastTarget = false;
        }
        else
        {
            Undo.RecordObject(label, UndoLabel + ": texto " + nombre);
        }

        label.text = texto;
        label.fontSize = tamano;
        label.alignment = alineacion;
        label.color = TextoBlanco;
        EditorUtility.SetDirty(label);

        return label;
    }

    private static void Stretch(RectTransform rect, float left, float right, float top, float bottom)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static Transform AsegurarHijoTransform(Transform padre, string nombre)
    {
        Transform existente = padre.Find(nombre);

        if (existente != null)
        {
            return existente;
        }

        var go = new GameObject(nombre);
        Undo.RegisterCreatedObjectUndo(go, UndoLabel + ": " + nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        return go.transform;
    }

    private static GameObject AsegurarHijoInactivo(Transform padre, string nombre)
    {
        Transform existente = padre.Find(nombre);

        if (existente != null)
        {
            if (existente.gameObject.activeSelf)
            {
                Undo.RecordObject(existente.gameObject, UndoLabel + ": ocultar " + nombre);
                existente.gameObject.SetActive(false);
                EditorUtility.SetDirty(existente.gameObject);
            }

            return existente.gameObject;
        }

        var go = new GameObject(nombre);
        Undo.RegisterCreatedObjectUndo(go, UndoLabel + ": " + nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        go.SetActive(false);

        return go;
    }

    private static string LimpiarNombre(string nombre)
    {
        return nombre.Replace(" ", string.Empty).Replace("ó", "o").Replace("é", "e");
    }

    private static GameObject FindInScene(string nombre)
    {
        GameObject[] raices = SceneManager.GetActiveScene().GetRootGameObjects();

        for (int i = 0; i < raices.Length; i++)
        {
            if (raices[i].name == nombre)
            {
                return raices[i];
            }
        }

        return null;
    }

    private static Transform FindDescendienteGlobal(string nombre)
    {
        GameObject[] raices = SceneManager.GetActiveScene().GetRootGameObjects();

        for (int i = 0; i < raices.Length; i++)
        {
            if (raices[i].name == nombre)
            {
                return raices[i].transform;
            }

            Transform encontrado = FindDescendiente(raices[i].transform, nombre);

            if (encontrado != null)
            {
                return encontrado;
            }
        }

        return null;
    }

    private static Transform FindDescendiente(Transform raiz, string nombre)
    {
        for (int i = 0; i < raiz.childCount; i++)
        {
            Transform hijo = raiz.GetChild(i);

            if (hijo.name == nombre)
            {
                return hijo;
            }

            Transform encontrado = FindDescendiente(hijo, nombre);

            if (encontrado != null)
            {
                return encontrado;
            }
        }

        return null;
    }
}
