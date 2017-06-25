using RedHomestead.Buildings;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using RedHomestead.Crafting;


public class DoorRotationLerpContext
{
    public bool Done { get; private set; }
    private Transform Target;
    private Quaternion FromRotation, ToRotation, EffectiveFromRotation;
    private float Duration;
    private float Time;
    private Coroutine ticker;

    public DoorRotationLerpContext(Transform target, Quaternion from, Quaternion to, float duration = 1f)
    {
        FromRotation = from;
        EffectiveFromRotation = from;
        ToRotation = to;
        Target = target;

        this.Time = 0f;
        this.Duration = Mathf.Max(duration, 0.00001f); //prevent divide by zero errors
        this.Done = false;
        this.ticker = null;
    }

    private void Reverse(bool setEffective = true)
    {
        var newToRot = FromRotation;

        FromRotation = ToRotation;

        if (setEffective)
            EffectiveFromRotation = FromRotation;

        ToRotation = newToRot;
    }

    public void Toggle(Func<IEnumerator, Coroutine> startCoroutine)
    {
        if (this.ticker == null)
        {
            this.ticker = startCoroutine(this._Toggle());
        }
        else
        {
            EffectiveFromRotation = Target.localRotation;
            this.Reverse(false);
            Time = Duration - Time;
        }
    }

    private IEnumerator _Toggle()
    {
        Done = false;

        while (!Done)
        {
            Tick();
            yield return null;
        }

        this.Reverse();
        this.Time = 0f;
        this.ticker = null;
        this.Target.name = this.Target.name == Airlock.OpenDoorName ? Airlock.ClosedDoorName : Airlock.OpenDoorName;
    }

    private void Tick()
    {
        this.Time += UnityEngine.Time.deltaTime;
        if (this.Time > this.Duration)
        {
            Target.localRotation = ToRotation;

            Done = true;
        }
        else
        {
            Target.localRotation = Quaternion.Lerp(EffectiveFromRotation, ToRotation, Time / Duration);
        }
    }
}

public class Workshop : ResourcelessHabitatGameplay, IDoorManager
{
    public Craftable CurrentCraftable { get { return this.Data.FlexCraftable; } private set { this.Data.FlexCraftable = value; } }
    public float CraftableProgress { get { return this.Data.FlexFloat; } private set { this.Data.FlexFloat = value; } }
    public Transform[] CraftableHolograms, Tools;
    public Transform SpawnPosition;
    private bool CurrentlyViewingDetail = false;
    private Dictionary<Transform, DoorRotationLerpContext> doorRotator = new Dictionary<Transform, DoorRotationLerpContext>();

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
                CraftableHolograms[Convert.ToInt32(this.Data.FlexCraftable)].gameObject.SetActive(false);
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

    public void ToggleDoor(Transform door)
    {
        //assumes all door transforms start shut
        if (!doorRotator.ContainsKey(door))
        {
            doorRotator[door] = new DoorRotationLerpContext(door, door.localRotation, Quaternion.Euler(0, 0, 90f), .4f);
        }

        doorRotator[door].Toggle(StartCoroutine);
    }
}
