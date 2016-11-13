using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;

public class PromptInfo
{
    public string Key { get; set; }
    public string Description { get; set; }
    public float Duration { get; set; }
}

public class GuiBridge : MonoBehaviour {
    public static GuiBridge Instance { get; private set; }

    public static PromptInfo StartBulkheadBridgeHint = new PromptInfo()
    {
        Description = "Select bulkhead to connect",
        Key = "E"
    };
    public static PromptInfo EndBulkheadBridgeHint = new PromptInfo()
    {
        Description = "Connect bulkheads",
        Key = "E"
    };
    public static PromptInfo BulkheadBridgeCompletedPrompt = new PromptInfo()
    {
        Description = "Bulkheads connected",
        Key = "E",
        Duration = 1500
    };
    public static PromptInfo DriveRoverPrompt = new PromptInfo()
    {
        Description = "Drive Rover",
        Key = "E"
    };

    public RectTransform PromptPanel;
    public Text PromptKey, PromptDescription;

    internal PromptInfo CurrentPrompt { get; set; }

    void Awake()
    {
        Instance = this;
        TogglePromptPanel(false);
    }

    private void TogglePromptPanel(bool isActive)
    {
        this.PromptPanel.gameObject.SetActive(isActive);
    }

    public void ShowPrompt(PromptInfo prompt)
    {
        PromptKey.text = prompt.Key;
        PromptDescription.text = prompt.Description;
        TogglePromptPanel(true);
        CurrentPrompt = prompt;

        if (prompt.Duration > 0)
        {
            StartCoroutine(HidePromptAfter(prompt.Duration));
        }
    }

    private IEnumerator HidePromptAfter(float duration)
    {
        yield return new WaitForSeconds(duration / 1000f);

        TogglePromptPanel(false);
    }

    public void HidePrompt()
    {
        if(CurrentPrompt != null)
        {
            //if it's manually turned on and off, hide it
            if (CurrentPrompt.Duration <= 0f)
            {
                TogglePromptPanel(false);
                CurrentPrompt = null;                
            }
            else
            {
                //if it's timed, let it time out
            }
        }
    }
}
