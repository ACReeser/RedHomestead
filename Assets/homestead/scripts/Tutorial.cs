using RedHomestead.Crafting;
using RedHomestead.Equipment;
using RedHomestead.Simulation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public enum FinishTutorialChoice { NextLesson, ExitToMenu, ListOfLessons }

[Serializable]
public class EventPanel
{
    public CanvasGroup Group;
    public Text Header, Description;
    public Image Sprite;

    internal bool Visible;

    public void Fill(string header, string description, Sprite sprite)
    {
        Header.text = header;
        Description.text = description;
        Sprite.sprite = sprite;
    }
}

[Serializable]
public class MainTutorialPanel
{
    public RectTransform Panel, ContinuePanel;
    public Text Header, Description, Steps;
}

public class Tutorial : MonoBehaviour, ITriggerSubscriber
{
    public Canvas TutorialCanvas;
    public EventPanel EventPanel;
    public Image Backdrop;
    public MainTutorialPanel TutorialPanel;

    public TriggerForwarder Mars101_LZTarget;

    public Transform OutsideHabAnchor, Arrow, RTGAnchor, RTGPowerArrow, HabPowerArrow;
    public LandingZone LZ;
    public RadioisotopeThermoelectricGenerator RTG;
    public Habitat Hab;

    internal TutorialLesson[] Lessons;
    private static int activeTutorialLessonIndex;
    internal TutorialLesson CurrentLesson { get { return Lessons[activeTutorialLessonIndex]; } }
    internal bool TutorialPanelInForeground;

	// Use this for initialization
	void Start () {
        Mars101_LZTarget.SetDad(this);
        Lessons = new TutorialLesson[]
        {
            new MarsSurvival101(this, StartCoroutine)
        };
        Backdrop.enabled = true;
        Backdrop.canvasRenderer.SetAlpha(0f);
        EventPanel.Group.gameObject.SetActive(false);
        Arrow.gameObject.SetActive(false);
        RTG.gameObject.SetActive(false);
        RTGPowerArrow.gameObject.SetActive(false);
        HabPowerArrow.gameObject.SetActive(false);
        TutorialPanel.Panel.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, GetLeftScreenHugXInset(), TutorialPanel.Panel.sizeDelta.x);
        CurrentLesson.Start();
	}

    public void FinishLesson(FinishTutorialChoice whatDo)
    {
        if (whatDo == FinishTutorialChoice.NextLesson)
        {
            if (activeTutorialLessonIndex == Lessons.Length - 1)
            {
                activeTutorialLessonIndex = 0;
            }
            else
            {
                activeTutorialLessonIndex++;
            }

            CurrentLesson.Start();
        }
    }
    
	void Update () {
	}

    public void ResetTriggerColliderFlags() { currentForwarder = null;  currentlyColliding = null;  currentResource = null; }
    public TriggerForwarder currentForwarder{ get; private set;}
    public Collider currentlyColliding{ get; private set;}
    public IMovableSnappable currentResource{ get; private set;}

    public bool PlayerInLZ { get { return currentForwarder == Mars101_LZTarget && PlayerInTrigger; } }
    public bool PlayerInTrigger { get { return currentlyColliding != null && currentlyColliding.CompareTag("Player"); } }

    public void OnChildTriggerEnter(TriggerForwarder child, Collider c, IMovableSnappable res)
    {
        currentForwarder = child;
        currentlyColliding = c;
        currentResource = res;
    }

    public float GetLeftScreenHugXInset()
    {
        return 0f;
    }

    internal float GetCenterScreenXInset()
    {
        return (TutorialCanvas.pixelRect.width / 2f) - (TutorialPanel.Panel.sizeDelta.x / 2f);
    }

    internal Airlock GetAirlock()
    {
        return FindObjectOfType<Airlock>();
    }
}

public class TutorialDescription
{
    public string Name;
    public string Description;
    private string[] steps;
    public string[] Steps { get { return steps; }
        set
        {
            steps = value;

            if (value != null)
                StepsComplete = new bool[value.Length];
        }
    }
    public bool[] StepsComplete { get; private set; }
}
public abstract class TutorialLesson
{
    protected Tutorial self;
    protected Func<IEnumerator, Coroutine> StartCoroutine;
    public Coroutine coroutine { get; private set; }
    protected TutorialDescription Description { get; private set; }

