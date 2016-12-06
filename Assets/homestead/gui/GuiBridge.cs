using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
using RedHomestead.Construction;
using System.Collections.Generic;



/// <summary>
/// Scripting interface for all GUI elements
/// syncs PlayerInput state to UI
/// has internal state for showing prompts and panels
/// </summary>
public class GuiBridge : MonoBehaviour {
    public static GuiBridge Instance { get; private set; }

    public RectTransform PromptPanel, ConstructionPanel, ConstructionGroupPanel, ConstructionModulesPanel, PlacingPanel, KilledPanel, FloorplanGroupPanel, FloorplanSubgroupPanel, FloorplanPanel;
    public Text PromptKey, PromptDescription, ConstructionHeader, ModeText, PlacingText, TimeText;
    public Button[] ConstructionGroupButtons;
    public Text[] ConstructionGroupHints, FloorplanGroupHints;
    public RectTransform[] ConstructionRequirements, ConstructionModuleButtons;
    public Image OxygenBar, WaterBar, PowerBar, FoodBar, RadBar, PowerImage, ColdImage, HotImage;

    internal Text[] ConstructionRequirementsText;

    internal PromptInfo CurrentPrompt { get; set; }

    void Awake()
    {
        Instance = this;
        TogglePromptPanel(false);
        this.ConstructionPanel.gameObject.SetActive(false);
        ConstructionRequirementsText = new Text[ConstructionRequirements.Length];
        this.RefreshPlanningUI();
        int i = 0;
        foreach (RectTransform t in ConstructionRequirements)
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
        PromptKey.transform.parent.gameObject.SetActive(prompt.Key != null);

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
        //show the name of the thing being built
        this.ConstructionPanel.gameObject.SetActive(true);
        this.ConstructionHeader.text = "Building a " + toBeBuilt.ToString();

        //show a list of resources gathered / resources required
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
        if (CurrentPrompt != null)
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

    /// <summary>
    /// cycle the selected construction group button up or down
    /// </summary>
    /// <param name="delta"></param>
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

    /// <summary>
    /// Top level groups that organize modules
    /// "None" means no groups should show
    /// "Undecided" means groups can show but no group is selected
    /// TODO: remove None and Undecided into their own booleans ShowingGroups and HasGroupSelected
    /// </summary>
    public enum ConstructionGroup { Undecided = -1, Habitation, Power, Extraction, Refinement, Storage }

    /// <summary>
    /// from group to list of modules
    /// </summary>
    public static Dictionary<ConstructionGroup, Module[]> ConstructionGroupmap = new Dictionary<ConstructionGroup, Module[]>()
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
                Module.SabatierReactor,
                Module.OreExtractor,
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
                Module.SmallGasTank,
                Module.LargeGasTank
            }
        },
    };
    
    private ConstructionGroup currentlySelectedGroup = ConstructionGroup.Undecided;

    public void RefreshPlanningUI()
    {
        ConstructionGroupPanel.gameObject.SetActive(PlayerInput.Instance.CurrentMode == PlayerInput.PlanningMode.Exterior);
        ConstructionModulesPanel.gameObject.SetActive(PlayerInput.Instance.CurrentMode == PlayerInput.PlanningMode.Exterior && currentlySelectedGroup != ConstructionGroup.Undecided);

        FloorplanGroupPanel.gameObject.SetActive(PlayerInput.Instance.CurrentMode == PlayerInput.PlanningMode.Interiors);
        FloorplanSubgroupPanel.gameObject.SetActive(PlayerInput.Instance.CurrentMode == PlayerInput.PlanningMode.Interiors && selectedFloorplanGroup != FloorplanGroup.Undecided);
    }

    public void SetConstructionGroup(int index)
    {
        ConstructionGroup newGroup = (ConstructionGroup)index;
        currentlySelectedGroup = newGroup;

        RefreshPlanningUI();

        if (currentlySelectedGroup == ConstructionGroup.Undecided)
        {
            RefreshButtonKeyHints();
        }
        else if (index > -1)
        {
            Module[] lists = ConstructionGroupmap[currentlySelectedGroup];
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

    /// <summary>
    /// to the left of the group names there are hints for which key to use to move to that group
    /// this shows and hides those
    /// </summary>
    private void RefreshButtonKeyHints()
    {
        for (int i = 0; i < this.ConstructionGroupHints.Length; i++)
        {
            bool previousHint = currentlySelectedGroup > ConstructionGroup.Habitation && i == (int)currentlySelectedGroup - 1;
            bool nextHint = currentlySelectedGroup < ConstructionGroup.Storage && i == (int)currentlySelectedGroup + 1;
            //bool exitHint = currentlySelectedGroup > ConstructionGroup.Habitation && i == (int)currentlySelectedGroup;
            
            this.ConstructionGroupHints[i].transform.parent.gameObject.SetActive(previousHint || nextHint);
            this.ConstructionGroupHints[i].text = previousHint ? "Q" : nextHint ? "Z" : "";
        }
    }

    /// <summary>
    /// syncs player input mode
    /// </summary>
    internal void RefreshMode()
    {
        switch(PlayerInput.Instance.CurrentMode)
        {
            case PlayerInput.PlanningMode.None:
                this.ModeText.text = "Switch to Planning";
                this.PlacingPanel.gameObject.SetActive(false);
                break;
            case PlayerInput.PlanningMode.Exterior:
                this.ModeText.text = "Stop Planning";
                this.SetConstructionGroup(-1);
                break;
            case PlayerInput.PlanningMode.Interiors:
                this.ModeText.text = "Stop Planning";
                this.SetConstructionGroup(-1);
                break;
        }

        this.RefreshPlanningUI();
    }

    /// <summary>
    /// called when a specific module is selected to plan
    /// </summary>
    /// <param name="index"></param>
    public void SelectConstructionPlan(int index)
    {
        Module planModule = ConstructionGroupmap[currentlySelectedGroup][index];
        this.PlacingPanel.gameObject.SetActive(true);
        this.PlacingText.text = planModule.ToString();
        PlayerInput.Instance.PlanModule(planModule);
        this.RefreshPlanningUI();
    }

    internal void ShowKillMenu()
    {
        KilledPanel.transform.gameObject.SetActive(true);
    }

    public void Restart()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    internal void RefreshOxygenBar(float percentage)
    {
        this.OxygenBar.fillAmount = percentage;
    }

    internal void RefreshWaterBar(float percentage)
    {
        this.WaterBar.fillAmount = percentage;
    }

    internal void RefreshFoodBar(float percentage)
    {
        this.FoodBar.fillAmount = percentage;
    }

    internal void RefreshRadiationBar(float percentage)
    {
        this.RadBar.fillAmount = percentage;
    }

    internal void RefreshPowerBar(float powerPercentage, float heatPercentage)
    {
        this.PowerImage.enabled = (powerPercentage > 0f);
        this.HotImage.enabled = (powerPercentage <= 0f) && (heatPercentage > .5f);
        this.ColdImage.enabled = (powerPercentage <= 0f) && (heatPercentage <= .5f);

        if (this.PowerImage.enabled)
        {
            this.PowerBar.fillAmount = powerPercentage;
        }
        else
        {
            this.PowerBar.fillAmount = heatPercentage;
        }
    }


    /// <summary>
    /// Top level groups that organize floorplans
    /// </summary>
    public enum FloorplanGroup { Undecided = -1, Floor, Edge, Corner }

    /// <summary>
    /// Second level groups that organize floorplans
    /// </summary>
    public enum FloorplanSubGroup { Solid, Mesh, Door, Window, SingleColumn, DoubleColumn }

    public enum FloorplanMaterial { Rock, Concrete, Plastic, Steel }

    private FloorplanGroup selectedFloorplanGroup;
    private FloorplanSubGroup selectedFloorplanSubgroup;
    private FloorplanMaterial selectedFloorplanMaterial;

    public static Dictionary<FloorplanGroup, FloorplanSubGroup[]> FloorplanGroupmap = new Dictionary<FloorplanGroup, FloorplanSubGroup[]>()
    {
        {
            FloorplanGroup.Floor,
            new FloorplanSubGroup[]
            {
                FloorplanSubGroup.Solid,
                FloorplanSubGroup.Mesh
            }
        },
        {
            FloorplanGroup.Edge,
            new FloorplanSubGroup[]
            {
                FloorplanSubGroup.Solid,
                FloorplanSubGroup.Window,
                FloorplanSubGroup.Door,
                FloorplanSubGroup.SingleColumn,
                FloorplanSubGroup.DoubleColumn
            }
        },
        {
            FloorplanGroup.Corner,
            new FloorplanSubGroup[]
            {
                FloorplanSubGroup.SingleColumn
            }
        }
    };
}
