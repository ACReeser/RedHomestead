using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using RedHomestead.Buildings;
using RedHomestead.Simulation;
using RedHomestead.Interiors;
using RedHomestead.Persistence;
using UnityEngine.PostProcessing;
using RedHomestead.Crafting;
using RedHomestead.Scoring;

[Serializable]
public struct ReportIORow
{
    public Text Name, Flow, Amount;
    internal Image IsConnected;

    internal void Bind(ReportIOData data, Sprite connected, Sprite disconnected)
    {
        Name.text = data.Name;
        Flow.text = data.Flow;
        Amount.text = data.Amount;
        IsConnected = Name.transform.GetChild(0).GetComponent<Image>();
        IsConnected.sprite = data.Connected ? connected : disconnected;
    }

    internal ReportIORow CreateNew(RectTransform parentTable)
    {
        ReportIORow result = new ReportIORow()
        {
            Name = GameObject.Instantiate(Name.gameObject).GetComponent<Text>(),
            Flow = GameObject.Instantiate(Flow.gameObject).GetComponent<Text>(),
            Amount = GameObject.Instantiate(Amount.gameObject).GetComponent<Text>(),
        };
        result.Name.transform.parent = parentTable;
        result.Flow.transform.parent = parentTable;
        result.Amount.transform.parent = parentTable;

        return result;
    }

    internal void Destroy()
    {
        GameObject.Destroy(Name.gameObject);
        GameObject.Destroy(Flow.gameObject);
        GameObject.Destroy(Amount.gameObject);
    }
}

internal struct ReportIOData
{
    public string Name, Flow, Amount;
    public bool Connected;
}

[Serializable]
public struct ReportFields
{
    public Text ModuleName, EnergyEfficiency, ReactionEfficiency, ReactionEquation;
    public RectTransform InputRow, OutputRow;
    public Sprite Connected, Disconnected;
}

public enum MiscIcon {
    Information,
    Rocket,
    Pipe,
    Plug,
    Harvest,
    Molecule,
    Umbilical,
    ClearSky,
    LightDust,
    HeavyDust,
    DustStorm,
    HammerAndPick,
    Bed,
    Money,
    PlanetFlag
}

[Serializable]
public struct Icons
{
    public Sprite[] ResourceIcons, CompoundIcons, MiscIcons;
}

public class NewsUI
{
    private const int CloseDurationMilliseconds = 200;
    public readonly RectTransform Panel, ProgressBar;
    private readonly Text Description;
    private readonly Image Icon, ProgressFill;
    public News News { get; private set; }
    public float TimeMilliseconds, DurationMilliseconds, CloseTimeMilliseconds;
    private float OriginalHeight;
    public bool IsShowing { get; private set; }

    public NewsUI(Transform panel)
    {
        this.Panel = panel.GetComponent<RectTransform>();
        this.Description = panel.GetChild(0).GetComponent<Text>();
        this.Icon = panel.GetChild(1).GetComponent<Image>();
        this.ProgressBar = panel.GetChild(2).GetComponent<RectTransform>();
        this.ProgressFill = ProgressBar.GetChild(0).GetComponent<Image>();
        this.Panel.gameObject.SetActive(false);
        this.ProgressBar.gameObject.SetActive(false);
        OriginalHeight = this.Panel.sizeDelta.y;
    }

    public void Fill(News news)
    {
        CloseTimeMilliseconds = 0f;
        TimeMilliseconds = 0f;
        DurationMilliseconds = news.DurationMilliseconds;
        News = news;
        Icon.sprite = GuiBridge.Instance.Icons.MiscIcons[Convert.ToInt32(news.Icon)];
        Description.text = news.Text;
        Panel.gameObject.SetActive(true);
        Panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, OriginalHeight);
        IsShowing = true;
    }

    public void Stop()
    {
    }

    public void Update()
    {
        if (TimeMilliseconds > DurationMilliseconds)
        {
            Panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Lerp(OriginalHeight, 0f, CloseTimeMilliseconds / CloseDurationMilliseconds));
            CloseTimeMilliseconds += UnityEngine.Time.deltaTime * 1000f;

            if (CloseTimeMilliseconds > CloseDurationMilliseconds)
            {
                IsShowing = false;
                Panel.gameObject.SetActive(false);
            }
        }
        else
        {
            //Panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, OriginalHeight);
            //Panel.sizeDelta = new Vector2(Panel.sizeDelta.x, OriginalHeight);
            TimeMilliseconds += Time.deltaTime * 1000f;
        }
    }
}

[Serializable]
public class NewsMenu
{
    public RectTransform VerticalPanel;

    private List<NewsUI> pool = new List<NewsUI>();
    private List<NewsUI> actives = new List<NewsUI>();
    private List<NewsUI> _toDisable = new List<NewsUI>();
    //private Queue<NewsUI> queue = new Queue<NewsUI>();

    public void Start()
    {
        foreach(Transform child in VerticalPanel)
        {
            pool.Add(new NewsUI(child));
        }
    }

    public void ShowNews(News news)
    {
        var usable = pool[0];
        pool.Remove(usable);
        usable.Fill(news);
        actives.Add(usable);
        usable.Panel.SetSiblingIndex(VerticalPanel.childCount - 1);
    }

    public void Update()
    {
        foreach(NewsUI ui in actives)
        {
            ui.Update();
            if (!ui.IsShowing)
            {
                _toDisable.Add(ui);
            }
        }
        if (_toDisable.Count > 0)
        {
            foreach(NewsUI ui in _toDisable)
            {
                actives.Remove(ui);
                pool.Add(ui);
            }
            _toDisable.Clear();
        }
    }
}

