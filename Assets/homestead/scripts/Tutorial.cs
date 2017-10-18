using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public enum FinishTutorialChoice { NextLesson, ExitToMenu, ListOfLessons }

public class Tutorial : MonoBehaviour, ITriggerSubscriber
{
    public RectTransform TutorialPanel;
    public Text Header, Description;
    public Image Backdrop;

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
	
	// Update is called once per frame
	void Update () {
		
	}

    public void ResetCurrentFlags() { currentForwarder = null;  currentlyColliding = null;  currentResource = null; }
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

    protected void UpdateDescription()
    {
        self.Header.text = Description.Name;
        StringBuilder sb = new StringBuilder(Description.Description);
        sb.AppendLine(); sb.AppendLine();
        for (int i = 0; i < Description.Steps.Length; i++)
        {
            sb.Append(Description.StepsComplete[i] ? "✔ " : "* ");
            sb.Append(Description.Steps[i]);
        }
        self.Description.text = sb.ToString();
    }
    internal abstract TutorialDescription GetDescription();

    public void Start()
    {
        UpdateDescription();
        this.coroutine = StartCoroutine(Main());
    }
    protected IEnumerator ToggleTutorialPanel()
    {
        Vector2 start, end;
        float newBackdropAlpha;
        bool moveCenterToLeft = self.TutorialPanelInForeground;
        bool moveLeftToCenter = !self.TutorialPanelInForeground;

        float y = self.TutorialPanel.anchoredPosition.y;
        if (moveCenterToLeft)
        {
            self.TutorialPanel.anchorMin = new Vector2(0, .5f);
            self.TutorialPanel.anchorMax = new Vector2(0, .5f);

            end = new Vector2(self.TutorialPanel.sizeDelta.x / 2f, y);
            newBackdropAlpha = 0f;
        }
        else
        {
            self.TutorialPanel.anchorMin = new Vector2(.5f, .5f);
            self.TutorialPanel.anchorMax = new Vector2(.5f, .5f);

            end = new Vector2(0f, y);
            newBackdropAlpha = 221f / 256f;
        }
        start = self.TutorialPanel.anchoredPosition;

        float time = 0f; float duration = .6f;
        self.Backdrop.CrossFadeAlpha(newBackdropAlpha, duration, true);

        while (time < duration)
        {
            self.TutorialPanel.anchoredPosition = Vector2.Lerp(start, end, time / duration);
            yield return null;
            time += Time.deltaTime;
        }

        self.TutorialPanel.anchoredPosition = end;
        self.TutorialPanelInForeground = !self.TutorialPanelInForeground;
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

    protected override IEnumerator Main()
    {
        yield return this.ToggleTutorialPanel();

        yield return new WaitForSeconds(5f);

        yield return this.ToggleTutorialPanel();

        self.Mars101_LZTarget.transform.position = self.LZ.transform.position;
        self.Mars101_LZTarget.gameObject.SetActive(true);

        do
        {
            yield return new WaitForSeconds(1f);
        }
        while (!self.PlayerInLZ);

        self.ResetCurrentFlags();
        self.Mars101_LZTarget.gameObject.SetActive(false);
        Description.StepsComplete[0] = true;

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
                "Please walk using <b>WASD</b> to the circle of lights: the <b>LANDING ZONE</b>."
            }
        };
    }
}
