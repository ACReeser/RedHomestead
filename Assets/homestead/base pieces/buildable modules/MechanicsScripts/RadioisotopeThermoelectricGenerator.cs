using System;
using System.Collections;
using System.Collections.Generic;
using RedHomestead.Buildings;
using UnityEngine;
using System.Linq;

namespace RedHomestead.Electricity
{
    public interface IPowerSupply
    {
        float WattsPerTick { get; }
    }
    public interface IBattery
    {
        EnergyContainer EnergyContainer { get; }
    }

    public class PowerGrids
    {
        private Dictionary<string, PowerGrid> grids = new Dictionary<string, PowerGrid>();

        internal void Attach(ModuleGameplay node1, ModuleGameplay node2)
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
        internal readonly string PowerGridInstanceID;
        protected List<ModuleGameplay> Consumers = new List<ModuleGameplay>();
        protected List<IPowerSupply> Producers = new List<IPowerSupply>();
        protected List<IBattery> Batteries = new List<IBattery>();

        public PowerGrid()
        {
            this.PowerGridInstanceID = Guid.NewGuid().ToString();
        }

        internal void Tick()
        {
            float capacityWatts = Producers.Sum(x => x.WattsPerTick) * Time.fixedDeltaTime;
            float loadWatts = Consumers.Sum(x => x.WattRequirementsPerTick) * Time.fixedDeltaTime;

            if (capacityWatts > loadWatts)
            {
                float unusedWatts = capacityWatts - loadWatts;

            }
            else
            {
                float batteryWatts = loadWatts - capacityWatts;
            }
        }

        internal void Add(ModuleGameplay mod)
        {
            if (mod is IPowerSupply)
            {
                Producers.Add(mod as IPowerSupply);
            }
            else
            {
                Consumers.Add(mod);
            }
            mod.PowerGridInstanceID = this.PowerGridInstanceID;
        }

        internal void Remove(ModuleGameplay mod)
        {
            //instead of contains && remove
            //we're going to straight up remove
            if (!Consumers.Remove(mod))
            {
                Producers.Remove(mod as IPowerSupply);
            }

            mod.PowerGridInstanceID = "";
        }
    }
}

public class RadioisotopeThermoelectricGenerator : ResourcelessGameplay, RedHomestead.Electricity.IPowerSupply
{
    public override float WattRequirementsPerTick
    {
        get
        {
            return 0;
        }
    }

    public float WattsPerTick
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
