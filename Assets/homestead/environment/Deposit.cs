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
    public Transform DrillingParticlesPrefab;

    private const float VerticalDrillOffset = 1.11f;
    private IceDrill snappedDrill;
    private Coroutine unsnapTimer;
    private Material typeMat;

    void Start()
    {
        typeMat = transform.GetChild(1).GetComponent<MeshRenderer>().material;

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
        snappedDrill = null;
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

        if (iceDrill != null && unsnapTimer == null)
        {
            iceDrill.SnapCrate(this, this.transform.position + Vector3.up * VerticalDrillOffset);
            snappedDrill = iceDrill;
        }
    }

    internal void ToggleMining(bool state)
    {
        if (PlayerInput.Instance.DrillingParticles == null)
        {
            ParticleSystem newSys = GameObject.Instantiate(DrillingParticlesPrefab, transform.position, Quaternion.Euler(90, 0, 0)).GetComponent<ParticleSystem>();
            PlayerInput.Instance.DrillingParticles = newSys;
        }

        if (state)
        {
            PlayerInput.Instance.DrillingParticles.transform.position = transform.position;
            PlayerInput.Instance.DrillingParticles.GetComponent<ParticleSystemRenderer>().material = this.typeMat;
            PlayerInput.Instance.DrillingParticles.Play();
        }
        else
        {
            PlayerInput.Instance.DrillingParticles.Stop();
        }
    }

    internal float Mine(float v)
    {
        return .75f;
    }
}
