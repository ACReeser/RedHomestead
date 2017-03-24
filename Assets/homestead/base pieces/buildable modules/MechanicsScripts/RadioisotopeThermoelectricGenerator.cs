using System;
using System.Collections;
using System.Collections.Generic;
using RedHomestead.Buildings;
using UnityEngine;
using System.Linq;

namespace RedHomestead.Electricity
{
    public interface IPowerable
    {
        string PowerGridInstanceID { get; set; }
    }
    public interface IPowerSupply: IPowerable
    {
        float WattsGeneratedPerTick { get; }
    }
    public interface IBattery: IPowerable
    {
        EnergyContainer EnergyContainer { get; }
    }
    public interface IPowerConsumer: IPowerable
    {
        float WattsConsumedPerTick { get; }
        bool HasPower { get; set; }
        void OnPowerChanged();
        bool IsOn { get; set; }
        void OnEmergencyShutdown();
#warning todo: add a priority field for shutdown
    }

    public class PowerGrids
    {
        private Dictionary<string, PowerGrid> grids = new Dictionary<string, PowerGrid>();

        internal void Attach(IPowerable node1, IPowerable node2)
        {
            if (!String.IsNullOrEmpty(node1.PowerGridInstanceID))
            {
                grids[node1.PowerGridInstanceID].Add(node2);
            }
            else if (!String.IsNullOrEmpty(node2.PowerGridInstanceID))
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
            foreach(PowerGrid g in grids.Values)
            {
                g.Tick();
            }
        }
    }
    
    public class PowerGrid
    {
        public enum GridMode { Unknown = -99, Blackout = -3, Brownout, BatteryDrain, Nominal = 0, BatteryRecharge }

        internal readonly string PowerGridInstanceID;
        internal GridMode Mode = GridMode.Unknown;
        protected List<IPowerConsumer> Consumers = new List<IPowerConsumer>();
        protected List<IPowerSupply> Producers = new List<IPowerSupply>();
        protected List<IBattery> Batteries = new List<IBattery>();

        public PowerGrid()
        {
            this.PowerGridInstanceID = Guid.NewGuid().ToString();
        }

        internal void Tick()
        {
            GridMode newGridMode = GridMode.Unknown;

            float capacityWatts = Producers.Sum(x => x.WattsGeneratedPerTick) * Time.fixedDeltaTime;
            float loadWatts = Consumers.Sum(x => x.IsOn ? x.WattsConsumedPerTick : 0f) * Time.fixedDeltaTime;
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
                UnityEngine.Debug.Log("power is now: "+ Mode.ToString());
                switch (Mode)
                {
                    case GridMode.Blackout:
                        foreach(IPowerConsumer c in Consumers)
                        {
                            EmergencyShutdown(c);
                        }
                        loadWatts = 0f;
                        surplusWatts = 0f;
                        break;
                    case GridMode.Brownout:
                        foreach (IPowerConsumer c in Consumers)
                        {
#warning todo: reference priority field during shutdown, sort by it probably

                            EmergencyShutdown(c);

                            deficitWatts -= c.WattsConsumedPerTick;

                            //stop the brownout when we have a new equilibrium
                            if (capacityWatts + batteryWatts > deficitWatts)
                                break;
                            //but wait until next tick to sort itself out
                        }
                        break;
                    case GridMode.BatteryDrain:
                        foreach (IPowerConsumer c in Consumers)
                        {
                            TurnOnPower(c);
                        }
                        break;
                    case GridMode.Nominal:
                        foreach (IPowerConsumer c in Consumers)
                        {
                            TurnOnPower(c);
                        }
                        break;
                    case GridMode.BatteryRecharge:
                        foreach (IPowerConsumer c in Consumers)
                        {
                            TurnOnPower(c);
                        }
                        break;
                }
            }

            if (Mode == GridMode.BatteryRecharge)
            {
                float recharged = surplusWatts;
                //todo: some sort of priority
                //so you can recharge your rover faster probably
                foreach (IBattery c in Batteries)
                {
                    recharged = c.EnergyContainer.Push(recharged);

                    if (recharged <= 0)
                        break;
                }
            }
            else if (Mode == GridMode.BatteryDrain)
            {
                float drained = deficitWatts;
                foreach (IBattery c in Batteries)
                {
                    drained -= c.EnergyContainer.Pull(drained);

                    if (drained <= 0)
                        break;
                }
            }
        }

        private static void TurnOnPower(IPowerConsumer c)
        {
            if (!c.HasPower)
            {
                c.HasPower = true;
                c.OnPowerChanged();
            }
        }

        private static void EmergencyShutdown(IPowerConsumer c)
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

        internal void Add(IPowerable mod)
        {
            if (mod is IPowerSupply)
            {
                Producers.Add(mod as IPowerSupply);
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
    }
}

public class RadioisotopeThermoelectricGenerator : ResourcelessGameplay, RedHomestead.Electricity.IPowerSupply
{
    public override float WattsConsumedPerTick
    {
        get
        {
            return 0;
        }
    }

    public float WattsGeneratedPerTick
    {
        get
        {
            return 130;
        }
    }

    public override Module GetModuleType()
    {
        return Module.RTG;
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
}
