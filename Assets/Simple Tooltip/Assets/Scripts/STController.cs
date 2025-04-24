using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class STController : MonoBehaviour
{
    public enum TextAlign { Left, Right };

    private Image panel;
    private TextMeshProUGUI toolTipTextLeft;
    private TextMeshProUGUI toolTipTextRight;
    private RectTransform rect;
    private int showInFrames = -1;
    private bool showNow = false;

    private void Awake()
    {
        // Load up both text layers
        var tmps = GetComponentsInChildren<TextMeshProUGUI>();
        for (int i = 0; i < tmps.Length; i++)
        {
            if (tmps[i].name == "_left")
                toolTipTextLeft = tmps[i];

            if (tmps[i].name == "_right")
                toolTipTextRight = tmps[i];
        }

        // Keep a reference for the panel image and transform
        panel = GetComponent<Image>();
        rect = GetComponent<RectTransform>();

        // Configure text settings to avoid word wrapping
        if (toolTipTextLeft != null)
            toolTipTextLeft.enableWordWrapping = false;

        if (toolTipTextRight != null)
            toolTipTextRight.enableWordWrapping = false;

        // Hide at the start
        HideTooltip();
    }

    void Update()
    {
        ResizeToMatchText();
        UpdateShow();
    }

    private void ResizeToMatchText()
    {
        // Find the biggest height between both text layers
        var bounds = toolTipTextLeft.textBounds;
        float biggestY = toolTipTextLeft.textBounds.size.y;
        float rightY = toolTipTextRight.textBounds.size.y;
        if (rightY > biggestY)
            biggestY = rightY;

        // Dont forget to add the margins
        var margins = toolTipTextLeft.margin.y * 2;

        // Update the height of the tooltip panel
        rect.sizeDelta = new Vector2(rect.sizeDelta.x, biggestY + margins);
    }

    private void UpdateShow()
    {
        if (showInFrames == -1)
            return;

        if (showInFrames == 0)
            showNow = true;

        if (showNow)
        {
            // Get mouse position and tooltip dimensions
            Vector2 mousePosition = Input.mousePosition;
            Vector2 size = rect.sizeDelta;

            // Calculate screen boundaries
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            // Adjust position to keep tooltip on screen
            // Calculate the actual tooltip width/height considering scale
            float tooltipWidth = size.x * rect.localScale.x;
            float tooltipHeight = size.y * rect.localScale.y;

            // Check right edge
            if (mousePosition.x + tooltipWidth > screenWidth)
            {
                mousePosition.x = screenWidth - tooltipWidth;
            }

            // Check top edge
            if (mousePosition.y + tooltipHeight > screenHeight)
            {
                mousePosition.y = screenHeight - tooltipHeight;
            }

            // Check bottom edge
            if (mousePosition.y - tooltipHeight < 0)
            {
                mousePosition.y = tooltipHeight;
            }

            rect.position = mousePosition;
        }

        showInFrames -= 1;
    }

    public void SetRawText(string text, TextAlign align = TextAlign.Left)
    {
        // Doesn't change style, just the text
        if (align == TextAlign.Left)
            toolTipTextLeft.text = text;
        if (align == TextAlign.Right)
            toolTipTextRight.text = text;
        ResizeToMatchText();
    }

    public void SetCustomStyledText(string text, SimpleTooltipStyle style, TextAlign align = TextAlign.Left)
    {
        // Update the panel sprite and color
        panel.sprite = style.slicedSprite;
        panel.color = style.color;

        // Update the font asset, size and default color
        toolTipTextLeft.font = style.fontAsset;
        toolTipTextLeft.color = style.defaultColor;
        toolTipTextRight.font = style.fontAsset;
        toolTipTextRight.color = style.defaultColor;

        // Convert all tags to TMPro markup
        var styles = style.fontStyles;
        for (int i = 0; i < styles.Length; i++)
        {
            string addTags = "</b></i></u></s>";
            addTags += "<color=#" + ColorToHex(styles[i].color) + ">";
            if (styles[i].bold) addTags += "<b>";
            if (styles[i].italic) addTags += "<i>";
            if (styles[i].underline) addTags += "<u>";
            if (styles[i].strikethrough) addTags += "<s>";
            text = text.Replace(styles[i].tag, addTags);
        }
        if (align == TextAlign.Left)
            toolTipTextLeft.text = text;
        if (align == TextAlign.Right)
            toolTipTextRight.text = text;
        ResizeToMatchText();
    }

    public string ColorToHex(Color color)
    {
        int r = (int)(color.r * 255);
        int g = (int)(color.g * 255);
        int b = (int)(color.b * 255);
        return r.ToString("X2") + g.ToString("X2") + b.ToString("X2");
    }

    public void ShowTooltip()
    {
        // After 2 frames, showNow will be set to TRUE
        // after that the frame count wont matter
        if (showInFrames == -1)
            showInFrames = 2;

        // Make tooltip visible
        panel.enabled = true;
        if (toolTipTextLeft != null)
            toolTipTextLeft.enabled = true;
        if (toolTipTextRight != null)
            toolTipTextRight.enabled = true;
    }

    public void HideTooltip()
    {
        showInFrames = -1;
        showNow = false;

        // Move offscreen as a backup
        rect.anchoredPosition = new Vector2(Screen.width * 2, Screen.height * 2);

        // Actually hide the tooltip components
        panel.enabled = false;
        if (toolTipTextLeft != null)
            toolTipTextLeft.enabled = false;
        if (toolTipTextRight != null)
            toolTipTextRight.enabled = false;
    }
}
