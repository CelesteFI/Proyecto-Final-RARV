using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Paso 6 del armado de NutriAR: deja la aplicacion lista para el APK.
/// - Pantallas: Panel_Splash -> Panel_MenuPrincipal -> Panel_CalibracionPlato -> Panel_AR,
///   con Panel_Perfil y Panel_GuiaNutricional colgando del menu principal.
/// - Camara AR gestionada por ARCameraController: solo vive en calibracion y en la vista AR.
/// - FoodCatalog con los 24 slots de modelos (placeholders ya en la escena, inactivos).
/// - Contraste de todos los botones (fondo oscuro o primario + texto blanco).
/// Todo se crea en Edit Time (cero instanciacion en runtime), es idempotente por nombre y
/// queda protegido con Undo en un unico grupo.
/// </summary>
public static class NutriAR_AppFlowSetup
{
    private const string MenuPath = "NutriAR/6. Flujo Completo (Splash, Calibracion, Guia)";
    private const string UndoLabel = "NutriAR: Flujo Completo";

    private const string ManagersName = "AR_Managers";
    private const string PanelSplashName = "Panel_Splash";
    private const string PanelInicioAntiguo = "Panel_PantallaInicio";
    private const string PanelMenuName = "Panel_MenuPrincipal";
    private const string PanelPerfilName = "Panel_Perfil";
    private const string PanelARName = "Panel_AR";
    private const string PanelCalibracionName = "Panel_CalibracionPlato";
    private const string PanelGuiaName = "Panel_GuiaNutricional";
    private const string PanelCargaName = "Panel_Carga";
    private const string PanelSuperiorName = "PanelSuperior";
    private const string PanelInferiorName = "PanelInferior";
    private const string MoldeName = "MoldeHolograma";
    private const string ModelosName = "ModelosHolograma";

    private const float AltoBoton = 96f;
    private const float AltoCampo = 88f;

    private static readonly Color FondoPantalla = new Color(0.05f, 0.06f, 0.10f, 1f);
    private static readonly Color FondoBanda = new Color(0f, 0f, 0f, 0.78f);
    private static readonly Color Primario = new Color(0.16f, 0.52f, 0.96f, 1f);
    private static readonly Color Secundario = new Color(0.20f, 0.23f, 0.30f, 1f);
    private static readonly Color TextoBlanco = Color.white;
    private static readonly Color TextoSuave = new Color(0.78f, 0.84f, 0.95f, 1f);

    private static readonly string[] AlimentosBase =
    {
        "Arroz", "Pasta", "Pollo", "Res", "Cerdo", "Salmon", "Atun", "Huevo", "Papa", "Zanahoria"
    };

    private static readonly string[] PlatosCompuestos =
    {
        "Guiso de Pollo", "Sopa de Verduras", "Arroz con Pollo", "Ensalada Mixta"
    };

    private const string TextoGuia =
        "METODO DEL PLATO\n" +
        "• 1/2 del plato: verduras y hortalizas de temporada.\n" +
        "• 1/4: proteinas (pollo, pescado, huevo, res, cerdo).\n" +
        "• 1/4: cereales y tuberculos (arroz, pasta, papa).\n" +
        "• Grasas saludables en porcion pequena (aguacate, aceite de oliva).\n\n" +
        "PORCIONES GUIA (ADULTO)\n" +
        "• Proteina: 100 a 150 g por comida.\n" +
        "• Cereal cocido: 150 a 200 g.\n" +
        "• Verduras: 200 g o mas.\n" +
        "• Fruta: 2 a 3 piezas al dia.\n\n" +
        "MACRONUTRIENTES\n" +
        "• Proteinas: 4 kcal por gramo. Construyen y reparan tejidos.\n" +
        "• Carbohidratos: 4 kcal por gramo. Energia de uso rapido.\n" +
        "• Grasas: 9 kcal por gramo. Reserva energetica y hormonas.\n" +
        "• Agua: 30 a 35 ml por kilo de peso al dia.\n\n" +
        "COMO USA NUTRIAR EL VOLUMEN\n" +
        "El volumen aparente se obtiene con V = masa / densidad. Por eso 100 g de brocoli " +
        "(0.38 g/cm3) ocupan casi el triple que 100 g de arroz crudo (0.85 g/cm3): el " +
        "holograma cambia de tamano aunque el peso sea el mismo.\n" +
        "La altura del molde se mantiene fija en 2.5 cm (eje Y = 0.0125) y el radio crece " +
        "hasta el borde del plato calibrado.\n\n" +
        "PASOS EN LA APP\n" +
        "1. Calibra el plato con el circulo guia.\n" +
        "2. Elige el alimento, el estado crudo/cocido y los gramos.\n" +
        "3. Compara el holograma con tu plato antes de servirte.\n";

    [MenuItem(MenuPath)]
    public static void Construir()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        GameObject managers = FindInScene(ManagersName);