    public TutorialLesson(Tutorial self, Func<IEnumerator, Coroutine> startCo)
    {
        this.self = self;
        this.StartCoroutine = startCo;
        this.Description = GetDescription();
    }

    public int currentStep { get; private set; }
    protected void CompleteCurrentStep()
    {
        Description.StepsComplete[currentStep] = true;
        currentStep++;
        UpdateStepsText();
    }

    protected void UpdateDescription()
    {
        self.TutorialPanel.Header.text = Description.Name;
        self.TutorialPanel.Description.text = Description.Description;
        self.TutorialPanel.ContinuePanel.gameObject.SetActive(true);
        self.TutorialPanel.Description.gameObject.SetActive(true);
        self.TutorialPanel.Steps.gameObject.SetActive(false);
    }
    protected void UpdateStepsText()
    {
        StringBuilder sb = new StringBuilder();
        //sb.AppendLine(); sb.AppendLine();
        for (int i = Mathf.Max(0, currentStep - 3); i < Description.Steps.Length; i++)
        {
            if (i > currentStep)
                break;

            sb.Append(Description.StepsComplete[i] ? "✔ " : "* ");
            sb.Append(Description.Steps[i]);
            if (i < Description.Steps.Length - 1)
                sb.Append('\n');
        }
        self.TutorialPanel.Steps.text = sb.ToString();
        self.TutorialPanel.ContinuePanel.gameObject.SetActive(false);
        self.TutorialPanel.Description.gameObject.SetActive(false);
        self.TutorialPanel.Steps.gameObject.SetActive(true);
    }

    internal abstract TutorialDescription GetDescription();

    public void Start()
    {
        UpdateDescription();
        this.coroutine = StartCoroutine(Main());
    }
    protected IEnumerator ToggleTutorialPanel()
    {
        float newBackdropAlpha;
        bool newBackdropActive;
        bool moveCenterToLeft = self.TutorialPanelInForeground;
        bool moveLeftToCenter = !self.TutorialPanelInForeground;
        
        float endInset, startInset;
        if (moveCenterToLeft)
        {
            startInset = self.GetCenterScreenXInset();
            endInset = self.GetLeftScreenHugXInset();
            newBackdropAlpha = 0f;
            newBackdropActive = false;
            Time.timeScale = 1f;
        }
        else //move left to center
        {
            startInset = self.GetLeftScreenHugXInset();
            endInset = self.GetCenterScreenXInset();
            newBackdropAlpha = 221f / 256f;
            newBackdropActive = true;
            Time.timeScale = 0f;
        }
        float time = 0f; float duration = .6f;
        self.Backdrop.gameObject.SetActive(!newBackdropActive);
        self.Backdrop.CrossFadeAlpha(newBackdropAlpha, duration, true);

        self.TutorialPanel.Panel.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, startInset, self.TutorialPanel.Panel.sizeDelta.x);

