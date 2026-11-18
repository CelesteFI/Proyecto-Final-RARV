# NutriAR — Proyecto Final RARV

Documentación de estructura del proyecto Unity **NutriAR** (Realidad Aumentada para porciones de alimentos).
Este archivo es solo descriptivo: no modifica scripts, escenas ni assets.

---

## 1. Entorno y versiones

| Elemento | Versión confirmada |
|---|---|
| Unity Editor | **6000.3.27f2** (revisión `22188f149405`) — verificado en `ProjectSettings/ProjectVersion.txt` |
| Universal Render Pipeline (URP) | **17.3.0** (`com.unity.render-pipelines.universal`) |
| AR Foundation | **6.3.5** (`com.unity.xr.arfoundation`) |
| ARCore XR Plugin (Android) | **6.3.5** (`com.unity.xr.arcore`) |
| XR Simulation Environments | **2.1.1** (`com.unity.xr-content.xr-sim-environments`) |
| uGUI | **2.0.0** (`com.unity.ugui`) |
| Input System | **1.20.1** (`com.unity.inputsystem`) |

Otros paquetes instalados de interés: `com.unity.test-framework 1.6.0`, `com.unity.timeline 1.8.13`, `com.unity.visualscripting 1.9.12`, `com.unity.ai.navigation 2.0.16`, `com.unity.ide.rider 3.1.0`, `com.unity.ide.visualstudio 2.0.26`, `com.unity.collab-proxy 2.13.6`, `com.unity.multiplayer.center 1.0.1`.

Plataforma objetivo: **Android** (escena de AR con ARCore).

---

## 2. Árbol de `Assets/Scripts`

```
Assets/Scripts/
├── AR/
│   ├── ARCameraController.cs
│   ├── ARState.cs
│   ├── PlateAnchorManager.cs
│   └── PlateShapeController.cs
├── Math/
│   ├── BiometricProfile.cs
│   ├── FoodCatalog.cs
│   ├── FoodDensityData.cs
│   └── VolumeCalculator.cs
└── UI/
    ├── ARUIManager.cs
    ├── MenuController.cs
    ├── MenuLateralARController.cs
    ├── NutritionGoalsPanel.cs
    ├── PanelCarga.cs
    ├── PlateCalibrationController.cs
    └── SplashController.cs
```

**15 scripts C# activos**, organizados en tres capas: `AR` (seguimiento y anclaje), `Math` (datos y cálculo de porciones) y `UI` (paneles y flujo de navegación).

### 2.1 Capa `AR/`

| Script | Clase | Qué hace |
|---|---|---|
| `AR/ARCameraController.cs` | `ARCameraController` (MonoBehaviour) | Activa y desactiva en bloque cámara AR, fondo, planos y raycast de AR Foundation; solo están activos en calibración y en la vista AR. |
| `AR/ARState.cs` | `ARState` (estática) | Estado global compartido del plato calibrado (radio y forma) con reinicio por sesión; lo leen el anclaje y los cálculos. |
| `AR/PlateAnchorManager.cs` | `PlateAnchorManager` (MonoBehaviour) | Ancla el molde y los aros con raycast de plano horizontal, los desvincula del XR Origin y gestiona el arrastre táctil restringido al radio del plato. |
| `AR/PlateShapeController.cs` | `PlateShapeController` (MonoBehaviour) | Conmuta circular/cuadrado en `ARState`, en el sprite guía y en los aros a través de `PlateAnchorManager`, sin instanciar nada. |

### 2.2 Capa `Math/`

| Script | Clase | Qué hace |
|---|---|---|
| `Math/BiometricProfile.cs` | `BiometricProfile` (MonoBehaviour) | Calcula TMB (Mifflin-St Jeor), kcal objetivo, agua y macros a partir de los inputs del usuario; alimenta a `ARUIManager` y `NutritionGoalsPanel`. |
| `Math/FoodCatalog.cs` | `FoodCatalog` + `FoodModelSlot` (MonoBehaviour) | Resuelve y activa el slot de modelo 3D del alimento (24 slots pre-creados para los 14 alimentos en crudo/cocido y sus colores) sin instanciar en runtime. |
| `Math/FoodDensityData.cs` | `FoodDensity`, `FoodNutrition`, `FoodDensityData`, `FoodNutritionData` | Tablas estáticas de densidad (g/cm³) y macros por 100 g de los 14 alimentos en crudo y cocido; fuente de `ARUIManager` y `VolumeCalculator`. |
| `Math/VolumeCalculator.cs` | `VolumeCalculator` (MonoBehaviour) | Convierte gramos y densidad en la escala local de un cilindro de comida, recortada al radio del plato. |

### 2.3 Capa `UI/`

