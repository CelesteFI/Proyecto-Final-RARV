using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Paso 3: crea Panel_DashboardMetas (Mis Metas: Agua / Kcal / Macros), Panel_Tutorial de 4
/// pasos, agrega los botones "Mis Metas" y "Ayuda" al menu principal y al menu lateral AR, y
/// cablea MenuController + BiometricProfile. Todo pre-creado en la jerarquia: cero Instantiate
/// en runtime.
/// </summary>
public static class AuraMetasTutorialBuild
{
    private static readonly Color FondoPanel = new Color(0.05f, 0.06f, 0.1f, 1f);
    private static readonly Color FondoTarjeta = new Color(0.078f, 0.098f, 0.176f, 1f);
    private static readonly Color FondoModal = new Color(0.02f, 0.03f, 0.06f, 0.965f);
    private static readonly Color TextoSuave = new Color(0.66f, 0.73f, 0.86f, 1f);
    private static readonly Color TextoBlanco = new Color(0.96f, 0.97f, 1f, 1f);
    private static readonly Color AzulPrimario = new Color(0.16f, 0.52f, 0.96f, 1f);
    private static readonly Color VerdeAgua = new Color(0.24f, 0.72f, 0.95f, 1f);
    private static readonly Color NaranjaKcal = new Color(0.98f, 0.62f, 0.24f, 1f);
    private static readonly Color VioletaMacro = new Color(0.62f, 0.45f, 0.95f, 1f);

    private static Sprite _redondeado;

    public static void Execute()
    {
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("[AuraMetasTutorialBuild] No se encontro 'Canvas'.");
            return;
        }

        GameObject panelGuia = canvas.transform.Find("Panel_GuiaNutricional").gameObject;
        GameObject panelCarga = canvas.transform.Find("Panel_Carga").gameObject;
        GameObject panelMenu = canvas.transform.Find("Panel_MenuPrincipal").gameObject;
        GameObject panelAR = canvas.transform.Find("Panel_AR").gameObject;

        _redondeado = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        GameObject xrOrigin = GameObject.Find("XR Origin");
        MenuController menu = xrOrigin.GetComponent<MenuController>();
        BiometricProfile perfil = xrOrigin.GetComponent<BiometricProfile>();
        ARUIManager arUI = xrOrigin.GetComponent<ARUIManager>();

        TMP_Text plantilla = Buscar<TMP_Text>(panelGuia, "TituloGuia");
        if (plantilla == null)
        {
            Debug.LogError("[AuraMetasTutorialBuild] Falta TituloGuia como plantilla de texto.");
            return;
        }

        GameObject dashboard = CrearDashboard(panelCarga, plantilla, perfil, menu);
        GameObject tutorial = CrearTutorial(panelCarga, plantilla, menu);

        // Orden en el Canvas: dashboard y tutorial justo antes de Panel_Carga (que debe seguir ultimo).
        dashboard.transform.SetSiblingIndex(panelCarga.transform.GetSiblingIndex());
        tutorial.transform.SetSiblingIndex(panelCarga.transform.GetSiblingIndex());

