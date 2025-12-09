using UnityEngine;

public class DeathZone : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if(other.TryGetComponent(out PlayerHealth player))
        {
            player.ChangeHealth(-player.health);
        }
    }
}
