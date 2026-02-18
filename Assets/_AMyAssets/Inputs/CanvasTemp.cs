using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CanvasTemp : MonoBehaviour
{
    [SerializeField] private PlayerCharacter playerCharacter;
    [SerializeField] private TextMeshProUGUI velocityText;

    void Update()
    {
        velocityText.text = playerCharacter._state.Velocity.ToString();

        
    }
}
