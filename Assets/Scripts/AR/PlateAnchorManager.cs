using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Proyecta el plato sobre la superficie detectada y lo clava en el mundo real.
/// El raycast central solo acepta planos HORIZONTALES (PlaneWithinPolygon + normal casi vertical);
/// mientras el plato no esta fijado, la guia dinamica sigue esa superficie. Al pulsar "Fijar Plato"
/// se toma UNA pose, se escriben el molde y los aros, y el conjunto se DESVINCULA del XR Origin
/// (pasa a colgar de la raiz de la escena en coordenadas absolutas de mundo), de modo que ARCore
/// no puede volver a moverlo: desplazar o inclinar el telefono solo cambia la perspectiva.
/// Los cuatro aros (guia/fijado x circular/cuadrado) existen pre-creados en la jerarquia y solo se
/// conmutan con SetActive: cero Instantiate en runtime.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlateAnchorManager : MonoBehaviour
{
    [Header("Referencias de escena")]
    [SerializeField] private Transform moldeHolograma;
    [SerializeField] private Button btnFijarPlato;
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private ARCameraController arController;
    [SerializeField] private MenuController menu;

    [Header("Aros AR pre-creados (guia dinamica / fijado)")]
    [Tooltip("Guia dinamica circular: sigue el plano detectado antes de fijar el plato.")]
    [SerializeField] private GameObject spriteGuiaCircular;
    [Tooltip("Aro fijado circular: se enciende al pulsar Fijar Plato.")]
    [SerializeField] private GameObject spriteFijadoCircular;
    [Tooltip("Guia dinamica cuadrada: sigue el plano detectado antes de fijar el plato.")]
    [SerializeField] private GameObject spriteGuiaCuadrado;
    [Tooltip("Aro fijado cuadrado: se enciende al pulsar Fijar Plato.")]
    [SerializeField] private GameObject spriteFijadoCuadrado;
    [Tooltip("Raiz comun de los 4 aros (Canvas en espacio de mundo): fija la escala de todos a la vez.")]
    [SerializeField] private Transform arosRaiz;

    [Header("Arrastre tactil de la comida")]
    [Tooltip("Camara AR con la que se proyecta el dedo sobre el plano del plato anclado.")]
    [SerializeField] private Camera camaraAR;

    [Header("Altura sobre el plano")]
    [Tooltip("El pivote del cilindro esta en su centro: media altura (2.5 cm / 2 = 1.25 cm) en metros.")]
    [SerializeField, Min(0f)] private float alturaSobrePlano = 0.0125f;

    [Header("Anclaje y aros")]
    [Tooltip("Separacion del aro respecto a la superficie del plano, para que no se solape con la mesa.")]
    [SerializeField, Min(0f)] private float separacionAro = 0.002f;

    [Tooltip("Radio del contorno dibujado dentro del sprite, como fraccion de su semiancho (250/256).")]
    [SerializeField, Range(0.3f, 1f)] private float radioPintadoAro = 0.9766f;

    [Tooltip("Coseno maximo admitido entre la normal del plano y la vertical: 0.87 ~ 30 grados de inclinacion.")]
    [SerializeField, Range(0f, 1f)] private float cosenoMaxInclinacion = 0.87f;

    // Buffer reutilizado: cero asignaciones por frame.
    private readonly List<ARRaycastHit> _impactos = new List<ARRaycastHit>(8);

    private bool _fijado;
    private bool _hayPose;
    private Pose _ultimaPose = Pose.identity;

    // Pose clavada al pulsar "Fijar Plato": es la que se reaplica al aro y al molde.
    private Pose _poseAnclada = Pose.identity;
    private bool _hayPoseAnclada;

    // Desplazamiento de la comida respecto al centro del plato, en metros sobre los ejes del
    // plato (x = derecha, y = adelante). Siempre recortado para que no se salga del borde.
    private Vector2 _desplazamientoComida;
    private bool _arrastrandoComida;

    // Renderer del molde cacheado: su caja da el radio real de la comida en metros.
    private Renderer _rendererMolde;

    // Padre original de molde y aros (el XR Origin): permite volver a vincularlos al recalibrar.
    private Transform _padreMolde;
    private Transform _padreAros;

    // Radio que el usuario esta ajustando en el slider de calibracion (cm), aun sin confirmar.
    private float _radioVisualCm;

    /// <summary>true cuando el plato ya esta anclado y el conjunto no vuelve a moverse.</summary>
    public bool Fijado => _fijado;

    /// <summary>true cuando la pose anclada procede de un plano real detectado.</summary>
    public bool AncladoASuperficie => _hayPoseAnclada;

    private void Start()
    {
        if (!ValidarReferencias())
        {
            enabled = false;
            return;
        }

        _padreMolde = moldeHolograma != null ? moldeHolograma.parent : null;
        _padreAros = arosRaiz != null ? arosRaiz.parent : null;

        _fijado = false;
        _hayPoseAnclada = false;
        _desplazamientoComida = Vector2.zero;
        _arrastrandoComida = false;
        _rendererMolde = moldeHolograma != null ? moldeHolograma.GetComponentInChildren<Renderer>(true) : null;

        // En la escena el molde arranca oculto: nada de hologramas en splash, menu ni calibracion.
        OcultarConjunto();

        // En la vista AR el boton "Fijar Plato" ya no ancla: devuelve a reajustar el tamano.
        btnFijarPlato.onClick.AddListener(VolverACalibracion);
    }

    private void OnDestroy()
    {
        if (btnFijarPlato != null)
        {
            btnFijarPlato.onClick.RemoveListener(VolverACalibracion);
        }
    }

    private void Update()
    {
        // Anclado: el molde no vuelve a moverse con el plano; solo responde al dedo.
        if (_fijado)
        {
            GestionarArrastreComida();
            return;
        }

        // Fuera de las pantallas de AR la camara esta apagada: no se consulta el raycast.
        if (arController != null && !arController.ARVisible)
        {
            return;
        }

        if (!TryRaycastCentro(out Pose pose))
        {
            return;
        }

        _ultimaPose = pose;
        _hayPose = true;

        // El molde sigue oculto hasta fijar; la guia dinamica si marca la superficie valida.
        ColocarMolde(pose);
        ColocarAros(pose);
    }

    /// <summary>
    /// Plato congelado: cada frame se reafirman molde y aro en la pose anclada, de modo que
    /// desplazar o inclinar el telefono cambia solo la perspectiva y nunca la posicion.
    /// </summary>
    private void LateUpdate()
    {
        if (!_fijado)
        {
            return;
        }

        AplicarPoseComida();
        ColocarAros(PoseDelPlato());
    }

    /// <summary>
    /// Confirma el plato: proyecta el circulo sobre la mesa detectada en este preciso instante,
    /// clava esa pose en el mundo, desvincula el conjunto del XR Origin y conmuta la guia
    /// dinamica por el aro fijado de la forma elegida.
    /// </summary>
    public void FijarPlato()
    {
        Pose pose;

        // Cada anclaje arranca con la comida centrada en el plato.
        _desplazamientoComida = Vector2.zero;
        _arrastrandoComida = false;

        if (TryRaycastCentro(out pose))
        {
            // Camino normal: hay mesa horizontal bajo el centro de pantalla.
            _ultimaPose = pose;
            _hayPose = true;
        }
        else if (_hayPose)
        {
            // Sin impacto justo ahora (p. ej. el plano salio de cuadro): se respeta la ultima superficie.
            pose = _ultimaPose;
            Debug.LogWarning("[PlateAnchorManager] Al fijar no habia plano horizontal bajo el centro de " +
                             "pantalla: se ancla el plato en la ultima superficie detectada.", this);
        }
        else
        {
            // Editor o dispositivo sin planos todavia: no se inventa una normal, se respeta el molde.
            Debug.LogWarning("[PlateAnchorManager] No hay superficie horizontal detectada: el plato queda " +
                             "anclado en la posicion actual del molde. Escanea la mesa y vuelve a fijar.", this);
            _hayPoseAnclada = false;
            _fijado = true;
            DesvincularDeARCore();
            MostrarConjunto();
            return;
        }

        _poseAnclada = pose;
        _hayPoseAnclada = true;
        _fijado = true;

        // Coordenadas absolutas de mundo: el conjunto deja de ser hijo del XR Origin.
        DesvincularDeARCore();

        ColocarMolde(_poseAnclada);
        ColocarAros(_poseAnclada);
        MostrarConjunto();
    }

    /// <summary>
    /// Vuelve a seguir el plano detectado: esconde el molde y los aros fijados, vuelve a vincular
    /// el conjunto al XR Origin y enciende la guia dinamica de la forma elegida.
    /// </summary>
    public void ReiniciarColocacion()
    {
        _fijado = false;
        _hayPoseAnclada = false;
        _desplazamientoComida = Vector2.zero;
        _arrastrandoComida = false;

        RestaurarVinculoConARCore();
        OcultarConjunto();
        AplicarEstadoAros();
    }

    /// <summary>Conmuta guia / fijado segun la forma del plato elegida (llamado por PlateShapeController).</summary>
    public void AplicarForma(PlateShape forma)
    {
        AplicarEstadoAros();
        EscalarAros();
    }

    /// <summary>Escala los aros en vivo mientras el usuario ajusta el slider de calibracion.</summary>
    public void EstablecerRadioVisual(float radioCm)
    {
        _radioVisualCm = Mathf.Max(radioCm, ARState.MinPlateRadiusCm);
        EscalarAros();
    }

    /// <summary>Reajustar el tamano del plato: el boton de la vista AR vuelve a la calibracion.</summary>
    public void VolverACalibracion()
    {
        if (menu != null)
        {
            menu.MostrarCalibracion();
        }
    }

    /// <summary>
    /// Raycast al centro de pantalla contra planos ya detectados. Solo se aceptan superficies
    /// horizontales: la normal del plano debe apuntar casi a la vertical (dot >= cosenoMaxInclinacion).
    /// </summary>
    private bool TryRaycastCentro(out Pose pose)
    {
        pose = Pose.identity;

        if (raycastManager == null)
        {
            return false;
        }

        Vector2 centroPantalla = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        if (!raycastManager.Raycast(centroPantalla, _impactos, TrackableType.PlaneWithinPolygon))
        {
            return false;
        }

        for (int i = 0; i < _impactos.Count; i++)
        {
            Pose candidata = _impactos[i].pose;

            // Descarta paredes, techos y cualquier plano inclinado.
            if (Vector3.Dot(candidata.up, Vector3.up) < cosenoMaxInclinacion)
            {
                continue;
            }

            pose = candidata;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Escribe la pose del plato en el molde: apoyado en la normal del plano y elevado media
    /// altura para que el cilindro no se hunda bajo la mesa.
    /// </summary>
    private void ColocarMolde(Pose pose)
    {
        if (moldeHolograma == null)
        {
            return;
        }

        moldeHolograma.SetPositionAndRotation(pose.position + pose.up * alturaSobrePlano,
                                              Quaternion.FromToRotation(Vector3.up, pose.up));
    }

    /// <summary>
    /// Pose de la comida ya anclada: centro del plato mas el desplazamiento tactil, apoyada en
    /// la normal de la mesa. No toca la escala, de modo que el modelo nunca se deforma.
    /// </summary>
    private void AplicarPoseComida()
    {
        if (moldeHolograma == null)
        {
            return;
        }

        Pose pose = PoseDelPlato();
        Quaternion rotacion = Quaternion.FromToRotation(Vector3.up, pose.up);

        Vector3 centro = pose.position + (pose.up * alturaSobrePlano);
        Vector3 desplazamiento = rotacion * new Vector3(_desplazamientoComida.x, 0f, _desplazamientoComida.y);

        moldeHolograma.SetPositionAndRotation(centro + desplazamiento, rotacion);
    }

    /// <summary>
    /// Arrastre de la comida con el dedo dentro del area del plato. Solo se agarra cuando el
    /// toque cae sobre el plato; a partir de ahi la posicion del dedo proyectada sobre el plano
    /// se recorta con Vector3.ClampMagnitude para que la comida nunca se salga del borde.
    /// </summary>
    private void GestionarArrastreComida()
    {
        if (camaraAR == null)
        {
            return;
        }

        if (arController != null && !arController.ARVisible)
        {
            _arrastrandoComida = false;
            return;
        }

        Touchscreen pantalla = Touchscreen.current;
        if (pantalla == null)
        {
            return;
        }

        TouchControl dedo = null;
        for (int i = 0; i < pantalla.touches.Count; i++)
        {
            if (pantalla.touches[i].press.isPressed)
            {
                dedo = pantalla.touches[i];
                break;
            }
        }

        if (dedo == null)
        {
            _arrastrandoComida = false;
            return;
        }

        Vector2 posicionDedo = dedo.position.ReadValue();

        if (!_arrastrandoComida)
        {
            if (!dedo.press.wasPressedThisFrame)
            {
                return;
            }

            if (!TryPuntoSobrePlato(posicionDedo, out Vector3 inicio))
            {
                return;
            }

            if (!DentroDelPlato(ALocal(inicio), RadioPlatoM()))
            {
                return;
            }

            _arrastrandoComida = true;
        }

        if (!TryPuntoSobrePlato(posicionDedo, out Vector3 punto))
        {
            return;
        }

        _desplazamientoComida = LimitarAlPlato(ALocal(punto));
    }

    /// <summary>Punto del mundo (sobre el plano del plato) bajo el dedo. Sin colliders ni fisicas.</summary>
    private bool TryPuntoSobrePlato(Vector2 posicionPantalla, out Vector3 punto)
    {
        punto = Vector3.zero;

        Pose pose = PoseDelPlato();
        Ray rayo = camaraAR.ScreenPointToRay(new Vector3(posicionPantalla.x, posicionPantalla.y, 0f));
        Plane plano = new Plane(pose.up, pose.position);

        if (!plano.Raycast(rayo, out float distancia) || distancia <= 0f)
        {
            return false;
        }

        punto = rayo.GetPoint(distancia);
        return true;
    }

    /// <summary>Pasa un punto del mundo al sistema local del plato (x derecha, z adelante).</summary>
    private Vector3 ALocal(Vector3 puntoMundo)
    {
        Pose pose = PoseDelPlato();
        Quaternion rotacion = Quaternion.FromToRotation(Vector3.up, pose.up);

        return Quaternion.Inverse(rotacion) * (puntoMundo - pose.position);
    }

    /// <summary>
    /// Desplazamiento pedido recortado a la zona util: radio del plato menos radio de la comida.
    /// En plato circular se usa Vector3.ClampMagnitude; en cuadrado, recorte por eje.
    /// </summary>
    private Vector2 LimitarAlPlato(Vector3 local)
    {
        float limite = Mathf.Max(0f, RadioPlatoM() - RadioComidaM());

        if (ARState.EsPlatoCuadrado)
        {
            return new Vector2(Mathf.Clamp(local.x, -limite, limite),
                               Mathf.Clamp(local.z, -limite, limite));
        }

        Vector3 recortado = Vector3.ClampMagnitude(new Vector3(local.x, 0f, local.z), limite);
        return new Vector2(recortado.x, recortado.z);
    }

    /// <summary>true si el punto local cae dentro del area del plato calibrado.</summary>
    private bool DentroDelPlato(Vector3 local, float radioPlato)
    {
        if (ARState.EsPlatoCuadrado)
        {
            return Mathf.Abs(local.x) <= radioPlato && Mathf.Abs(local.z) <= radioPlato;
        }

        return new Vector2(local.x, local.z).sqrMagnitude <= radioPlato * radioPlato;
    }

    /// <summary>Radio del plato en metros (1 unidad = 1 m).</summary>
    private float RadioPlatoM()
    {
        // Mientras se calibra manda el slider; ya fijado, el radio confirmado en ARState.
        if (!_fijado && _radioVisualCm > 0f)
        {
            return _radioVisualCm * 0.01f;
        }

        float radioCm = ARState.HasPlate ? ARState.SelectedPlateRadius : ARState.DefaultPlateRadiusCm;
        return Mathf.Max(radioCm, ARState.MinPlateRadiusCm) * 0.01f;
    }

    /// <summary>Radio real de la comida en metros, leido de la caja del molde visible.</summary>
    private float RadioComidaM()
    {
        if (_rendererMolde == null && moldeHolograma != null)
        {
            _rendererMolde = moldeHolograma.GetComponentInChildren<Renderer>(true);
        }

        if (_rendererMolde == null)
        {
            return 0f;
        }

        Bounds caja = _rendererMolde.bounds;
        return Mathf.Min(Mathf.Max(caja.extents.x, caja.extents.z), RadioPlatoM());
    }

    /// <summary>
    /// Saca el molde y los aros del XR Origin: quedan en la raiz de la escena, en coordenadas
    /// absolutas de mundo. Se respeta el localScale de cada uno (la comida jamas se deforma).
    /// </summary>
    private void DesvincularDeARCore()
    {
        if (moldeHolograma != null && moldeHolograma.parent != null)
        {
            Vector3 escala = moldeHolograma.localScale;
            moldeHolograma.SetParent(null, true);
            moldeHolograma.localScale = escala;
        }

        if (arosRaiz != null && arosRaiz.parent != null)
        {
            Vector3 escala = arosRaiz.localScale;
            arosRaiz.SetParent(null, true);
            arosRaiz.localScale = escala;
        }
    }

    /// <summary>Devuelve molde y aros al XR Origin para que vuelvan a seguir el trackeo.</summary>
    private void RestaurarVinculoConARCore()
    {
        if (moldeHolograma != null && _padreMolde != null && moldeHolograma.parent != _padreMolde)
        {
            Vector3 escala = moldeHolograma.localScale;
            moldeHolograma.SetParent(_padreMolde, true);
            moldeHolograma.localScale = escala;
        }

        if (arosRaiz != null && _padreAros != null && arosRaiz.parent != _padreAros)
        {
            Vector3 escala = arosRaiz.localScale;
            arosRaiz.SetParent(_padreAros, true);
            arosRaiz.localScale = escala;
        }
    }

    /// <summary>Enciende el aro fijado de la forma activa y apaga el resto.</summary>
    private void AplicarEstadoAros()
    {
        bool cuadrado = ARState.EsPlatoCuadrado;

        ConmutarAro(spriteGuiaCircular, !cuadrado && !_fijado);
        ConmutarAro(spriteGuiaCuadrado, cuadrado && !_fijado);
        ConmutarAro(spriteFijadoCircular, !cuadrado && _fijado);
        ConmutarAro(spriteFijadoCuadrado, cuadrado && _fijado);
    }

    private static void ConmutarAro(GameObject aro, bool activo)
    {
        if (aro != null && aro.activeSelf != activo)
        {
            aro.SetActive(activo);
        }
    }

    /// <summary>
    /// Escala los 4 aros con la misma formula: diametro del plato dividido por el radio pintado
    /// en el sprite. Guia y fijado comparten escala, asi que el cambio de estado es 1:1.
    /// </summary>
    private void EscalarAros()
    {
        if (arosRaiz == null)
        {
            return;
        }

        float radio = RadioPlatoM();
        float fraccion = radioPintadoAro > 0.01f ? radioPintadoAro : 1f;
        float escala = (2f * radio) / fraccion;

        arosRaiz.localScale = new Vector3(escala, escala, escala);
    }

    /// <summary>Sitúa la raiz de los aros tumbada sobre la superficie detectada.</summary>
    private void ColocarAros(Pose pose)
    {
        if (arosRaiz == null)
        {
            return;
        }

        // El Canvas mira con su +Z: se alinea con la normal de la mesa para quedar plano.
        arosRaiz.SetPositionAndRotation(pose.position + (pose.up * separacionAro),
                                        Quaternion.LookRotation(pose.up, Vector3.forward));
        EscalarAros();
    }

    private void MostrarConjunto()
    {
        if (moldeHolograma != null && !moldeHolograma.gameObject.activeSelf)
        {
            moldeHolograma.gameObject.SetActive(true);
        }

        AplicarEstadoAros();
        EscalarAros();
    }

    private void OcultarConjunto()
    {
        if (moldeHolograma != null && moldeHolograma.gameObject.activeSelf)
        {
            moldeHolograma.gameObject.SetActive(false);
        }

        ConmutarAro(spriteGuiaCircular, false);
        ConmutarAro(spriteGuiaCuadrado, false);
        ConmutarAro(spriteFijadoCircular, false);
        ConmutarAro(spriteFijadoCuadrado, false);
    }

    /// <summary>Pose vigente del plato: la anclada, la ultima detectada o la del propio molde.</summary>
    private Pose PoseDelPlato()
    {
        if (_hayPoseAnclada)
        {
            return _poseAnclada;
        }

        if (_hayPose)
        {
            return _ultimaPose;
        }

        return moldeHolograma != null
            ? new Pose(moldeHolograma.position, moldeHolograma.rotation)
            : new Pose(Vector3.zero, Quaternion.identity);
    }

    private bool ValidarReferencias()
    {
        bool valido = true;

        valido &= Validar(moldeHolograma, nameof(moldeHolograma));
        valido &= Validar(btnFijarPlato, nameof(btnFijarPlato));
        valido &= Validar(raycastManager, nameof(raycastManager));
        valido &= Validar(arosRaiz, nameof(arosRaiz));

        if (spriteGuiaCircular == null || spriteFijadoCircular == null ||
            spriteGuiaCuadrado == null || spriteFijadoCuadrado == null)
        {
            Debug.LogWarning("[PlateAnchorManager] Faltan aros por asignar: la conmutacion guia/fijado " +
                             "quedara incompleta en alguna de las dos formas.", this);
        }

        if (camaraAR == null)
        {
            Debug.LogWarning("[PlateAnchorManager] Sin camaraAR: el arrastre tactil de la comida queda " +
                             "desactivado.", this);
        }

        return valido;
    }

    private bool Validar(Object referencia, string nombreCampo)
    {
        if (referencia != null)
        {
            return true;
        }

        Debug.LogError($"[PlateAnchorManager] Referencia sin asignar: {nombreCampo}.", this);
        return false;
    }
}
