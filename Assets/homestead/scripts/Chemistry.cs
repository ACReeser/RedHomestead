using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using RedHomestead.Industry;

namespace RedHomestead.Simulation
{
    public enum ContainerSize { Quarter = 1, Full = 4 } //, Quadratic = 16 }

    public enum Energy { Electrical, Thermal }
    
    //todo: resource could be flags to allow quick "is this in requirements", only if 64 or less resources tho
    public enum Matter {
        Hydrogen = -6, Oxygen, CarbonMonoxide, CarbonDioxide, Methane, Water,
        Unspecified = 0,
        //ores on 0, ingots on 1, powders on 2
        IronOre = 1,         Iron = 10,      IronPowder = 20,
        Bauxite = 2,   Aluminium  = 11, AluminiumPowder = 21,
        CopperOre = 3,     Copper = 12,    CopperPowder = 22,
        NickelOre = 4,     Nickel = 13,    NickelPowder = 23,
        SilverOre = 5,     Silver = 14,    SilverPowder = 24,
        MagnesiumOre = 6,
        GoldOre = 7,         Gold = 17,      GoldPowder = 27,
        PlatinumOre = 8, Platinum = 18,
                            Steel = 19,     SteelPowder = 29,
        //metallic elements
        Sulfur = 30,
        Silica,
        Uranium,

        //plastic compounds
        Polyethylene = 40,
        
        //manufactured goods
        Glass = 50,
        SiliconWafers, SolarPanels, Microchips,
        Canvas, CopperWire,

        //organics
        Biomass = 70,
        Produce, OrganicMeals,
        MealPowders, MealShakes,
        Flax,

        //processed
        RationMeals,

        //building components
        PressureCanvas = 80,
        Piping, 
        Bulkhead,
        IronSheeting,
        IronBeams,
        ElectricMotor
    }
    
    public interface IResourceEntry
    {
        Matter Type { get; set; }
        
        float AmountByVolume { get; }
        int AmountByUnits { get; }

        string ToString();
        string ToStringWithAvailableVolume(float availableVolume);
    }

    public class ResourceVolumeEntry: IResourceEntry
    {
        public Matter Type { get; set; }
        //resource in Units volume
        public float AmountByVolume { get; private set; }
        public int AmountByUnits { get { return Mathf.RoundToInt(AmountByVolume * Type.UnitsPerCubicMeter()); } }

        public ResourceVolumeEntry(float volume, Matter type)
        {
            this.Type = type;
            this.AmountByVolume = volume;
        }

        public override string ToString()
        {
            return String.Format("{0} x{1:0.##}", this.Type, this.AmountByVolume);
        }
        public string ToStringWithAvailableVolume(float availableVolume)
        {
            return String.Format("{0} {2:0}/{1:0.##}", this.Type, this.AmountByVolume, availableVolume);
        }
    }

    public class ResourceUnitEntry: IResourceEntry
    {
        public Matter Type { get; set; }
        //resource in subUnits
        public int AmountByUnits { get; private set; }
        public float AmountByVolume { get { return Mathf.RoundToInt(AmountByUnits / Type.UnitsPerCubicMeter()); } }

        public ResourceUnitEntry(int count, Matter type)
        {
            this.Type = type;
            this.AmountByUnits = count;
        }

        public override string ToString()
        {
            return String.Format("{0} x{1:0}", this.Type, this.AmountByUnits);
        }
        public string ToStringWithAvailableVolume(float availableVolume)
        {
            return String.Format("{0} {2:0}/{1:0}", this.Type, this.AmountByVolume, availableVolume * Type.UnitsPerCubicMeter());
        }
    }

