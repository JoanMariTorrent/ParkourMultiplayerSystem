using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class RebindAction : MonoBehaviour
{
    [SerializeField] private InputActionReference actionReference;
    [SerializeField] private TextMeshProUGUI displayKeyText;
    [SerializeField] private int bindingIndex = 0;
    [SerializeField] private Player player;

    private InputActionRebindingExtensions.RebindingOperation rebindingOperation;

    void OnEnable()
    {
        string rebinds = PlayerPrefs.GetString("rebinds", string.Empty);
        if(!string.IsNullOrEmpty(rebinds))
        {
            actionReference.asset.LoadBindingOverridesFromJson(rebinds);
        }

        RefreshDisplay();
    }

    public void StartRebinding()
    {
        if (actionReference == null) return;

        displayKeyText.text = "...";

        actionReference.action.actionMap.Disable();

        rebindingOperation = actionReference.action.PerformInteractiveRebinding(bindingIndex)
            .WithControlsExcluding("<Pointer>/position")
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(operation => FinishRebind())
            .OnCancel(operation => FinishRebind())
            .Start();
    }

    private void FinishRebind()
    {
        rebindingOperation.Dispose();
        string currentRebinds = actionReference.asset.SaveBindingOverridesAsJson();

        PlayerPrefs.SetString("rebinds", currentRebinds);
        PlayerPrefs.Save();

        player.ApplyInputOverrides(currentRebinds);

        actionReference.action.actionMap.Enable();
        RefreshDisplay();
    }

    public void RefreshDisplay()
    {
        if (actionReference != null && displayKeyText != null)
        {
            string displayString = actionReference.action.GetBindingDisplayString(bindingIndex);
            
            if (string.IsNullOrEmpty(displayString))
            {
                displayKeyText.text = "None (Index Error?)";
                Debug.LogWarning($"El índice {bindingIndex} en {actionReference.action.name} está vacío.");
            }
            else
            {
                displayKeyText.text = displayString;
            }
        }
    }
}