using RedHomestead.Simulation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using RedHomestead.Persistence;
using RedHomestead.Geography;

[Serializable]
public class DepositData : FacingData
{
    public string DepositInstanceID;
    public ResourceContainer Extractable;
    public float Purity = -1f;
    internal string ExtractableHint { get { return Extractable.UtilizationPercentageString() + " " + Extractable.MatterType;  } }
}

public class Deposit : MonoBehaviour, IDataContainer<DepositData>, ICrateSnapper
{
    [SerializeField]
    private DepositData data;
    public DepositData Data { get { return data; } set { data = value; } }
    public Transform OrePrefab;

    private const float VerticalDrillOffset = 1.11f;
    private IceDrill snappedDrill;
    private Coroutine unsnapTimer;
    private Material typeMat;
    private ResourceComponent snappedCrate;
    private Vector3 cratePosition;
    internal static OreLerper OreLerper;
    internal bool HasCrate { get { return snappedCrate != null; } }

    void Start()
    {
        typeMat = transform.GetChild(1).GetComponent<MeshRenderer>().material;
        cratePosition = this.transform.TransformPoint(new Vector3(-2f, 0.45f, -2f));

        if (String.IsNullOrEmpty(data.DepositInstanceID))
        {
            data.DepositInstanceID = System.Guid.NewGuid().ToString();
        }

        FlowManager.Instance.DepositMap.Add(data.DepositInstanceID, this);

        if (this.data.Purity < 0f)
        {
            this.data.Purity = this.data.Extractable.MatterType.RandomPurity();
            MarsRegionData regionData = Base.Current.Region.Data();
            if (this.data.Extractable.MatterType == Matter.Water)
            {
                this.data.Purity *= regionData.WaterMultiplier;
            }
            else
            {
                this.data.Purity *= regionData.MineralMultiplier;
            }
        }
    }

    public void DetachCrate(IMovableSnappable detaching)
    {
#pragma warning disable CS0252 // Possible unintended reference comparison; left hand side needs cast
        if (detaching == snappedDrill)
#pragma warning restore CS0252 // Possible unintended reference comparison; left hand side needs cast
            snappedDrill = null;
#pragma warning disable CS0252 // Possible unintended reference comparison; left hand side needs cast
        else if (detaching == snappedCrate)
#pragma warning restore CS0252 // Possible unintended reference comparison; left hand side needs cast
            snappedCrate = null;

        unsnapTimer = StartCoroutine(UnsnapTimer());
    }

    private IEnumerator UnsnapTimer()
    {
        yield return new WaitForSeconds(2f);
        unsnapTimer = null;
    }

    void OnTriggerEnter(Collider other)
    {
        var iceDrill = other.GetComponent<IceDrill>();

        if (iceDrill != null && Data.Extractable.MatterType == Matter.Water && unsnapTimer == null)
        {
            iceDrill.SnapCrate(this, this.transform.TransformPoint(Vector3.up * VerticalDrillOffset));
            snappedDrill = iceDrill;
        }
        else
        {
            var res = other.GetComponent<ResourceComponent>();
            if (res != null && unsnapTimer == null)
            {
                if (res.Data.Container.MatterType == Matter.Unspecified)
                {
                    res.Data.Container.MatterType = this.Data.Extractable.MatterType;
                    res.RefreshLabel();
                }

                if (res.Data.Container.MatterType == this.Data.Extractable.MatterType)
                {
                    res.SnapCrate(this, this.cratePosition);
                    snappedCrate = res;
                }
            }
        }
    }

    internal void ToggleMining(bool state)
    {
        if (Deposit.OreLerper == null)
        {
            OreLerper = 
                GameObject.Instantiate(OrePrefab, transform.position, transform.rotation).GetComponent<OreLerper>();
        }

        OreLerper.Toggle(state, this.transform.position, this.cratePosition, this.typeMat);
    }

    internal float Mine(float amount)
    {
        snappedCrate.Data.Container.Push(Data.Extractable.Pull(amount));
        return Data.Extractable.UtilizationPercentage;
    }
}