[Serializable]
public class RadialMenu
{
    public RectTransform RadialPanel, RadialsParent;
    public Image RadialSelector;
    internal Image[] Radials = null;

    public static Color HoverColor = new Color(1, 1, 1, 1);
    public static Color DefaultColor = new Color(1, 1, 1, 0.6f);
}

[Serializable]
public struct PromptUI
{
    public RectTransform Panel, ProgressBar, Background;
    public Text Key, Description, SecondaryKey, SecondaryDescription, TypeText;
    public Image ProgressFill;
}

[Serializable]
public class SurvivalBar
{
    public Image Bar;
    internal Text Hours;
    public virtual void Initialize()
    {
        Hours = Bar.transform.GetChild(0).GetComponent<Text>();
    }
}

[Serializable]
public struct SurvivalBarsUI
{
    public SurvivalBar Food;
    public SurvivalBar Water;
    public SurvivalBar Power;
    public SurvivalBar Oxygen;
    public SurvivalBar RoverPower;
    public SurvivalBar RoverOxygen;
    public SurvivalBar HabitatPower;
    public SurvivalBar HabitatOxygen;
}

[Serializable]
public struct PrinterUI
{
    public RectTransform Panel, AvailablePanel, AllPanel, AllList, AllListButtonPrefab, AvailableList, NoneAvailableFlag, CurrentPrintButtonHintsPanel, AvailableDetailPanel, AvailableDetailMaterialsListParent;
    public Text AvailableMaterialsHeader, AvailablePrintTime, TabSwitchText, DetailName, DetailDescription;
    public Image TimeFill;

    private ThreeDPrinter currentPrinter;
    private bool showingAll;
    private Matter hoverComponent;
    private bool showing;
    public bool Showing { get; private set; }

    public void SetShowing(bool showing, ThreeDPrinter printer = null)
    {
        Showing = showing;
        Panel.gameObject.SetActive(showing);

        if (showing && printer)
        {
            ToggleAvailable(true, printer);
        }
    }

    public void ToggleAvailable(bool? showAvailable = null, ThreeDPrinter printer = null)
    {
        if (!showAvailable.HasValue)
            showAvailable = showingAll;

        if (printer)
        {
            currentPrinter = printer;
        }

        AvailablePanel.gameObject.SetActive(showAvailable.Value);
        AllPanel.gameObject.SetActive(!showAvailable.Value);

        showingAll = !showAvailable.Value;

        if (showingAll)
            FillAllList();
        else if (currentPrinter != null)
            FillFirstScreen(currentPrinter);
    }

    public void FillAllList()
    {
        TabSwitchText.text = "Available Prints";
        foreach (Transform child in AllList.transform)
        {
            GameObject.Destroy(child.gameObject);
        }
        foreach (var kvp in Crafting.PrinterData)
        {
            var newGuy = GameObject.Instantiate<RectTransform>(AllListButtonPrefab, AllList.transform);
            newGuy.GetChild(0).GetComponent<Text>().text = kvp.Key.ToString();
            newGuy.GetChild(1).GetComponent<Image>().sprite = kvp.Key.Sprite();
            newGuy.GetChild(2).GetComponent<Text>().text = kvp.Value.BuildTimeHours + "<size=10>hrs</size>";

            var requires = newGuy.GetChild(3);
            for (int j = 1; j < 5; j++)
            {
                int craftI = j - 1;
                bool hasRequirement = craftI < kvp.Value.Requirements.Count;
                requires.GetChild(j).gameObject.SetActive(hasRequirement);

                if (hasRequirement)
                {
                    requires.GetChild(j).GetComponent<Image>().sprite = kvp.Value.Requirements[craftI].Type.Sprite();
                }
            }
        }
    }

    private string GetTimeText(float hours)
    {
        return String.Format("{0:0.#}<size=22>hrs</size>\n<size=18>{1}</size>", hours, IsCurrentlyPrinting ? "remaining" : "required");
    }

    internal void SetHover(Matter hoverMatter)
    {
        hoverComponent = hoverMatter;

        RefreshDetail();
    }

    private Matter lastEffectiveDetail;
    internal void RefreshDetail()
    {
        Matter newEffectiveDetail;
        if (IsCurrentlyPrinting)
            newEffectiveDetail = currentPrinter.FlexData.Printing;
        else
            newEffectiveDetail = hoverComponent;

        //things you do regardless of detail changing
        if (IsCurrentlyPrinting)
        {
            AvailablePrintTime.text = GetTimeText(Crafting.PrinterData[newEffectiveDetail].BuildTimeHours * (1f - currentPrinter.FlexData.Progress));
            TimeFill.fillAmount = currentPrinter.FlexData.Progress;
        }

        //things you only do if things change
        if (newEffectiveDetail != lastEffectiveDetail)
        {
            bool isBlank = newEffectiveDetail == Matter.Unspecified;
            
            AvailableDetailPanel.gameObject.SetActive(!isBlank);

            if (isBlank)
            {
                DetailName.text = DetailDescription.text = AvailablePrintTime.text = "";
                TimeFill.fillAmount = 0f;
            }
            else
            {
                DetailName.text = newEffectiveDetail.ToString();
                DetailDescription.text = Crafting.PrinterData[newEffectiveDetail].Description;

                if (IsCurrentlyPrinting)
                {
                    AvailableMaterialsHeader.text = "MATERIALS\nCONSUMED";
                    //set detail to use whole width
                    AvailableDetailPanel.offsetMin = new Vector2(0, 0);
                }
                else
                {
                    AvailableMaterialsHeader.text = "MATERIALS\nREQUIRED";
                    AvailablePrintTime.text = GetTimeText(Crafting.PrinterData[newEffectiveDetail].BuildTimeHours);
                    //set detail to give 200px to list of available
                    AvailableDetailPanel.offsetMin = new Vector2(200, 0);

                    TimeFill.fillAmount = 0f;
                }

                FillMaterials(newEffectiveDetail);
            }
        }
        lastEffectiveDetail = newEffectiveDetail;
    }

