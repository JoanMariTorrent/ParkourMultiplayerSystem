using UnityEngine;
using UnityEngine.SceneManagement;

public class EndGameView : View
{
    [SerializeField] private Transform scoreEntriesParent;
    [SerializeField] private EndGameScore scorePrefab;

    public override void OnShow(){ Cursor.lockState = CursorLockMode.None; Cursor.visible = true;}

    public override void OnHide(){ Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;}


    public void AddPlayerToScore(string playerName, int playerKills, int playerDeaths, int playerDamage)
    {
        EndGameScore scoreInstance = Instantiate(scorePrefab, scoreEntriesParent);
        scoreInstance.Intialize(playerName, playerKills, playerDeaths, playerDamage);
    }

    public void ExitGame()
    { 
        SceneManager.LoadScene(0);
    }
}