        if (canvas == null || managers == null)
        {
            Debug.LogError("[NutriAR] Faltan el Canvas o '" + ManagersName + "'. Ejecuta los pasos 1 a 5 antes.");
            return;
        }

        MenuController menu = managers.GetComponent<MenuController>();
        if (menu == null)
        {
            Debug.LogError("[NutriAR] '" + ManagersName + "' no tiene MenuController. Ejecuta el paso 2.");
            return;
        }

        GameObject splash = RenombrarSplash(canvas.transform);
        GameObject panelMenu = BuscarPanel(canvas.transform, PanelMenuName);
        GameObject panelPerfil = BuscarPanel(canvas.transform, PanelPerfilName);
        GameObject panelAR = BuscarPanel(canvas.transform, PanelARName);
        Transform molde = FindDescendienteGlobal(MoldeName);

        if (splash == null || panelMenu == null || panelPerfil == null || panelAR == null || molde == null)
        {
            Debug.LogError("[NutriAR] Faltan piezas base (splash, menu, perfil, AR o MoldeHolograma).");
            return;
        }

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(UndoLabel);

        ARCameraController camaraAR = ConstruirCamaraAR(managers);
        GameObject panelCalibracion = ConstruirCalibracion(canvas.transform, menu, managers);
        GameObject panelGuia = ConstruirGuia(canvas.transform, menu);

        ConstruirSplash(splash, menu);
        ConstruirMenuPrincipal(panelMenu, menu);
        AsegurarBandasAR(panelAR.transform, menu);
        CablearRetrocesos(panelPerfil.transform, panelAR.transform, panelGuia.transform, menu);
        ActualizarOpcionesAlimentos(panelAR.transform);
        ConstruirCatalogo(managers, molde);
        AsegurarContrasteBotones(canvas.transform);

        CablearMenuController(menu, splash, panelMenu, panelPerfil, panelAR, panelCalibracion, panelGuia, camaraAR);
        CablearAnclaje(managers, camaraAR);
        AplicarEstadoInicial(canvas.transform, splash);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

