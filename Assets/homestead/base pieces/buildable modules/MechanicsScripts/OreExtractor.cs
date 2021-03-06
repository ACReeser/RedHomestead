﻿using System;
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

public class OreExtractor : Converter, ICrateSnapper, ITriggerSubscriber, IPowerConsumerToggleable, IFlexDataContainer<MultipleResourceModuleData, OreExtractorFlexData> {

    public override float WattsConsumed
    {
        get
        {
            return ElectricityConstants.WattsPerBlock * 4f;
        }
    }

    public OreExtractorFlexData FlexData { get; set; }

    public Transform CrateSnapAnchor;
    public MeshFilter powerCabinet;
    public MeshFilter PowerCabinet { get { return powerCabinet; } }
    
    protected override void OnStart()
    {
        base.OnStart();

        if (FlexData != null && attachedDeposit == null)
            InitializeDeposit(FlexData.DepositInstanceID);

        this.RefreshPowerSwitch();
        this.RefreshSpinAndParticles();
    }

    public new bool IsOn { get { return Data.IsOn; } set { Data.IsOn = value; RefreshSpinAndParticles(); } }

    public Spin wheel;
    public ParticleSystem ore1, ore2;
    private void RefreshSpinAndParticles()
    {
        if (Data.IsOn)
        {
            wheel.transform.localRotation = Quaternion.Euler(0, -90f, 90f);
            wheel.enabled = true;
            ore1.Play();
            ore2.Play();
        }
        else
        {
            wheel.enabled = false;
            ore1.Stop();
            ore2.Stop();
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

    private Deposit attachedDeposit;
    private const float OrePerTick = .01f;
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

            if (oreBucket.Data.Container.MatterType == Matter.Unspecified)
                oreBucket.Data.Container.MatterType = attachedDeposit.Data.Extractable.MatterType;

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

    public override void Convert()
    {
        if (HasPower && IsOn && attachedDeposit != null)
        {
            if (oreOut != null && attachedDeposit.Data.Extractable.CurrentAmount > 0)
            {
                oreOut.Data.Container.Push(attachedDeposit.Data.Extractable.Pull(OrePerTick), attachedDeposit.Data.Purity);
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

    public override void InitializeStartingData()
    {
        base.InitializeStartingData();

        if (attachedDeposit == null)
            UnityEngine.Debug.LogWarning("Ore extractor started with no attached deposit!");
        else
        {
            FlexData.DepositInstanceID = attachedDeposit.Data.DepositInstanceID;
            BounceLander.CreateCratelike(attachedDeposit.Data.Extractable.MatterType, 0f, this.CrateSnapAnchor.position);
        }
    }

    internal void InitializeDeposit(string depositInstanceID)
    {
        attachedDeposit = FlowManager.Instance.DepositMap[depositInstanceID];
    }
}
