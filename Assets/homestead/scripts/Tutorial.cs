using RedHomestead.Simulation;
using System;
using System.Collections;
using System.Collections.Generic;
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

    public LandingZone LZ;

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
        for (int i = 0; i < Description.Steps.Length; i++)
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
        }
        else //move left to center
        {
            startInset = self.GetLeftScreenHugXInset();
            endInset = self.GetCenterScreenXInset();
            newBackdropAlpha = 221f / 256f;
            newBackdropActive = true;
        }
        float time = 0f; float duration = .6f;
        self.Backdrop.gameObject.SetActive(!newBackdropActive);
        self.Backdrop.CrossFadeAlpha(newBackdropAlpha, duration, true);

        self.TutorialPanel.Panel.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, startInset, self.TutorialPanel.Panel.sizeDelta.x);

        while (time < duration)
        {
            self.TutorialPanel.Panel.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, Mathf.Lerp(startInset, endInset, time / duration), self.TutorialPanel.Panel.sizeDelta.x);
            yield return null;
            time += Time.deltaTime;
        }

        self.Backdrop.gameObject.SetActive(newBackdropActive);
        self.TutorialPanel.Panel.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, endInset, self.TutorialPanel.Panel.sizeDelta.x);
        self.TutorialPanelInForeground = !self.TutorialPanelInForeground;
    }

    
    protected IEnumerator ToggleEventPanel()
    {
        float from = (self.EventPanel.Visible) ? 1f : 0f;
        float to = (self.EventPanel.Visible) ? 0f : 1f;

        if (!self.EventPanel.Visible)
            self.EventPanel.Group.gameObject.SetActive(true);

        float time = 0f; float duration = .6f;

        self.EventPanel.Group.alpha = from;
        while (time < duration)
        {
            self.EventPanel.Group.alpha = Mathf.Lerp(from, to, time / duration);
            yield return null;
            time += Time.deltaTime;
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

    protected abstract IEnumerator Main();
}

public class MarsSurvival101: TutorialLesson
{
    public MarsSurvival101(Tutorial self, Func<IEnumerator, Coroutine> startCo): base(self, startCo) { }

    protected bool ContinueButtonDown()
    {
        return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space);
    }

    protected override IEnumerator Main()
    {
        yield return this.ToggleTutorialPanel();
        yield return new WaitUntil(ContinueButtonDown);
        yield return this.ToggleTutorialPanel();
        UpdateStepsText();

        self.Mars101_LZTarget.transform.position = self.LZ.transform.position;
        self.Mars101_LZTarget.gameObject.SetActive(true);

        do
        {
            yield return new WaitForSeconds(1f);
        }
        while (!self.PlayerInLZ);

        self.ResetTriggerColliderFlags();
        self.Mars101_LZTarget.gameObject.SetActive(false);
        CompleteCurrentStep();

        yield return ToggleEventPanel();
        yield return new WaitUntil(ContinueButtonDown);
        yield return ToggleEventPanel();

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

        End();
    }

    internal override TutorialDescription GetDescription()
    {
        return new TutorialDescription()
        {
            Name = "MARS SURVIVAL 101",
            Description = @"Welcome to Antarctica! 
In order to prepare new homesteaders for the harsh Martian terrain, the <b>UN MARS AUTHORITY</b> has set up this training base.",
            Steps = new string[]
            {
                "Walk using <b>WASD</b> to the circle of lights: the <b>LANDING ZONE</b>.",
                "Step away from the <b>LANDING ZONE</b> and wait for the <b>CARGO LANDER</b>."
            }
        };
    }
}
