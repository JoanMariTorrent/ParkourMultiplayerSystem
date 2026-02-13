using UnityEngine;
using PurrNet;

public class GrapplingHook : Utility
{
    [Header("Grapple Logic")]
    [SerializeField] private float maxDistance = 40f;
    [SerializeField] private float maxGrappleDuration = 3f;
    [SerializeField] private LayerMask hookLayer;
    [SerializeField] private LineRenderer lineRenderer; 
    [SerializeField] private Transform ropeOrigin; 

    private bool isGrappling = false;
    private Vector3 currentHookPoint;
    private float currentGrappleTimer = 0f;

    // Usamos el input del padre
    public override void UseItem(bool inputDown, bool inputHeld, bool inputUp)
    {
        if (isInCooldown) return;

        if (inputDown && !isGrappling)
        {
            AttemptGrapple();
        }
        else if (inputUp && isGrappling)
        {
            StopGrappling();
        }
    }

    private void Update()
    {
        if (isGrappling && isOwner)
        {
            currentGrappleTimer += Time.deltaTime;

            if (currentGrappleTimer >= maxGrappleDuration)
            {
                StopGrappling();
            }
        }

        // Visuales
        if (isGrappling && lineRenderer != null && ropeOrigin != null)
        {
            lineRenderer.enabled = true;
            lineRenderer.SetPosition(0, ropeOrigin.position);
            lineRenderer.SetPosition(1, currentHookPoint);
        }
        else if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
    }

    private void AttemptGrapple()
    {
        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit hit, maxDistance, hookLayer))
        {
            isGrappling = true;
            currentHookPoint = hit.point;
            currentGrappleTimer = 0f;

            playerCharacter.StartGrapple(currentHookPoint);

            RequestGrappleServerRpc(currentHookPoint, true);
        }
    }

    private void StopGrappling()
    {
        if (!isGrappling) return;

        isGrappling = false;
        currentGrappleTimer = 0f; 
        
        playerCharacter.StopGrapple();

        RequestGrappleServerRpc(Vector3.zero, false);
        
        StartCoroutine(CooldownRoutine());
    }

    // --- Lógica de Red ---
    [ServerRpc]
    private void RequestGrappleServerRpc(Vector3 point, bool active)
    {
        if (active)
        {
             playerCharacter.StartGrapple(point);
        }
        else
        {
             playerCharacter.StopGrapple();
        }
        SyncVisualsObserversRpc(point, active);
    }
    
    [ObserversRpc]
    private void SyncVisualsObserversRpc(Vector3 point, bool active)
    {
        if(isOwner) return;
        isGrappling = active;
        currentHookPoint = point;
    }

    protected override void ExecuteUtilityLogic(Vector3 position, Vector3 direction) { }
}