        while (time < duration)
        {
            self.TutorialPanel.Panel.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, Mathf.Lerp(startInset, endInset, time / duration), self.TutorialPanel.Panel.sizeDelta.x);
            yield return null;
            time += Time.unscaledDeltaTime;
        }

        self.Backdrop.gameObject.SetActive(newBackdropActive);
        self.TutorialPanel.Panel.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, endInset, self.TutorialPanel.Panel.sizeDelta.x);
        self.TutorialPanelInForeground = !self.TutorialPanelInForeground;
    }

    protected bool ContinueButtonDown()
    {
        return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space);
    }

    protected IEnumerator ShowEvent(string header, string description, Sprite sprite)
    {
        self.EventPanel.Fill(header, description+"\n\n"+Description.Steps[currentStep], sprite);
        yield return ToggleEventPanel();
        yield return new WaitUntil(ContinueButtonDown);
        yield return ToggleEventPanel();
    }
    
    protected IEnumerator ToggleEventPanel()
    {
        float from = (self.EventPanel.Visible) ? 1f : 0f;
        float to = (self.EventPanel.Visible) ? 0f : 1f;

        if (!self.EventPanel.Visible)
        {
            //start showing panel
            self.EventPanel.Group.gameObject.SetActive(true);
            Time.timeScale = 0f;
        }
        else
        {
            //end showing panel
            Time.timeScale = 1f;
        }

        float time = 0f; float duration = .6f;

        self.EventPanel.Group.alpha = from;
        while (time < duration)
        {
            self.EventPanel.Group.alpha = Mathf.Lerp(from, to, time / duration);
            yield return null;
            time += Time.unscaledDeltaTime;
        }
        self.EventPanel.Group.alpha = to;

        self.EventPanel.Visible = !self.EventPanel.Visible;

        if (!self.EventPanel.Visible)
            self.EventPanel.Group.gameObject.SetActive(false);
    }

    protected void End()
    {
        this.coroutine = null;
        this.self.FinishLesson(FinishTutorialChoice.NextLesson);
    }

    protected IEnumerator HighlightPositionAndWaitUntilPlayerInIt(Vector3 position, float positionRadius)
    {
        self.Mars101_LZTarget.transform.position = position;
        self.Mars101_LZTarget.gameObject.SetActive(true);
        self.Mars101_LZTarget.GetComponent<CapsuleCollider>().radius = positionRadius;

        do
        {
            yield return new WaitForSeconds(1f);
        }
        while (!self.PlayerInLZ);

        self.ResetTriggerColliderFlags();
        self.Mars101_LZTarget.gameObject.SetActive(false);
    }

    protected void SetArrow(Transform bulkhead)
    {
        self.Arrow.position = bulkhead.position + bulkhead.TransformVector(new Vector3(-0.33f, 0, 3.2f));
    }

    protected abstract IEnumerator Main();
}

public class MarsSurvival101: TutorialLesson
{
    public MarsSurvival101(Tutorial self, Func<IEnumerator, Coroutine> startCo): base(self, startCo) { }


