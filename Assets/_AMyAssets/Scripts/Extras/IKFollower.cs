using UnityEngine;

// Pon este script en tu IK_Target
public class IKFollower : MonoBehaviour
{
    public Transform targetToFollow;

    void LateUpdate() 
    {
        if (targetToFollow != null)
        {
            transform.position = targetToFollow.position;
            transform.rotation = targetToFollow.rotation;
        }
    }

    public void SetTarget(Transform newTarget)
    {
        if(newTarget != null) targetToFollow = newTarget;
    }
}
