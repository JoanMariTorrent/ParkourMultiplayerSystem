using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class RebindAction : MonoBehaviour
{
    [Header("Configuración de Acción")]
    [SerializeField] private InputActionReference actionReference;
    [SerializeField] private int bindingIndex = 0;
    [Header("Refrencias UI")]
    [SerializeField] private TextMeshProUGUI nameInputText;
    [SerializeField] private TextMeshProUGUI displayKeyText;
    [Header("Referencia Jugador")]
    [SerializeField] private Player player;

    private InputActionRebindingExtensions.RebindingOperation rebindingOperation;
    private string FilePath => SavePathManager.GetPath("rebinds.json");

    void OnEnable()
    {
        if (File.Exists(FilePath))
        {
            string json = File.ReadAllText(FilePath);
            actionReference.asset.LoadBindingOverridesFromJson(json);
        }

        if(nameInputText != null) nameInputText.text = actionReference.action.name;

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

    public void ResetBinding()
    {
        if(actionReference == null) return;

        actionReference.action.RemoveBindingOverride(bindingIndex);
        FinishRebind();
    }



    private void FinishRebind()
    {
        rebindingOperation?.Dispose();

        string currentRebinds = actionReference.asset.SaveBindingOverridesAsJson();

        try 
        {
            File.WriteAllText(FilePath, currentRebinds);
            Debug.Log($"<color=green>Inputs guardados en: {FilePath}</color>");
        }
        catch (Exception e) 
        {
            Debug.LogError($"No se pudo guardar el archivo de controles: {e.Message}");
        }

        if(player != null)player.ApplyInputOverrides(currentRebinds);

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