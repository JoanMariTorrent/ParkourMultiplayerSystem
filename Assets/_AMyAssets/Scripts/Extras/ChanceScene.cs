using UnityEngine;
using UnityEngine.SceneManagement;

public class ChanceScene : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        other.TryGetComponent(out Player player);
        if(player == null) return;

        SceneManager.LoadScene(1);
    }
}
