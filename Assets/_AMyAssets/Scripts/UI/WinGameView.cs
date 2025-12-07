using UnityEngine;
using UnityEngine.SceneManagement;

public class WinGameView : View
{
    public override void OnShow(){ }

    public override void OnHide(){ }

    public void Exit()
    {
        SceneManager.LoadScene(0);
    }
}