    private void FillMaterials(Matter component)
    {
        var reqs = Crafting.PrinterData[component].Requirements;
        for (int i = 1; i < AvailableDetailMaterialsListParent.childCount; i++)
        {
            var t = AvailableDetailMaterialsListParent.GetChild(i);
            int reqI = i - 1;
            bool show =  reqI < reqs.Count;
            t.gameObject.SetActive(show);
            if (show)
            {
                t.GetComponent<Text>().text = reqs[reqI].ToString();
                t.GetChild(0).GetComponent<Image>().sprite = reqs[reqI].Type.Sprite();
            }
        }        
    }

    private bool IsCurrentlyPrinting
    {
        get { return currentPrinter != null && currentPrinter.FlexData.Printing != Matter.Unspecified; }
    }
    private bool HasDetail
    {
        get { return IsCurrentlyPrinting || hoverComponent != Matter.Unspecified; }
    }

    public void FillFirstScreen(ThreeDPrinter printer)
    {
        TabSwitchText.text = "All Prints";
        CurrentPrintButtonHintsPanel.gameObject.SetActive(IsCurrentlyPrinting);
        AvailableList.gameObject.SetActive(!IsCurrentlyPrinting);

        if (!IsCurrentlyPrinting)
        {
            FillAvailableList(printer);
        }

        //this is...complicated
        //technically you should never be able to detail CO
        //so this is to allow Detail to populate when we change to Unspecified
        //blow out the cache on the last effective detail
        lastEffectiveDetail = Matter.CarbonMonoxide;
        RefreshDetail();
    }

    public void FillAvailableList(ThreeDPrinter printer)
    {
        List<KeyValuePair<Matter, PrinterData>> available = new List<KeyValuePair<Matter, PrinterData>>();
        foreach (var kvp in Crafting.PrinterData)
        {
            bool canPrint = true;
            foreach (var req in kvp.Value.Requirements)
            {
                bool has = printer.Has(req);
                if (!has)
                {
                    canPrint = false;
                    break;
                }
                else
                {
                    canPrint = canPrint && true;
                }
            }

            if (canPrint)
            {
                available.Add(kvp);
            }
        }
        int i = 0;
        foreach (Transform child in AvailableList.transform)
        {
            if (child == NoneAvailableFlag)
                continue;

            bool show = i < available.Count;
            child.gameObject.SetActive(show);

            if (show)
            {
                KeyValuePair<Matter, PrinterData> kvp = available[i];
                child.name = Convert.ToInt32(kvp.Key).ToString();
                child.GetChild(0).GetComponent<Text>().text = kvp.Key.ToString();
                child.GetChild(1).GetComponent<Image>().sprite = kvp.Key.Sprite();
            }
            i++;
        }
        AvailableDetailPanel.gameObject.SetActive(available.Count > 0);
        NoneAvailableFlag.gameObject.SetActive(available.Count == 0);
    }

    internal void Hover(int number)
    {
        SetHover((Matter)int.Parse(AvailableList.GetChild(number).name));
    }

    internal void Select(int number)
    {
        currentPrinter.BeginPrinting((Matter)int.Parse(AvailableList.GetChild(number).name));
        FillFirstScreen(currentPrinter);
    }
}

[Serializable]
public class TemperatureUI
{
    public Text WorldTemperatureText;
    public Text HabitatTemperatureText;
}

/// <summary>
/// Scripting interface for all GUI elements
/// syncs PlayerInput state to UI
/// has internal state for showing prompts and panels
/// </summary>
public class GuiBridge : MonoBehaviour {
    public static GuiBridge Instance { get; private set; }

    public Canvas GUICanvas;
    public RectTransform ConstructionPanel, ConstructionGroupPanel, ConstructionModulesPanel, PlacingPanel, KilledPanel, FloorplanGroupPanel, FloorplanSubgroupPanel, FloorplanPanel, HelpPanel, ReportPanel, EscapeMenuPanel, Crosshair;
    public Text ConstructionHeader, EquippedText, PlacingText, TimeText, TimeChevronText, KilledByReasonText;
    public Button[] ConstructionGroupButtons;
    public Text[] ConstructionGroupHints, FloorplanGroupHints;
    public RectTransform[] ConstructionRequirements, ConstructionModuleButtons;
    public Image EquippedImage, ColdImage, HotImage, AutosaveIcon, SprintIcon;
    public AudioSource ComputerAudioSource;
    public SurvivalBarsUI SurvivalBars;
    public ReportIORow ReportRowTemplate;
    public ReportFields ReportTexts;
    public RadialMenu RadialMenu;
    public RedHomestead.Equipment.EquipmentSprites EquipmentSprites;
    public PromptUI Prompts;
    public Icons Icons;
    public NewsMenu News;
    public PostProcessingProfile PostProfile;
    public PrinterUI Printer;
    public TemperatureUI Temperature;
    public PowerGridScreen PowerGrid;

