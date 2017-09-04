using System;
using System.Collections;
using System.Collections.Generic;
using RedHomestead.Buildings;
using UnityEngine;
using RedHomestead.Simulation;
using RedHomestead.Electricity;
using RedHomestead.Persistence;

[Serializable]
public class OreExtractorFlexData
{
    public string DepositInstanceID;
}

public class OreExtractor : Converter, ICrateSnapper, ITriggerSubscriber, IPowerToggleable, IPowerConsumer, IFlexDataContainer<MultipleResourceModuleData, OreExtractorFlexData> {
    public bool IsOn { get; set; }

    public override float WattsConsumed
    {
        get
        {
            return 0f;
        }
    }

    public OreExtractorFlexData FlexData { get; set; }

    public void DetachCrate(IMovableSnappable detaching)
    {
        if ((object)detaching == oreOut)
        {
            oreOut = null;
        }
    }

    public override Module GetModuleType()
    {
        return Module.OreExtractor;
    }

    public override void OnAdjacentChanged()
    {

    }

    private Deposit attachedDeposit;
    private const float OrePerTick = .0001f;
    private Deposit deposit;
    private ResourceComponent oreOut;
    public void OnChildTriggerEnter(TriggerForwarder child, Collider c, IMovableSnappable res)
    {
        ResourceComponent oreBucket = res.transform.GetComponent<ResourceComponent>();
        if (oreBucket != null && 
            oreOut == null && 
            attachedDeposit != null &&
            oreBucket.Data.Container.MatterType.IsRawMaterial() && 
            matches(oreBucket.Data.Container.MatterType, attachedDeposit.Data.Extractable.MatterType))
        {
            oreOut = oreBucket;
            res.SnapCrate(this, child.transform.position);
        }
    }

    private bool matches(Matter crateType, Matter depositType)
    {
        return crateType == Matter.Unspecified || crateType == depositType;
    }

    public void OnEmergencyShutdown()
    {
    }

    public override void Report()
    {
    }

    public void TogglePower()
    {
    }

    public override void Convert()
    {
        if (attachedDeposit == null && 
            FlexData != null && 
            !String.IsNullOrEmpty(FlexData.DepositInstanceID) && 
            FlowManager.Instance.DepositMap.ContainsKey(FlexData.DepositInstanceID))
        {
            attachedDeposit = FlowManager.Instance.DepositMap[FlexData.DepositInstanceID];
        }

        //if ((HasPower && IsOn))
        if (attachedDeposit != null)
        {
            if (oreOut != null && attachedDeposit.Data.Extractable.CurrentAmount > 0)
            {
                oreOut.Data.Container.Push(attachedDeposit.Data.Extractable.Pull(OrePerTick));
            }
        }
    }

    public override void ClearSinks()
    {
    }

    public override ResourceContainerDictionary GetStartingDataContainers()
    {
        FlexData = new OreExtractorFlexData();
        return new ResourceContainerDictionary();
    }
}
