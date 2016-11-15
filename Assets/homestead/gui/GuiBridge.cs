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

    public RectTransform PromptPanel, ConstructionPanel, ConstructionGroupPanel, ConstructionModulesPanel;
    public Text PromptKey, PromptDescription, ConstructionHeader, ModeText;
    public Button[] ConstructionGroupButtons;
    public Text[] ConstructionGroupHints;
    public RectTransform[] ConstructionRequirements, ConstructionModuleButtons;

    internal Text[] ConstructionRequirementsText;

    internal PromptInfo CurrentPrompt { get; set; }

    void Awake()
    {
        Instance = this;
        TogglePromptPanel(false);
        this.ConstructionPanel.gameObject.SetActive(false);
        ConstructionRequirementsText = new Text[ConstructionRequirements.Length];
        this.SetConstructionGroup(-2);
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

    public void CycleConstruction(int delta)
    {
        //negative means up the list
        if ((delta < 0 && currentlySelectedGroup > ConstructionGroup.Habitation) ||
        //positive means down the list
            (delta > 0 && currentlySelectedGroup < ConstructionGroup.Storage))
        {
            ConstructionGroupButtons[(int)currentlySelectedGroup + delta].onClick.Invoke();
        }
    }

    public enum ConstructionGroup { None = -2, Undecided = -1, Habitation, Power, Extraction, Refinement, Storage }
    public static Dictionary<ConstructionGroup, Module[]> Groupmap = new Dictionary<ConstructionGroup, Module[]>()
    {
        {
            ConstructionGroup.Habitation,
            new Module[]
            {
                Module.Habitat,
                Module.Workspace
            }
        },
        {
            ConstructionGroup.Power,
            new Module[]
            {
                Module.SolarPanelSmall
            }
        },
        {
            ConstructionGroup.Extraction,
            new Module[]
            {
                Module.AtmosphereMiner
            }
        },
        {
            ConstructionGroup.Refinement,
            new Module[]
            {
                Module.Smelter
            }
        },
        {
            ConstructionGroup.Storage,
            new Module[]
            {
                Module.OxygenTank
            }
        },
    };

    private ConstructionGroup currentlySelectedGroup = ConstructionGroup.None;
    public void SetConstructionGroup(int index)
    {
        ConstructionGroup newGroup = (ConstructionGroup)index;
        currentlySelectedGroup = newGroup;
        ConstructionGroupPanel.gameObject.SetActive(index > -2);
        ConstructionModulesPanel.gameObject.SetActive(index > -1);

        if (currentlySelectedGroup == ConstructionGroup.None)
        {
            //noop
        }
        else if (currentlySelectedGroup == ConstructionGroup.Undecided)
        {
            RefreshButtonKeyHints();
        }
        else if (index > -1)
        {
            Module[] lists = Groupmap[currentlySelectedGroup];
            for (int i = 0; i < this.ConstructionModuleButtons.Length; i++)
            {
                if (i < lists.Length)
                {
                    this.ConstructionModuleButtons[i].gameObject.SetActive(true);
                    this.ConstructionModuleButtons[i].transform.GetChild(0).GetComponent<Text>().text = lists[i].ToString();
                }
                else
                {
                    this.ConstructionModuleButtons[i].gameObject.SetActive(false);
                }
            }

            RefreshButtonKeyHints();
        }
    }

    private void RefreshButtonKeyHints()
    {
        for (int i = 0; i < this.ConstructionGroupHints.Length; i++)
        {
            bool previousHint = currentlySelectedGroup > ConstructionGroup.Habitation && i == (int)currentlySelectedGroup - 1;
            bool nextHint = currentlySelectedGroup < ConstructionGroup.Storage && i == (int)currentlySelectedGroup + 1;
            //bool exitHint = currentlySelectedGroup > ConstructionGroup.Habitation && i == (int)currentlySelectedGroup;
            print(nextHint);
            this.ConstructionGroupHints[i].transform.parent.gameObject.SetActive(previousHint || nextHint);
            this.ConstructionGroupHints[i].text = previousHint ? "Q" : nextHint ? "Z" : "";
        }
    }

    internal void RefreshMode()
    {
        switch(PlayerInput.Instance.Mode)
        {
            case PlayerInput.InputMode.Default:
                this.ModeText.text = "Switch to Planning";
                this.SetConstructionGroup(-2);
                break;
            case PlayerInput.InputMode.Planning:
                this.ModeText.text = "Stop Planning";
                this.SetConstructionGroup(-1);
                break;
        }

    }

    public void SelectConstructionPlan(int index)
    {
        Module planModule = (Module)index;
        PlayerInput.Instance.PlannedModule = planModule;
        this.currentlySelectedGroup = ConstructionGroup.None;
    }
}
