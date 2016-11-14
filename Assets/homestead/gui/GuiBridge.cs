using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
using RedHomestead.Construction;
using System.Collections.Generic;

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
    public static PromptInfo PickupHint = new PromptInfo()
    {
        Description = "Pick up",
        Key = "E"
    };
    internal static PromptInfo DropHint = new PromptInfo()
    {
        Description = "Drop",
        Key = "E"
    };
    internal static PromptInfo ConstructHint = new PromptInfo()
    {
        Description = "Construct",
        Key = "E"
    };

    public RectTransform PromptPanel, ConstructionPanel;
    public Text PromptKey, PromptDescription, ConstructionHeader;
    public RectTransform[] ConstructionRequirements;

    internal Text[] ConstructionRequirementsText;

    internal PromptInfo CurrentPrompt { get; set; }

    void Awake()
    {
        Instance = this;
        TogglePromptPanel(false);
        this.ConstructionPanel.gameObject.SetActive(false);
        ConstructionRequirementsText = new Text[ConstructionRequirements.Length];

        int i = 0;
        foreach(RectTransform t in ConstructionRequirements)
        {
            ConstructionRequirementsText[i] = t.GetChild(0).GetComponent<Text>();
            i++;
        }
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

    //todo: just pass constructionZone, it's less params
    internal void ShowConstruction(List<ResourceEntry> requiresList, Dictionary<Resource, int> hasCount, Module toBeBuilt)
    {
        this.ConstructionPanel.gameObject.SetActive(true);
        this.ConstructionHeader.text = "Building a " + toBeBuilt.ToString();

        for (int i = 0; i < this.ConstructionRequirements.Length; i++)
        {
            if (i < requiresList.Count)
            {
                ResourceEntry resourceEntry = requiresList[i];
                string output = resourceEntry.Type.ToString() + ": " + hasCount[resourceEntry.Type] + "/" + resourceEntry.Count;
                this.ConstructionRequirementsText[i].text = output;
                this.ConstructionRequirements[i].gameObject.SetActive(true);
            }
            else
            {
                this.ConstructionRequirements[i].gameObject.SetActive(false);
            }
        }
    }

    internal void HideConstruction()
    {
        this.ConstructionPanel.gameObject.SetActive(false);
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
