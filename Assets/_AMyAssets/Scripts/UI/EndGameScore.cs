using TMPro;
using UnityEngine;

public class EndGameScore : MonoBehaviour
{
    public TextMeshProUGUI NameText;
    public TextMeshProUGUI KillsText;
    public TextMeshProUGUI DeathsText;
    public TextMeshProUGUI DamageText;

    public void Intialize(string playerName, int playerKills, int playerDeaths, int playerDamage)
    {
        NameText.text = playerName;
        KillsText.text = playerKills.ToString();
        DeathsText.text = playerDeaths.ToString();
        DamageText.text = playerDamage.ToString();
    }
}
