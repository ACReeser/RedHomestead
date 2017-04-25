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
    public RectTransform ScoutPanels, ClaimHomesteadButton, SelectLocationButton;
    public Transform ScoutCameraAnchor, ScoutRegions, ScoutOrreyVertical, ScoutOrreyHorizontal, ScoutCursor;
    public Light Sun;
    public Behaviour Halo;
    public Spin PlanetSpin;
    public Text RegionName, RegionSolar, RegionMinerals, RegionWater, RegionRemote, RegionMultiplier, LatLongText;

    public void FillScoutInfo(MarsRegion region, LatLong latlong)
    {
        RegionName.text = region.Name();
        LatLongText.text = latlong.ToString();
        RegionWater.text = region.Data().WaterMultiplierString;
        RegionMinerals.text = region.Data().MineralMultiplierString;
        RegionSolar.text = region.Data().SolarMultiplierString;
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
    private int smallLogoW, smallLogoH, scoutLogoW, scoutLogoH;
    private LerpContext cameraLerp;
    private string lastPlayerName;
    private string[] savedPlayerNames;

	// Use this for initialization
	void Start ()
    {
        smallLogoW = UnityEngine.Screen.width / 2;
        smallLogoH = UnityEngine.Screen.height / 2;
        scoutLogoW = (int)(UnityEngine.Screen.width * .6666f);
        scoutLogoH = (int)(UnityEngine.Screen.height * .6666f);
        cameraLerp.Seed(Camera.main.transform, null);
        cameraLerp.Duration = transitionDuration;
        MainMenuButtons.gameObject.SetActive(true);
        NewGamePanels.gameObject.SetActive(false);
        ToggleScoutMode(false);
        SetSelectedLocation(null);

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
            ScoutToggle(onScout);
        }

        if (onScout)
        {
            HandleScoutInput();
        }
	}

    private BaseLocation hoverLocation, selectedLocation;

    private void HandleScoutInput()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (selectedLocation == null && Physics.Raycast(ray, out hit))
        {
            hoverLocation = new BaseLocation()
            {
                Region = GeoExtensions.ParseRegion(hit.collider.name),
                LatLong = LatLong.FromPointOnUnitSphere(ScoutFields.ScoutOrreyHorizontal.transform.InverseTransformPoint(hit.point))
            };
            ScoutFields.FillScoutInfo(hoverLocation.Region, hoverLocation.LatLong);
            ScoutFields.ScoutCursor.position = hit.point;
            ScoutFields.ScoutCursor.rotation = Quaternion.LookRotation(hit.normal);
        }

        if (Input.GetMouseButtonDown(0))
        {
            SetSelectedLocation(hoverLocation);
        }
        else if (Input.GetMouseButtonDown(1))
        {
            SetSelectedLocation(null);
        }
        else
        {
            float xDelta = CrossPlatformInputManager.GetAxis("Horizontal");
            float yDelta = CrossPlatformInputManager.GetAxis("Vertical");

            if (xDelta != 0f)
                ScoutFields.ScoutOrreyHorizontal.transform.Rotate(Vector3.up, xDelta, Space.Self);

            if (yDelta != 0f)
                ScoutFields.ScoutOrreyVertical.transform.Rotate(Vector3.forward, -yDelta, Space.Self);
        }
    }

    private void SetSelectedLocation(BaseLocation loc)
    {
        selectedLocation = loc;
        ScoutFields.ClaimHomesteadButton.gameObject.SetActive(loc != null);
        ScoutFields.SelectLocationButton.gameObject.SetActive(loc == null);
        ScoutFields.ScoutCursor.transform.SetParent(loc == null ? null : ScoutFields.ScoutOrreyHorizontal);
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

            ToggleLogoAndCamera(!state, AfterNewGame, smallLogoH, smallLogoW);
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
                this.ToggleScoutMode(toScout);
            }, scoutLogoH, scoutLogoW);
        }
    }

    private void ToggleScoutMode(bool isScout)
    {
        MainMenuButtons.gameObject.SetActive(!isScout);

        RenderSettings.ambientLight = isScout ? new Color(1, 1, 1, .5f) : new Color(0, 0, 0, 0);

        ScoutFields.Halo.enabled = !isScout;
        ScoutFields.Sun.enabled = !isScout;
        ScoutFields.ScoutRegions.gameObject.SetActive(isScout);
        ScoutFields.ScoutPanels.gameObject.SetActive(isScout);
        ScoutFields.PlanetSpin.enabled = !isScout;

        if (isScout)
            ScoutFields.PlanetSpin.transform.localRotation = Quaternion.Euler(-90, -90, 0);
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
            ToggleLogoAndCamera(!state, AfterSettings, smallLogoH, smallLogoW);
        }
    }

    private void AfterSettings()
    {
        MainMenuButtons.gameObject.SetActive(onMainMenu);
    }

    private void ToggleLogoAndCamera(bool toMainMenuView, Action onFinishTransition, int logoH, int logoW)
    {
        transitionTime = 0f;
        if (toMainMenuView)
        {
            cameraLerp.Reverse();
            //toggle to fullscreen and non-orbit camera
            StartCoroutine(LogoCameraChange(logoH, logoW, 0, 0, onFinishTransition));
        }
        else
        {
            //toggle to off
            StartCoroutine(LogoCameraChange(0, 0, logoH, logoW, onFinishTransition));
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
