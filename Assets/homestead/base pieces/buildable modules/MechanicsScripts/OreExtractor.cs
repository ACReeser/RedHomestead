using System;
using System.Collections;
using System.Collections.Generic;
using RedHomestead.Buildings;
using UnityEngine;
using RedHomestead.Simulation;
using RedHomestead.Electricity;

public class OreExtractor : Converter, ICrateSnapper, ITriggerSubscriber, IPowerToggleable, IPowerConsumer {
    public bool IsOn { get; set; }

    public override float WattsConsumed
    {
        get
        {
            return 0f;
        }
    }

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

    private const float OrePerTick = .0001f;
    private Deposit deposit;
    private ResourceComponent oreOut;
    public void OnChildTriggerEnter(TriggerForwarder child, Collider c, IMovableSnappable res)
    {
        ResourceComponent ore = res.transform.GetComponent<ResourceComponent>();
        if (ore != null && oreOut == null && ore.Data.Container.MatterType.IsRawMaterial() && matches(ore.Data.Container.MatterType, Matter.IronOre))
        {
            oreOut = ore;
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
        if ((HasPower && IsOn) || true)
        {
            if (oreOut != null)
            {
                oreOut.Data.Container.Push(OrePerTick);
            }
        }
    }

    public override void ClearHooks()
    {
    }

    public override ResourceContainerDictionary GetStartingDataContainers()
    {
        return new ResourceContainerDictionary();
    }
}
