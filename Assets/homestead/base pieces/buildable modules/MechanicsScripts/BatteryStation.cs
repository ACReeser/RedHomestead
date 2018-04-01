using RedHomestead.Buildings;
using RedHomestead.Electricity;
using RedHomestead.Simulation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RedHomestead.Persistence;
using System;
using System.Linq;

public class BatteryStationFlexData
{
    public EnergyContainer EnergyContainer; 
}

public class BatteryStation : ResourcelessGameplay, IFlexDataContainer<ResourcelessModuleData, BatteryStationFlexData>, IBattery, ITriggerSubscriber, ICrateSnapper
{
    public BatteryStationFlexData FlexData { get; set; }
    public Mesh[] backings = new Mesh[4];

    private PowerCube[] cubes = new PowerCube[4];
    private float synergyBonusWatts = ElectricityConstants.WattHoursPerBatteryBlock;

    public EnergyContainer EnergyContainer
    {
        get
        {
            return FlexData.EnergyContainer;
        }
    }

    public override float WattsConsumed
    {
        get
        {
            return 0;
        }
    }

    public override void InitializeStartingData()
    {
        this.FlexData = new BatteryStationFlexData()
        {
            EnergyContainer = new EnergyContainer(0)
        };

        base.InitializeStartingData();
    }

    public override Module GetModuleType()
    {
        return Module.BatteryStation;
    }

    public override void OnAdjacentChanged()
    {

    }

    public override void Report()
    {

    }

    public override void Tick()
    {

    }

    protected override void OnStart()
    {
        base.OnStart();
        RefreshPowerBackings();
    }

    public void OnChildTriggerEnter(TriggerForwarder child, Collider c, IMovableSnappable movesnap)
    {
        if (unsnapTimer == null)
        {
            var cube = movesnap.transform.GetComponent<PowerCube>();
            if (cube != null)
            {
                movesnap.SnapCrate(this, child.transform.position, globalRotation: child.transform.rotation);
                BalanceAddNew(cube, child.transform.GetSiblingIndex());
            }
        }
    }

    public void DetachCrate(IMovableSnappable detaching)
    {
        var cube = detaching.transform.GetComponent<PowerCube>();

        if (cube != null)
        {
            BalanceRemove(cube);

            unsnapTimer = StartCoroutine(UnsnapTimer());
        }
    }

    private Coroutine unsnapTimer;
    private IEnumerator UnsnapTimer()
    {
        yield return new WaitForSeconds(2f);
        unsnapTimer = null;
    }

    public void BalanceAddNew(PowerCube cube, int siblingIndex)
    {
        int currentCubeCount = cubes.Count(x => x != null);
        if (currentCubeCount <= 0f)
        {
            FlexData.EnergyContainer = new EnergyContainer(cube.EnergyContainer.CurrentAmount)
            {
                TotalCapacity = ElectricityConstants.WattHoursPerBatteryBlock + synergyBonusWatts
            };
        }
        else
        {
            FlexData.EnergyContainer = new EnergyContainer(FlexData.EnergyContainer.CurrentAmount + cube.EnergyContainer.CurrentAmount)
            {
                //current cubes + new cube + synergy bonus
                TotalCapacity = ElectricityConstants.WattHoursPerBatteryBlock * (currentCubeCount + 1f) + synergyBonusWatts
            };
        }

        cubes[siblingIndex] = cube;
        RefreshPowerBackings();
        cube.RefreshVisualization();
        cube.Hide();
    }

    private void RefreshPowerBackings()
    {
        int currentCubeCount = cubes.Count(x => x != null);
        int index = currentCubeCount - 1;

        if (currentCubeCount <= 0)
        {
            this.powerViz.PowerBacking.mesh = null;
        }
        else
        {
            this.powerViz.PowerBacking.mesh = backings[index];
        }

        this.RefreshVisualization();
    }

    public void BalanceRemove(PowerCube cube)
    {
        int currentCubeCount = cubes.Count(x => x != null);

        if (FlexData.EnergyContainer.CurrentAmount >= ElectricityConstants.WattHoursPerBatteryBlock * 4f)
        {
            cube.EnergyContainer.Push(ElectricityConstants.WattHoursPerBatteryBlock);
            FlexData.EnergyContainer.Pull(ElectricityConstants.WattHoursPerBatteryBlock);
        }
        else
        {
            //zero out the leaving cube
            cube.EnergyContainer.Pull(ElectricityConstants.WattHoursPerBatteryBlock);

            //get the amount of watts per cube
            float perBalance = FlexData.EnergyContainer.CurrentAmount / (float)currentCubeCount;

            //and set that to the cube that is leaving
            cube.EnergyContainer.Push(perBalance);
            FlexData.EnergyContainer.Pull(perBalance);
        }

        if (currentCubeCount <= 1)
        {
            //no more cubes, zero out the battery
            FlexData.EnergyContainer.Pull(ElectricityConstants.WattHoursPerBatteryBlock * 2f);
        }


        for (int i = 0; i < cubes.Length; i++)
        {
            if (cubes[i] != null)
            {
                cubes[i] = null;
            }
        }
        RefreshPowerBackings();
        cube.Show();
        cube.RefreshVisualization();
    }
}