        Selection.activeGameObject = canvas.gameObject;
        Debug.Log("[NutriAR] Flujo completo listo: Panel_Splash -> Panel_MenuPrincipal -> " +
                  "Panel_CalibracionPlato -> Panel_AR (+ Panel_Perfil y Panel_GuiaNutricional). " +
                  "Camara AR solo en calibracion y AR; catalogo de 24 modelos creado; contraste de " +
                  "botones normalizado. Ctrl+Z revierte todo el proceso en un solo paso.", canvas);
    }

    // ------------------------------------------------------------- Panel_Splash

    private static GameObject RenombrarSplash(Transform canvas)
    {
        Transform panel = FindDescendiente(canvas, PanelInicioAntiguo);

        if (panel == null)
        {
            panel = FindDescendiente(canvas, PanelSplashName);
        }

        if (panel == null)
        {
            Debug.LogError("[NutriAR] No existe '" + PanelInicioAntiguo + "' ni '" + PanelSplashName + "'.");
            return null;
        }

        if (panel.name != PanelSplashName)
        {
            Undo.RecordObject(panel.gameObject, UndoLabel + ": renombrar splash");
            panel.gameObject.name = PanelSplashName;
            EditorUtility.SetDirty(panel.gameObject);
        }

        return panel.gameObject;
    }

    /// <summary>
    /// Splash: logotipo grande y nada mas. Los botones de la pantalla de inicio anterior se
    /// retiran porque la navegacion ahora vive en Panel_MenuPrincipal.
    /// </summary>
    private static void ConstruirSplash(GameObject splash, MenuController menu)
    {
        AsegurarImagen(splash, FondoPantalla, true);

        VerticalLayoutGroup layout = AsegurarLayoutVertical(splash, 140, 160);
        Undo.RecordObject(layout, UndoLabel + ": layout splash");
        layout.childAlignment = TextAnchor.MiddleCenter;
        EditorUtility.SetDirty(layout);

        QuitarHijo(splash.transform, "Btn_IrMedicion");
        QuitarHijo(splash.transform, "Btn_IrPerfil");

        TextMeshProUGUI titulo = AsegurarTexto(splash.transform, "TituloNutriAR", "NutriAR", 140f,
                                               TextAlignmentOptions.Center);
        titulo.color = TextoBlanco;
        AsegurarLayoutElement(titulo.gameObject, 200f, 0f);

        TextMeshProUGUI subtitulo = AsegurarTexto(splash.transform, "SubtituloSplash",
                                                  "Medicion nutricional con realidad aumentada", 34f,
                                                  TextAlignmentOptions.Center);
        subtitulo.color = TextoSuave;
        AsegurarLayoutElement(subtitulo.gameObject, 120f, 0f);

        TextMeshProUGUI pie = AsegurarTexto(splash.transform, "PieSplash",
                                            "Proyecto final · Ingenieria en Computacion", 26f,
                                            TextAlignmentOptions.Center);
        pie.color = TextoSuave;
        AsegurarLayoutElement(pie.gameObject, 80f, 0f);

        OrdenarHijos(splash, "TituloNutriAR", "SubtituloSplash", "PieSplash");

        SplashController controlador = EnsureComponent<SplashController>(splash);
        Cablear(controlador, serialized =>
        {
            Asignar(serialized, "menu", menu);
        });
    }

    // ------------------------------------------------------- Panel_MenuPrincipal

    private static void ConstruirMenuPrincipal(GameObject panel, MenuController menu)
    {
        AsegurarImagen(panel, FondoPantalla, true);

        VerticalLayoutGroup layout = AsegurarLayoutVertical(panel, 120, 180);
        Undo.RecordObject(layout, UndoLabel + ": layout menu");
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 36f;
        EditorUtility.SetDirty(layout);

        TextMeshProUGUI titulo = AsegurarTexto(panel.transform, "TituloMenu", "Menu principal", 60f,
                                               TextAlignmentOptions.Center);
        titulo.color = TextoBlanco;
        AsegurarLayoutElement(titulo.gameObject, 140f, 0f);

        // El boton "Volver al inicio" del menu lateral anterior se reconvierte en la entrada
        // al flujo de medicion: conserva su posicion y su LayoutElement.
        Button btnCalibracion = ReutilizarBoton(panel.transform, "Btn_VolverInicio", "Btn_IniciarCalibracion",
                                                "Iniciar Calibracion y Medicion");
        Button btnPerfil = AsegurarBoton(panel.transform, "Btn_IrPerfil", "Perfil Biometrico", Primario);
        Button btnGuia = AsegurarBoton(panel.transform, "Btn_GuiaNutricional", "Guia Nutricional", Primario);

        ReemplazarListener(btnCalibracion, "MostrarCalibracion", menu.MostrarCalibracion);
        ReemplazarListener(btnPerfil, "MostrarPerfil", menu.MostrarPerfil);
        ReemplazarListener(btnGuia, "MostrarGuia", menu.MostrarGuia);

        OrdenarHijos(panel, "TituloMenu", "Btn_IniciarCalibracion", "Btn_IrPerfil", "Btn_GuiaNutricional");
    }

    // -------------------------------------------------- Panel_CalibracionPlato

    private static GameObject ConstruirCalibracion(Transform canvas, MenuController menu, GameObject managers)
    {
        RectTransform panel = AsegurarRect(canvas, PanelCalibracionName);
        Stretch(panel, 0f, 0f, 0f, 0f);

        // Fondo transparente: la camara AR se ve detras y el panel no captura clics vacios.
        AsegurarImagen(panel.gameObject, new Color(0f, 0f, 0f, 0f), false);

        // --- Circulo guia (anillo translucido, centrado en pantalla: ahi apunta el raycast)
        Sprite anillo = AsegurarSpriteCirculo();
        RectTransform circulo = AsegurarRect(panel, "CirculoGuia");
        Undo.RecordObject(circulo, UndoLabel + ": circulo guia");
        circulo.anchorMin = new Vector2(0.5f, 0.5f);
        circulo.anchorMax = new Vector2(0.5f, 0.5f);
        circulo.pivot = new Vector2(0.5f, 0.5f);
        circulo.anchoredPosition = Vector2.zero;
        circulo.sizeDelta = new Vector2(336f, 336f);

        Image imagenCirculo = AsegurarImagen(circulo.gameObject, new Color(1f, 1f, 1f, 0.9f), false);
        Undo.RecordObject(imagenCirculo, UndoLabel + ": sprite del circulo");
        imagenCirculo.sprite = anillo;
        imagenCirculo.type = Image.Type.Simple;
        imagenCirculo.preserveAspect = true;
        EditorUtility.SetDirty(imagenCirculo);

        // --- Banda superior con la instruccion
        RectTransform bandaSuperior = AsegurarRect(panel, "PanelCalibracionSuperior");
        Undo.RecordObject(bandaSuperior, UndoLabel + ": banda superior");
        bandaSuperior.anchorMin = new Vector2(0f, 1f);
        bandaSuperior.anchorMax = new Vector2(1f, 1f);
        bandaSuperior.pivot = new Vector2(0.5f, 1f);
        bandaSuperior.anchoredPosition = Vector2.zero;
        bandaSuperior.sizeDelta = new Vector2(0f, 200f);

        AsegurarImagen(bandaSuperior.gameObject, FondoBanda, true);

        TextMeshProUGUI instruccion = AsegurarTexto(bandaSuperior, "TituloCalibracion",
                                                    "Ajusta el circulo hasta que coincida con tu plato", 32f,
                                                    TextAlignmentOptions.Center);
        instruccion.color = TextoBlanco;
        Stretch(instruccion.rectTransform, 40f, 40f, 20f, 20f);

        // --- Banda inferior con los controles
        RectTransform bandaInferior = AsegurarRect(panel, "PanelCalibracionInferior");
        Undo.RecordObject(bandaInferior, UndoLabel + ": banda inferior");
        bandaInferior.anchorMin = new Vector2(0f, 0f);
        bandaInferior.anchorMax = new Vector2(1f, 0f);
        bandaInferior.pivot = new Vector2(0.5f, 0f);
        bandaInferior.anchoredPosition = Vector2.zero;
        bandaInferior.sizeDelta = new Vector2(0f, 420f);

        AsegurarImagen(bandaInferior.gameObject, FondoBanda, true);

        VerticalLayoutGroup layout = AsegurarLayoutVertical(bandaInferior.gameObject, 60, 40);
        Undo.RecordObject(layout, UndoLabel + ": layout calibracion");
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 18f;
        EditorUtility.SetDirty(layout);

        TextMeshProUGUI textoRadio = AsegurarTexto(bandaInferior, "TextoRadioPlato",
                                                   "Radio del plato: 12.0 cm", 30f, TextAlignmentOptions.Center);
        textoRadio.color = TextoBlanco;
        AsegurarLayoutElement(textoRadio.gameObject, 56f, 0f);

        Slider slider = AsegurarSlider(bandaInferior, "SliderRadioPlato", ARState.MinPlateRadiusCm,
                                       ARState.MaxPlateRadiusCm, ARState.DefaultPlateRadiusCm);

        Button boton = AsegurarBoton(bandaInferior, "Btn_FijarPlatoCalibracion", "Fijar Plato", Primario);

        PlateCalibrationController controlador = EnsureComponent<PlateCalibrationController>(panel.gameObject);
        PlateAnchorManager anclaje = managers.GetComponent<PlateAnchorManager>();

        Cablear(controlador, serialized =>
        {
            Asignar(serialized, "sliderRadioPlato", slider);
            Asignar(serialized, "btnFijarPlatoCalibracion", boton);
            Asignar(serialized, "textoRadioPlato", textoRadio);
            Asignar(serialized, "circuloGuia", circulo);
            Asignar(serialized, "menu", menu);
            Asignar(serialized, "plateAnchor", anclaje);
        });

        return panel.gameObject;
    }

    // ---------------------------------------------------- Panel_GuiaNutricional

    private static GameObject ConstruirGuia(Transform canvas, MenuController menu)
    {
        RectTransform panel = AsegurarRect(canvas, PanelGuiaName);
        Stretch(panel, 0f, 0f, 0f, 0f);

        AsegurarImagen(panel.gameObject, FondoPantalla, true);

        VerticalLayoutGroup layout = AsegurarLayoutVertical(panel.gameObject, 60, 60);
        Undo.RecordObject(layout, UndoLabel + ": layout guia");
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.spacing = 20f;
        EditorUtility.SetDirty(layout);

        TextMeshProUGUI titulo = AsegurarTexto(panel, "TituloGuia", "Guia Nutricional", 56f,
                                               TextAlignmentOptions.Center);
        titulo.color = TextoBlanco;
        AsegurarLayoutElement(titulo.gameObject, 110f, 0f);

        TextMeshProUGUI cuerpo = AsegurarTexto(panel, "TextoGuia", TextoGuia, 26f, TextAlignmentOptions.TopLeft);
        cuerpo.color = TextoBlanco;
        AsegurarLayoutElement(cuerpo.gameObject, 1500f, 1f);

        Button boton = AsegurarBoton(panel, "Btn_VolverGuia", "Volver", Secundario);
        ReemplazarListener(boton, "MostrarMenuPrincipal", menu.MostrarMenuPrincipal);

        OrdenarHijos(panel.gameObject, "TituloGuia", "TextoGuia", "Btn_VolverGuia");

        return panel.gameObject;
    }

    // --------------------------------------------------------------- Panel_AR

    /// <summary>
    /// Las bandas de la vista AR no tenian fondo: sobre el video de la camara el texto perdia
    /// contraste. Se les pone una banda oscura translucida y el fondo del panel pasa a ser
    /// transparente para no velar la camara.
    /// </summary>
    private static void AsegurarBandasAR(Transform panelAR, MenuController menu)
    {
        AsegurarImagen(panelAR.gameObject, new Color(0f, 0f, 0f, 0f), true);

        Transform superior = FindDescendiente(panelAR, PanelSuperiorName);
        if (superior != null)
        {
            AsegurarImagen(superior.gameObject, FondoBanda, true);

            Button abrirMenu = BuscarBoton(superior, "Btn_AbrirMenu");
            if (abrirMenu != null)
            {
                AsegurarEtiqueta(abrirMenu.transform, "Regresar", Primario);
                ReemplazarListener(abrirMenu, "MostrarMenuPrincipal", menu.MostrarMenuPrincipal);
            }
        }

        Transform inferior = FindDescendiente(panelAR, PanelInferiorName);
        if (inferior != null)
        {
            AsegurarImagen(inferior.gameObject, FondoBanda, true);
        }
    }

    private static void CablearRetrocesos(Transform panelPerfil, Transform panelAR, Transform panelGuia, MenuController menu)
    {
        Transform perfil = FindDescendiente(panelPerfil, "Btn_Volver");
        Button botonPerfil = perfil != null ? perfil.GetComponent<Button>() : null;
        ReemplazarListener(botonPerfil, "MostrarMenuPrincipal", menu.MostrarMenuPrincipal);

        Transform guia = FindDescendiente(panelGuia, "Btn_VolverGuia");
        Button botonGuia = guia != null ? guia.GetComponent<Button>() : null;
        ReemplazarListener(botonGuia, "MostrarMenuPrincipal", menu.MostrarMenuPrincipal);
    }

    /// <summary>
    /// 10 ingredientes base en crudo y cocido + 4 platos compuestos. Los platos compuestos se
    /// anaden al selector de alimentos; los dos alimentos que no forman parte de los 24 modelos
    /// (Brocoli y Frijol) salen del selector para que cada opcion tenga su slot.
    /// </summary>
    private static void ActualizarOpcionesAlimentos(Transform panelAR)
    {
        Transform encontrado = FindDescendiente(panelAR, "dropdownAlimentos");
        TMP_Dropdown dropdown = encontrado != null ? encontrado.GetComponent<TMP_Dropdown>() : null;

        if (dropdown == null)
        {
            Debug.LogWarning("[NutriAR] No se encontro 'dropdownAlimentos': el selector queda como estaba.");
            return;
        }

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

    // -------------------------------------------------------- camara AR / catalogo

    private static ARCameraController ConstruirCamaraAR(GameObject managers)
    {
        Transform camara = FindDescendienteGlobal("Main Camera");

        ARCameraManager cameraManager = camara != null ? camara.GetComponent<ARCameraManager>() : null;
        ARCameraBackground cameraBackground = camara != null ? camara.GetComponent<ARCameraBackground>() : null;
        ARPlaneManager planeManager = managers.GetComponent<ARPlaneManager>();
        ARRaycastManager raycastManager = managers.GetComponent<ARRaycastManager>();

        if (cameraManager == null)
        {
            Debug.LogWarning("[NutriAR] No se encontro ARCameraManager en 'Main Camera'.");
        }

        ARCameraController controlador = EnsureComponent<ARCameraController>(managers);

        Cablear(controlador, serialized =>
        {
            Asignar(serialized, "cameraManager", cameraManager);
            Asignar(serialized, "cameraBackground", cameraBackground);
            Asignar(serialized, "planeManager", planeManager);
            Asignar(serialized, "raycastManager", raycastManager);
        });

        return controlador;
    }

    private static void ConstruirCatalogo(GameObject managers, Transform molde)
    {
        Transform contenedor = AsegurarHijoTransform(molde, ModelosName);
        FoodCatalog catalogo = EnsureComponent<FoodCatalog>(managers);

        var slots = new FoodModelSlot[FoodCatalog.TotalSlots];
        int indice = 0;

        for (int i = 0; i < AlimentosBase.Length; i++)
        {
            indice = CrearSlotModelo(contenedor, slots, indice, AlimentosBase[i], FoodCatalog.EstadoCrudo);
            indice = CrearSlotModelo(contenedor, slots, indice, AlimentosBase[i], FoodCatalog.EstadoCocido);
        }

        for (int i = 0; i < PlatosCompuestos.Length; i++)
        {
            indice = CrearSlotModelo(contenedor, slots, indice, PlatosCompuestos[i], FoodCatalog.EstadoPlato);
        }

        Undo.RecordObject(catalogo, UndoLabel + ": catalogo de 24 modelos");
        catalogo.ConfigurarSlots(slots);
        EditorUtility.SetDirty(catalogo);
    }

    private static int CrearSlotModelo(Transform contenedor, FoodModelSlot[] slots, int indice, string nombre, string estado)
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
            modelo = modelo
        };

        return indice + 1;
    }

    private static void CablearAnclaje(GameObject managers, ARCameraController camaraAR)
    {
        PlateAnchorManager anclaje = managers.GetComponent<PlateAnchorManager>();

        if (anclaje == null)
        {
            return;
        }

        Cablear(anclaje, serialized =>
        {
            Asignar(serialized, "arController", camaraAR);
        });
    }

    private static void CablearMenuController(MenuController menu, GameObject splash, GameObject panelMenu,
                                              GameObject panelPerfil, GameObject panelAR,
                                              GameObject panelCalibracion, GameObject panelGuia,
                                              ARCameraController camaraAR)
    {
        Cablear(menu, serialized =>
        {
            Asignar(serialized, "panelInicio", splash);
            Asignar(serialized, "panelMenuLateral", panelMenu);
            Asignar(serialized, "panelPerfil", panelPerfil);
            Asignar(serialized, "panelAR", panelAR);
            Asignar(serialized, "panelCalibracion", panelCalibracion);
            Asignar(serialized, "panelGuia", panelGuia);
            Asignar(serialized, "arCameraController", camaraAR);
        });
    }

    // ----------------------------------------------------------- estado inicial

    private static void AplicarEstadoInicial(Transform canvas, GameObject splash)
    {
        string[] paneles =
        {
            PanelSplashName, PanelMenuName, PanelPerfilName, PanelARName, PanelCalibracionName, PanelGuiaName
        };

        for (int i = 0; i < paneles.Length; i++)
        {
            Transform panel = FindDescendiente(canvas, paneles[i]);

            if (panel == null)
            {
                continue;
            }

            bool debeEstarActivo = panel.gameObject == splash;

            if (panel.gameObject.activeSelf == debeEstarActivo)
            {
                continue;
            }

            Undo.RecordObject(panel.gameObject, UndoLabel + ": estado " + panel.name);
            panel.gameObject.SetActive(debeEstarActivo);
            EditorUtility.SetDirty(panel.gameObject);
        }

        Transform carga = FindDescendiente(canvas, PanelCargaName);

        if (carga == null)
        {
            return;
        }

        CanvasGroup grupo = EnsureComponent<CanvasGroup>(carga.gameObject);
        Undo.RecordObject(grupo, UndoLabel + ": CanvasGroup de carga");
        // alpha 0: es negro opaco a pantalla completa; PanelCarga.Start() lo sube a 1 en Play.
        grupo.alpha = 0f;
        grupo.interactable = false;
        grupo.blocksRaycasts = false;
        EditorUtility.SetDirty(grupo);

        if (!carga.gameObject.activeSelf)
        {
            Undo.RecordObject(carga.gameObject, UndoLabel + ": activar carga");
            carga.gameObject.SetActive(true);
            EditorUtility.SetDirty(carga.gameObject);
        }

        Undo.RegisterFullObjectHierarchyUndo(canvas.gameObject, UndoLabel + ": orden de paneles");
        carga.SetAsLastSibling();
    }

    // ----------------------------------------------------------------- sprite

    private static Sprite AsegurarSpriteCirculo()
    {
        const string carpeta = "Assets/Sprites";
        const string ruta = carpeta + "/CirculoGuia.png";

        Sprite existente = AssetDatabase.LoadAssetAtPath<Sprite>(ruta);

        if (existente != null)
        {
            return existente;
        }

        const int tamano = 512;
        float centro = (tamano - 1) * 0.5f;
        float radioExterno = tamano * 0.5f - 2f;
        float radioInterno = radioExterno * 0.90f;

        var textura = new Texture2D(tamano, tamano, TextureFormat.RGBA32, false);
        var pixeles = new Color32[tamano * tamano];

        for (int y = 0; y < tamano; y++)
        {
            for (int x = 0; x < tamano; x++)
            {
                float dx = x - centro;
                float dy = y - centro;
                float distancia = Mathf.Sqrt((dx * dx) + (dy * dy));

                float exterior = Mathf.Clamp01(radioExterno - distancia);
                float interior = Mathf.Clamp01(distancia - radioInterno + 1f);
                byte alfa = (byte)Mathf.RoundToInt(Mathf.Clamp01(exterior * interior) * 255f);

                pixeles[(y * tamano) + x] = new Color32(255, 255, 255, alfa);
            }
        }

        textura.SetPixels32(pixeles);
        textura.Apply();

        byte[] png = textura.EncodeToPNG();
        UnityEngine.Object.DestroyImmediate(textura);

        if (!AssetDatabase.IsValidFolder(carpeta))
        {
            AssetDatabase.CreateFolder("Assets", "Sprites");
        }

        System.IO.File.WriteAllBytes(ruta, png);
        AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceSynchronousImport);

        var importer = AssetImporter.GetAtPath(ruta) as TextureImporter;

        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
    }

    // ------------------------------------------------------------- contraste

    /// <summary>
    /// Ningun boton puede quedar con texto claro sobre fondo claro: los fondos aclarados se
    /// oscurecen y todas las etiquetas pasan a blanco (o casi negro si el fondo sigue claro).
    /// </summary>
    private static void AsegurarContrasteBotones(Transform canvas)
    {
        Button[] botones = canvas.GetComponentsInChildren<Button>(true);
        int ajustados = 0;

        for (int i = 0; i < botones.Length; i++)
        {
            Button boton = botones[i];
            Image fondo = boton.targetGraphic as Image;

            if (fondo == null)
            {
                fondo = boton.GetComponent<Image>();
            }

            if (fondo == null)
            {
                continue;
            }

            Color color = fondo.color;
            float luminancia = (color.r + color.g + color.b) / 3f;

            if (luminancia > 0.55f)
            {
                Undo.RecordObject(fondo, UndoLabel + ": contraste " + boton.name);
                fondo.color = Secundario;
                EditorUtility.SetDirty(fondo);
                color = Secundario;
                ajustados++;
            }

            Color colorTexto = ColorDeTextoPara(color);
            TextMeshProUGUI[] etiquetas = boton.GetComponentsInChildren<TextMeshProUGUI>(true);

            for (int j = 0; j < etiquetas.Length; j++)
            {
                if (etiquetas[j].color == colorTexto)
                {
                    continue;
                }

                Undo.RecordObject(etiquetas[j], UndoLabel + ": texto " + etiquetas[j].name);
                etiquetas[j].color = colorTexto;
                EditorUtility.SetDirty(etiquetas[j]);
            }
        }

        if (ajustados > 0)
        {
            Debug.Log("[NutriAR] Contraste corregido en " + ajustados + " boton(es) con fondo claro.");
        }
    }

    private static Color ColorDeTextoPara(Color fondo)
    {
        float luminancia = (fondo.r + fondo.g + fondo.b) / 3f;
        return luminancia > 0.6f ? new Color(0.08f, 0.08f, 0.08f, 1f) : TextoBlanco;
    }

    // -------------------------------------------------------------- cableado

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

    private static void ReemplazarListener(Button boton, string nombreMetodo, UnityAction accion)
    {
        if (boton == null)
        {
            Debug.LogWarning("[NutriAR] Boton no encontrado para '" + nombreMetodo + "': ruteo manual.");
            return;
        }

        Undo.RecordObject(boton, UndoLabel + ": " + boton.name + "." + nombreMetodo);

        for (int i = boton.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
        {
            UnityEventTools.RemovePersistentListener(boton.onClick, i);
        }

        UnityEventTools.AddVoidPersistentListener(boton.onClick, accion);
        EditorUtility.SetDirty(boton);
    }

    // --------------------------------------------------------------- helpers UI

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

    private static VerticalLayoutGroup AsegurarLayoutVertical(GameObject panel, int paddingHorizontal, int paddingVertical)
    {
        VerticalLayoutGroup grupo = EnsureComponent<VerticalLayoutGroup>(panel);

        Undo.RecordObject(grupo, UndoLabel + ": layout " + panel.name);
        grupo.padding = new RectOffset(paddingHorizontal, paddingHorizontal, paddingVertical, paddingVertical);
        grupo.spacing = 24f;
        grupo.childAlignment = TextAnchor.UpperCenter;
        grupo.childControlWidth = true;
        grupo.childControlHeight = true;
        grupo.childForceExpandWidth = true;
        grupo.childForceExpandHeight = false;

        EditorUtility.SetDirty(grupo);
        return grupo;
    }

    private static LayoutElement AsegurarLayoutElement(GameObject target, float alto, float flexible)
    {
        LayoutElement elemento = target.GetComponent<LayoutElement>();

        if (elemento == null)
        {
            elemento = Undo.AddComponent<LayoutElement>(target);
        }
        else
        {
            Undo.RecordObject(elemento, UndoLabel + ": layout element " + target.name);
        }

        elemento.minHeight = alto;
        elemento.preferredHeight = alto;
        elemento.flexibleHeight = flexible;

        EditorUtility.SetDirty(elemento);
        return elemento;
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

    private static Button AsegurarBoton(Transform padre, string nombre, string etiqueta, Color color)
    {
        Transform existente = padre.Find(nombre);
        Button previo = existente != null ? existente.GetComponent<Button>() : null;

        if (previo != null)
        {
            AsegurarEtiqueta(existente, etiqueta, color);
            return previo;
        }

        RectTransform root = AsegurarRect(padre, nombre);
        Image fondo = AsegurarImagen(root.gameObject, color, true);

        Button boton = EnsureComponent<Button>(root.gameObject);
        Undo.RecordObject(boton, UndoLabel + ": " + nombre);
        boton.targetGraphic = fondo;
        boton.transition = Selectable.Transition.ColorTint;

        TextMeshProUGUI label = AsegurarTexto(root, "Label", etiqueta, 30f, TextAlignmentOptions.Center);
        Stretch(label.rectTransform, 10f, 10f, 0f, 0f);
        label.color = ColorDeTextoPara(color);

        AsegurarLayoutElement(root.gameObject, AltoBoton, 0f);
        EditorUtility.SetDirty(boton);

        return boton;
    }

    /// <summary>Reutiliza un boton existente: lo renombra y actualiza etiqueta y fondo.</summary>
    private static Button ReutilizarBoton(Transform padre, string nombreActual, string nombreNuevo, string etiqueta)
    {
        Transform existente = padre.Find(nombreActual);

        if (existente != null && existente.name != nombreNuevo)
        {
            Undo.RecordObject(existente.gameObject, UndoLabel + ": renombrar " + nombreActual);
            existente.gameObject.name = nombreNuevo;
            EditorUtility.SetDirty(existente.gameObject);
        }

        return AsegurarBoton(padre, nombreNuevo, etiqueta, Primario);
    }

    private static void AsegurarEtiqueta(Transform boton, string etiqueta, Color colorFondo)
    {
        Image fondo = boton.GetComponent<Image>();

        if (fondo != null && fondo.color != colorFondo)
        {
            Undo.RecordObject(fondo, UndoLabel + ": fondo " + boton.name);
            fondo.color = colorFondo;
            EditorUtility.SetDirty(fondo);
        }

        TextMeshProUGUI label = AsegurarTexto(boton, "Label", etiqueta, 30f, TextAlignmentOptions.Center);
        Stretch(label.rectTransform, 10f, 10f, 0f, 0f);
        label.color = ColorDeTextoPara(colorFondo);
    }

    private static Slider AsegurarSlider(Transform padre, string nombre, float minimo, float maximo, float valor)
    {
        Transform existente = padre.Find(nombre);
        Slider previo = existente != null ? existente.GetComponent<Slider>() : null;

        if (previo != null)
        {
            Undo.RecordObject(previo, UndoLabel + ": " + nombre);
            previo.minValue = minimo;
            previo.maxValue = maximo;
            previo.value = Mathf.Clamp(valor, minimo, maximo);
            EditorUtility.SetDirty(previo);
            return previo;
        }

        RectTransform root = AsegurarRect(padre, nombre);
        AsegurarLayoutElement(root.gameObject, AltoCampo, 0f);

        RectTransform fondoRt = AsegurarRect(root, "Background");
        Stretch(fondoRt, 0f, 0f, 26f, 26f);
        AsegurarImagen(fondoRt.gameObject, new Color(1f, 1f, 1f, 0.18f), true);

        RectTransform areaRelleno = AsegurarRect(root, "Fill Area");
        Stretch(areaRelleno, 20f, 20f, 30f, 30f);

        RectTransform relleno = AsegurarRect(areaRelleno, "Fill");
        Stretch(relleno, 0f, 0f, 0f, 0f);
        AsegurarImagen(relleno.gameObject, Primario, false);

        RectTransform areaHandle = AsegurarRect(root, "Handle Slide Area");
        Stretch(areaHandle, 20f, 20f, 0f, 0f);

        RectTransform handle = AsegurarRect(areaHandle, "Handle");
        handle.sizeDelta = new Vector2(44f, 44f);
        Image imagenHandle = AsegurarImagen(handle.gameObject, Color.white, true);

        Slider slider = EnsureComponent<Slider>(root.gameObject);
        Undo.RecordObject(slider, UndoLabel + ": " + nombre);
        slider.fillRect = relleno;
        slider.handleRect = handle;
        slider.targetGraphic = imagenHandle;
        slider.transition = Selectable.Transition.ColorTint;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = minimo;
        slider.maxValue = maximo;
        slider.wholeNumbers = false;
        slider.value = Mathf.Clamp(valor, minimo, maximo);
        EditorUtility.SetDirty(slider);

        return slider;
    }

    private static void Stretch(RectTransform rect, float left, float right, float top, float bottom)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static void OrdenarHijos(GameObject panel, params string[] orden)
    {
        Undo.RegisterFullObjectHierarchyUndo(panel, UndoLabel + ": orden " + panel.name);

        for (int i = 0; i < orden.Length; i++)
        {
            Transform hijo = panel.transform.Find(orden[i]);

            if (hijo != null)
            {
                hijo.SetSiblingIndex(i);
            }
        }

        EditorUtility.SetDirty(panel);
    }

    private static void QuitarHijo(Transform padre, string nombre)
    {
        Transform hijo = padre.Find(nombre);

        if (hijo == null)
        {
            return;
        }

        Undo.DestroyObjectImmediate(hijo.gameObject);
    }

    // --------------------------------------------------------- helpers de escena

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

    private static Button BuscarBoton(Transform raiz, string nombre)
    {
        Transform encontrado = FindDescendiente(raiz, nombre);
        return encontrado != null ? encontrado.GetComponent<Button>() : null;
    }

    private static GameObject BuscarPanel(Transform canvas, string nombre)
    {
        Transform panel = FindDescendiente(canvas, nombre);

        if (panel == null)
        {
            Debug.LogWarning("[NutriAR] No se encontro el panel '" + nombre + "' dentro del Canvas.");
            return null;
        }

        return panel.gameObject;
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
}