        if (menu != null)
        {
            SerializedObject so = new SerializedObject(menu);
            Asignar(so, "panelDashboard", dashboard);
            Asignar(so, "panelTutorial", tutorial);
            Asignar(so, "arUI", arUI);

            SerializedProperty pasos = so.FindProperty("pasosTutorial");
            pasos.arraySize = 4;
            for (int i = 0; i < 4; i++)
            {
                Transform paso = tutorial.transform.Find("TarjetaTutorial/Paso_" + (i + 1));
                pasos.GetArrayElementAtIndex(i).objectReferenceValue = paso != null ? paso.gameObject : null;
            }

            Asignar(so, "indicadorTutorial", Buscar<TMP_Text>(tutorial, "Indicador"));
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        if (perfil != null)
        {
            SerializedObject so = new SerializedObject(perfil);
            Asignar(so, "dashAguaNumero", Buscar<TMP_Text>(dashboard, "AguaNumero"));
            Asignar(so, "dashAguaSufijo", Buscar<TMP_Text>(dashboard, "AguaSufijo"));
            Asignar(so, "dashKcalNumero", Buscar<TMP_Text>(dashboard, "KcalNumero"));
            Asignar(so, "dashKcalSufijo", Buscar<TMP_Text>(dashboard, "KcalSufijo"));
            Asignar(so, "dashProteinaNumero", Buscar<TMP_Text>(dashboard, "ProtNumero"));
            Asignar(so, "dashProteinaSufijo", Buscar<TMP_Text>(dashboard, "ProtSufijo"));
            Asignar(so, "dashCarboNumero", Buscar<TMP_Text>(dashboard, "CarboNumero"));
            Asignar(so, "dashCarboSufijo", Buscar<TMP_Text>(dashboard, "CarboSufijo"));
            Asignar(so, "dashGrasaNumero", Buscar<TMP_Text>(dashboard, "GrasaNumero"));
            Asignar(so, "dashGrasaSufijo", Buscar<TMP_Text>(dashboard, "GrasaSufijo"));
            Asignar(so, "dashEstado", Buscar<TMP_Text>(dashboard, "Estado"));
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        AgregarBotonesMenuPrincipal(panelMenu, plantilla, menu);
        AgregarBotonesMenuLateral(panelAR, plantilla, menu);

        EditorUtility.SetDirty(dashboard);
        EditorUtility.SetDirty(tutorial);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("[AuraMetasTutorialBuild] Dashboard de metas y tutorial de 4 pasos creados y cableados.");
    }

    private static GameObject CrearDashboard(GameObject panelCarga, TMP_Text plantilla, BiometricProfile perfil, MenuController menu)
    {
        GameObject panel = Nuevo("Panel_DashboardMetas", panelCarga.transform.parent);
        RectTransform rt = (RectTransform)panel.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image fondo = panel.AddComponent<Image>();
        fondo.color = FondoPanel;

        TMP_Text titulo = Texto(plantilla, panel.transform, "Titulo", "Mis metas", 48f, TextoBlanco);
        Colocar((RectTransform)titulo.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -80f), new Vector2(900f, 80f));
        titulo.fontStyle = FontStyles.Bold;
        titulo.alignment = TextAlignmentOptions.Center;

        TMP_Text subtitulo = Texto(plantilla, panel.transform, "Subtitulo",
                                   "Guia nutricional diaria segun tu perfil", 26f, TextoSuave);
        Colocar((RectTransform)subtitulo.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -165f), new Vector2(900f, 50f));
        subtitulo.alignment = TextAlignmentOptions.Center;

