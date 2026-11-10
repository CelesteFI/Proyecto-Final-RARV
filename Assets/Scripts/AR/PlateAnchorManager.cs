using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Proyecta el plato sobre la superficie detectada y lo clava en el mundo real.
/// Mientras se calibra, el molde sigue con un raycast al centro de pantalla (PlaneWithinPolygon)
/// para saber donde esta la mesa; al pulsar "Fijar Plato" se toma UNA sola pose del plano, se
/// escribe en el Transform del molde y del aro, y el sistema deja de consultar el raycast: a
/// partir de ahi el conjunto queda rigido en coordenadas de mundo, de modo que desplazar o
/// inclinar el telefono solo cambia la perspectiva. El XR Origin es el origen de tracking y no
/// se mueve (el que se mueve es la camara), asi que el molde, al ser hijo suyo, conserva su
/// posicion real sobre la mesa.
/// No instancia objetos: solo mueve y activa Transforms ya serializados.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlateAnchorManager : MonoBehaviour
{
    [Header("Referencias de escena")]
    [SerializeField] private Transform moldeHolograma;
    [SerializeField] private Transform aroReferencia;
    [SerializeField] private Button btnFijarPlato;
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private ARCameraController arController;
    [SerializeField] private MenuController menu;

    [Header("Altura sobre el plano")]
    [Tooltip("El pivote del cilindro esta en su centro: media altura (2.5 cm / 2 = 1.25 cm) en metros.")]
    [SerializeField, Min(0f)] private float alturaSobrePlano = 0.0125f;

    [Header("Aro de referencia")]
    [Tooltip("Separacion del aro respecto a la superficie del plano, para que no se solape con la mesa.")]
    [SerializeField, Min(0f)] private float separacionAro = 0.002f;

    // Buffer reutilizado: cero asignaciones por frame.
    private readonly List<ARRaycastHit> _impactos = new List<ARRaycastHit>(8);

    private bool _fijado;
    private bool _hayPose;
    private Pose _ultimaPose = Pose.identity;

    // Pose clavada al pulsar "Fijar Plato": es la que se reaplica al aro y al molde.
    private Pose _poseAnclada = Pose.identity;
    private bool _hayPoseAnclada;

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

        _fijado = false;
        _hayPoseAnclada = false;

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
        // Anclado: el molde no vuelve a moverse aunque el usuario se desplace o incline la camara.
        if (_fijado)
        {
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

        ColocarMolde(pose);
    }

    /// <summary>
    /// Confirma el plato: proyecta el circulo sobre la mesa detectada en este preciso instante,
    /// clava esa pose en el mundo y muestra el molde y el aro punteado con el radio calibrado.
    /// </summary>
    public void FijarPlato()
    {
        Pose pose;

        if (TryRaycastCentro(out pose))
        {
            // Camino normal: hay mesa bajo el centro de pantalla.
            _ultimaPose = pose;
            _hayPose = true;
        }
        else if (_hayPose)
        {
            // Sin impacto justo ahora (p. ej. el plano salio de cuadro): se respeta la ultima superficie.
            pose = _ultimaPose;
            Debug.LogWarning("[PlateAnchorManager] Al fijar no habia plano bajo el centro de pantalla: " +
                             "se ancla el plato en la ultima superficie detectada.", this);
        }
        else
        {
            // Editor o dispositivo sin planos todavia: no se inventa una normal, se respeta el molde.
            Debug.LogWarning("[PlateAnchorManager] No hay superficie detectada: el plato queda anclado en la " +
                             "posicion actual del molde. Escanea la mesa moviendo el telefono y vuelve a fijar.", this);
            _hayPoseAnclada = false;
            _fijado = true;
            MostrarConjunto();
            return;
        }

        _poseAnclada = pose;
        _hayPoseAnclada = true;
        _fijado = true;

        ColocarMolde(_poseAnclada);
        MostrarConjunto();
    }

    /// <summary>Vuelve a seguir el plano detectado y esconde molde y aro (pantalla de calibracion).</summary>
    public void ReiniciarColocacion()
    {
        _fijado = false;
        _hayPoseAnclada = false;
        OcultarConjunto();
    }

    /// <summary>Reajustar el tamano del plato: el boton de la vista AR vuelve a la calibracion.</summary>
    public void VolverACalibracion()
    {
        if (menu != null)
        {
            menu.MostrarCalibracion();
        }
    }

    /// <summary>Raycast al centro de pantalla contra planos ya detectados (la mesa).</summary>
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

        pose = _impactos[0].pose;
        return true;
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

    private void MostrarConjunto()
    {
        if (moldeHolograma != null && !moldeHolograma.gameObject.activeSelf)
        {
            moldeHolograma.gameObject.SetActive(true);
        }

        ColocarAro();

        if (aroReferencia != null && !aroReferencia.gameObject.activeSelf)
        {
            aroReferencia.gameObject.SetActive(true);
        }
    }

    private void OcultarConjunto()
    {
        if (moldeHolograma != null && moldeHolograma.gameObject.activeSelf)
        {
            moldeHolograma.gameObject.SetActive(false);
        }

        if (aroReferencia != null && aroReferencia.gameObject.activeSelf)
        {
            aroReferencia.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Clava el aro en la mesa, apoyado en la normal del plano. El aro se modela como un circulo
    /// de radio 1, asi que su escala es exactamente el radio del plato en metros (1 unidad = 1 m).
    /// </summary>
    private void ColocarAro()
    {
        if (aroReferencia == null)
        {
            return;
        }

        Pose pose = PoseDelPlato();

        float radioCm = ARState.HasPlate ? ARState.SelectedPlateRadius : ARState.DefaultPlateRadiusCm;
        float radioMetros = Mathf.Max(radioCm, ARState.MinPlateRadiusCm) * 0.01f;

        aroReferencia.SetPositionAndRotation(pose.position + (pose.up * separacionAro),
                                            Quaternion.FromToRotation(Vector3.up, pose.up));
        aroReferencia.localScale = new Vector3(radioMetros, radioMetros, radioMetros);
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
        valido &= Validar(aroReferencia, nameof(aroReferencia));
        valido &= Validar(btnFijarPlato, nameof(btnFijarPlato));
        valido &= Validar(raycastManager, nameof(raycastManager));

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