| Script | Clase | Qué hace |
|---|---|---|
| `UI/ARUIManager.cs` | `ARUIManager` (MonoBehaviour) | Puente entre slider, dropdown y toggle con el catálogo y la calculadora: escribe metas, detalle, línea de porción ideal y solo sirve el molde al presionar "Servir en plato". |
| `UI/MenuController.cs` | `MenuController` (MonoBehaviour) | Router de los paneles de la app con `SetActive`, enciende la vista AR y gobierna el tutorial de inicio. |
| `UI/MenuLateralARController.cs` | `MenuLateralARController` (MonoBehaviour) | Menú flotante de la vista AR; delega la forma en `PlateShapeController` y el resto en `PlateAnchorManager` y `MenuController`. |
| `UI/NutritionGoalsPanel.cs` | `NutritionGoalsPanel` (MonoBehaviour) | Muestra u oculta su `CanvasGroup` y escribe meta, kcal, agua y macros leyendo `BiometricProfile`. |
| `UI/PanelCarga.cs` | `PanelCarga` (MonoBehaviour) | Funde y autodesactiva su `CanvasGroup` en tiempo real, dejándolo inerte a raycasts. |
| `UI/PlateCalibrationController.cs` | `PlateCalibrationController` (MonoBehaviour) | Slider de 5–30 cm que redimensiona el círculo guía y el aro; "Fijar Plato" guarda `ARState`, ancla el plato y pasa a la vista AR. |
| `UI/SplashController.cs` | `SplashController` (MonoBehaviour) | Espera a que `PanelCarga` se desactive, funde el logo y pide a `MenuController` abrir el menú principal. |

> Los scripts de automatización `Aura*.cs` que aparecen en la **raíz del proyecto** (fuera de `Assets/`) son utilidades de editor para construir y cablear la escena; **no forman parte del build** ni de la lógica del juego.

---

## 3. Árbol de `Assets/Scenes`

```
Assets/Scenes/
├── NutriAR.unity
└── SampleScene.unity
```

| Escena | Descripción |
|---|---|
| `NutriAR.unity` | Escena principal y única del flujo del juego: splash, carga, menú principal, perfil biométrico, guía nutricional, metas, tutorial y vista AR con el plato anclado. Verificada en Play Mode. |
| `SampleScene.unity` | Escena de plantilla de Unity; no contiene lógica del proyecto. |

---

## 4. Árbol de `Assets/Sprites`

```
Assets/Sprites/
├── CirculoGuia.png
├── CuadradoGuia.png
├── ArosAR/
│   ├── AroFijadoCircular.png
│   ├── AroFijadoCuadrado.png
│   ├── AroGuiaCircular.png
│   └── AroGuiaCuadrado.png
└── IconosAlimentos/            (22 PNG)
    ├── Icono_Pollo.png
    ├── Icono_Res.png
    ├── Icono_Cerdo.png
    ├── Icono_FrijolNegro.png
    ├── Icono_Lentejas.png
    ├── Icono_Garbanzos.png
    ├── Icono_Espinaca.png
    ├── Icono_Zanahoria.png
    ├── Icono_Papa.png
    ├── Icono_Calabacita.png
    ├── Icono_ArrozconFrijoles.png
    ├── Icono_PicadilloRes.png
    ├── Icono_GuisodePollo.png
    ├── Icono_LentejasconCerdo.png
    ├── Icono_Arroz.png                (legado, sin uso en el catálogo)
    ├── Icono_Pasta.png                (legado, sin uso en el catálogo)
    ├── Icono_Salmon.png               (legado, sin uso en el catálogo)
    ├── Icono_Atun.png                 (legado, sin uso en el catálogo)
    ├── Icono_Huevo.png                (legado, sin uso en el catálogo)
    ├── Icono_SopadeVerduras.png       (legado, sin uso en el catálogo)
    ├── Icono_ArrozconPollo.png        (legado, sin uso en el catálogo)
    └── Icono_EnsaladaMixta.png        (legado, sin uso en el catálogo)
```

| Grupo | Uso |
|---|---|
| `ArosAR/*` (4 sprites) | Estados **guía** y **fijado** de las dos formas del plato; se alternan con `SetActive` desde `PlateAnchorManager` / `PlateShapeController`. |
| `CirculoGuia.png`, `CuadradoGuia.png` | Sprites guía heredados de la primera versión del sistema de aros. |
| `IconosAlimentos/` — 14 PNG | Íconos de los 14 alimentos finales, usados por el dropdown del `Panel_AR` y por las 14 tarjetas de `GridTarjetas` en `Panel_GuiaNutricional`. |
| `IconosAlimentos/` — 8 PNG | Íconos de alimentos retirados del catálogo; permanecen en disco como respaldo histórico. |

---

## 5. Catálogo de alimentos (estado actual)

14 alimentos: **Pollo, Res, Cerdo, Frijol Negro, Lentejas, Garbanzos, Espinaca, Zanahoria, Papa, Calabacita, Arroz con Frijoles, Picadillo de Res, Guisado de Pollo, Lentejas con Cerdo**.

- Los 10 ingredientes tienen estado **Crudo** y **Cocido**; los 4 guisados solo **Cocido**.
- Cálculo de porción: `G = volumen del slider (cm³) × densidad del alimento/estado`, `Valor = (Valor por 100 g / 100) × G`.
- Línea "Ideal para ti: {g} g · {kcal} kcal" derivada del `BiometricProfile`, recalculada solo al modificar el perfil.
- El modelo 3D del plato (`MoldeHolograma`) permanece desactivado al fijar el plato y solo se enciende al presionar "Servir en plato".
