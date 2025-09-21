using PurrNet;
using UnityEngine;

public class RotationMimic : NetworkBehaviour
{
    [SerializeField] private Transform _mimicRotation;

    protected override void OnSpawned()
    {
        base.OnSpawned();

        enabled = isOwner;
    }

    private void Update()
    {
        if(!_mimicRotation)
            return;

        transform.rotation = _mimicRotation.rotation;
    }
}
