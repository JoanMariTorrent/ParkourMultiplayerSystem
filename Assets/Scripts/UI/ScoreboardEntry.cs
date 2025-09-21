using TMPro;
using UnityEngine;

public class ScoreboardEntry : MonoBehaviour
{
    [SerializeField] private TMP_Text _nameText, _killsText, _deathsText, _damageText;

    public void SetData(string playerName, int kills, int deaths, int damage)
    {
        _nameText.text = playerName;
        _killsText.text = kills.ToString();
        _deathsText.text = deaths.ToString();
        _damageText.text = damage.ToString();



    }
}