        TMP_Text estado = Texto(plantilla, panel.transform, "Estado", "Calcula tu perfil para ver tus metas", 26f, TextoSuave);
        Colocar((RectTransform)estado.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -215f), new Vector2(900f, 50f));
        estado.alignment = TextAlignmentOptions.Center;

        // Sub-tarjeta AGUA.
        GameObject agua = Tarjeta(panel.transform, "TarjetaAgua", new Vector2(0f, -290f), new Vector2(880f, 290f));
        TarjetaCabecera(plantilla, agua, "Agua", VerdeAgua);
        TarjetaValor(plantilla, agua, "Agua", "2.4", "L / dia");

        // Sub-tarjeta KCAL.
        GameObject kcal = Tarjeta(panel.transform, "TarjetaKcal", new Vector2(0f, -600f), new Vector2(880f, 290f));
        TarjetaCabecera(plantilla, kcal, "Energia", NaranjaKcal);
        TarjetaValor(plantilla, kcal, "Kcal", "1978", "kcal / dia");

        // Sub-tarjeta MACROS.
        GameObject macros = Tarjeta(panel.transform, "TarjetaMacros", new Vector2(0f, -910f), new Vector2(880f, 470f));
        TarjetaCabecera(plantilla, macros, "Macronutrientes", VioletaMacro);
        Fila(plantilla, macros, "Prot", "Proteinas", -150f);
        Fila(plantilla, macros, "Carbo", "Carbohidratos", -250f);
        Fila(plantilla, macros, "Grasa", "Grasas", -350f);

        GameObject botonGo = Nuevo("Btn_VolverMetas", panel.transform);
        RectTransform botonRt = (RectTransform)botonGo.transform;
        Colocar(botonRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 80f), new Vector2(420f, 92f));
        Image botonImg = botonGo.AddComponent<Image>();
        botonImg.sprite = _redondeado;
        botonImg.type = Image.Type.Sliced;
        botonImg.color = new Color(0.18f, 0.21f, 0.30f, 1f);
        Button boton = botonGo.AddComponent<Button>();
        boton.targetGraphic = botonImg;

        TMP_Text etiqueta = Texto(plantilla, botonGo.transform, "Label", "Volver", 30f, Color.white);
        Estirar((RectTransform)etiqueta.transform);
        etiqueta.fontStyle = FontStyles.Bold;
        etiqueta.alignment = TextAlignmentOptions.Center;

        if (menu != null)
        {
            UnityEventTools.AddVoidPersistentListener(boton.onClick, menu.MostrarMenuPrincipal);
        }

        panel.SetActive(false);
        return panel;
    }

    private static void TarjetaCabecera(TMP_Text plantilla, GameObject tarjeta, string texto, Color acento)
    {
        GameObject barra = Nuevo("Acento", tarjeta.transform);
        Colocar((RectTransform)barra.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, -20f), new Vector2(12f, 240f));
        Image barraImg = barra.AddComponent<Image>();
        barraImg.color = acento;
        barraImg.raycastTarget = false;

        TMP_Text titulo = Texto(plantilla, tarjeta.transform, "Titulo", texto, 34f, TextoBlanco);
        Colocar((RectTransform)titulo.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(60f, -40f), new Vector2(500f, 54f));
        titulo.fontStyle = FontStyles.Bold;
        titulo.alignment = TextAlignmentOptions.Left;
    }

    private static void TarjetaValor(TMP_Text plantilla, GameObject tarjeta, string prefijo, string numero, string sufijo)
    {
        TMP_Text valor = Texto(plantilla, tarjeta.transform, prefijo + "Numero", numero, 84f, TextoBlanco);
        Colocar((RectTransform)valor.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(60f, -140f), new Vector2(400f, 120f));
        valor.fontStyle = FontStyles.Bold;
        valor.alignment = TextAlignmentOptions.Left;

        TMP_Text unidad = Texto(plantilla, tarjeta.transform, prefijo + "Sufijo", sufijo, 30f, TextoSuave);
        Colocar((RectTransform)unidad.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(70f, -238f), new Vector2(400f, 50f));
        unidad.alignment = TextAlignmentOptions.Left;
    }

    private static void Fila(TMP_Text plantilla, GameObject tarjeta, string prefijo, string etiqueta, float y)
    {
        TMP_Text titulo = Texto(plantilla, tarjeta.transform, prefijo + "Titulo", etiqueta, 30f, TextoSuave);
        Colocar((RectTransform)titulo.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(60f, y), new Vector2(420f, 50f));
        titulo.alignment = TextAlignmentOptions.Left;

        TMP_Text numero = Texto(plantilla, tarjeta.transform, prefijo + "Numero", "--", 34f, TextoBlanco);
        Colocar((RectTransform)numero.transform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-150f, y), new Vector2(200f, 50f));
        numero.fontStyle = FontStyles.Bold;
        numero.alignment = TextAlignmentOptions.Right;

        TMP_Text sufijo = Texto(plantilla, tarjeta.transform, prefijo + "Sufijo", "g", 26f, TextoSuave);
        Colocar((RectTransform)sufijo.transform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-110f, y), new Vector2(100f, 50f));
        sufijo.alignment = TextAlignmentOptions.Left;
    }

    private static GameObject Tarjeta(Transform padre, string nombre, Vector2 posicion, Vector2 tam)
    {
        GameObject tarjeta = Nuevo(nombre, padre);
        Colocar((RectTransform)tarjeta.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), posicion, tam);

        Image img = tarjeta.AddComponent<Image>();
        img.sprite = _redondeado;
        img.type = Image.Type.Sliced;
        img.color = FondoTarjeta;

        return tarjeta;
    }

    private static GameObject CrearTutorial(GameObject panelCarga, TMP_Text plantilla, MenuController menu)
    {
        GameObject panel = Nuevo("Panel_Tutorial", panelCarga.transform.parent);
        RectTransform rt = (RectTransform)panel.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image modal = panel.AddComponent<Image>();
        modal.color = FondoModal;
        modal.raycastTarget = true;

        GameObject tarjeta = Nuevo("TarjetaTutorial", panel.transform);
        RectTransform tarjetaRt = (RectTransform)tarjeta.transform;
        tarjetaRt.anchorMin = new Vector2(0.5f, 0.5f);
        tarjetaRt.anchorMax = new Vector2(0.5f, 0.5f);
        tarjetaRt.pivot = new Vector2(0.5f, 0.5f);
        tarjetaRt.anchoredPosition = Vector2.zero;
        tarjetaRt.sizeDelta = new Vector2(880f, 1280f);

        Image tarjetaImg = tarjeta.AddComponent<Image>();
        tarjetaImg.sprite = _redondeado;
        tarjetaImg.type = Image.Type.Sliced;
        tarjetaImg.color = new Color(0.063f, 0.082f, 0.149f, 1f);

        TMP_Text indicador = Texto(plantilla, tarjeta.transform, "Indicador", "1 / 4", 26f, TextoSuave);
        Colocar((RectTransform)indicador.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -50f), new Vector2(300f, 44f));
        indicador.alignment = TextAlignmentOptions.Center;

        string[] titulos =
        {
            "1. Elige la forma",
            "2. Ajusta el tamano",
            "3. Fija el plato",
            "4. Sirve y compara"
        };

        string[] descripciones =
        {
            "En el menu lateral de la vista AR elige plato circular o cuadrado. Los aros de la mesa cambian de forma contigo.",
            "Desliza el control hasta que el circulo coincida con tu plato real: el aro proyectado sobre la mesa se redimensiona exactamente igual.",
            "Apunta al centro de la mesa y pulsa Fijar Plato. El plato queda clavado en el mundo real y deja de seguir la camara.",
            "Toca un alimento en el catalogo, revisa sus Kcal y macros y pulsa Servir en el plato. Arrastra la comida con el dedo para comparar porciones."
        };

        Sprite[] iconos =
        {
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/CuadradoGuia.png"),
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/CirculoGuia.png"),
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/ArosAR/AroFijadoCircular.png"),
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/ArosAR/AroGuiaCircular.png")
        };

        for (int i = 0; i < 4; i++)
        {
            GameObject paso = Nuevo("Paso_" + (i + 1), tarjeta.transform);
            RectTransform pasoRt = (RectTransform)paso.transform;
            pasoRt.anchorMin = new Vector2(0f, 0f);
            pasoRt.anchorMax = new Vector2(1f, 1f);
            pasoRt.offsetMin = new Vector2(40f, 220f);
            pasoRt.offsetMax = new Vector2(-40f, -110f);

            GameObject icono = Nuevo("Icono", paso.transform);
            Colocar((RectTransform)icono.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -30f), new Vector2(300f, 300f));
            Image iconoImg = icono.AddComponent<Image>();
            iconoImg.sprite = iconos[i];
            iconoImg.preserveAspect = true;
            iconoImg.raycastTarget = false;
            iconoImg.color = iconos[i] != null ? Color.white : new Color(1f, 1f, 1f, 0f);

            TMP_Text titulo = Texto(plantilla, paso.transform, "Titulo", titulos[i], 44f, TextoBlanco);
            Colocar((RectTransform)titulo.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -380f), new Vector2(760f, 70f));
            titulo.fontStyle = FontStyles.Bold;
            titulo.alignment = TextAlignmentOptions.Center;

            TMP_Text descripcion = Texto(plantilla, paso.transform, "Descripcion", descripciones[i], 28f, TextoSuave);
            Colocar((RectTransform)descripcion.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -470f), new Vector2(760f, 300f));
            descripcion.alignment = TextAlignmentOptions.Top;
            descripcion.fontStyle = FontStyles.Normal;

            paso.SetActive(i == 0);
        }

        Button anterior = BotonTutorial(plantilla, tarjeta.transform, "Btn_Anterior", "Atras",
                                        new Vector2(-230f, 110f), new Color(0.18f, 0.21f, 0.30f, 1f));
        Button siguiente = BotonTutorial(plantilla, tarjeta.transform, "Btn_Siguiente", "Siguiente",
                                         new Vector2(230f, 110f), AzulPrimario);
        Button cerrar = BotonTutorial(plantilla, tarjeta.transform, "Btn_CerrarTutorial", "Entendido",
                                      new Vector2(0f, 20f), new Color(0.14f, 0.17f, 0.25f, 1f));

        if (menu != null)
        {
            UnityEventTools.AddVoidPersistentListener(anterior.onClick, menu.TutorialAnterior);
            UnityEventTools.AddVoidPersistentListener(siguiente.onClick, menu.TutorialSiguiente);
            UnityEventTools.AddVoidPersistentListener(cerrar.onClick, menu.CerrarTutorial);
        }

        panel.SetActive(false);
        return panel;
    }

    private static Button BotonTutorial(TMP_Text plantilla, Transform padre, string nombre, string etiqueta,
                                        Vector2 posicion, Color fondo)
    {
        GameObject go = Nuevo(nombre, padre);
        Colocar((RectTransform)go.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), posicion, new Vector2(360f, 88f));

        Image img = go.AddComponent<Image>();
        img.sprite = _redondeado;
        img.type = Image.Type.Sliced;
        img.color = fondo;

        Button boton = go.AddComponent<Button>();
        boton.targetGraphic = img;

        TMP_Text texto = Texto(plantilla, go.transform, "Label", etiqueta, 28f, Color.white);
        Estirar((RectTransform)texto.transform);
        texto.fontStyle = FontStyles.Bold;
        texto.alignment = TextAlignmentOptions.Center;

        return boton;
    }

    private static void AgregarBotonesMenuPrincipal(GameObject panelMenu, TMP_Text plantilla, MenuController menu)
    {
        Button plantillaBoton = Buscar<Button>(panelMenu, "Btn_GuiaNutricional");
        if (plantillaBoton == null || menu == null)
        {
            Debug.LogWarning("[AuraMetasTutorialBuild] Sin Btn_GuiaNutricional o MenuController en el menu principal.");
            return;
        }

        Button metas = DuplicarBoton(plantillaBoton, "Btn_MisMetas", "Mis Metas");
        UnityEventTools.AddVoidPersistentListener(metas.onClick, menu.MostrarMetas);

        Button ayuda = DuplicarBoton(plantillaBoton, "Btn_Ayuda", "Como usar NutriAR");
        UnityEventTools.AddVoidPersistentListener(ayuda.onClick, menu.AbrirTutorial);
    }

    private static void AgregarBotonesMenuLateral(GameObject panelAR, TMP_Text plantilla, MenuController menu)
    {
        Transform lateral = panelAR.transform.Find("MenuLateralAR");
        if (lateral == null)
        {
            Debug.LogWarning("[AuraMetasTutorialBuild] No existe MenuLateralAR.");
            return;
        }

        Button plantillaBoton = Buscar<Button>(lateral.gameObject, "Btn_Catalogo");
        MenuLateralARController controlador = lateral.GetComponent<MenuLateralARController>();

        if (plantillaBoton == null)
        {
            Debug.LogWarning("[AuraMetasTutorialBuild] No existe Btn_Catalogo como plantilla.");
            return;
        }

        Button metas = DuplicarBoton(plantillaBoton, "Btn_MisMetas", "Mis Metas");
        Button ayuda = DuplicarBoton(plantillaBoton, "Btn_Ayuda", "Ayuda");

        if (controlador != null)
        {
            UnityEventTools.AddVoidPersistentListener(metas.onClick, controlador.AbrirMetas);
            UnityEventTools.AddVoidPersistentListener(ayuda.onClick, controlador.AbrirTutorial);
        }

        // El cajon crece para alojar 8 controles y se reordena.
        RectTransform lateralRt = (RectTransform)lateral;
        lateralRt.sizeDelta = new Vector2(520f, 1040f);

        ColocarHijo(lateral, "TituloMenuAR", -60f, 460f, 60f);
        ColocarHijo(lateral, "EtiquetaForma", -170f, 460f, 56f);
        ColocarHijo(lateral, "Btn_RefijarPlato", -250f, 460f, 84f);
        ColocarHijo(lateral, "Btn_FormaCircular", -350f, 460f, 84f);
        ColocarHijo(lateral, "Btn_FormaCuadrada", -450f, 460f, 84f);
        ColocarHijo(lateral, "Btn_Catalogo", -550f, 460f, 84f);
        ColocarHijo(lateral, "Btn_MisMetas", -650f, 460f, 84f);
        ColocarHijo(lateral, "Btn_Ayuda", -750f, 460f, 84f);
        ColocarHijo(lateral, "Btn_CerrarMenu", -860f, 460f, 84f);
    }

    private static void ColocarHijo(Transform padre, string nombre, float y, float ancho, float alto)
    {
        Transform hijo = padre.Find(nombre);
        if (hijo == null)
        {
            Debug.LogWarning("[AuraMetasTutorialBuild] No existe " + nombre + " en " + padre.name);
            return;
        }

        Colocar((RectTransform)hijo, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, y), new Vector2(ancho, alto));

        LayoutElement layout = hijo.GetComponent<LayoutElement>();
        if (layout == null)
        {
            return;
        }

        UnityEngine.Object.DestroyImmediate(layout);
    }

    private static Button DuplicarBoton(Button original, string nombre, string etiqueta)
    {
        GameObject copia = UnityEngine.Object.Instantiate(original.gameObject, original.transform.parent);
        copia.name = nombre;
        copia.transform.SetAsLastSibling();

        RectTransform rt = (RectTransform)copia.transform;
        rt.localScale = Vector3.one;

        Button boton = copia.GetComponent<Button>();
        LimpiarListeners(boton.onClick);

        TMP_Text texto = Buscar<TMP_Text>(copia, "Label");
        if (texto != null)
        {
            texto.text = etiqueta;
        }

        return boton;
    }

    private static TMP_Text Texto(TMP_Text plantilla, Transform padre, string nombre, string contenido,
                                  float tamano, Color color)
    {
        GameObject go = UnityEngine.Object.Instantiate(plantilla.gameObject, padre, false);
        go.name = nombre;

        LayoutElement layout = go.GetComponent<LayoutElement>();
        if (layout != null)
        {
            UnityEngine.Object.DestroyImmediate(layout);
        }

        TMP_Text texto = go.GetComponent<TMP_Text>();
        texto.text = contenido;
        texto.fontSize = tamano;
        texto.color = color;
        texto.raycastTarget = false;
        texto.fontStyle = FontStyles.Normal;
        texto.alignment = TextAlignmentOptions.Left;

        return texto;
    }

    private static void Estirar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static GameObject Nuevo(string nombre, Transform padre)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        return go;
    }

    private static void Colocar(RectTransform rt, Vector2 ancla, Vector2 pivote, Vector2 posicion, Vector2 tam)
    {
        rt.anchorMin = ancla;
        rt.anchorMax = ancla;
        rt.pivot = pivote;
        rt.anchoredPosition = posicion;
        rt.sizeDelta = tam;
    }

    private static T Buscar<T>(GameObject raiz, string nombre) where T : Component
    {
        Transform[] todos = raiz.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < todos.Length; i++)
        {
            if (todos[i].gameObject.name != nombre)
            {
                continue;
            }

            T componente = todos[i].GetComponent<T>();
            if (componente != null)
            {
                return componente;
            }
        }

        return null;
    }

    private static void LimpiarListeners(UnityEngine.Events.UnityEventBase evento)
    {
        for (int i = evento.GetPersistentEventCount() - 1; i >= 0; i--)
        {
            UnityEventTools.RemovePersistentListener(evento, i);
        }
    }

    private static void Asignar(SerializedObject so, string campo, Object valor)
    {
        SerializedProperty prop = so.FindProperty(campo);
        if (prop == null)
        {
            Debug.LogError("[AuraMetasTutorialBuild] Campo no encontrado: " + campo);
            return;
        }

        prop.objectReferenceValue = valor;
    }
}
