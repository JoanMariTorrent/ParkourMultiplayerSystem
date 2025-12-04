using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class CrosshairController : MonoBehaviour
{
    [Header("Referencias UI")]
    public Image centerDot;
    public RectTransform[] innerLines;

    [Header("Datos")]
    public SettingsData settings;

    private void OnValidate()
    {
        UpdateCrosshair();
    }

    public void UpdateCrosshair()
    {
        if (innerLines.Length < 4) return;

        Color finalColor = settings.crosshairColor;
        finalColor.a = settings.innerOpacity;

        Color dotFinalColor = settings.crosshairColor;
        dotFinalColor.a = settings.centerDotOpacity;

        centerDot.gameObject.SetActive(settings.useCenterDot);
        centerDot.rectTransform.sizeDelta = new Vector2(settings.centerDotSize, settings.centerDotSize);
        centerDot.color = settings.crosshairColor;

        foreach (var line in innerLines)
        {
            line.gameObject.SetActive(settings.showInnerLines);
            line.GetComponent<Image>().color = finalColor;
        }

        centerDot.color = dotFinalColor;

        // Línea Superior (Top)
        innerLines[0].sizeDelta = new Vector2(settings.innerThickness, settings.innerLenght);
        innerLines[0].anchoredPosition = new Vector2(0, settings.innerOffset + (settings.innerLenght / 2));

        // Línea Inferior (Bottom)
        innerLines[1].sizeDelta = new Vector2(settings.innerThickness, settings.innerLenght);
        innerLines[1].anchoredPosition = new Vector2(0, -settings.innerOffset - (settings.innerLenght / 2));

        // Línea Izquierda (Left)
        innerLines[2].sizeDelta = new Vector2(settings.innerLenght, settings.innerThickness);
        innerLines[2].anchoredPosition = new Vector2(-settings.innerOffset - (settings.innerLenght / 2), 0);

        // Línea Derecha (Right)
        innerLines[3].sizeDelta = new Vector2(settings.innerLenght, settings.innerThickness);
        innerLines[3].anchoredPosition = new Vector2(settings.innerOffset + (settings.innerLenght / 2), 0);
    }
}
