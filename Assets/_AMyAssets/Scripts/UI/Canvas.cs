using UnityEngine;
using PurrNet;
using System.Collections.Generic;

public class Canvas : NetworkBehaviour
{
    [SerializeField] private NetworkManager _NetworkManager;
    public List<View> _allViews = new();
    [SerializeField] private View _defaulView;
    public GameObject slotMachine;
    public GameMainView gameMainView;


    private void Awake()
    {
        InstanceHandler.RegisterInstance(this);


        foreach (var view in _allViews)
        { 
            HideViewInternal(view);
        }

        ShowViewInternal(_defaulView);
        slotMachine.SetActive(false);
    }

    private void ShowViewInternal(View _view)
    { 
        _view._canvasGroup.alpha = 1f;
        _view._canvasGroup.interactable = true;
        _view._canvasGroup.blocksRaycasts = true;
        _view.OnShow(); 
    }
    
    private void HideViewInternal(View _view)
    { 
        _view._canvasGroup.alpha = 0f;
        _view._canvasGroup.interactable = false;
        _view._canvasGroup.blocksRaycasts = false;
        _view.OnHide();
    }

    public void ShowView<T>(bool hideOthers = true) where T : View
    {
        foreach (var view in _allViews)
        {
            if (view.GetType() == typeof(T))
            {
                ShowViewInternal(view);
            }
            else
            {
                if (hideOthers)
                {
                    HideViewInternal(view);
                }
                
            }
        }
    }

    public void HideView<T>() where T : View
    {
        foreach (var view in _allViews)
        {
            if (view.GetType() == typeof(T))
            {
                HideViewInternal(view);
            }
        }
    }


    public void StartServer()
    {
        _NetworkManager.StartHost();
        Debug.Log("Servidor iniciado");
    }

    public void StartClient()
    {
        _NetworkManager.StartClient();
        Debug.Log("Cliente iniciado");
    }
}

public abstract class View : MonoBehaviour
{
    public CanvasGroup _canvasGroup;

    public abstract void OnShow();
    public abstract void OnHide();
}

