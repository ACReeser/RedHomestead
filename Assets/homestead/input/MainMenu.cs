using UnityEngine;
using System.Collections;
using System;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using RedHomestead.Persistence;

public class MainMenu : MonoBehaviour {
    public Image BigLogo;
    public RectTransform MainMenuButtons, NewGamePanels, QuickstartBackdrop, QuickstartTrainingEquipmentRow;
    public Transform OrbitCameraAnchor;
    public Button LoadButton;

    private bool transitioning, onMainMenu = true;
    private const float transitionDuration = 1f;
    private const string DefaultRadioButtonName = "default";
    private const string RadioTagPostfix = "radio";
    private float transitionTime = 0f;
    private int smallLogoW, smallLogoH;
    private LerpContext cameraLerp;
    private string lastPlayerName;
    private string[] savedPlayerNames;

	// Use this for initialization
	void Start ()
    {
        smallLogoW = UnityEngine.Screen.width / 2;
        smallLogoH = UnityEngine.Screen.height / 2;
        cameraLerp.Seed(Camera.main.transform, null);
        cameraLerp.Duration = transitionDuration;
        NewGamePanels.gameObject.SetActive(false);

        //if we start here from the escape menu, time is paused
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        loadSavedPlayers();
    }

    private void loadSavedPlayers()
    {
        try
        {
            lastPlayerName = PersistentDataManager.GetLastPlayedPlayerName();
            savedPlayerNames = PersistentDataManager.GetPlayerNames();

            if (!String.IsNullOrEmpty(lastPlayerName))
            {
                LoadButton.interactable = true;
                LoadButton.transform.GetChild(0).GetComponent<Text>().text = "LOAD GAME as " + lastPlayerName;
            }

        }
        catch (Exception e){
            UnityEngine.Debug.LogError(e.ToString());
        }
    }

    private GameObject[] defaultQuickstartClones = new GameObject[2];

    private void InitializeRadioButtons()
    {
        foreach (NewGameRadioButtons r in Enum.GetValues(typeof(NewGameRadioButtons)))
        {
            foreach (GameObject g in GameObject.FindGameObjectsWithTag(r.ToString() + RadioTagPostfix))
            {
                if (g.name == DefaultRadioButtonName)
                {
                    this.activeRadioTransform[r] = g.transform;
                    if (defaultQuickstartClones[(int)r] == null)
                    {
                        defaultQuickstartClones[(int)r] = GameObject.Instantiate(g);
                        defaultQuickstartClones[(int)r].transform.SetParent(QuickstartTrainingEquipmentRow);
                        //remove the checkbox
                        defaultQuickstartClones[(int)r].transform.GetChild(0).gameObject.SetActive(false);
                        defaultQuickstartClones[(int)r].tag = "Untagged";
                    }
                }
                else
                {
                    g.transform.GetChild(0).gameObject.SetActive(false);
                }
            }
        }
    }

    // Update is called once per frame
    void Update () {
	
	}

    public void NewGameToggle(bool state)
    {
        if (!transitioning)
        {
            transitioning = !transitioning;

            if (onMainMenu)
            {
                cameraLerp.Seed(Camera.main.transform, OrbitCameraAnchor);
            }
            else
            {
                NewGamePanels.gameObject.SetActive(false);
            }

            ToggleLogoAndCamera(!state, AfterNewGame);
        }
    }

    private void AfterNewGame()
    {
        MainMenuButtons.gameObject.SetActive(onMainMenu);

        if (!onMainMenu)
        {
            NewGamePanels.gameObject.SetActive(true);

            //unselect all radio buttons
            InitializeRadioButtons();

            QuickstartBackdrop.gameObject.SetActive(true);
        }
    }

    public void SettingsClick(bool state)
    {
        if (!transitioning)
        {
            transitioning = !transitioning;
            if (onMainMenu)
            {
                cameraLerp.Seed(Camera.main.transform, OrbitCameraAnchor);
            }
            ToggleLogoAndCamera(!state, AfterSettings);
        }
    }

    private void AfterSettings()
    {
        MainMenuButtons.gameObject.SetActive(onMainMenu);
    }

