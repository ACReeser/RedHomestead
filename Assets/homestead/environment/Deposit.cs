using RedHomestead.Simulation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using RedHomestead.Persistence;

[Serializable]
public class DepositData : FacingData
{
    public string DepositInstanceID;
    public ResourceContainer Extractable;
    internal string ExtractableHint { get { return Extractable.UtilizationPercentageString() + " " + Extractable.MatterType;  } }
}

public class Deposit : MonoBehaviour, IDataContainer<DepositData>, ICrateSnapper
{
    [SerializeField]
    private DepositData data;
    public DepositData Data { get { return data; } set { data = value; } }
    private const float VerticalDrillOffset = 1.11f;
    private IceDrill snappedDrill;
    private Coroutine unsnapTimer;

    void Start()
    {
        if (String.IsNullOrEmpty(data.DepositInstanceID))
        {
            data.DepositInstanceID = System.Guid.NewGuid().ToString();
        }

        FlowManager.Instance.DepositMap.Add(data.DepositInstanceID, this);
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
}
