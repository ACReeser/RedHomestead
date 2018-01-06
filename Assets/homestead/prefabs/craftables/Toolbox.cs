using RedHomestead.Equipment;
using RedHomestead.Persistence;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ToolboxData: FacingData
{
    public Equipment[] Equipment;
}

[RequireComponent(typeof(Rigidbody))]
public class Toolbox : MovableSnappable, IDoorManager, IEquipmentSwappable, IDataContainer<ToolboxData>
{
    private DoorRotationLerpContext lerp;
    
    public Transform[] tools, lockers;
    public Transform[] Tools { get { return tools; } }
    public Transform[] Lockers { get { return lockers; } }

    private Dictionary<Transform, Equipment> equipmentLockers = new Dictionary<Transform, Equipment>();
    public Dictionary<Transform, Equipment> EquipmentLockers { get { return equipmentLockers; } }

    private Equipment[] locker;
    private Rigidbody rigid;

    public Equipment[] LockerEquipment { get { return Data.Equipment; } }

    private ToolboxData data;

    public DoorType DoorType { get { return DoorType.Small; } }
    public ToolboxData Data { get { return data; } set { data = value; } }

    void Start()
    {
        this.rigid = GetComponent<Rigidbody>();
        if (data == null)
        {
            data = new ToolboxData()
            {
                Equipment = new Equipment[] {
                    Equipment.Wrench,
                    Equipment.Blower,
                    Equipment.PowerDrill,
                    Equipment.Sledge,
                    Equipment.RockDrill,
                    Equipment.ChemicalSniffer,
                }
            };
        }
        this.InitializeSwappable();
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
