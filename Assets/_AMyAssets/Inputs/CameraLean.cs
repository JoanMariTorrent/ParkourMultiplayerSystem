using Unity.Mathematics;
using UnityEngine;
using PurrNet;

public class CameraLean : NetworkBehaviour
{
    [Header("Timing")]
    [SerializeField] private float attackDamping = 0.5f;
    [SerializeField] private float decayDamping = 0.3f;

    [Header("Strength")]
    [SerializeField] private float walkStrenght = 0.075f;
    [SerializeField] private float slideStrenght = 0.2f;
    [SerializeField] private float strenghtResponse = 5f; // transición entre walk/slide
    [SerializeField] private float maxLeanAngle = 10f;    // grados máximos de inclinación

    private Vector3 _dampedAcceleration;
    private Vector3 _dampedAccelerationVel;
    private float _smoothStrenght;
    private float _strenghtVel;

    protected override void OnSpawned()
    {
        base.OnSpawned();
        
        enabled = isOwner;
    }

    public void Initialize()
    {
        _smoothStrenght = walkStrenght;
    }

    public void UpdateLean(float deltaTime, bool sliding, Vector3 acceleration, Vector3 up)
    {
        // Proyectamos la aceleración en el plano definido por "up"
        var planarAcceleration = Vector3.ProjectOnPlane(acceleration, up);

        // Limitamos su magnitud para evitar picos
        planarAcceleration = Vector3.ClampMagnitude(planarAcceleration, 10f);

        // Seleccionamos el damping adecuado (ataque o decaimiento)
        var damping = planarAcceleration.magnitude > _dampedAcceleration.magnitude
            ? attackDamping
            : decayDamping;

        // Suavizamos la aceleración
        _dampedAcceleration = Vector3.SmoothDamp(
            _dampedAcceleration,
            planarAcceleration,
            ref _dampedAccelerationVel,
            damping,
            float.PositiveInfinity,
            deltaTime
        );

        if (_dampedAcceleration.sqrMagnitude < 0.0001f)
            return;

        // Calculamos el eje de inclinación
        var leanAxis = Vector3.Cross(_dampedAcceleration.normalized, up).normalized;

        // Transición suave entre fuerza de caminata y deslizamiento
        var targetStrenght = sliding ? slideStrenght : walkStrenght;

        _smoothStrenght = Mathf.SmoothDamp(
            _smoothStrenght,
            targetStrenght,
            ref _strenghtVel,
            0.25f // tiempo de transición (ajústalo si quieres más rápido/lento)
        );

        // Calculamos el ángulo final con límite
        float leanAngle = Mathf.Min(_dampedAcceleration.magnitude * _smoothStrenght, maxLeanAngle);

        // Aplicamos la rotación local (respecto al padre)
        transform.localRotation = Quaternion.AngleAxis(-leanAngle, leanAxis);
    }
}