    public RadioUI Radio;

    internal Text[] ConstructionRequirementsText;

    internal bool RadialMenuOpen = false;
    internal PromptInfo CurrentPrompt { get; set; }

    void Awake()
    {
        Instance = this;
        TogglePromptPanel(false);
        this.ConstructionPanel.gameObject.SetActive(false);
        ConstructionRequirementsText = new Text[ConstructionRequirements.Length];
        int i = 0;
        foreach (RectTransform t in ConstructionRequirements)
        {
            ConstructionRequirementsText[i] = t.GetChild(0).GetComponent<Text>();
            i++;
        }
        SurvivalBars.Oxygen.Initialize();
        SurvivalBars.Power.Initialize();
        SurvivalBars.Water.Initialize();
        SurvivalBars.Food.Initialize();
        SurvivalBars.RoverOxygen.Initialize();
        SurvivalBars.RoverPower.Initialize();
        SurvivalBars.HabitatOxygen.Initialize();
        SurvivalBars.HabitatPower.Initialize();
        ToggleReportMenu(false);
        ToggleRadialMenu(false);
        ToggleAutosave(false);
        TogglePrinter(false);
        PowerGrid.Toggle(false);
        Radio.Group.alpha = 0;
        //same as ToggleEscapeMenu(false) basically
        this.EscapeMenuPanel.gameObject.SetActive(false);
    }

    internal void ToggleAutosave(bool state)
    {
        this.AutosaveIcon.gameObject.SetActive(state);
    }

    void Start()
    {
        //this.RefreshPlanningUI();
        News.Start();

        if (Game.Current.IsNewGame)
        {
            print("hello new gamer");
            ShowNews(NewsSource.ToolOpenHint);
            ShowNews(NewsSource.FOneHint);
        }

        RefreshSprintIcon(false);
    }

    //private Queue<News> newsTicker = new Queue<News>();
    private Coroutine newsCoroutine;
    internal void ShowNews(News news)
    {
        if (news != null)
        {
            News.ShowNews(news);
            if (newsCoroutine == null)
            {
                newsCoroutine = StartCoroutine(StartShowNews());
            }
        }
    }

    private IEnumerator StartShowNews()
    {
        while (isActiveAndEnabled)
        {
            News.Update();
            yield return new WaitForEndOfFrame();
        }

        newsCoroutine = null;
    }

    private void TogglePromptPanel(bool isActive)
    {
        this.Prompts.Panel.gameObject.SetActive(isActive);
    }

    public void ShowPrompt(PromptInfo prompt)
    {
        Prompts.Key.text = prompt.Key;
        Prompts.Key.transform.parent.gameObject.SetActive(prompt.Key != null);
        Prompts.Description.text = prompt.Description;

        Prompts.ProgressBar.gameObject.SetActive(prompt.UsesProgress);
        Prompts.ProgressFill.fillAmount = prompt.Progress;
        
        Prompts.SecondaryDescription.gameObject.SetActive(prompt.HasSecondary);
        Prompts.SecondaryDescription.text = prompt.SecondaryDescription;

        Prompts.SecondaryKey.transform.parent.gameObject.SetActive(prompt.HasSecondary);
        Prompts.SecondaryKey.text = prompt.SecondaryKey;

        Prompts.Background.offsetMin = new Vector2(0, prompt.HasSecondary ? -66 : 0);

        Prompts.TypeText.text = prompt.ItalicizedText;

        TogglePromptPanel(true);
        CurrentPrompt = prompt;
    }

    //todo: just pass constructionZone, it's less params
    internal void ShowConstruction(List<IResourceEntry> requiresList, Dictionary<Matter, float> hasCount, Module toBeBuilt)
    {
        //show the name of the thing being built
        this.ConstructionPanel.gameObject.SetActive(true);
        this.ConstructionHeader.text = "Building a " + toBeBuilt.ToString();

        //show a list of resources gathered / resources required
        for (int i = 0; i < this.ConstructionRequirements.Length; i++)
        {
            if (i < requiresList.Count)
            {
                IResourceEntry resourceEntry = requiresList[i];
                string output = resourceEntry.ToStringWithAvailableVolume(hasCount[resourceEntry.Type]);
                this.ConstructionRequirementsText[i].text = output;
                this.ConstructionRequirements[i].gameObject.SetActive(true);
            }
            else
            {
                this.ConstructionRequirements[i].gameObject.SetActive(false);
            }
        }
    }

    #region cinematic mode
    private enum CinematicModes { None, WithGUI, NoGUI }
    private CinematicModes CinematicMode = CinematicModes.None;
    internal void ToggleCinematicMode()
    {
        int newCinematic = (((int)this.CinematicMode) + 1);

        if (newCinematic > (int)CinematicModes.NoGUI)
            newCinematic = 0;

        CinematicMode = (CinematicModes)newCinematic;

        switch (CinematicMode)
        {
            case CinematicModes.None:
                GUICanvas.enabled = true;
                PlayerInput.Instance.FPSController.MouseLook.smooth = false;
                break;
            case CinematicModes.WithGUI:
                GUICanvas.enabled = true;
                PlayerInput.Instance.FPSController.MouseLook.smooth = true;
                break;
            case CinematicModes.NoGUI:
                PlayerInput.Instance.FPSController.MouseLook.smooth = true;
                GUICanvas.enabled = false;
                break;
        }
        PostProfile.motionBlur.enabled = CinematicMode != CinematicModes.None;
        PlayerInput.Instance.FPSController.MouseLook.smooth = PostProfile.motionBlur.enabled;
    }
    #endregion

