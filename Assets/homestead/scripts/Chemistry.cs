using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace RedHomestead.Simulation
{
    public enum Energy { Electrical, Thermal }

    public enum Compound { Unspecified = -1, Hydrogen, Oxygen, CarbonMonoxide, CarbonDioxide, Methane, Water }

    //todo: resource could be flags to allow quick "is this in requirements", only if 64 or less resources tho
    public enum Resource { Steel = 1, SiliconWafers, Aluminium, Biomass, OrganicMeal, MealPowder, MealShake, RationMeal,
        Silica,
        Copper,
        Uranium,
        Polyethylene,
        Platinum,
        Glass,
        Gold,
        Silver,
        Bauxite
    }

    public class ResourceEntry
    {
        public Resource Type { get; set; }
        public int Count { get; set; }

        public ResourceEntry(int count, Resource type)
        {
            this.Type = type;
            this.Count = count;
        }
    }

    public static class MatterExtensions
    {
        //http://www.engineeringtoolbox.com/density-solids-d_1265.html
        private static Dictionary<Resource, float> DensityKgPerCubicMeter = new Dictionary<Resource, float>()
        {
            { Resource.Steel, 7850f },
            { Resource.SiliconWafers, 2330f },
            { Resource.Aluminium, 2800f },
            { Resource.Bauxite, 1280f },
            { Resource.Biomass, 760f }, //same as wheat
            { Resource.Silica, 2100 },
            { Resource.Copper, 8790 },
            { Resource.Uranium, 19100 },
            { Resource.Polyethylene, 960 },
            { Resource.Platinum, 21500 },
            { Resource.Gold, 19290 },
            { Resource.Silver, 10500 },
            { Resource.Glass, 2600 },
            { Resource.MealShake, 1100 }, //slightly denser than water
            { Resource.MealPowder, 1600 }, //same as sand??
            { Resource.RationMeal, 870 }, //same as butter
            { Resource.OrganicMeal, 950 }, //same as beef tallow??? what am i thinking
        };

        public static float Kilograms(this Resource r, float volumeCubicMeter = 1f)
        {
            return DensityKgPerCubicMeter[r] * volumeCubicMeter;
        }

        public static Sprite Sprite(this Resource r)
        {
            return GuiBridge.Instance.Icons.ResourceIcons[(int)r];
        }

        public static Sprite Sprite(this Compound r)
        {
            return GuiBridge.Instance.Icons.CompoundIcons[(int)r];
        }
    }


    public class Tracker
    {
        public float Produced { get; set; }
        public float Consumed { get; set; }
    }
    
    //wish we could where T: enum
    public class History<T> where T : struct
    {
        private Dictionary<T, Tracker> _history = new Dictionary<T, Tracker>();

        public History()
        {
            System.Type seed = typeof(T);
            if (!seed.IsEnum)
                throw new ArgumentException("Cannot use non enum");

            foreach(T e in System.Enum.GetValues(seed))
            {
                _history[e] = new Tracker();
            }
        }

        public virtual void Produce(T type, float additionalAmount)
        {
            _history[type].Produced += additionalAmount;
        }

        public virtual void Consume(T type, float additionalAmount)
        {
            _history[type].Consumed += additionalAmount;
        }

        public Tracker this[T key]
        {
            get
            {
                return _history[key];
            }
        }
    }

    public class LocalEnergyHistory : History<Energy>
    {
        public override void Produce(Energy type, float additionalAmount)
        {
            base.Produce(type, additionalAmount);
            GlobalHistory.Energy.Produce(type, additionalAmount);
        }

        public override void Consume(Energy type, float additionalAmount)
        {
            base.Consume(type, additionalAmount);
            GlobalHistory.Energy.Consume(type, additionalAmount);
        }
    }

    public class LocalCompoundHistory : History<Compound>
    {
        public override void Produce(Compound type, float additionalAmount)
        {
            base.Produce(type, additionalAmount);
            GlobalHistory.Compound.Produce(type, additionalAmount);
        }

        public override void Consume(Compound type, float additionalAmount)
        {
            base.Consume(type, additionalAmount);
            GlobalHistory.Compound.Consume(type, additionalAmount);
        }
    }

    public static class GlobalHistory
    {
        public static History<Energy> Energy = new History<Simulation.Energy>();
        public static History<Compound> Compound = new History<Simulation.Compound>();
        public static History<Resource> Resource = new History<Simulation.Resource>();
    }
}
