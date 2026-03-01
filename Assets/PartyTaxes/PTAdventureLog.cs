using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class PTAdventureLog : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject logPanel;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform logContent;
    [SerializeField] private TextMeshProUGUI logText;
    
    [Header("Settings")]
    [SerializeField] private int maxLogEntries = 20;
    [SerializeField] private bool useVerticalLayoutEntries = true;
    [SerializeField] private bool autoScrollWhenNearBottom = true;
    [SerializeField, Range(0.01f, 0.25f)] private float nearBottomThreshold = 0.1f;
    [SerializeField] private Color logTextColor = Color.white;
    [SerializeField] private float fadeAlpha = 0.8f;
    
    private readonly List<string> logEntries = new List<string>();
    private readonly List<TextMeshProUGUI> visualEntries = new List<TextMeshProUGUI>();
    private bool isPanelFocused = false;
    private TextMeshProUGUI entryTemplate;

    void Start()
    {
        if (logPanel != null)
        {
            SetPanelAlpha(fadeAlpha);
        }

        if (scrollRect != null && logContent != null)
        {
            scrollRect.content = logContent;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
        }

        // ContentSizeFitter on the Content object so it grows with text
        if (logContent != null)
        {
            ContentSizeFitter fitter = logContent.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = logContent.gameObject.AddComponent<ContentSizeFitter>();
            }
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        if (useVerticalLayoutEntries && logText != null)
        {
            entryTemplate = logText;
            entryTemplate.color = logTextColor;
            entryTemplate.gameObject.SetActive(false);
        }

        AddLogEntry("Adventure log initialized...");
    }

    void Update()
    {
       
    }


    public void AddLogEntry(string message)
    {
        if (string.IsNullOrEmpty(message)) return;

        bool shouldAutoScroll = !autoScrollWhenNearBottom || IsNearBottom();
        
        string timestamp = System.DateTime.Now.ToString("HH:mm");
        string formattedEntry = $"[{timestamp}] {message}";
        
        logEntries.Add(formattedEntry);

        // Keep log size manageable
        if (logEntries.Count > maxLogEntries)
        {
            logEntries.RemoveAt(0);

            if (useVerticalLayoutEntries && visualEntries.Count > 0)
            {
                TextMeshProUGUI oldestEntry = visualEntries[0];
                visualEntries.RemoveAt(0);
                if (oldestEntry != null)
                {
                    Destroy(oldestEntry.gameObject);
                }
            }
        }

        if (useVerticalLayoutEntries)
        {
            AddVisualEntry(formattedEntry);
            RebuildLayout(shouldAutoScroll);
        }
        else
        {
            UpdateLogDisplay(shouldAutoScroll);
        }

        
    }

    private void UpdateLogDisplay(bool shouldAutoScroll = true)
    {
        if (logText == null) return;
        
        logText.text = string.Join("\n", logEntries);
        logText.color = logTextColor;
        RebuildLayout(shouldAutoScroll);
    }

    private void AddVisualEntry(string formattedEntry)
    {
        if (entryTemplate == null) return;

        TextMeshProUGUI newEntry = Instantiate(entryTemplate, logContent);
        newEntry.gameObject.SetActive(true);
        newEntry.text = formattedEntry;
        newEntry.color = logTextColor;
        visualEntries.Add(newEntry);
    }

    private void RebuildLayout(bool shouldAutoScroll)
    {
        Canvas.ForceUpdateCanvases();

        if (logContent != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(logContent);
        }

        if (shouldAutoScroll && scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    private bool IsNearBottom()
    {
        if (scrollRect == null || logContent == null)
        {
            return true;
        }

        RectTransform viewport = scrollRect.viewport != null ? scrollRect.viewport : scrollRect.GetComponent<RectTransform>();
        if (viewport == null)
        {
            return true;
        }

        float scrollableHeight = logContent.rect.height - viewport.rect.height;
        if (scrollableHeight <= 1f)
        {
            return true;
        }

        return scrollRect.verticalNormalizedPosition <= nearBottomThreshold;
    }

    public void ToggleFocus()
    {
        isPanelFocused = !isPanelFocused;
        SetPanelAlpha(isPanelFocused ? 1f : fadeAlpha);
    }

    private void SetPanelAlpha(float alpha)
    {
        if (logPanel != null)
        {
            CanvasGroup canvasGroup = logPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = logPanel.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = alpha;
        }
    }

    public void ClearLog()
    {
        logEntries.Clear();

        for (int index = 0; index < visualEntries.Count; index++)
        {
            if (visualEntries[index] != null)
            {
                Destroy(visualEntries[index].gameObject);
            }
        }

        visualEntries.Clear();
        UpdateLogDisplay();
    }

    public static void Log(string message)
    {
        PTAdventureLog logger = FindFirstObjectByType<PTAdventureLog>();
        if (logger != null)
        {
            logger.AddLogEntry(message);
        }
    }
}
