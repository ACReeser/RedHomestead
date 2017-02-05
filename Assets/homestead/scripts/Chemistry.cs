using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace RedHomestead.Simulation
{
    public enum Energy { Electrical, Thermal }
    
    //todo: resource could be flags to allow quick "is this in requirements", only if 64 or less resources tho
    public enum Matter {
        Hydrogen = -6, Oxygen, CarbonMonoxide, CarbonDioxide, Methane, Water,
        Unspecified = 0,
        Steel = 1, SiliconWafers, Aluminium, Biomass, OrganicMeal, MealPowder, MealShake, RationMeal,
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
        public Matter Type { get; set; }
        public int Count { get; set; }

        public ResourceEntry(int count, Matter type)
        {
            this.Type = type;
            this.Count = count;
        }
    }

    public static class MatterExtensions
    {
        //http://www.engineeringtoolbox.com/density-solids-d_1265.html
        private static Dictionary<Matter, float> DensityKgPerCubicMeter = new Dictionary<Matter, float>()
        {
            { Matter.Steel, 7850f },
            { Matter.SiliconWafers, 2330f },
            { Matter.Aluminium, 2800f },
            { Matter.Bauxite, 1280f },
            { Matter.Biomass, 760f }, //same as wheat
            { Matter.Silica, 2100 },
            { Matter.Copper, 8790 },
            { Matter.Uranium, 19100 },
            { Matter.Polyethylene, 960 },
            { Matter.Platinum, 21500 },
            { Matter.Gold, 19290 },
            { Matter.Silver, 10500 },
            { Matter.Glass, 2600 },
            { Matter.MealShake, 1100 }, //slightly denser than water
            { Matter.MealPowder, 1600 }, //same as sand??
            { Matter.RationMeal, 870 }, //same as butter
            { Matter.OrganicMeal, 950 }, //same as beef tallow??? what am i thinking
        };

        public static float Kilograms(this Matter r, float volumeCubicMeter = 1f)
        {
            return DensityKgPerCubicMeter[r] * volumeCubicMeter;
        }

        public static Sprite Sprite(this Matter r)
        {
            int i = (int)r;
            if (i < 0)
            {
                return GuiBridge.Instance.Icons.CompoundIcons[i + 6]; //6 "compounds" are negative
            }
            else
            {
                return GuiBridge.Instance.Icons.ResourceIcons[i - 1]; // 1 unspecified
            }
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

    public class LocalMatterHistory : History<Matter>
    {
        public override void Produce(Matter type, float additionalAmount)
        {
            base.Produce(type, additionalAmount);
            GlobalHistory.Compound.Produce(type, additionalAmount);
        }

        public override void Consume(Matter type, float additionalAmount)
        {
            base.Consume(type, additionalAmount);
            GlobalHistory.Compound.Consume(type, additionalAmount);
        }
    }

    public static class GlobalHistory
    {
        public static History<Energy> Energy = new History<Simulation.Energy>();
        public static History<Matter> Compound = new History<Simulation.Matter>();
        public static History<Matter> Resource = new History<Simulation.Matter>();
    }
}
