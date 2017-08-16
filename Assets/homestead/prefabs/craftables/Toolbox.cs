using RedHomestead.Equipment;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Toolbox : MovableSnappable, IDoorManager, IEquipmentSwappable
{
    private DoorRotationLerpContext lerp;
    
    public Transform[] tools, lockers;
    public Transform[] Tools { get { return tools; } }
    public Transform[] Lockers { get { return lockers; } }

    private Dictionary<Transform, Equipment> equipmentLockers = new Dictionary<Transform, Equipment>();
    public Dictionary<Transform, Equipment> EquipmentLockers { get { return equipmentLockers; } }

    private Equipment[] lockerEquipment = new Equipment[] {
        Equipment.Wrench,
        Equipment.Blower,
        Equipment.PowerDrill,
        Equipment.Sledge
    };
    private Rigidbody rigid;

    public Equipment[] LockerEquipment { get { return lockerEquipment; } }

    void Start()
    {
        this.InitializeSwappable();
        this.rigid = GetComponent<Rigidbody>();
    }

    public void ToggleDoor(Transform door)
    {
        //assumes all door transforms start shut
        if (lerp == null)
            lerp = new DoorRotationLerpContext(door, door.localRotation, Quaternion.Euler(0, -90, 90f), .4f, this.onToggle);

        this.rigid.isKinematic = true;
        lerp.Toggle(StartCoroutine);
    }

    private void onToggle()
    {
        this.rigid.isKinematic = false;
    }

    public override string GetText()
    {
        return "Toolbox";
    }
}
