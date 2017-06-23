using RedHomestead.Simulation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace RedHomestead.Electricity
{
    public interface IPowerable: IRepairable
    {
        /// <summary>
        /// The persisted ID of this powerable
        /// </summary>
        string PowerableInstanceID { get; }
        /// <summary>
        /// The transient, only used in game memory ID of the current power grid
        /// </summary>
        string PowerGridInstanceID { get; set; }
        PowerVisualization PowerViz { get; }
    }

    [Serializable]
    public struct PowerVisualization
    {
        public MeshFilter PowerBacking;
        public Transform PowerMask;
        public Transform PowerActive;

        public bool IsAssigned
        {
            get
            {
                return PowerBacking != null && PowerMask != null && PowerActive != null;
            }
        }
    }

    public interface IPowerSupply : IPowerable
    {
        float WattsGenerated { get; }
    }

    public interface IVariablePowerSupply : IPowerSupply
    {
        float MaximumWattsGenerated { get; }
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
        public const float WattsPerBlock = SolarPanel.MaximumWattsPerModule / 10f;
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
                c.TurnOnPower();
            }
        }

        public static void TurnOnPower(this IPowerConsumer consumer)
        {
            UnityEngine.Debug.Log("turning on consumer");
            if (!consumer.HasPower)
            {
                consumer.HasPower = true;
                consumer.RefreshVisualization();
                consumer.OnPowerChanged();
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

            c.RefreshVisualization();
        }

        public static void InitializePowerVisualization(this IPowerable powerable)
        {
            if (powerable is IPowerConsumer)
            {
                powerable.PowerViz.PowerBacking.mesh = FlowManager.Instance.ConsumerMeshes.BackingMeshes[(powerable as IPowerConsumer).ConsumptionInPowerUnits() - 1];
                (powerable as IPowerConsumer).RefreshVisualization();
            }

            if (powerable is IBattery)
            {
                PowerVisualization viz;
                if (powerable is Habitat)
                    viz = (powerable as Habitat).BatteryViz;
                else
                    viz = powerable.PowerViz;

                viz.PowerBacking.mesh = FlowManager.Instance.BatteryMeshes.BackingMeshes[(powerable as IBattery).BatteryUnitCapacity() - 1];
                (powerable as IBattery).RefreshVisualization();
            }

            if (powerable is IPowerSupply)
            {
                if (powerable is IVariablePowerSupply)
                {
                    powerable.PowerViz.PowerBacking.mesh = FlowManager.Instance.GeneratorMeshes.BackingMeshes[(powerable as IVariablePowerSupply).MaximumGenerationInPowerUnits() - 1];
                }
                else
                {
                    powerable.PowerViz.PowerBacking.mesh = FlowManager.Instance.GeneratorMeshes.BackingMeshes[(powerable as IPowerSupply).GenerationInPowerUnits() - 1];
                }
                (powerable as IPowerSupply).RefreshVisualization();
            }
        }

        public static void RefreshVisualization(this IPowerConsumer c)
        {
            if (c.PowerViz.PowerActive != null)
                c.PowerViz.PowerActive.gameObject.SetActive((!(c.FaultedPercentage > 0f)) && c.IsOn);
        }

        public static void RefreshVisualization(this IPowerSupply s)
        {
            //when faulted, have the mask show 0 units
            if (s.FaultedPercentage > 0f)
            {
                s.PowerViz.PowerMask.transform.localScale = ElectricityConstants._BackingScale + Vector3.forward * 10f;
            }
            else
            {
                s.PowerViz.PowerMask.transform.localScale = ElectricityConstants._BackingScale + Vector3.forward * (10 - s.GenerationInPowerUnits());
            }
        }

        public static void RefreshVisualization(this IBattery b)
        {
            PowerVisualization viz;

            if (b is Habitat)
                viz = (b as Habitat).BatteryViz;
            else
                viz = b.PowerViz;

            if (viz.PowerMask != null)
                viz.PowerMask.transform.localScale = ElectricityConstants._BackingScale + Vector3.forward *  (10 - b.CurrentBatteryUnits());
        }

        public static int BatteryUnitCapacity(this IBattery b)
        {
            return Math.Max(1, Mathf.RoundToInt(b.EnergyContainer.AvailableCapacity / ElectricityConstants.WattHoursPerBatteryBlock));
        }

        public static float CurrentBatteryUnits(this IBattery b)
        {
            return b.EnergyContainer.CurrentAmount / ElectricityConstants.WattHoursPerBatteryBlock;
        }

        public static int ConsumptionInPowerUnits(this IPowerConsumer c)
        {
            return Math.Max(1, Mathf.RoundToInt(c.WattsConsumed / ElectricityConstants.WattsPerBlock));
        }

        public static int GenerationInPowerUnits(this IPowerSupply c)
        {
            return Math.Max(0, Mathf.RoundToInt(c.WattsGenerated / ElectricityConstants.WattsPerBlock));
        }

        public static int MaximumGenerationInPowerUnits(this IVariablePowerSupply c)
        {
            return Math.Max(1, Mathf.RoundToInt(c.MaximumWattsGenerated / ElectricityConstants.WattsPerBlock));
        }
    }

    public class PowerGrids
    {
        private Dictionary<string, PowerGrid> grids = new Dictionary<string, PowerGrid>();
        internal Dictionary<IPowerable, List<Powerline>> Edges = new Dictionary<IPowerable, List<Powerline>>();
        internal PowerGridTickData LastGlobalTickData;

        public delegate void PowerTickEventHandler();
        public event PowerTickEventHandler OnPowerTick;

        internal void Add(Habitat h)
        {
            if (!h.HasPowerGrid())
            {
                PowerGrid newPG = new PowerGrid();
                grids.Add(newPG.PowerGridInstanceID, newPG);
                newPG.Add(h);
            }

            h.OnPowerChanged();
        }

        internal void Attach(Powerline edge, IPowerable node1, IPowerable node2)
        {
            bool node1Powered = node1.HasPowerGrid() && grids.ContainsKey(node1.PowerGridInstanceID);
            bool node2Powered = node2.HasPowerGrid() && grids.ContainsKey(node2.PowerGridInstanceID);

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

            AddEdge(edge, node1);
            AddEdge(edge, node2);
        }

        private void AddEdge(Powerline edge, IPowerable node)
        {
            if (Edges.ContainsKey(node))
                Edges[node].Add(edge);
            else
                Edges[node] = new List<Powerline>() { edge };
        }

        internal void Detach(IPowerable node)
        {
            Powerline[] lines = Edges[node].ToArray();
            foreach (Powerline p in lines)
            {
                p.Remove();
            }
        }

        internal void Detach(Powerline edge, IPowerable node1, IPowerable node2)
        {
            PowerGrid splittingPowerGrid = grids[node1.PowerGridInstanceID];

            //let's take care of the easy cases first
            if (Edges[node1].Count == 1 && Edges[node2].Count == 1) //both are each other's leaf
            {
                //so set them both as "unpowered"
                splittingPowerGrid.Remove(node1);
                splittingPowerGrid.Remove(node2);
                Edges.Remove(node1);
                Edges.Remove(node2);
                //and remove this power grid completely
                grids.Clear();
                grids.Remove(splittingPowerGrid.PowerGridInstanceID);
            }
            else if (Edges[node1].Count == 1) //node 1 is a leaf
            {
                splittingPowerGrid.Remove(node1);
                Edges.Remove(node1);
                Edges[node2].Remove(edge);
            }
            else if (Edges[node2].Count == 1) //node 2 is a leaf
            {
                splittingPowerGrid.Remove(node2);
                Edges.Remove(node2);
                Edges[node1].Remove(edge);
            }
            else //node 1 and 2 are connected to a larger graph
            {
                Edges[node1].Remove(edge);
                Edges[node2].Remove(edge);

                BuildNewPowerGrid(node1);
                BuildNewPowerGrid(node2);
                
                splittingPowerGrid.Clear();
                grids.Remove(splittingPowerGrid.PowerGridInstanceID);
            }
        }

        private void BuildNewPowerGrid(IPowerable node1)
        {
            PowerGrid newPG = new PowerGrid();
            Dictionary<IPowerable, bool> visited = new Dictionary<IPowerable, bool>();

            BuildPowerGridVisitPowerNodes(node1, newPG, visited);

            grids.Add(newPG.PowerGridInstanceID, newPG);
        }
        
        private void BuildPowerGridVisitPowerNodes(IPowerable parentNode, PowerGrid parentGrid, Dictionary<IPowerable, bool> visited)
        {
            parentGrid.Add(parentNode);
            visited.Add(parentNode, true);

            foreach(Powerline edge in Edges[parentNode])
            {
#pragma warning disable CS0253 // Possible unintended reference comparison; right hand side needs cast
                IPowerable child = (edge.Data.From == parentNode) ? edge.Data.To as IPowerable : edge.Data.From as IPowerable;
#pragma warning restore CS0253 // Possible unintended reference comparison; right hand side needs cast

                if (!visited.ContainsKey(child))
                    BuildPowerGridVisitPowerNodes(child, parentGrid, visited);
            }
        }

        internal void Tick()
        {
            LastGlobalTickData = new PowerGridTickData();
            foreach (PowerGrid g in grids.Values)
            {
                g.Tick();
                LastGlobalTickData += g.Data;
            }

            if (OnPowerTick != null)
                OnPowerTick();
        }

        internal void HandleElectricalFailure(IRepairable victim)
        {
            if (victim is IPowerConsumer)
            {
                (victim as IPowerConsumer).EmergencyShutdown();
            }

            this.OnElectricalFailureChange(victim);
        }

        internal void OnElectricalFailureChange(IRepairable repairable)
        {
            if (repairable is IPowerConsumer)
            {
                (repairable as IPowerConsumer).RefreshVisualization();
            }

            //batteries don't lose charge, just can't charge/discharge
            //if (victim is IBattery)
            //{
            //    (victim as IBattery).RefreshVisualization();
            //}

            if (repairable is IPowerSupply)
            {
                (repairable as IPowerSupply).RefreshVisualization();
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

    /// <summary>
    /// A snapshot of a power grid's data, as calculated by the sums of the producers/consumers/batteries
    /// Includes formatted strings for display
    /// </summary>
    public struct PowerGridTickData
    {
        public float RatedCapacityWatts;
        public float CurrentCapacityWatts;
        public string CapacityString {
            get {
                if (RatedCapacityWatts == 0f)
                    return "0%";
                else
                    return String.Format("{0:0.##}%", CurrentCapacityWatts / RatedCapacityWatts * 100f);
            }
        }

        public float LoadWatts;
        public float InstalledBatteryWatts;
        public string InstalledBatteryWattHours
        {
            get
            {
                return String.Format("{0:0} Wh", Mathf.Round(InstalledBatteryWatts / SunOrbit.GameMinutesPerGameDay));
            }
        }
        public float CurrentBatteryWatts;
        public string CurrentBatteryWattHours
        {
            get
            {
                return String.Format("{0:0} Wh", Mathf.Round(CurrentBatteryWatts / SunOrbit.GameMinutesPerGameDay));
            }
        }
        public string BatteryString {
            get
            {
                if (InstalledBatteryWatts == 0f)
                    return "0%";
                else
                    return String.Format("{0:0.##}%", CurrentBatteryWatts / InstalledBatteryWatts * 100f);
            }
        }

        public float SurplusWatts;
        public float DeficitWatts;

        /// <summary>
        /// Allows snapshots of individual power grids to be composed into one global structure
        /// This allows us to display the global power statistics
        /// </summary>
        /// <param name="alpha"></param>
        /// <param name="beta"></param>
        /// <returns></returns>
        public static PowerGridTickData operator +(PowerGridTickData alpha, PowerGridTickData beta)
        {
            return new PowerGridTickData()
            {
                RatedCapacityWatts = alpha.RatedCapacityWatts + beta.RatedCapacityWatts,
                CurrentCapacityWatts = alpha.CurrentCapacityWatts + beta.CurrentCapacityWatts,
                LoadWatts = alpha.LoadWatts + beta.LoadWatts,
                InstalledBatteryWatts = alpha.InstalledBatteryWatts + beta.InstalledBatteryWatts,
                CurrentBatteryWatts = alpha.CurrentBatteryWatts + beta.CurrentBatteryWatts,
                SurplusWatts = alpha.SurplusWatts + beta.SurplusWatts,
                DeficitWatts = alpha.DeficitWatts + beta.DeficitWatts
            };
        } 
    }

    public class PowerGrid
    {
        public enum GridMode { Unknown = -99, Blackout = -3, Brownout, BatteryDrain, Nominal = 0, BatteryRecharge }

        internal readonly string PowerGridInstanceID;
        internal GridMode Mode = GridMode.Unknown;
        internal PowerGridTickData Data;

        //if you add anymore lists, update .Usurp
        protected List<IPowerConsumer> Consumers = new List<IPowerConsumer>();
        protected List<IPowerSupply> Producers = new List<IPowerSupply>();
        protected List<IVariablePowerSupply> VariableProducers = new List<IVariablePowerSupply>();
        protected List<IBattery> Batteries = new List<IBattery>();

        public PowerGrid()
        {
            this.PowerGridInstanceID = Guid.NewGuid().ToString();
        }

        internal void Tick()
        {
            GridMode newGridMode = GridMode.Unknown;

            Data.CurrentCapacityWatts = Producers.Sum(x => x.FaultedPercentage > 0f ? 0f : x.WattsGenerated);
            Data.LoadWatts = Consumers.Sum(x => !x.IsOn && x.FaultedPercentage > 0f ? 0f : x.WattsConsumed);
            Data.CurrentBatteryWatts =  Batteries.Sum(x => x.FaultedPercentage > 0f ? 0f : x.EnergyContainer.CurrentAmount);

            Data.SurplusWatts = Data.CurrentCapacityWatts - Data.LoadWatts;
            Data.DeficitWatts = Data.LoadWatts - Data.CurrentCapacityWatts;

            if ((Data.CurrentCapacityWatts + Data.CurrentBatteryWatts == 0f) && Data.LoadWatts > 0f)
            {
                newGridMode = GridMode.Blackout;

                if (Mode != GridMode.Blackout)
                {
                    //GuiBridge.Instance.ComputerAudioSource.PlayOneShot();
                }
            }
            else if (Data.CurrentCapacityWatts > Data.LoadWatts)
            {
                if (Data.SurplusWatts == 0f)
                    newGridMode = GridMode.Nominal;
                else //surplus > 0f
                    newGridMode = GridMode.BatteryRecharge;
            }
            else if (Data.CurrentCapacityWatts < Data.LoadWatts)
            {
                float availableWatts = Data.CurrentCapacityWatts + Data.CurrentBatteryWatts;

                if (availableWatts >= Data.DeficitWatts)
                    newGridMode = GridMode.BatteryDrain;
                else //if (availableWatts < Data.DeficitWatts)
                    newGridMode = GridMode.Brownout;
            }
            else // capacity + battery > 0 || load == 0 && capacity == load
            {
                newGridMode = GridMode.Nominal;
            }

            if (Mode != newGridMode)
            {
                Mode = newGridMode;
                switch (Mode)
                {
                    case GridMode.Blackout:
                        foreach (IPowerConsumer c in Consumers)
                        {
                            c.EmergencyShutdown();
                        }
                        Data.LoadWatts = 0f;
                        Data.SurplusWatts = 0f;
                        break;
                    case GridMode.Brownout:
                        foreach (IPowerConsumer c in Consumers)
                        {
#warning todo: reference priority field during shutdown, sort by it probably
                            if (c.FaultedPercentage > 0f)
                            {
                                //noop
                            }
                            else
                            {
                                c.EmergencyShutdown();

                                Data.DeficitWatts -= c.WattsConsumed;

                                c.RefreshVisualization();

                                //stop the brownout when we have a new equilibrium
                                if (Data.CurrentCapacityWatts + Data.CurrentBatteryWatts > Data.DeficitWatts)
                                    break;
                                //but wait until next tick to sort itself out
                            }
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
                float recharged = Data.SurplusWatts;
                //todo: some sort of priority
                //so you can recharge your rover faster probably
                foreach (IBattery batt in Batteries)
                {
                    if (batt.FaultedPercentage > 0f)
                    {
                        //noop
                    }
                    else
                    {
                        recharged = batt.EnergyContainer.Push(recharged);
                        batt.RefreshVisualization();

                        if (recharged <= 0)
                            break;
                    }
                }
            }
            else if (Mode == GridMode.BatteryDrain)
            {
                float drained = Data.DeficitWatts;
                foreach (IBattery batt in Batteries)
                {
                    if (batt.FaultedPercentage > 0f)
                    {
                        //noop
                    }
                    else
                    {
                        drained -= batt.EnergyContainer.Pull(drained);
                        batt.RefreshVisualization();

                        if (drained <= 0)
                            break;
                    }
                }
            }

            foreach(IPowerSupply s in VariableProducers)
            {
                s.RefreshVisualization();
            }
        }

        internal void Add(IPowerable mod)
        {
            if (mod is IPowerSupply)
            {
                Producers.Add(mod as IPowerSupply);

                if (mod is IVariablePowerSupply)
                {
                    VariableProducers.Add(mod as IVariablePowerSupply);
                    Data.RatedCapacityWatts += (mod as IVariablePowerSupply).MaximumWattsGenerated;
                }
                else
                {
                    Data.RatedCapacityWatts += (mod as IPowerSupply).WattsGenerated;
                }
            }
            else if (mod is IPowerConsumer)
            {
                Consumers.Add(mod as IPowerConsumer);

                //consumers need to have their HasPower state refreshed
                switch (Mode)
                {
                    case GridMode.BatteryDrain:
                    case GridMode.BatteryRecharge:
                    case GridMode.Nominal:
                        (mod as IPowerConsumer).TurnOnPower();
                        break;
                }
            }

            if (mod is IBattery)
            {
                Batteries.Add(mod as IBattery);
                Data.InstalledBatteryWatts += (mod as IBattery).EnergyContainer.TotalCapacity;
            }
            mod.PowerGridInstanceID = this.PowerGridInstanceID;
        }

        internal void Remove(IPowerable mod)
        {
            if (mod is IPowerSupply)
            {
                Producers.Remove(mod as IPowerSupply);

                if (mod is IVariablePowerSupply)
                {
                    VariableProducers.Remove(mod as IVariablePowerSupply);
                    Data.RatedCapacityWatts -= (mod as IVariablePowerSupply).MaximumWattsGenerated;
                }
                else
                {
                    Data.RatedCapacityWatts -= (mod as IPowerSupply).WattsGenerated;
                }
            }
            else if (mod is IPowerConsumer)
            {
                Consumers.Remove(mod as IPowerConsumer);
                
                (mod as IPowerConsumer).EmergencyShutdown();
            }

            if (mod is IBattery)
            {
                Batteries.Remove(mod as IBattery);
                Data.InstalledBatteryWatts -= (mod as IBattery).EnergyContainer.TotalCapacity;
            }

            mod.PowerGridInstanceID = "";
        }

        private void SetPowerableParentToMe(IPowerable p) { p.PowerGridInstanceID = this.PowerGridInstanceID; }

        /// <summary>
        /// Take all the children of the other powergrid
        /// </summary>
        /// <param name="other"></param>
        internal void Usurp(PowerGrid other)
        {
            this.Data += other.Data;
            other.Consumers.ForEach(SetPowerableParentToMe);
            Consumers.AddRange(other.Consumers);

            other.Producers.ForEach(SetPowerableParentToMe);
            Producers.AddRange(other.Producers);
            //since variable producers are just producers, we don't need to set powerable parent on them all
            VariableProducers.AddRange(other.VariableProducers);

            other.Batteries.ForEach(SetPowerableParentToMe);
            Batteries.AddRange(other.Batteries);
        }

        internal void Clear()
        {
            Consumers.Clear();
            Batteries.Clear();
            Producers.Clear();
            VariableProducers.Clear();
        }
    }
}