    internal void RefreshSprintIcon(bool sprinting)
    {
        this.SprintIcon.gameObject.SetActive(sprinting);
    }

    private void _doToggleEscapeMenu()
    {
        bool escapeMenuWillBeVisible = !this.EscapeMenuPanel.gameObject.activeInHierarchy;
        this.EscapeMenuPanel.gameObject.SetActive(escapeMenuWillBeVisible);

        Cursor.visible = escapeMenuWillBeVisible;
        Cursor.lockState = escapeMenuWillBeVisible ? CursorLockMode.None : CursorLockMode.Confined;
    }

    /// <summary>
    /// Called by cancel button
    /// </summary>
    public void ToggleEscapeMenu()
    {
        //player input will actually call _ToggleEscapeMenuProgrammatically for us
        //which calls _doToggleEscapeMenu
        //so don't call _doToggleEscapeMenu here
        PlayerInput.Instance.ToggleMenu();
    }

    /// <summary>
    /// Called by player input
    /// </summary>
    internal void _ToggleEscapeMenuProgrammatically()
    {
        _doToggleEscapeMenu();
    }

    public void ConfirmQuit()
    {
        Autosave.Instance.AutosaveEnabled = false;
        UnityEngine.SceneManagement.SceneManager.LoadScene("menu", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
    public void ConfirmSaveAndQuit()
    {
        Autosave.Instance.Save();
        this.ConfirmQuit();
    }

    internal void ToggleReportMenu(bool isOn)
    {
        ReportPanel.gameObject.SetActive(isOn);
    }

    internal void HideConstruction()
    {
        this.ConstructionPanel.gameObject.SetActive(false);
    }

    public void HidePrompt()
    {
        if (CurrentPrompt != null)
        {
            TogglePromptPanel(false);
            CurrentPrompt = null;
        }
    }
    
    internal void ToggleHelpMenu()
    {
        HelpPanel.gameObject.SetActive(!HelpPanel.gameObject.activeSelf);
    }
    /// <summary>
    /// syncs player input mode
    /// </summary>
    internal void RefreshMode()
    {
        switch (PlayerInput.Instance.CurrentMode)
        {
            default:
                RefreshEquipped();
                break;
            case PlayerInput.InputMode.Pipeline:
                this.EquippedText.text = "Pipeline";
                this.EquippedImage.sprite = Icons.MiscIcons[(int)MiscIcon.Pipe];
                break;
            case PlayerInput.InputMode.Powerline:
                this.EquippedText.text = "Powerline";
                this.EquippedImage.sprite = Icons.MiscIcons[(int)MiscIcon.Plug];
                break;
            case PlayerInput.InputMode.Umbilical:
                this.EquippedText.text = "Umbilical";
                this.EquippedImage.sprite = Icons.MiscIcons[(int)MiscIcon.Umbilical];
                break;
        }

        if (PlayerInput.Instance.Loadout.Equipped != RedHomestead.Equipment.Equipment.Blueprints)
        {
            this.PlacingPanel.gameObject.SetActive(false);
        }

        //this.RefreshPlanningUI();
    }

    internal void RefreshEquipped()
    {
        this.EquippedText.text = PlayerInput.Instance.Loadout.Equipped.ToString();
        this.EquippedImage.sprite = EquipmentSprites.FromEquipment(PlayerInput.Instance.Loadout.Equipped);
    }

    internal void ShowKillMenu(string reason)
    {
        KilledPanel.transform.gameObject.SetActive(true);
        KilledByReasonText.text = "DEATH BY " + reason;
    }

    public void Restart()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    public void Continue()
    {
        KilledPanel.transform.gameObject.SetActive(false);
        PlayerInput.Instance.ResetAfterKillPlayer();
    }

    private void RefreshBarWarningCriticalText(Text textElement, int hoursLeftHint)
    {
        textElement.enabled = hoursLeftHint < 3;
        textElement.text = string.Format("<{0}h", hoursLeftHint);
    }

    internal void RefreshOxygenBar(float percentage, int hoursLeftHint)
    {
        this.SurvivalBars.Oxygen.Bar.fillAmount = percentage;
        this.RefreshBarWarningCriticalText(this.SurvivalBars.Oxygen.Hours, hoursLeftHint);
    }

    internal void RefreshWaterBar(float percentage, int hoursLeftHint)
    {
        this.SurvivalBars.Water.Bar.fillAmount = percentage;
        this.RefreshBarWarningCriticalText(this.SurvivalBars.Water.Hours, hoursLeftHint);
    }

    internal void RefreshFoodBar(float percentage, int hoursLeftHint)
    {
        this.SurvivalBars.Food.Bar.fillAmount = percentage;
        this.RefreshBarWarningCriticalText(this.SurvivalBars.Food.Hours, hoursLeftHint);
    }

    //internal void RefreshRadiationBar(float percentage)
    //{
    //    this.SurvivalBars.Rad.Bar.fillAmount = percentage;
    //}

    internal void RefreshPowerBar(float percentage, int hoursLeftHint)
    {
        this.SurvivalBars.Power.Bar.fillAmount = percentage;
        this.RefreshBarWarningCriticalText(this.SurvivalBars.Power.Hours, hoursLeftHint);
    }
    internal void RefreshRoverOxygenBar(float percentage, int hoursLeftHint)
    {
        this.SurvivalBars.RoverOxygen.Bar.fillAmount = percentage;
        this.RefreshBarWarningCriticalText(this.SurvivalBars.RoverOxygen.Hours, hoursLeftHint);
    }

    internal void RefreshRoverPowerBar(float percentage, int hoursLeftHint)
    {
        this.SurvivalBars.RoverPower.Bar.fillAmount = percentage;
        this.RefreshBarWarningCriticalText(this.SurvivalBars.RoverPower.Hours, hoursLeftHint);
    }

    internal void RefreshHabitatOxygenBar(float percentage, int hoursLeftHint)
    {
        this.SurvivalBars.HabitatOxygen.Bar.fillAmount = percentage;
        this.RefreshBarWarningCriticalText(this.SurvivalBars.HabitatOxygen.Hours, hoursLeftHint);
    }

    internal void RefreshHabitatPowerBar(float percentage, int hoursLeftHint)
    {
        this.SurvivalBars.HabitatPower.Bar.fillAmount = percentage;
        this.RefreshBarWarningCriticalText(this.SurvivalBars.HabitatPower.Hours, hoursLeftHint);
    }

    internal void RefreshTemperatureGauges()
    {
        this.HotImage.enabled = SurvivalTimer.Instance.ExternalTemperature == global::Temperature.Hot;
        this.ColdImage.enabled = SurvivalTimer.Instance.ExternalTemperature == global::Temperature.Cold;
    }

    public FloorplanGroup selectedFloorplanGroup = FloorplanGroup.Undecided;
    public Floorplan selectedFloorplanSubgroup;
    public FloorplanMaterial selectedFloorplanMaterial;


    private RedHomestead.Equipment.Slot lastHoverSlot = (RedHomestead.Equipment.Slot)(-1);

    internal RedHomestead.Equipment.Slot ToggleRadialMenu(bool isMenuOpen)
    {
        this.RadialMenu.RadialPanel.gameObject.SetActive(isMenuOpen);
        this.RadialMenuOpen = isMenuOpen;
        this.RadialMenu.RadialSelector.gameObject.SetActive(isMenuOpen);

        //no need to actually show the cursor
        //Cursor.visible = isMenuOpen;
        Cursor.lockState = isMenuOpen ? CursorLockMode.None : CursorLockMode.Locked;

        return lastHoverSlot;
    }

    internal void BuildRadialMenu(RedHomestead.Equipment.Loadout load)
    {
        if (RadialMenu.Radials == null)
        {
            RadialMenu.Radials = new Image[RadialMenu.RadialsParent.childCount];

            foreach(Transform t in RadialMenu.RadialsParent)
            {
                Image img = t.GetComponent<Image>();
                int index = int.Parse(img.name);
                RedHomestead.Equipment.Slot s = (RedHomestead.Equipment.Slot)index;
                RadialMenu.Radials[(int)s] = img;
            }
        }


        int i = 0;
        foreach(Image img in RadialMenu.Radials)
        {
            RedHomestead.Equipment.Slot s = (RedHomestead.Equipment.Slot)i;
            img.sprite = EquipmentSprites.FromEquipment(load[s]);
            i++;
        }
    }

    //use half the width of the sectors
    private const float sectorThetaOffset = 30f;
    internal void HighlightSector(float theta)
    {
        var rotation = Mathf.Lerp(0, 360, Mathf.InverseLerp(-180f, 180f, theta));

        int index = (int)Mathf.Round((rotation - sectorThetaOffset) / 60f);
        //corresponds to enum -^

        //corresponds to UI rotation -v
        rotation = (index + 2) * 60f;
        this.RadialMenu.RadialSelector.rectTransform.localRotation = Quaternion.Euler(0, 0, rotation);

        if ((int)lastHoverSlot > -1)
            RadialMenu.Radials[(int)lastHoverSlot].color = RadialMenu.DefaultColor;

        RadialMenu.Radials[index].color = RadialMenu.HoverColor;

        lastHoverSlot = (RedHomestead.Equipment.Slot)index;
    }

    private ReportIORow[] currentIORows;
    public Texture2D noiseTexture;
    public Shader blurShader;

    internal void WriteReport(string moduleName, string reaction, string energyEfficiency, string reactionEfficiency, 
        ReportIOData? power, ReportIOData[] inputs, ReportIOData[] outputs)
    {
        ToggleReportMenu(true);

        ReportTexts.ModuleName.text = moduleName;
        ReportTexts.EnergyEfficiency.text = "Energy Efficiency: "+energyEfficiency;
        ReportTexts.ReactionEfficiency.text = "Reaction Efficiency: "+reactionEfficiency;
        ReportTexts.ReactionEquation.text = reaction;
        
        if (currentIORows != null)
        {
            foreach(ReportIORow row in currentIORows)
            {
                row.Destroy();
            }
            currentIORows = null;
        }

        if (power.HasValue)
        {
            ReportRowTemplate.Bind(power.Value, ReportTexts.Connected, ReportTexts.Disconnected);
        }

        currentIORows = new ReportIORow[inputs.Length + outputs.Length];
        int i;
        for (i = 0; i < inputs.Length; i++)
        {
            currentIORows[i] = ReportRowTemplate.CreateNew(ReportTexts.InputRow);
            currentIORows[i].Bind(inputs[i], ReportTexts.Connected, ReportTexts.Disconnected);
        }
        for (int j = 0; j < outputs.Length; j++)
        {
            currentIORows[i+j] = ReportRowTemplate.CreateNew(ReportTexts.OutputRow);
            currentIORows[i+j].Bind(outputs[j], ReportTexts.Connected, ReportTexts.Disconnected);
        }
    }

    internal void RefreshSurvivalPanel(bool isInVehicle, bool isInHabitat)
    {
        this.SurvivalBars.RoverOxygen.Bar.transform.parent.gameObject.SetActive(isInVehicle);
        this.SurvivalBars.RoverPower.Bar.transform.parent.gameObject.SetActive(isInVehicle);
        this.SurvivalBars.HabitatOxygen.Bar.transform.parent.gameObject.SetActive(isInHabitat);
        this.SurvivalBars.HabitatPower.Bar.transform.parent.gameObject.SetActive(isInHabitat);
    }

    void OnDestroy()
    {
        ResetDeprivationUX();
        PostProfile.motionBlur.enabled = false;
        if (newsCoroutine != null)
            StopCoroutine(newsCoroutine);
    }

    internal void TogglePrinter(bool show, ThreeDPrinter printer = null)
    {
        Printer.SetShowing(show, printer);

        Cursor.visible = show;
        Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Confined;
    }

    public void HoverPrintable(int number)
    {
        Printer.Hover(number);
    }
    public void SelectPrintable(int number)
    {
        Printer.Select(number);
    }

    internal void ResetDeprivationUX()
    {
        Reset(PostProfile.colorGrading);
        Reset(PostProfile.vignette);
        Reset(PostProfile.depthOfField);
        Reset(PostProfile.motionBlur);
        PlayerInput.Instance.HeartbeatSource.Stop();
        PlayerInput.Instance.VocalSource.Stop();
    }

    private void Reset(PostProcessingModel model)
    {
        model.Reset();
        model.enabled = false;
    }

    internal bool ShowingDeprivationUX { get; private set; }
    internal void RefreshDeprivationUX(SurvivalTimer survivalTimer)
    {
        bool isPowerDeprived = survivalTimer.Data.Power.DeprivationSeconds > 0f,
             isOxygenDeprived = survivalTimer.Data.Oxygen.DeprivationSeconds > 0f,
             isFoodDeprived = survivalTimer.Data.Food.DeprivationSeconds > 0f,
             isWaterDeprived = survivalTimer.Data.Water.DeprivationSeconds > 0f;

        ShowingDeprivationUX = isPowerDeprived || isOxygenDeprived || isFoodDeprived || isWaterDeprived;

        //sounds
        if (isOxygenDeprived && !PlayerInput.Instance.HeartbeatSource.isPlaying)
        {
            PlayerInput.Instance.VocalSource.clip = PlayerInput.Instance.HeartbeatsAndVocals.Gasping;
            PlayerInput.Instance.VocalSource.Play();
            PlayerInput.Instance.HeartbeatSource.clip = PlayerInput.Instance.HeartbeatsAndVocals.SlowToDeathHeartbeat;
            PlayerInput.Instance.HeartbeatSource.Play();
        }
        else if (isPowerDeprived && !PlayerInput.Instance.HeartbeatSource.isPlaying)
        {
            PlayerInput.Instance.VocalSource.clip = PlayerInput.Instance.HeartbeatsAndVocals.Chattering;
            PlayerInput.Instance.VocalSource.Play();
            PlayerInput.Instance.HeartbeatSource.clip = PlayerInput.Instance.HeartbeatsAndVocals.SlowHeartbeat;
            PlayerInput.Instance.HeartbeatSource.Play();
        }
        else if (isFoodDeprived && !PlayerInput.Instance.HeartbeatSource.isPlaying)
        {
            PlayerInput.Instance.HeartbeatSource.clip = PlayerInput.Instance.HeartbeatsAndVocals.SlowHeartbeat;
            PlayerInput.Instance.HeartbeatSource.Play();
        }
        else if (isWaterDeprived && !PlayerInput.Instance.HeartbeatSource.isPlaying)
        {
            PlayerInput.Instance.HeartbeatSource.clip = PlayerInput.Instance.HeartbeatsAndVocals.SlowHeartbeat;
            PlayerInput.Instance.HeartbeatSource.Play();
        }
        else if (!ShowingDeprivationUX && PlayerInput.Instance.HeartbeatSource.isPlaying)
        {
            PlayerInput.Instance.HeartbeatSource.Stop();
            PlayerInput.Instance.VocalSource.Stop();
        }

        //colorgrading
        if (isOxygenDeprived)
        {
            PostProfile.colorGrading.enabled = true;
            float lerpT = survivalTimer.Data.Oxygen.DeprivationSeconds / survivalTimer.DeprivationDurations.OxygenDeprivationSurvivalTimeSeconds;
            ColorGradingModel.Settings newSettings = ColorGradingModel.Settings.defaultSettings;
            ColorGradingModel.BasicSettings newBasics = ColorGradingModel.BasicSettings.defaultSettings;
            newBasics.saturation = Mathf.Min(1f, Mathf.Lerp(1f, 0f, lerpT)+.25f);
            newBasics.contrast = Mathf.Lerp(1f, 2f, lerpT);
            newSettings.basic = newBasics;
            PostProfile.colorGrading.settings = newSettings;
        }
        else if (isPowerDeprived)
        {
            PostProfile.colorGrading.enabled = true;
            float lerpT = survivalTimer.Data.Power.DeprivationSeconds / survivalTimer.DeprivationDurations.PowerDeprivationSurvivalTimeSeconds;
            ColorGradingModel.Settings newSettings = ColorGradingModel.Settings.defaultSettings;
            ColorGradingModel.BasicSettings newBasics = ColorGradingModel.BasicSettings.defaultSettings;
            newBasics.temperature = Mathf.Lerp(0f, -40f, lerpT);
            newSettings.basic = newBasics;
            PostProfile.colorGrading.settings = newSettings;
        }
        else if (isWaterDeprived)
        {
            PostProfile.colorGrading.enabled = true;
            float lerpT = survivalTimer.Data.Water.DeprivationSeconds / survivalTimer.DeprivationDurations.WaterDeprivationSurvivalTimeSeconds;
            ColorGradingModel.Settings newSettings = ColorGradingModel.Settings.defaultSettings;
            ColorGradingModel.BasicSettings newBasics = ColorGradingModel.BasicSettings.defaultSettings;
            newBasics.temperature = Mathf.Lerp(0f, 35f, lerpT);
            newBasics.saturation = Mathf.Lerp(1f, 0f, lerpT);
            newSettings.basic = newBasics;
            PostProfile.colorGrading.settings = newSettings;
        }
        else if (isFoodDeprived)
        {
            PostProfile.colorGrading.enabled = true;
            float lerpT = survivalTimer.Data.Food.DeprivationSeconds / survivalTimer.DeprivationDurations.FoodDeprivationSurvivalTimeSeconds;
            ColorGradingModel.Settings newSettings = ColorGradingModel.Settings.defaultSettings;
            ColorGradingModel.BasicSettings newBasics = ColorGradingModel.BasicSettings.defaultSettings;
            newBasics.contrast = Mathf.Lerp(1f, 1.5f, lerpT);
            newBasics.saturation = Mathf.Lerp(1f, 0f, lerpT);
            newSettings.basic = newBasics;
            PostProfile.colorGrading.settings = newSettings;
        }
        else
        {
            PostProfile.colorGrading.enabled = false;
        }

        //motion blur
        if (isFoodDeprived)
        {
            PostProfile.motionBlur.enabled = true;
            float lerpT = survivalTimer.Data.Food.DeprivationSeconds / survivalTimer.DeprivationDurations.FoodDeprivationSurvivalTimeSeconds;
            MotionBlurModel.Settings newSettings = MotionBlurModel.Settings.defaultSettings;
            newSettings.frameBlending = Mathf.Lerp(1f, 0.5f, lerpT);
            newSettings.shutterAngle = Mathf.Lerp(0f, 360f, lerpT);
            newSettings.sampleCount = 4;
            PostProfile.motionBlur.settings = newSettings;
        }

        //depth of field
        if (isWaterDeprived)
        {
            PostProfile.depthOfField.enabled = true;
            float lerpT = survivalTimer.Data.Water.DeprivationSeconds / survivalTimer.DeprivationDurations.WaterDeprivationSurvivalTimeSeconds;
            DepthOfFieldModel.Settings newSettings = DepthOfFieldModel.Settings.defaultSettings;
            //DepthOfFieldModel.BasicSettings newBasics = DepthOfFieldModel.BasicSettings.defaultSettings;
            newSettings.aperture = Mathf.Lerp(5f, 0.05f, lerpT);
            newSettings.useCameraFov = true;
            newSettings.focusDistance = 1f;
            newSettings.kernelSize = DepthOfFieldModel.KernelSize.VeryLarge;
            PostProfile.depthOfField.settings = newSettings;
        }

        //vignetting
        if (isOxygenDeprived)
        {
            PostProfile.vignette.enabled = true;
            float lerpT = survivalTimer.Data.Oxygen.DeprivationSeconds;
            lerpT -= (float)Math.Truncate(lerpT);

            VignetteModel.Settings newSettings = VignetteModel.Settings.defaultSettings;
            newSettings.color = new Color(51f / 255f, 51f / 255f, 51f / 255f);
            newSettings.intensity = Mathf.PingPong(lerpT, .5f);
            newSettings.smoothness = 1f;
            newSettings.roundness = 1f;
            PostProfile.vignette.settings = newSettings;
        }
        else if (isPowerDeprived)
        {
            PostProfile.vignette.enabled = true;
            float lerpT = survivalTimer.Data.Power.DeprivationSeconds / survivalTimer.DeprivationDurations.PowerDeprivationSurvivalTimeSeconds;

            VignetteModel.Settings newSettings = VignetteModel.Settings.defaultSettings;
            newSettings.color = new Color(68f/255f, 108f / 255f, 255f / 255f);
            newSettings.intensity = Mathf.Lerp(0f, 0.76f, lerpT);
            newSettings.smoothness = 0.169f;
            newSettings.roundness = 1;
            PostProfile.vignette.settings = newSettings;
        }
        else if (isFoodDeprived)
        {
            PostProfile.vignette.enabled = true;
            float lerpT = survivalTimer.Data.Food.DeprivationSeconds / survivalTimer.DeprivationDurations.FoodDeprivationSurvivalTimeSeconds;

            VignetteModel.Settings newSettings = VignetteModel.Settings.defaultSettings;
            newSettings.color = new Color(32f / 255f, 32f / 255f, 32f / 255f);
            newSettings.intensity = Mathf.Lerp(0f, 0.4f, lerpT);
            newSettings.smoothness = Mathf.Lerp(0f, 0.5f, lerpT);
            newSettings.roundness = 1;
            PostProfile.vignette.settings = newSettings;
        }
        else
        {
            PostProfile.vignette.enabled = false;
        }
    }

    internal void ShowScoringEvent(ScoringReward result)
    {
        this.ShowNews(NewsSource.ScoringEvent.Clone(result.Title+"\n+"+result.Score));
    }
}

[Serializable]
public class RadioUI
{
    public Text AgentName;
    public CanvasGroup Group;
}