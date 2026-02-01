using UnityEngine;
using PurrNet;
public class BallisticProjectile : NetworkBehaviour
{
    [Header("Balística")]
    [SerializeField] private float speed = 250f;
    [SerializeField] private float gravityScale = 1f;
    [SerializeField] private float maxLifetime = 5f;
    [SerializeField] private LayerMask hitLayers;

    private Collider ownCollider;

    private Vector3 _velocity;
    private int _damage;
    private ProjectileGun _ownerGun;
    private bool _isActive = false;

    // Inicializamos pasando la referencia del ProjectileGun
    public void Initialize(int damage, Vector3 direction, ProjectileGun gunScript, Collider ownCollider)
    {
        _damage = damage;
        _velocity = direction * speed;
        _ownerGun = gunScript; 
        _isActive = true;
        this.ownCollider = ownCollider;
        
        // Destrucción local por tiempo
        Destroy(gameObject, maxLifetime);
    }

    private void Update()
    {
        if (!_isActive) return;
        MoveBullet(Time.deltaTime);
    }

    private void MoveBullet(float deltaTime)
    {
        Vector3 currentPos = transform.position;
        _velocity += Physics.gravity * gravityScale * deltaTime;
        Vector3 displacement = _velocity * deltaTime;
        Vector3 nextPos = currentPos + displacement;

        if (Physics.Linecast(currentPos, nextPos, out RaycastHit hit, hitLayers))
        {
            HandleImpact(hit);
        }
        else
        {
            transform.position = nextPos;
            if (_velocity != Vector3.zero) transform.forward = _velocity.normalized;
        }
    }

    private void HandleImpact(RaycastHit hit)
    {
        // Ignorar colisión con el dueño del arma
        if (hit.collider == ownCollider) return;

        // --- LÓGICA DE IMPACTO ---
        
        // 1. Si golpeamos a un jugador
        if (hit.collider.TryGetComponent(out PlayerHealth ph))
        {
            _ownerGun.ReportPlayerHit(ph, _damage);
        }
        // 2. Si golpeamos un objeto destructible
        else if (hit.collider.TryGetComponent(out HealthObject obj))
        {
            _ownerGun.ReportObjectHit(obj, _damage, hit.point);
        }

        // Efectos visuales
        
        Destroy(gameObject);
    }
}