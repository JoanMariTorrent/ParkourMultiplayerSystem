using UnityEngine;
using PurrNet;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class Canvas : NetworkBehaviour
{
    [SerializeField] private NetworkManager _NetworkManager;
    public List<View> _allViews = new();
    [SerializeField] private View _defaulView;
    public GameObject slotMachine;
    public GameMainView gameMainView;

    void OnEnable()
    {
        //gameObject.SetActive(isOwner);
    }


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
        _view.OnShow();
    }
    
    private void HideViewInternal(View _view)
    { 
        _view._canvasGroup.alpha = 0f;
        _view.OnHide();
    }

    private void OnDestroy()
    {
        InstanceHandler.UnregisterInstance<Canvas>();
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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            SceneManager.LoadScene(0);
        }
    }
}

public abstract class View : MonoBehaviour
{
    public CanvasGroup _canvasGroup;

    public abstract void OnShow();
    public abstract void OnHide();
}