    protected override IEnumerator Main()
    {
        yield return this.ToggleTutorialPanel();
        yield return new WaitUntil(ContinueButtonDown);
        yield return this.ToggleTutorialPanel();
        UpdateStepsText();
        
        yield return HighlightPositionAndWaitUntilPlayerInIt(self.LZ.transform.position, 12f);
        CompleteCurrentStep();

        yield return ShowEvent("INCOMING SUPPLIES", @"The training program is sending a <b>CARGO LANDER</b> to drop off supplies for your homestead.", IconAtlas.Instance.MiscIcons[(int)MiscIcon.Rocket]);
        self.LZ.Deliver(new RedHomestead.Economy.Order()
        {
            LineItemUnits = new ResourceUnitCountDictionary()
            {
                { Matter.Piping, 2 },
                { Matter.IronSheeting, 2 },
            },
            Vendor = new RedHomestead.Economy.Vendor(),
            Via = RedHomestead.Economy.DeliveryType.Lander,
        });
        do
        {
            yield return new WaitForSeconds(1f);
        }
        while (self.LZ.Cargo == null || self.LZ.Cargo.Data.State == CargoLander.FlightState.Landing);
        CompleteCurrentStep();

        yield return ShowEvent("MOVING CRATES", @"Resource crates can be picked up and moved by holding down <b>LEFT MOUSE BUTTON</b>.", Craftable.Crate.AtlasSprite());
        do
        {
            yield return new WaitForSeconds(1f);
        }
        while (self.LZ.Cargo.Bays.Values.Any(x => x != null));
        CompleteCurrentStep();


        yield return ShowEvent("AIRLOCK CONSTRUCTION", @"With our new resources, we can build an <b>AIRLOCK</b>.", Craftable.Toolbox.AtlasSprite());        
        yield return HighlightPositionAndWaitUntilPlayerInIt(self.OutsideHabAnchor.position, 10f);
        CompleteCurrentStep();

        yield return ShowEvent("CONSTRUCTION BLUEPRINTS", @"Open up the <b>BLUEPRINTS</b> menu and select the <b>AIRLOCK</b> blueprint.", Craftable.Toolbox.AtlasSprite());
        do
        {
            yield return null;
        }
        while (PlayerInput.Instance.Loadout.Equipped != Equipment.Blueprints || !PlayerInput.Instance.ModulePlan.IsActive);
        CompleteCurrentStep();

        yield return ShowEvent("STARTING CONSTRUCTION", @"Place the <b>AIRLOCK</b> blueprint near the <b>HABITAT</b> bulkhead door.", Craftable.Toolbox.AtlasSprite());
        do
        {
            yield return null;
        }
        while (ConstructionZone.LastPlacedZone == null);
        CompleteCurrentStep();
        
        yield return ShowEvent("CONSTRUCTION MATERIALS", @"Building a module requires the correct materials inside the construction zone.", Craftable.Crate.AtlasSprite());
        do
        {
            yield return null;
        }
        while (ConstructionZone.LastPlacedZone == null || !ConstructionZone.LastPlacedZone.CanConstruct);
        CompleteCurrentStep();


        yield return ShowEvent("CONSTRUCTION TOOL", @"Constructing modules requires the correct tool: the <b>POWER DRILL</b>.", Craftable.Toolbox.AtlasSprite());
        do
        {
            yield return null;
        }
        while (PlayerInput.Instance.Loadout.Equipped != Equipment.PowerDrill);
        CompleteCurrentStep();

        yield return ShowEvent("CONSTRUCTION", @"Using the <b>POWER DRILL</b>, one can construct the module.", Craftable.Toolbox.AtlasSprite());
        do
        {
            yield return null;
        }
        while (ConstructionZone.LastPlacedZone != null);
        CompleteCurrentStep();

        yield return ShowEvent("BULKHEADS", @"In order to move between habitat modules, <b>BULKHEADS</b> must be made into a <b>CORRIDOR</b>.", Craftable.Toolbox.AtlasSprite());
        self.Arrow.gameObject.SetActive(true);
        SetArrow(self.GetAirlock().Bulkheads[0]);
        do
        {
            yield return null;
        }
        while (PlayerInput.Instance.selectedBulkhead == null);
        CompleteCurrentStep();

        yield return ShowEvent("CORRIDORS", @"<b>CORRIDORS</b> connect habitat modules and distribute power and oxygen.", Craftable.PowerCube.AtlasSprite());
        SetArrow(self.Hab.Bulkheads[1]);
        do
        {
            yield return new WaitForSeconds(1f);
        }
        while (self.Hab.AdjacentModules == null || !self.Hab.AdjacentModules.Any(x => x is Airlock));
        self.Arrow.gameObject.SetActive(false);
        CompleteCurrentStep();        

        yield return ShowEvent("PRESSURIZATION", @"The <b>AIRLOCK</b> is the only way to get into the <b>HABITAT</b> .", Matter.Oxygen.AtlasSprite());
        do
        {
            yield return new WaitForSeconds(1f);
        }
        while (SurvivalTimer.Instance.IsNotInHabitat);
        CompleteCurrentStep();

        
        yield return ShowEvent("HOME SWEET HOME", @"The <b>HABITAT</b> provides pressure, oxygen, and heat. It also stores power, food, and water.", IconAtlas.Instance.MiscIcons[(int)MiscIcon.Bed]);
        yield return HighlightPositionAndWaitUntilPlayerInIt(self.Hab.transform.position, 3f);
        CompleteCurrentStep();

        yield return ShowEvent("POWER GENERATION", @"The <b>HABITAT</b> requires large amounts of power to function.", Craftable.PowerCube.AtlasSprite());
        yield return HighlightPositionAndWaitUntilPlayerInIt(self.RTGAnchor.transform.position, 5f);
        CompleteCurrentStep();

        self.RTG.gameObject.SetActive(true);
        yield return ShowEvent("POWER OVERLAY", @"A <b>RTG</b> (nuclear power source) has been provided for electricity generation. The <b>POWER OVERLAY</b> allows power generation and consumption to be visualized.", Craftable.PowerCube.AtlasSprite());
        do
        {
            yield return null;
        }
        while (!PlayerInput.Instance.AlternativeCamera.enabled);
        CompleteCurrentStep();

        self.HabPowerArrow.gameObject.SetActive(true);
        yield return ShowEvent("POWERLINES", @"<b>POWERLINES</b> must be added between modules that generate, store, and consume electricity.", Craftable.PowerCube.AtlasSprite());
        do
        {
            yield return null;
        }
        while (PlayerInput.Instance.selectedPowerSocket == null);
        self.HabPowerArrow.gameObject.SetActive(false);
        CompleteCurrentStep();

        self.RTGPowerArrow.gameObject.SetActive(true);
        yield return ShowEvent("POWERLINES", @"<b>POWERLINES</b>, like corridors, do not require resources.", Craftable.PowerCube.AtlasSprite());
        do
        {
            yield return null;
        }
        while (!self.Hab.HasPower);
        self.RTGPowerArrow.gameObject.SetActive(false);
        CompleteCurrentStep();

        yield return ShowEvent("HABITAT COMPLETE", @"The <b>HABITAT</b> now has <b>POWER</b> and an <b>AIRLOCK</b>.", Craftable.PowerCube.AtlasSprite());
        yield return HighlightPositionAndWaitUntilPlayerInIt(self.Hab.transform.position, 2f);
        CompleteCurrentStep();


        yield return ShowEvent("TUTORIAL COMPLETE", @"Go to <b>SLEEP</b> to finish the tutorial.", Craftable.PowerCube.AtlasSprite());
        do
        {
            yield return null;
        }
        while (PlayerInput.Instance.CurrentMode != PlayerInput.InputMode.Sleep);
        CompleteCurrentStep();

        End();
    }

