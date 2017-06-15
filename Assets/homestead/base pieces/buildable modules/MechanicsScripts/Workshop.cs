using RedHomestead.Buildings;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using RedHomestead.Crafting;

public class Workshop : ResourcelessHabitatGameplay
{
    private Craftable _currentCraftable = Craftable.Unspecified;
    public Craftable CurrentCraftable { get { return _currentCraftable; } private set { _currentCraftable = value; } }
    public float CraftableProgress;
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

    void Update()
    {
        if (this.CurrentlyViewingDetail)
            FloorplanBridge.Instance.UpdateDetailCraftableProgressView(this._currentCraftable, this.CraftableProgress);
    }

    public override void OnAdjacentChanged() { }

    public override void Report() {}

    public override void Tick() { }

    protected override void OnStart()
    {
        this.SetCurrentCraftable(Craftable.Unspecified);
    }

    public void SetCurrentCraftable(Craftable c)
    {
        if (_currentCraftable != Craftable.Unspecified)
        {
            CraftableHolograms[Convert.ToInt32(_currentCraftable)].gameObject.SetActive(false);
        }

        _currentCraftable = c;

        if (_currentCraftable == Craftable.Unspecified)
        {
            CraftableHolograms[0].parent.gameObject.SetActive(false);
        }
        else
        {
            CraftableHolograms[0].parent.gameObject.SetActive(true);
            CraftableHolograms[Convert.ToInt32(_currentCraftable)].gameObject.SetActive(true);
        }
    }

    internal void MakeProgress(float deltaTime)
    {
        if (_currentCraftable != Craftable.Unspecified)
        {
            float moreHours = (SunOrbit.MartianSecondsPerGameSecond * deltaTime) / 60 / 60;
            CraftableProgress += moreHours / Crafting.CraftData[_currentCraftable].BuildTime;

            if (CraftableProgress >= 1)
            {
                SpawnCraftable(_currentCraftable);
                _currentCraftable = Craftable.Unspecified;
                CraftableProgress = 0f;
                SunOrbit.Instance.ResetToNormalTime();
                PlayerInput.Instance.wakeyWakeySignal = PlayerInput.WakeSignal.PlayerCancel;
            }
        }
    }

    private void SpawnCraftable(Craftable _currentCraftable)
    {
        BounceLander.CreateCratelike(_currentCraftable, this.SpawnPosition.position);
    }

    internal void ToggleCraftableView(bool overallState)
    {
        bool detailState = _currentCraftable != Craftable.Unspecified;

        FloorplanBridge.Instance.ToggleCraftablePanel(overallState, detailState);
        this.CurrentlyViewingDetail = detailState;
        
        if (overallState && detailState)
            FloorplanBridge.Instance.SetCurrentCraftableDetail(this._currentCraftable, this.CraftableProgress);
        else
            FloorplanBridge.Instance.SetCurrentCraftableDetail(Craftable.Unspecified);
    }
}
