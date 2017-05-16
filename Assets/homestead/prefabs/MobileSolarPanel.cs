using RedHomestead.Electricity;
using RedHomestead.Persistence;
using RedHomestead.Simulation;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MobileSolarPanelData : FacingData
{
    public string PowerableInstanceID;
    public float FaultedPercentage;
}

public interface IDeployable: IMovableSnappable
{
    void ToggleDeploy(bool? deployed = null);
    bool Deployed { get; }
}

public class MobileSolarPanel : MovableSnappable, IVariablePowerSupply, IDataContainer<MobileSolarPanelData>, IDeployable
{
    public Transform[] Panels;

    public FailureAnchors failureEffectAnchors;
    public FailureAnchors FailureEffectAnchors { get { return failureEffectAnchors; } }
    public float FaultedPercentage { get { return data.FaultedPercentage; } set { data.FaultedPercentage = value; } }
    public const int Meter2PerModule = 5;

    public float MaximumWattsGenerated { get { return SolarPanel.EquatorAnnualAverageInsolationPerMeter2 * Meter2PerModule * SolarPanel.MinimumEfficiency; } }
    public float WattsGenerated { get { return Convert.ToInt32(Deployed) * MaximumWattsGenerated * Game.Current.Environment.SolarIntensity(); } }

    private MobileSolarPanelData data;
    public MobileSolarPanelData Data { get { return data; } set { data = value; } }

    public string PowerGridInstanceID { get; set; }
    public string PowerableInstanceID { get { return data.PowerableInstanceID; } }

    public PowerVisualization _powerViz;
    private bool PanelsDeployed = true;
    public bool Deployed { get { return PanelsDeployed; } }
    private Coroutine panelMove = null;

    public PowerVisualization PowerViz { get { return _powerViz; } }

    public override string GetText()
    {
        return "Solar Panel";
    }

    // Use this for initialization
    void Start () {
        if (this.data == null)
            this.data = new MobileSolarPanelData()
            {
                PowerableInstanceID = Guid.NewGuid().ToString()
            };

        this.ToggleDeploy(false);

        this.InitializePowerVisualization();
    }
	
	// Update is called once per frame
	void Update () {

    }

    protected override void OnSnap()
    {
        //TogglePanels(false);
    }

    public void ToggleDeploy(bool? deployed = null)
    {
        if (deployed == null)
            deployed = !this.PanelsDeployed;

        this.PanelsDeployed = deployed.Value;

        if (panelMove == null)
            panelMove = StartCoroutine(MovePanels());

        this.RefreshVisualization();
    }

    private float GetRotationXFromDeployedState()
    {
        return PanelsDeployed ? -90 : -180f;
    }
    private float GetRotationAlongYFromDeployedState()
    {
        return PanelsDeployed ? -90 : 0f;
    }

    private float PanelTime;
    private float PanelDuration = .75f;
    private IEnumerator MovePanels()
    {
        PanelTime = 0f;
        //we aren't using hatch.rotate here because reading from localRotation.eulerAngles is super unreliable
        while (PanelTime < PanelDuration)
        {
            float lerpCoeff = (PanelsDeployed) ? 1 : -1f;
            float lerpOffset = (PanelsDeployed) ? 1 : 0f;

            PanelTime += Time.deltaTime;
            int i = 0;
            foreach(Transform p in Panels)
            {
                float lerp = lerpOffset - (PanelTime * lerpCoeff / PanelDuration);
                switch (i)
                {
                    case 0:
                        p.localRotation = Quaternion.Euler(Mathf.Lerp(-90f, 0f, lerp), 0f, 0f);
                        break;
                    case 1:
                        p.localRotation = Quaternion.Euler(Mathf.Lerp(-90f, 0f, lerp), -90f, 90f);
                        break;
                    case 2:
                        p.localRotation = Quaternion.Euler(Mathf.Lerp(-90f, -180f, lerp), 0f, 0f);
                        break;
                    case 3:
                        p.localRotation = Quaternion.Euler(Mathf.Lerp(-90f, 0f, lerp), 90f, -90f);
                        break;
                }
                i++;
            }
            yield return null;
        }
        panelMove = null;
    }

    protected override void OnDetach()
    {
        if (PanelsDeployed)
            ToggleDeploy(false);
    }

    public override void OnPickedUp()
    {
        base.OnPickedUp();

        if (FlowManager.Instance.PowerGrids.Edges.ContainsKey(this) && FlowManager.Instance.PowerGrids.Edges[this].Count > 0)
        {
            FlowManager.Instance.PowerGrids.Detach(this);
        }
    }
}