    internal override TutorialDescription GetDescription()
    {
        return new TutorialDescription()
        {
            Name = "HOMESTEAD SETUP",
            Description = @"Welcome to Antarctica! 
In order to prepare new homesteaders for the harsh Martian terrain, the <b>UN MARS AUTHORITY</b> has set up this training base.",
            Steps = new string[]
            {
                "Walk using <b>WASD</b> to the circle of lights: the <b>LANDING ZONE</b>.",
                "Step away from the <b>LANDING ZONE</b> and wait for the <b>CARGO LANDER</b>.",
                "Take the <b>RESOURCE CRATES</b> out of the <b>CARGO LANDER</b> using <b>LMB</b>.",
                "Walk using <b>WASD</b> to the <b>HABITAT</b>.",
                "Open <b>BLUEPRINTS</b> by holding <b>TAB</b> and select <b>LIFE SUPPORT > AIRLOCK</b>.",
                "Use <b>E</b> to place the <b>AIRLOCK</b> near the <b>HABITAT</b>.",
                "Use <b>LMB</b> to bring the <b>CRATES</b> into the <b>CONSTRUCTION ZONE</b>.",
                "Equip the <b>POWER DRILL</b> by holding <b>TAB</b> and selecting it.",
                "Step outside the <b>CONSTRUCTION ZONE</b> and hold <b>LMB</b> while facing the <b>ZONE</b>.",
                "Use <b>E</b> to select the <b>AIRLOCK BULKHEAD</b>.",
                "Use <b>E</b> to select the <b>HABITAT BULKHEAD</b>.",
                "Get into the <b>AIRLOCK</b>, shut the door, and <b>PRESSURIZE</b> it.",
                "Walk into the <b>HABITAT</b>.",
                "Walk outside the <b>HABITAT</b>, by <b>DEPRESSURIZING</b> the <b>AIRLOCK</b>.",
                "Use <b>V</b> to see the <b>POWER OVERLAY</b>.",
                "Use <b>E</b> to select the <b>HABITAT POWER SOCKET</b>.",
                "Use <b>E</b> to select the <b>RTG POWER SOCKET</b>.",
                "Walk into the <b>HABITAT</b>.",
                "Use <b>Z</b> when looking at the <b>BED</b> to <b>SLEEP</b>.",
            }
        };
    }
}
