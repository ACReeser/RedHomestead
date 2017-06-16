using RedHomestead.Buildings;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using RedHomestead.Crafting;

public class Workshop : ResourcelessHabitatGameplay
{
    public Craftable CurrentCraftable { get { return this.Data.FlexCraftable; } private set { this.Data.FlexCraftable = value; } }
    public float CraftableProgress { get { return this.Data.FlexFloat; } private set { this.Data.FlexFloat = value; } }
    public Transform[] CraftableHolograms;
    public Transform SpawnPosition;
    private bool CurrentlyViewingDetail = false;

    public override float WattsConsumed
    {
        get
        {
            return 0f;
        }
    }

    public override Module GetModuleType()
    {
        return Module.Workshop;
    }

    public override void InitializeStartingData()
    {
        base.InitializeStartingData();
        this.Data.FlexCraftable = Craftable.Unspecified;
        this.Data.FlexFloat = 0f;
    }

    void Update()
    {
        if (this.CurrentlyViewingDetail)
            FloorplanBridge.Instance.UpdateDetailCraftableProgressView(this.Data.FlexCraftable, this.CraftableProgress);
    }

    public override void OnAdjacentChanged() { }

    public override void Report() {}

    public override void Tick() { }

    protected override void OnStart()
    {
        this.SetCurrentCraftable(this.Data.FlexCraftable);
    }

    public void SetCurrentCraftable(Craftable c)
    {
        if (this.Data.FlexCraftable != Craftable.Unspecified)
        {
            CraftableHolograms[Convert.ToInt32(this.Data.FlexCraftable)].gameObject.SetActive(false);
        }

        this.Data.FlexCraftable = c;

        if (this.Data.FlexCraftable == Craftable.Unspecified)
        {
            CraftableHolograms[0].parent.gameObject.SetActive(false);
        }
        else
        {
            CraftableHolograms[0].parent.gameObject.SetActive(true);
            CraftableHolograms[Convert.ToInt32(this.Data.FlexCraftable)].gameObject.SetActive(true);
        }
    }

    internal void MakeProgress(float deltaTime)
    {
        if (this.Data.FlexCraftable != Craftable.Unspecified)
        {
            float moreHours = (SunOrbit.MartianSecondsPerGameSecond * deltaTime) / 60 / 60;
            CraftableProgress += moreHours / Crafting.CraftData[this.Data.FlexCraftable].BuildTime;

            if (CraftableProgress >= 1)
            {
                SpawnCraftable(this.Data.FlexCraftable);
                this.Data.FlexCraftable = Craftable.Unspecified;
                CraftableProgress = 0f;
                SunOrbit.Instance.ResetToNormalTime();
                PlayerInput.Instance.wakeyWakeySignal = PlayerInput.WakeSignal.PlayerCancel;
            }
        }
    }

    private void SpawnCraftable(Craftable craftable)
    {
        BounceLander.CreateCratelike(craftable, this.SpawnPosition.position);
    }

    internal void ToggleCraftableView(bool overallState)
    {
        bool detailState = this.Data.FlexCraftable != Craftable.Unspecified;

        FloorplanBridge.Instance.ToggleCraftablePanel(overallState, detailState);
        this.CurrentlyViewingDetail = detailState;
        
        if (overallState && detailState)
            FloorplanBridge.Instance.SetCurrentCraftableDetail(this.Data.FlexCraftable, this.CraftableProgress);
        else
            FloorplanBridge.Instance.SetCurrentCraftableDetail(Craftable.Unspecified);
    }
}