    private void ToggleLogoAndCamera(bool toMainMenuView, Action onFinishTransition)
    {
        if (toMainMenuView)
        {
            cameraLerp.Reverse();
            //toggle to fullscreen and non-orbit camera
            StartCoroutine(LogoCameraChange(smallLogoH, smallLogoW, 0, 0, onFinishTransition));
        }
        else
        {
            //toggle to off
            StartCoroutine(LogoCameraChange(0, 0, smallLogoH, smallLogoW, onFinishTransition));
        }
    }

    private IEnumerator LogoCameraChange(float startBottom, float startRight, int endBottom, int endRight, Action onFinishTransition)
    {
        while(transitioning)
        {
            transitionTime += Time.deltaTime;

            cameraLerp.Tick(Camera.main.transform);

            if (transitionTime > transitionDuration)
            {
                SetBottomRight(BigLogo, endBottom, endRight);
                transitioning = false;
                transitionTime = 0f;
                onMainMenu = !onMainMenu;
                onFinishTransition();
            }
            else
            {
                float lerpAmt = transitionTime / transitionDuration;
                SetBottomRight(BigLogo, (int)Mathf.Lerp(startBottom, endBottom, lerpAmt), (int)Mathf.Lerp(startRight, endRight, lerpAmt) );

                yield return null;
            }
        }
    }

    private void SetBottomRight(Image bigLogo, int bottom, int right)
    {
        bigLogo.rectTransform.offsetMax = new Vector2(-right, 0f);
        bigLogo.rectTransform.offsetMin = new Vector2(0f, bottom);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void LaunchGame()
    {
        PersistentDataManager.StartNewGame();
        UnityEngine.SceneManagement.SceneManager.LoadScene("main", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    public void StartQuickstart()
    {
#warning todo: make sure quickstart at quickstart equipment/training
        LaunchGame();
    }

    public void StartCustomize()
    {
        QuickstartBackdrop.gameObject.SetActive(false);
    }

    private enum NewGameRadioButtons { financing, training }

    private Dictionary<NewGameRadioButtons, Transform> activeRadioTransform = new Dictionary<NewGameRadioButtons, Transform>();

    private int currentlySelectedTrainingIndex;
    public void SelectTraining(int trainingIndex)
    {
        OnRadioSelect(NewGameRadioButtons.training);
    }

    public void SelectFinancing(int financeIndex)
    {
        OnRadioSelect(NewGameRadioButtons.financing);
    }

    private void OnRadioSelect(NewGameRadioButtons radioGroup)
    {
        var thisT = EventSystem.current.currentSelectedGameObject.transform;

        if (thisT.CompareTag("Untagged"))
            return;

        if (this.activeRadioTransform.ContainsKey(radioGroup))
        {
            this.activeRadioTransform[radioGroup].GetChild(0).gameObject.SetActive(false);
        }
        this.activeRadioTransform[radioGroup] = thisT;

        thisT.GetChild(0).gameObject.SetActive(true);
    }

    public void LoadLastGame()
    {
        if (!String.IsNullOrEmpty(lastPlayerName))
        {
            GameObject g = new GameObject("loadBridge");
            LoadGameBridge loadScript = g.AddComponent<LoadGameBridge>();
            loadScript.playerNameToLoad = lastPlayerName;
            UnityEngine.SceneManagement.SceneManager.LoadScene("main", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }

    private struct LerpContext
    {
        public Vector3 FromPosition, ToPosition;
        public Quaternion FromRotation, ToRotation;
        public float Duration;
        private float Time;
        public bool Done;

        public void Seed(Transform from, Transform to)
        {
            FromPosition = from.position;
            FromRotation = Quaternion.LookRotation(from.forward, from.up);
            if (to != null)
            {
                ToPosition = to.position;
                ToRotation = Quaternion.LookRotation(to.forward, to.up);
            }
        }

        public void Reverse()
        {
            var newToPos = FromPosition;
            var newToRot = FromRotation;
            FromPosition = ToPosition;
            FromRotation = ToRotation;
            ToPosition = newToPos;
            ToRotation = newToRot;
            Done = false;
            Time = 0f;
        }

        public void Tick(Transform transform)
        {
            this.Time += UnityEngine.Time.deltaTime;
            if (this.Time > this.Duration)
            {
                transform.position = ToPosition; 
                transform.rotation = ToRotation;
                Done = true;
            }
            else
            {
                transform.position = Vector3.Lerp(FromPosition, ToPosition, Time / Duration);
                transform.rotation = Quaternion.Lerp(FromRotation, ToRotation, Time / Duration);
            }
        }
    }
}