    public static class MatterExtensions
    {
        //http://www.engineeringtoolbox.com/density-solids-d_1265.html
        //https://www.wolframalpha.com/input/?i=density+of+hydrogen+at+0+deg+C+and+350+bar
        private static float DensityKgPerCubicMeter(this Matter mat)
        {
            switch(mat)
            {
                //gases and water
                case Matter.Hydrogen:
                    return 24f; // 0 deg and 350 bar
                case Matter.Oxygen: return 498f; // 0 deg and 350 bar
                case Matter.Methane: return 258f; // 0 deg and 350 bar
                case Matter.Water: return 1000f; // 0.1 deg and 1 atm

                //ores
                case Matter.Bauxite: return 1280f;
                case Matter.Aluminium: return 2800f;

                // metals
                case Matter.IronOre: 
                case Matter.IronPowder:
                    return 7850f;
                case Matter.Steel:
                case Matter.SteelPowder:
                    return 7850f;
                case Matter.Copper:
                case Matter.CopperPowder:
                    return 8790;

                case Matter.Uranium: return 19100;
                case Matter.Platinum: return 21500;
                case Matter.Gold: return 19290;
                case Matter.Silver: return 10500;

                // minerals
                case Matter.Silica: return 2100;
                case Matter.Glass: return 2600;

                // processed
                case Matter.SiliconWafers:
                case Matter.SolarPanels:
                    return 2330f;

                // organics
                case Matter.Polyethylene: return 960;
                case Matter.Canvas: return 1500; //same as starch: return artificial wool: return heavy paper
                
                // food
                case Matter.Biomass: return 1000f; //same as wheat
                case Matter.MealPowders: return 1600; //same as sand??
                case Matter.MealShakes: return 1100; //slightly denser than water
                case Matter.OrganicMeals: return 950; //same as beef tallow??? what am i thinking
                case Matter.Produce: return 950; //same as beef tallow??? what am i thinking
                case Matter.RationMeals: return 870; //same as butter

                default: return 1000;
            }
        }

        public static int MaxMatter()
        {
            int max = -1;
            var values = Enum.GetValues(typeof(Matter));
            for (int i = 0; i < values.Length; i++)
            {
                int v = (int)values.GetValue(i);
                max = max > v ? max : v;
            }

            return max;
        }

        public static float Kilograms(this Matter matter, float? volumeCubicMeter = null)
        {
            if (volumeCubicMeter.HasValue)
            {
                return matter.DensityKgPerCubicMeter() * volumeCubicMeter.Value;
            }
            else
            {
                return matter.DensityKgPerCubicMeter() * matter.BaseCubicMeters();
            }
        }

        internal static Matter FromValveTag(string valveTag)
        {
            switch (valveTag)
            {
                case "watervalve":
                    return Matter.Water;
                case "oxygenvalve":
                    return Matter.Oxygen;
                case "hydrogenvalve":
                    return Matter.Hydrogen;
                case "carbondioxidevalve":
                    return Matter.CarbonDioxide;
                case "methanevalve":
                    return Matter.Methane;
            }
            return Matter.Unspecified;
        }

        public static float KgPerUnit(this Matter unit)
        {
            float denominator = unit.UnitsPerCubicMeter();

            if (denominator != 0)
            {
                return unit.Kilograms() / denominator;
            }
            else
            {
                return 0;
            }
        }

        public static float UnitsPerCubicMeter(this Matter matter, float cubicMeters = 1f)
        {
            switch (matter)
            {
                case Matter.MealShakes:
                case Matter.MealPowders:
                    return 36f / cubicMeters;
                case Matter.OrganicMeals:
                case Matter.RationMeals:
                case Matter.Biomass:
                case Matter.Produce:
                    return 18f / cubicMeters;
                case Matter.SolarPanels:
                case Matter.Piping:
                    return 4f / cubicMeters;
                case Matter.IronSheeting:
                case Matter.Canvas:
                case Matter.PressureCanvas:
                case Matter.Glass:
                    return 2f / cubicMeters;
                default:
                    return 1f / cubicMeters;
            }
        }

        public static float CubicMetersPerUnit(this Matter matter)
        {
            float denominator = matter.UnitsPerCubicMeter();

            if (denominator != 0)
            {
                return 1 / denominator;
            }
            else
            {
                return 0;
            }
        }

        public static float Calories(this Matter meal)
        {
            switch (meal)
            {
                case Matter.MealShakes:
                    return 600f;
                case Matter.OrganicMeals:
                case Matter.RationMeals:
                    return 1200f;
                default:
                    return 0f;
            }
        }

        public static bool IsFurnaceOutput(this Matter matt)
        {
            return matt != Matter.Polyethylene && matt.Is3DPrinterFeedstock();
        }

