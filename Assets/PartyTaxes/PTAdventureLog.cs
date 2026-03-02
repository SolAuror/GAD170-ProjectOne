using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class PTAdventureLog : MonoBehaviour
{
#region Variables
    [Header("UI References")]
    public GameObject logPanel;                                                         //variable for the adventure log panel
    [SerializeField] private ScrollRect logScrollRect;                                  //variable for the scroll rect component of the adventure log panel
    [SerializeField] private RectTransform logContent;                                  //variable for the content object of the scroll rect, where log entries will be added as children
    [SerializeField] private TextMeshProUGUI logText;                                   //variable for the text component of the adventure log panel
    
    [Header("Settings")]
    [SerializeField] private int maxLogEntries = 20;
    [SerializeField] private bool useVerticalLayoutEntries = true;
    [SerializeField] private bool autoScrollWhenNearBottom = true;
    [SerializeField, Range(0.01f, 0.25f)] private float nearBottomThreshold = 0.1f;
    [SerializeField] private Color logTextColor = Color.white;
    
    private readonly List<string> logEntries = new List<string>();                         // Store raw log files in a readonly list of strings.        -Could be used for a potential future saving/loading system. 
    private readonly List<TextMeshProUGUI> visualEntries = new List<TextMeshProUGUI>();    //store visual log entries in a readonly list of TextMeshProUGUI objects.
    private TextMeshProUGUI entryTemplate;
#endregion

#region Initialization
    void Awake()                                                                          //on awake, initialize the adventure log panel before other scripts' Start() runs
    {
        if (logScrollRect != null && logContent != null)
        {
            logScrollRect.content = logContent;
            logScrollRect.horizontal = false;
            logScrollRect.vertical = true;
        }

        if (logContent != null)                                                                 //use ContentSizeFitter component on the Content object so it grows with text
        {
            ContentSizeFitter fitter = logContent.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = logContent.gameObject.AddComponent<ContentSizeFitter>();
            }
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        if (useVerticalLayoutEntries && logText != null)                                        //use the verticallayoutentries option to create a template entry for visual log entries, and disable the original text component.
        {
            entryTemplate = logText;
            entryTemplate.color = logTextColor;
            entryTemplate.gameObject.SetActive(false);
        }

        AddLogEntry("Adventure log initialized... Welcome to Party Taxes.");                   //log initialization messgae, INIT COMMIT!
    }

    //void Update(){}
#endregion

#region Log Management Methods
    public void AddLogEntry(string message)                                    //method for adding a log entry, take a string message as input 
    {                                                                           
        if (string.IsNullOrEmpty(message)) return;                             

        bool shouldAutoScroll = !autoScrollWhenNearBottom || IsNearBottom();
        
        string timestamp = System.DateTime.Now.ToString("HH:mm:ss");               // Get current time in hours and minutes format
        string formattedEntry = $"[{timestamp}] {message}";                     // Format the log entry with the timestamp
        
        logEntries.Add(formattedEntry);                                         // Add the formatted entry to the log entries list

        if (logEntries.Count > maxLogEntries)                                   //remove oldest log entries when max is exceeded.
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
            AddVisualEntry(formattedEntry);                                     //add it to the visual log if using vertical layout entries component, 
            RebuildLayout(shouldAutoScroll);
        }
        else
        {
            UpdateLogDisplay(shouldAutoScroll);                                 //otherwise just update the text of the original log text component.
        }   
    }

    private void UpdateLogDisplay(bool shouldAutoScroll = true)                 //method to update the text of the original log text component with the current log entries,
    {                                                                           //       and auto scroll to the bottom if near the bottom when adding a new entry.
        if (logText == null) return;
        
        logText.text = string.Join("\n", logEntries);
        logText.color = logTextColor;
        RebuildLayout(shouldAutoScroll);
    }

    private void AddVisualEntry(string formattedEntry)                          //Method to add new visual log entries as children of the content obj,
    {                                                                           //       and inherit the properties of the template entry
        if (entryTemplate == null) return;

        TextMeshProUGUI newEntry = Instantiate(entryTemplate, logContent);
        newEntry.gameObject.SetActive(true);
        newEntry.text = formattedEntry;
        newEntry.color = logTextColor;
        visualEntries.Add(newEntry);
    }

    private void RebuildLayout(bool shouldAutoScroll)                           //Method for rebuilding the layout of the log entries, and auto scrolling to bottom if within the NearBottomThreshhold  
    {
        Canvas.ForceUpdateCanvases();

        if (logContent != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(logContent);
        }

        if (shouldAutoScroll && logScrollRect != null)
        {
            logScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    private bool IsNearBottom()                                                //method for calculating if the scroll rect is near the bottom, to reset scroll position on log entry addition.
    {
        if (logScrollRect == null || logContent == null)
        {
            return true;
        }

        RectTransform viewport = logScrollRect.viewport != null 
                               ? logScrollRect.viewport : logScrollRect.GetComponent<RectTransform>();
        if (viewport == null)
        {
            return true;
        }

        float scrollableHeight = logContent.rect.height - viewport.rect.height;
        if (scrollableHeight <= 1f)
        {
            return true;
        }

        return logScrollRect.verticalNormalizedPosition <= nearBottomThreshold;
    }
    public void ClearLogInstance()                                                //Instance method that performs the actual log clearing, keeps fields private
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
#endregion
    public static void ClearLog()                                                //Static method for clearing the log, finds the instance and delegates to ClearLogInstance()
    {
        PTAdventureLog logger = FindFirstObjectByType<PTAdventureLog>();
        if (logger != null) logger.ClearLogInstance();
    }

    public static void Log(string message)                                                    //Static Method for logging messages to the adventure log,
    {                                                                                         //(NOTE TO SELF: this can be referenced from other scripts without a rerference to this component because it finds the first instance of          
        PTAdventureLog logger = FindFirstObjectByType<PTAdventureLog>();                      //this component in the scene and calls the AddLogEntry method on it)
        if (logger != null)
        {
            logger.AddLogEntry(message);
        }
    }
}
