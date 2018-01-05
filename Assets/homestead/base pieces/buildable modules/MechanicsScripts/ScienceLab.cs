using RedHomestead.Buildings;
using RedHomestead.Equipment;
using RedHomestead.Persistence;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ScienceLabFlexData
{
    //public Craftable CurrentCraftable = Craftable.Unspecified;
    public float Progress;
}

public class ScienceLab : ResourcelessHabitatGameplay, IEquipmentSwappable, IFlexDataContainer<ResourcelessModuleData, ScienceLabFlexData>
{
    public ScienceLabFlexData FlexData { get; set; }
    public Transform[] ToolsInLockers, lockers;

    public Transform[] Tools { get { return ToolsInLockers; } }
    public Transform[] Lockers { get { return lockers; } }
    private Dictionary<Transform, Equipment> equipmentLockers = new Dictionary<Transform, Equipment>();

    private Equipment[] lockerEquipment = new Equipment[] {
        Equipment.Sampler,
        Equipment.GPS
    };
    public Equipment[] LockerEquipment { get { return lockerEquipment; } }
    public Dictionary<Transform, Equipment> EquipmentLockers { get { return equipmentLockers; } }

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
        this.FlexData = new ScienceLabFlexData();
    }

    public override void OnAdjacentChanged() { }

    public override void Report() { }

    public override void Tick() { }

}
