using System.Collections;
using UnityEngine;

public class TutorialView : MonoBehaviour
{
    [SerializeField] private GameObject movementView;
    [SerializeField] private GameObject crouchView;
    [SerializeField] private GameObject jumpView;
    [SerializeField] private GameObject dobleJumpView;
    [SerializeField] private GameObject slideView;
    [SerializeField] private GameObject climbView;
    [SerializeField] private GameObject wallRunView;
    [SerializeField] private GameObject finalView;


    [SerializeField] private float timeToHideView;

    private GameObject viewToShow;
    private Coroutine showViewCoroutine;



    public void ShowView(int index)
    {
        if(showViewCoroutine != null)
        {
            StopCoroutine(showViewCoroutine);
        }

        if(viewToShow != null)
        {
            viewToShow.SetActive(false);
            viewToShow = null;
        }

        switch (index)
        {
            case 1: 
                viewToShow = movementView;
                break;

            case 2: 
                viewToShow = crouchView;
                break;

            case 3: 
                viewToShow = jumpView;
                break;
            
            case 4: 
                viewToShow = dobleJumpView;
                break;

            case 5: 
                viewToShow = slideView;
                break;

            case 6: 
                viewToShow = climbView;
                break;

            case 7: 
                viewToShow = wallRunView;
                break;

            case 8: 
                viewToShow = finalView;
                break;
            
        }

        if(viewToShow != null) showViewCoroutine = StartCoroutine(ShowViewCoroutine(viewToShow));
    }


    private IEnumerator ShowViewCoroutine(GameObject view)
    {
        view.SetActive(true);

        yield return new WaitForSeconds(timeToHideView);

        view.SetActive(false);
        viewToShow = null;
        showViewCoroutine = null;
    }
}
