using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace RedHomestead.Electricity
{
    public interface IPowerable
    {
        string PowerGridInstanceID { get; set; }
        PowerVisualization PowerViz { get; }
    }

    [Serializable]
    public struct PowerVisualization
    {
        public MeshFilter PowerBacking;
        public Transform PowerMask;
        public Transform PowerActive;
    }

    public interface IPowerSupply : IPowerable
    {
        bool VariablePowerSupply { get; }
        float WattsGenerated { get; }
    }

    public interface IBattery : IPowerable
    {
        EnergyContainer EnergyContainer { get; }
    }

    public interface IPowerConsumer : IPowerable
    {
        float WattsConsumed { get; }
        bool HasPower { get; set; }
        void OnPowerChanged();
        bool IsOn { get; set; }
        void OnEmergencyShutdown();
#warning todo: add a priority field for shutdown
    }

    public static class ElectricityConstants
    {
        public const float WattHoursPerBatteryBlock = RadioisotopeThermoelectricGenerator.WattHoursGeneratedPerDay / 10f / 2f;
        public static Vector3 _BackingScale = new Vector3(1.2f, 1.2f, 0f);
    }

    public static class ElectricityExtensions
    {
        public static bool HasPowerGrid(this IPowerable node)
        {
            return !String.IsNullOrEmpty(node.PowerGridInstanceID);
        }

        public static void TurnOnPower(this List<IPowerConsumer> list)
        {
            foreach (IPowerConsumer c in list)
            {
                if (!c.HasPower)
                {
                    c.HasPower = true;
                    c.OnPowerChanged();
                }
            }
        }

        public static void EmergencyShutdown(this IPowerConsumer c)
        {
            if (c.HasPower)
            {
                c.HasPower = false;
                c.OnPowerChanged();
            }
            if (c.IsOn)
            {
                c.IsOn = false;
                c.OnEmergencyShutdown();
            }
        }

        public static void RefreshVisualization(this IPowerConsumer c)
        {
        }

        public static void RefreshVisualization(this IPowerSupply s)
        {

        }

        public static void RefreshVisualization(this IBattery b)
        {
            b.PowerViz.PowerMask.transform.localScale = ElectricityConstants._BackingScale + Vector3.forward *  (10 - b.CurrentBatteryUnits());
        }

        public static int BatteryUnitCapacity(this IBattery b)
        {
            return Mathf.RoundToInt(b.EnergyContainer.AvailableCapacity / ElectricityConstants.WattHoursPerBatteryBlock);
        }

        public static float CurrentBatteryUnits(this IBattery b)
        {
            return b.EnergyContainer.CurrentAmount / ElectricityConstants.WattHoursPerBatteryBlock;
        }
    }

    public class PowerGrids
    {
        private Dictionary<string, PowerGrid> grids = new Dictionary<string, PowerGrid>();

        internal void Attach(IPowerable node1, IPowerable node2)
        {
            bool node1Powered = node1.HasPowerGrid();
            bool node2Powered = node2.HasPowerGrid();

            if (node1Powered && node2Powered)
            {
                //deprecate node2's power grid by having node1's power grid usurp it
                PowerGrid deprecatedPowergrid = grids[node2.PowerGridInstanceID];

                grids[node1.PowerGridInstanceID].Usurp(deprecatedPowergrid);

                grids.Remove(deprecatedPowergrid.PowerGridInstanceID);
            }
            else if (node1Powered)
            {
                grids[node1.PowerGridInstanceID].Add(node2);
            }
            else if (node2Powered)
            {
                grids[node2.PowerGridInstanceID].Add(node1);
            }
            else
            {
                PowerGrid newPG = new PowerGrid();
                grids.Add(newPG.PowerGridInstanceID, newPG);
                newPG.Add(node1);
                newPG.Add(node2);
            }
        }

        internal void Tick()
        {
            foreach (PowerGrid g in grids.Values)
            {
                g.Tick();
            }
        }
    }

    [Serializable]
    public struct ElectricityIndicatorMeshes
    {
        public Mesh[] BackingMeshes;
        public Mesh ActiveMesh;
    }

    [Serializable]
    public struct ElectricityIndicatorMeshesForConsumers
    {
        public Mesh[] BackingMeshes;
        public Mesh[] ActiveMeshes;
    }

    public class PowerGrid
    {
        public enum GridMode { Unknown = -99, Blackout = -3, Brownout, BatteryDrain, Nominal = 0, BatteryRecharge }

        internal readonly string PowerGridInstanceID;
        internal GridMode Mode = GridMode.Unknown;
        protected List<IPowerConsumer> Consumers = new List<IPowerConsumer>();
        protected List<IPowerSupply> Producers = new List<IPowerSupply>();
        protected List<IPowerSupply> VariableProducers = new List<IPowerSupply>();
        protected List<IBattery> Batteries = new List<IBattery>();

        public PowerGrid()
        {
            this.PowerGridInstanceID = Guid.NewGuid().ToString();
        }

        internal void Tick()
        {
            GridMode newGridMode = GridMode.Unknown;

            float capacityWatts = Producers.Sum(x => x.WattsGenerated) * Time.fixedDeltaTime;
            float loadWatts = Consumers.Sum(x => x.IsOn ? x.WattsConsumed : 0f) * Time.fixedDeltaTime;
            float batteryWatts = Batteries.Sum(x => x.EnergyContainer.CurrentAmount);

            float surplusWatts = capacityWatts - loadWatts;
            float deficitWatts = loadWatts - capacityWatts;

            if (capacityWatts + batteryWatts == 0f)
            {
                newGridMode = GridMode.Blackout;
            }
            else if (capacityWatts > loadWatts)
            {
                if (surplusWatts == 0f)
                    newGridMode = GridMode.Nominal;
                else //surplus > 0f
                    newGridMode = GridMode.BatteryRecharge;
            }
            else //capacity < load
            {
                if (capacityWatts + batteryWatts > deficitWatts)
                    newGridMode = GridMode.BatteryDrain;
                else //deficit > capacity + battery
                    newGridMode = GridMode.Brownout;
            }

            if (Mode != newGridMode)
            {
                Mode = newGridMode;
                UnityEngine.Debug.Log("power is now: " + Mode.ToString());
                switch (Mode)
                {
                    case GridMode.Blackout:
                        foreach (IPowerConsumer c in Consumers)
                        {
                            c.EmergencyShutdown();
                        }
                        loadWatts = 0f;
                        surplusWatts = 0f;
                        break;
                    case GridMode.Brownout:
                        foreach (IPowerConsumer c in Consumers)
                        {
#warning todo: reference priority field during shutdown, sort by it probably

                            c.EmergencyShutdown();

                            deficitWatts -= c.WattsConsumed;

                            //stop the brownout when we have a new equilibrium
                            if (capacityWatts + batteryWatts > deficitWatts)
                                break;
                            //but wait until next tick to sort itself out
                        }
                        break;
                    case GridMode.BatteryDrain:
                        Consumers.TurnOnPower();
                        break;
                    case GridMode.Nominal:
                        Consumers.TurnOnPower();
                        break;
                    case GridMode.BatteryRecharge:
                        Consumers.TurnOnPower();
                        break;
                }
            }

            if (Mode == GridMode.BatteryRecharge)
            {
                float recharged = surplusWatts;
                //todo: some sort of priority
                //so you can recharge your rover faster probably
                foreach (IBattery batt in Batteries)
                {
                    recharged = batt.EnergyContainer.Push(recharged);
                    batt.RefreshVisualization();

                    if (recharged <= 0)
                        break;

                }
            }
            else if (Mode == GridMode.BatteryDrain)
            {
                float drained = deficitWatts;
                foreach (IBattery batt in Batteries)
                {
                    drained -= batt.EnergyContainer.Pull(drained);
                    batt.RefreshVisualization();

                    if (drained <= 0)
                        break;
                }
            }


        }

        internal void Add(IPowerable mod)
        {
            if (mod is IPowerSupply)
            {
                Producers.Add(mod as IPowerSupply);

                if ((mod as IPowerSupply).VariablePowerSupply)
                {
                    VariableProducers.Add(mod as IPowerSupply);
                }
            }
            else if (mod is IPowerConsumer)
            {
                Consumers.Add(mod as IPowerConsumer);
            }

            if (mod is IBattery)
            {
                Batteries.Add(mod as IBattery);
            }
            mod.PowerGridInstanceID = this.PowerGridInstanceID;
        }

        internal void Remove(IPowerable mod)
        {
            if (mod is IPowerSupply)
            {
                Producers.Remove(mod as IPowerSupply);

                if ((mod as IPowerSupply).VariablePowerSupply)
                {
                    VariableProducers.Remove(mod as IPowerSupply);
                }
            }
            else if (mod is IPowerConsumer)
            {
                Consumers.Remove(mod as IPowerConsumer);
            }

            if (mod is IBattery)
            {
                Batteries.Remove(mod as IBattery);
            }

            mod.PowerGridInstanceID = "";
        }

        private void SetPowerableParentToMe(IPowerable p) { p.PowerGridInstanceID = this.PowerGridInstanceID; }
        internal void Usurp(PowerGrid other)
        {
            other.Consumers.ForEach(SetPowerableParentToMe);
            Consumers.AddRange(other.Consumers);

            other.Producers.ForEach(SetPowerableParentToMe);
            Producers.AddRange(other.Producers);

            other.Batteries.ForEach(SetPowerableParentToMe);
            Batteries.AddRange(other.Batteries);
        }
    }
}