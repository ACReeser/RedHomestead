using RedHomestead.Buildings;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using RedHomestead.Crafting;
using RedHomestead.Equipment;
using System.Linq;
using RedHomestead.Persistence;

public class DoorRotationLerpContext
{
    public bool Done { get; private set; }
    private Transform Target;
    private Quaternion FromRotation, ToRotation, EffectiveFromRotation;
    private float Duration;
    private float Time;
    private Coroutine ticker;
    private readonly Action onToggleComplete;

    public DoorRotationLerpContext(Transform target, Quaternion from, Quaternion to, float duration = 1f, Action onToggleComplete = null)
    {
        FromRotation = from;
        EffectiveFromRotation = from;
        ToRotation = to;
        Target = target;

        this.Time = 0f;
        this.Duration = Mathf.Max(duration, 0.00001f); //prevent divide by zero errors
        this.Done = false;
        this.ticker = null;
        this.onToggleComplete = onToggleComplete;
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

            if (this.onToggleComplete != null)
                this.onToggleComplete();
        }
        else
        {
            Target.localRotation = Quaternion.Lerp(EffectiveFromRotation, ToRotation, Time / Duration);
        }
    }
}

[Serializable]
public class WorkshopFlexData
{
    public Craftable CurrentCraftable = Craftable.Unspecified;
    public float Progress;
}

[Serializable]
public class EVAUpgradeSuitTransforms
{
    public Transform Batteries, OxygenTank, Toolbelt, Jetpack;
}

public class Workshop : ResourcelessHabitatGameplay, IDoorManager, IEquipmentSwappable, IFlexDataContainer<ResourcelessModuleData, WorkshopFlexData>
{
    public WorkshopFlexData FlexData { get; set; }
    public Craftable CurrentCraftable { get { return this.FlexData.CurrentCraftable; } private set { this.FlexData.CurrentCraftable = value; } }
    public float CraftableProgress { get { return this.FlexData.Progress; } private set { this.FlexData.Progress = value; } }
    public Transform[] CraftableHolograms, ToolsInLockers, lockers;
    public Transform SpawnPosition;

    public Transform[] Tools { get { return ToolsInLockers; } }
    public Transform[] Lockers { get { return lockers; } }
    public EVAUpgradeSuitTransforms EVAUpgrades;

    private bool CurrentlyViewingDetail = false;
    private Dictionary<Transform, DoorRotationLerpContext> doorRotator = new Dictionary<Transform, DoorRotationLerpContext>();

    public DoorType DoorType { get { return DoorType.Small; } }

    private Dictionary<Transform, Equipment> equipmentLockers = new Dictionary<Transform, Equipment>();
    public Dictionary<Transform, Equipment> EquipmentLockers { get { return equipmentLockers; } }

    private Equipment[] lockerEquipment = new Equipment[] {
        Equipment.Sledge,
        Equipment.PowerDrill,
        Equipment.Wrench,
        Equipment.Blower,
        Equipment.ChemicalSniffer,
        Equipment.Blueprints,
        Equipment.Multimeter,
        Equipment.GPS,
    };
    public Equipment[] LockerEquipment { get { return lockerEquipment; } }

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
        this.FlexData = new WorkshopFlexData();
    }

    void Update()
    {
        if (this.CurrentlyViewingDetail)
            FloorplanBridge.Instance.UpdateDetailCraftableProgressView(this.FlexData.CurrentCraftable, this.CraftableProgress);
    }

    public override void OnAdjacentChanged() { }

    public override void Report() {}

    public override void Tick() { }

    protected override void OnStart()
    {
        this.SetCurrentCraftable(this.FlexData.CurrentCraftable);
        this.InitializeSwappable();
        this.RefreshSuitDisplay();
    }

    public void SetCurrentCraftable(Craftable c)
    {
        if (this.FlexData.CurrentCraftable != Craftable.Unspecified)
        {
            CraftableHolograms[Convert.ToInt32(this.FlexData.CurrentCraftable)].gameObject.SetActive(false);
        }

        this.FlexData.CurrentCraftable = c;

        if (this.FlexData.CurrentCraftable == Craftable.Unspecified)
        {
            this.FlexData.Progress = 0f;
            CraftableHolograms[0].parent.gameObject.SetActive(false);
        }
        else
        {
            CraftableHolograms[0].parent.gameObject.SetActive(true);
            CraftableHolograms[Convert.ToInt32(this.FlexData.CurrentCraftable)].gameObject.SetActive(true);
        }
    }

    internal void MakeProgress(float deltaTime)
    {
        if (this.FlexData.CurrentCraftable != Craftable.Unspecified)
        {
            float moreHours = (SunOrbit.MartianSecondsPerGameSecond * deltaTime) / 60 / 60;
            CraftableProgress += moreHours / Crafting.CraftData[this.FlexData.CurrentCraftable].BuildTimeHours;

            if (CraftableProgress >= 1)
            {
                CraftableHolograms[Convert.ToInt32(this.FlexData.CurrentCraftable)].gameObject.SetActive(false);
                SpawnCraftable(this.FlexData.CurrentCraftable);
                this.FlexData.CurrentCraftable = Craftable.Unspecified;
                CraftableProgress = 0f;
                Game.Current.Score.AddScoringEvent(RedHomestead.Scoring.ScoreType.Craft, GuiBridge.Instance);
                SunOrbit.Instance.ResetToNormalTime();
                PlayerInput.Instance.wakeyWakeySignal = PlayerInput.WakeSignal.PlayerCancel;
            }
        }
    }

    private void SpawnCraftable(Craftable craftable)
    {
        if (craftable.IsCraftableEVASuitComponent())
        {
            Game.Current.Player.PackData.SetUpgrade(craftable.ToEVASuitUpgrade());
            RefreshSuitDisplay();
        }
        else
        {
            BounceLander.CreateCratelike(craftable, this.SpawnPosition.position);
        }
    }

    private void RefreshSuitDisplay()
    {
        EVAUpgrades.OxygenTank.gameObject.SetActive(Game.Current.Player.PackData.HasUpgrade(RedHomestead.EVA.EVAUpgrade.Oxygen));
        EVAUpgrades.Batteries.gameObject.SetActive(Game.Current.Player.PackData.HasUpgrade(RedHomestead.EVA.EVAUpgrade.Battery));
        EVAUpgrades.Toolbelt.gameObject.SetActive(Game.Current.Player.PackData.HasUpgrade(RedHomestead.EVA.EVAUpgrade.Toolbelt));
        EVAUpgrades.Jetpack.gameObject.SetActive(Game.Current.Player.PackData.HasUpgrade(RedHomestead.EVA.EVAUpgrade.Jetpack));
    }

    internal void ToggleCraftableView(bool overallState)
    {
        bool detailState = this.FlexData.CurrentCraftable != Craftable.Unspecified;

        FloorplanBridge.Instance.ToggleCraftablePanel(overallState, detailState, overallState ? this : null);
        this.CurrentlyViewingDetail = detailState;
        
        if (overallState && detailState)
            FloorplanBridge.Instance.SetCurrentCraftableDetail(this.FlexData.CurrentCraftable, this.CraftableProgress);
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
