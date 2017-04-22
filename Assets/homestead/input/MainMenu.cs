using UnityEngine;
using System.Collections;
using System;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using RedHomestead.Persistence;
using UnityStandardAssets.CrossPlatformInput;
using RedHomestead.Geography;

[Serializable]
public struct ScoutFields
{
    public RectTransform ScoutPanels;
    public Transform ScoutCameraAnchor, ScoutRegions, ScoutOrreyVertical, ScoutOrreyHorizontal, ScoutCursor;
    public Light Sun;
    public Behaviour Halo;
    public Spin PlanetSpin;
    public Text RegionName, RegionSolar, RegionMinerals, RegionWater, RegionRemote, RegionMultiplier, LatLongText;

    public void FillScoutInfo(MarsRegion region, LatLong latlong)
    {
        RegionName.text = region.Name();
        LatLongText.text = latlong.ToString();
    }
}

public class MainMenu : MonoBehaviour {
    public Image BigLogo;
    public RectTransform MainMenuButtons, NewGamePanels, QuickstartBackdrop, QuickstartTrainingEquipmentRow;
    public Transform OrbitCameraAnchor;
    public Button LoadButton;
    public ScoutFields ScoutFields;

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
        ScoutFields.ScoutRegions.gameObject.SetActive(false);
        ScoutFields.ScoutPanels.gameObject.SetActive(false);
        RenderSettings.ambientLight = new Color(0, 0, 0, 0);


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

    private bool onScout = false;
    // Update is called once per frame
    void Update() {
	    if (Input.GetKeyDown(KeyCode.Space))
        {
            onScout = !onScout;
            print("now onscout: " + onScout);
            ScoutToggle(onScout);
        }

        if (onScout)
        {
            HandleScoutInput();
        }
	}

    private void HandleScoutInput()
    {
        float xDelta = CrossPlatformInputManager.GetAxis("Horizontal");
        float yDelta = CrossPlatformInputManager.GetAxis("Vertical");

        if (xDelta != 0f)
            ScoutFields.ScoutOrreyHorizontal.transform.Rotate(Vector3.up, xDelta, Space.Self);

        if (yDelta != 0f)
        {
            ScoutFields.ScoutOrreyVertical.transform.Rotate(Vector3.forward, -yDelta, Space.Self);

            //print(Orrey.transform.localRotation.eulerAngles.z);
            if (ScoutFields.ScoutOrreyVertical.transform.localRotation.eulerAngles.z > 25 || ScoutFields.ScoutOrreyVertical.transform.localRotation.eulerAngles.x < -25)
                ScoutFields.ScoutOrreyVertical.transform.Rotate(Vector3.forward, yDelta, Space.Self);
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            ScoutFields.FillScoutInfo(GeoExtensions.ParseRegion(hit.collider.name), LatLong.FromPointOnUnitSphere(ScoutFields.ScoutOrreyHorizontal.transform.InverseTransformPoint(hit.point)));
            ScoutFields.ScoutCursor.position = hit.point;
            ScoutFields.ScoutCursor.rotation = Quaternion.LookRotation(hit.normal);
        }
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

    public void ScoutToggle(bool toScout)
    {
        if (!transitioning)
        {
            transitioning = true;

            if (toScout)
                cameraLerp.Seed(Camera.main.transform, ScoutFields.ScoutCameraAnchor);

            ToggleLogoAndCamera(!toScout, () =>
            {
                RenderSettings.ambientLight = new Color(1, 1, 1, .5f);
                ScoutFields.Halo.enabled = false;
                ScoutFields.Sun.enabled = false;
                ScoutFields.ScoutRegions.gameObject.SetActive(true);
                ScoutFields.ScoutPanels.gameObject.SetActive(true);
                ScoutFields.PlanetSpin.enabled = false;
                ScoutFields.PlanetSpin.transform.localRotation = Quaternion.Euler(-90, -90, 0);
            });
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
        transitionTime = 0f;
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