        public static bool Is3DPrinterFeedstock(this Matter matt)
        {
            switch (matt)
            {
                case Matter.IronPowder:
                case Matter.AluminiumPowder:
                case Matter.CopperPowder:
                case Matter.NickelPowder:
                case Matter.SilverPowder:
                case Matter.GoldPowder:
                case Matter.SteelPowder:

                case Matter.Canvas:
                case Matter.Polyethylene:

                case Matter.CopperWire:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsRawMaterial(this Matter matt)
        {
            switch (matt) {
                case Matter.IronOre:
                case Matter.Bauxite:
                case Matter.CopperOre:
                case Matter.NickelOre:
                case Matter.SilverOre:
                case Matter.MagnesiumOre:
                case Matter.GoldOre:
                case Matter.PlatinumOre:
                case Matter.Steel:
                case Matter.Sulfur:
                case Matter.Silica:
                case Matter.Uranium:
                    return true;
                default:
                    return false;
            }
        }

        public static Matter MatchingPowder(this Matter ore)
        {
            switch (ore)
            {
                case Matter.IronOre:
                    return Matter.IronPowder;
                case Matter.Bauxite:
                    return Matter.AluminiumPowder;
                case Matter.CopperOre:
                    return Matter.CopperPowder;
                case Matter.NickelOre:
                    return Matter.NickelPowder;
                case Matter.SilverOre:
                    return Matter.SilverPowder;
                //case Matter.MagnesiumOre:
                //    return Matter.;
                case Matter.GoldOre:
                    return Matter.GoldPowder;
                //case Matter.PlatinumOre:
                //    return Matter.;
                default:
                    return Matter.Unspecified;
            }
        }

        public static Matter MatchingOre(this Matter refined)
        {
            switch (refined)
            {
                case Matter.Iron:
                case Matter.IronPowder:
                    return Matter.IronOre;
                case Matter.Aluminium:
                case Matter.AluminiumPowder:
                    return Matter.Bauxite;
                case Matter.Copper:
                case Matter.CopperPowder:
                    return Matter.CopperOre;
                case Matter.Nickel:
                case Matter.NickelPowder:
                    return Matter.NickelOre;
                case Matter.Silver:
                case Matter.SilverPowder:
                    return Matter.SilverOre;
                case Matter.Gold:
                case Matter.GoldPowder:
                    return Matter.GoldOre;
                case Matter.Platinum:
                    return Matter.PlatinumOre;
                default:
                    return Matter.Unspecified;
            }
        }

        public static float BaseCubicMeters(this Matter r)
        {
            return 1f;
        }

        public static Sprite Sprite(this Matter r)
        {
            int i = (int)r;
            if (i < 0)
            {
                return IconAtlas.Instance.CompoundIcons[i + 6]; //6 "compounds" are negative
            }
            else
            {
                return IconAtlas.Instance.ResourceIcons[i]; // 1 unspecified
            }
        }

        public static Sprite AtlasSprite(this Matter r)
        {
            int i = (int)r;
            if (i < 0)
            {
                return IconAtlas.Instance.CompoundIcons[i + 6]; //6 "compounds" are negative
            }
            else
            {
                return IconAtlas.Instance.ResourceIcons[i - 1]; // 1 unspecified
            }
        }

        public static bool IsStoredInHabitat(this Matter r) {
            switch (r)
            {
                case Matter.OrganicMeals:
                case Matter.MealPowders:
                case Matter.MealShakes:
                case Matter.RationMeals:
                case Matter.Produce:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsPressureVessel(this Matter r)
        {
            switch (r)
            {
                case Matter.Hydrogen:
                case Matter.Oxygen:
                case Matter.CarbonDioxide:
                case Matter.CarbonMonoxide:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsTankVessel(this Matter r)
        {
            switch (r)
            {
                case Matter.Water:
                    return true;
                default:
                    return false;
            }
        }
        
        public static float RandomPurity(this Matter ore)
        {
            switch (ore)
            {
                case Matter.IronOre:
                    return UnityEngine.Random.Range(.25f, .45f);
                case Matter.Bauxite:
                    return UnityEngine.Random.Range(.25f, .45f);
                case Matter.NickelOre:
                    return UnityEngine.Random.Range(.1f, .25f);
                case Matter.CopperOre:
                    return UnityEngine.Random.Range(.1f, .2f);

                case Matter.Silica:
                    return UnityEngine.Random.Range(.15f, .35f);
                case Matter.Sulfur:
                    return UnityEngine.Random.Range(.15f, .35f);
                default:
                    return 1f;
            }
        }
    }

    public class FormulaComponent
    {
        public ResourceContainer Container;
        public float ReactionRate;
        public Matter Matter;
        public bool Required;
        public bool Optional { get { return !Required; } }
    }

    public class Formula
    {
        private readonly ResourceContainerDictionary buffer;
        private readonly FormulaComponent[] inputs;
        private readonly FormulaComponent[] outputs;
        private int RequiredOutputMinimum = 0;
        private int RequiredOutputCount = 0;

        public Formula(ResourceContainerDictionary buffer, FormulaComponent output, params FormulaComponent[] inputs)
        {
            this.buffer = buffer;
            this.inputs = inputs;
            this.outputs = new FormulaComponent[] { output };

            foreach(FormulaComponent component in outputs)
            {
                if (component.Required)
                    RequiredOutputMinimum++;
            }
        }

        public void AddSink(ISink sink, Matter type)
        {
            if (sink.HasContainerFor(type))
            {
                AddInput(sink.Get(type));
            }
        }

        public void AddInput(ResourceContainer container)
        {
            _MatchOnEmptyOfType(inputs, container.MatterType, (component) => component.Container = container);
        }

        public void RemoveInput(ResourceContainer container)
        {
            _MatchExisting(inputs, container, (component) => {
                component.Container = null;
            });
        }

        public void AddOutput(ResourceContainer container)
        {
            _MatchOnEmptyOfType(outputs, container.MatterType, (component) =>
            {
                component.Container = container;

                if (component.Required)
                    RequiredOutputCount++;
            });
        }

        public void RemoveOutput(ResourceContainer container)
        {
            _MatchExisting(outputs, container, (component) => {
                RemoveOutput(component);
            });
        }

        private void RemoveOutput(FormulaComponent component)
        {
            component.Container = null;

            if (component.Required)
                RequiredOutputCount--;
        }

        public void _MatchOnEmptyOfType(FormulaComponent[] collection, Matter matterType, Action<FormulaComponent> onMatch)
        {
            foreach (FormulaComponent component in inputs)
            {
                if (component.Matter == matterType && component.Container == null)
                {
                    onMatch(component);
                    break;
                }
            }
        }

        public void _MatchExisting(FormulaComponent[] collection, ResourceContainer toMatch, Action<FormulaComponent> onMatch)
        {
            foreach (FormulaComponent component in inputs)
            {
                if (component.Container == toMatch)
                {
                    onMatch(component);
                    break;
                }
            }
        }

        public bool TryCombine()
        {
            if (RequiredOutputCount < RequiredOutputMinimum)
                return false;

            foreach(FormulaComponent component in inputs)
            {
                if (component.Container == null)
                    return false;
                //already have it in buffer (from previous ticks)
                else if (buffer[component.Container.MatterType].CurrentAmount >= component.ReactionRate)
                    continue;
                //can't get enough to react from buffer+container
                else if (buffer[component.Container.MatterType].CurrentAmount + component.Container.CurrentAmount <= component.ReactionRate)
                    return false;
                //need a lil more
                else
                    buffer[component.Container.MatterType].FillFrom(component.Container, component.ReactionRate);
            }

            //if we got this far we have enough to react
            foreach(FormulaComponent component in inputs)
            {
                //so pull from all buffers
                buffer[component.Container.MatterType].Pull(component.ReactionRate);
            }

            //if we got this far we have enough to react
            foreach (FormulaComponent component in outputs)
            {
                if (component.Container != null)
                {
                    component.Container.Push(component.ReactionRate);
                }
            }

            return true;
        }

        internal void ClearContainers()
        {
            foreach (FormulaComponent component in inputs)
            {
                component.Container = null;
            }
            foreach (FormulaComponent component in outputs)
            {
                RemoveOutput(component);
            }
        }
    }

    [Serializable]
    public class Tracker
    {
        //minifying to save us some size on disk and cycles
        [SerializeField]
        private float P;
        public float Produced { get { return P; } set { P = value; } }
        [SerializeField]
        private float C;
        public float Consumed { get { return C; } set { C = value; } }
    }
    
    [Serializable]
    public class SerializableHistory : SerializableDictionary<int, Tracker> { }

    //wish we could where T: enum
    public class History<T> where T : struct, IConvertible
    {
        public SerializableHistory _history = new SerializableHistory();

        public History()
        {
            System.Type seed = typeof(T);
            if (!seed.IsEnum)
                throw new ArgumentException("Cannot use non enum");

            foreach (T e in System.Enum.GetValues(seed))
            {
                _history[Convert.ToInt32(e)] = new Tracker();
            }
        }

        public virtual void Produce(T type, float additionalAmount)
        {
            _history[Convert.ToInt32(type)].Produced += additionalAmount;
        }

        public virtual void Consume(T type, float additionalAmount)
        {
            _history[Convert.ToInt32(type)].Consumed += additionalAmount;
        }

        public Tracker this[T key]
        {
            get
            {
                return _history[Convert.ToInt32(key)];
            }
        }
    }
    
    [Serializable]
    public class EnergyHistory: History<Energy> { }
    [Serializable]
    public class MatterHistory : History<Matter> { }

    [Serializable]
    public class LocalEnergyHistory : EnergyHistory
    {
        public override void Produce(Energy type, float additionalAmount)
        {
            base.Produce(type, additionalAmount);
            Persistence.Game.Current.History.Energy.Produce(type, additionalAmount);
        }

        public override void Consume(Energy type, float additionalAmount)
        {
            base.Consume(type, additionalAmount);
            Persistence.Game.Current.History.Energy.Consume(type, additionalAmount);
        }
    }

    [Serializable]
    public class LocalMatterHistory : MatterHistory
    {
        public override void Produce(Matter type, float additionalAmount)
        {
            base.Produce(type, additionalAmount);
            Persistence.Game.Current.History.Matter.Produce(type, additionalAmount);
        }

        public override void Consume(Matter type, float additionalAmount)
        {
            base.Consume(type, additionalAmount);
            Persistence.Game.Current.History.Matter.Consume(type, additionalAmount);
        }
    }

    [Serializable]
    public class GlobalHistory
    {
        public EnergyHistory Energy = new EnergyHistory();
        public MatterHistory Matter = new MatterHistory();
    }

    public interface ICrateSnapper
    {
        void DetachCrate(IMovableSnappable detaching);
        Transform transform { get; }
    }
    
    [Serializable]
    public class Container
    {
        public Container() { }
        public Container(float initialAmountUnits, float capacityUnits)
        {
            this.Amount = initialAmountUnits;
            this.TotalCapacity = capacityUnits;
        }
        public Container(float Units, ContainerSize size = ContainerSize.Full)
        {
            this.Amount = Units;
            this.TotalCapacity = (float)size / 4f;
        }

        //Serializable
        public float TotalCapacity = 1f;
        [SerializeField]
        protected float Amount;

        public float CurrentAmount { get { return Amount; } }

        public float UtilizationPercentage
        {
            get
            {
                if (TotalCapacity <= 0)
                    return 0;

                return Amount / TotalCapacity;
            }
        }

        public string UtilizationPercentageString()
        {
            return (int)(UtilizationPercentage * 100) + "%";
        }

        public float AvailableCapacity
        {
            get
            {
                return TotalCapacity - Amount;
            }
        }

        /// <summary>
        /// tries to push an amount into the container, returns leftovers
        /// </summary>
        /// <param name="pushAmount"></param>
        /// <returns>the amount unable to be stored in the container</returns>
        public float Push(float pushAmount)
        {
            if (AvailableCapacity > 0)
            {
                Amount += pushAmount;

                if (Amount > TotalCapacity)
                {
                    float overage = Amount - TotalCapacity;

                    Amount = TotalCapacity;

                    return overage;
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                return pushAmount;
            }
        }

        /// <summary>
        /// tries to pull an amount from the container, return amount actually pulled
        /// </summary>
        /// <param name="pullAmount"></param>
        /// <returns>the amount actually pulled</returns>
        public float Pull(float pullAmount)
        {
            if (Amount <= 0)
            {
                return 0;
            }
            else
            {
                if (Amount > pullAmount)
                {
                    Amount -= pullAmount;

                    return pullAmount;
                }
                else
                {
                    float allToGive = Amount;

                    Amount = 0;

                    return allToGive;
                }
            }
        }
        
        /// <summary>
        /// Tries to pull an amount, returns whether pull succeeded within bounds (due to float inaccuracy)
        /// </summary>
        /// <param name="pullAmount"></param>
        /// <returns></returns>
        public bool TryConsumeVolume(float pullAmount)
        {
            float pulled = this.Pull(pullAmount);
            bool pullSuccess = (Math.Abs(pullAmount - pulled) <= 0.000001f);
            if (!pullSuccess)
                this.Push(pulled);

            return pullSuccess;
        }

        public void FillFrom(ResourceContainer source, float transferAmount)
        {
            this.Push(source.Pull(transferAmount));
        }
    }

    [Serializable]
    public class ResourceContainer : Container
    {
        public ResourceContainer() { }
        public ResourceContainer(float initialAmount) : base(initialAmount) { }
        public ResourceContainer(Matter type, float initialAmount) : base(initialAmount)
        {
            this.MatterType = type;
        }

        public ResourceContainer(Matter type, float initialAmount, ContainerSize size) : base(initialAmount, size)
        {
            this.MatterType = type;
        }

        //Serializable
        public Matter MatterType;
        public float AvgPurity = 0f;

        public float Push(float pushAmount, float purityOfBatch)
        {
            float initial = CurrentAmount;
            float leftover = this.Push(pushAmount);
            float newlyAdded = pushAmount - leftover;

            float numer = (initial * AvgPurity) + (newlyAdded * purityOfBatch);
            AvgPurity = numer / CurrentAmount;
            
            return leftover;
        }
    }

    [Serializable]
    public class EnergyContainer : Container
    {
        public EnergyContainer() { }
        public EnergyContainer(float initialAmount) : base(initialAmount) { }

        //Serializable
        public Energy EnergyType;
    }
}
