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
        Bauxite,
        Canvas
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
        //https://www.wolframalpha.com/input/?i=density+of+hydrogen+at+0+deg+C+and+350+bar
        private static Dictionary<Matter, float> DensityKgPerCubicMeter = new Dictionary<Matter, float>()
        {
            //gases and water
            { Matter.Hydrogen, 24f }, // 0 deg and 350 bar
            { Matter.Oxygen, 498f }, // 0 deg and 350 bar
            { Matter.Methane, 258f }, // 0 deg and 350 bar
            { Matter.Water, 1000f }, // 0.1 deg and 1 atm
            //ores
            { Matter.Bauxite, 1280f },
            //metals
            { Matter.Steel, 7850f },
            { Matter.Aluminium, 2800f },
            { Matter.Copper, 8790 },
            { Matter.Uranium, 19100 },
            { Matter.Platinum, 21500 },
            { Matter.Gold, 19290 },
            { Matter.Silver, 10500 },
            //minerals
            { Matter.Silica, 2100 },
            //processed
            { Matter.SiliconWafers, 2330f },
            { Matter.Glass, 2600 },
            { Matter.MealShake, 1100 }, //slightly denser than water
            ///organics
            { Matter.Biomass, 760f }, //same as wheat
            { Matter.Polyethylene, 960 },
            { Matter.Canvas, 1500 }, //same as starch, artificial wool, heavy paper
            { Matter.MealPowder, 1600 }, //same as sand??
            { Matter.RationMeal, 870 }, //same as butter
            { Matter.OrganicMeal, 950 }, //same as beef tallow??? what am i thinking
        };

        public static float Kilograms(this Matter r, float? volumeCubicMeter = null)
        {
            if (volumeCubicMeter.HasValue)
            {
                return DensityKgPerCubicMeter[r] * volumeCubicMeter.Value;
            }
            else
            {
                return DensityKgPerCubicMeter[r] * r.BaseCubicMeters();
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
                return GuiBridge.Instance.Icons.CompoundIcons[i + 6]; //6 "compounds" are negative
            }
            else
            {
                return GuiBridge.Instance.Icons.ResourceIcons[i - 1]; // 1 unspecified
            }
        }

        public static bool IsStoredInHabitat(this Matter r) {
            switch (r)
            {
                case Matter.OrganicMeal:
                case Matter.MealPowder:
                case Matter.MealShake:
                case Matter.RationMeal:
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

    public interface ICrateSnapper
    {
        void DetachCrate(ResourceComponent detaching);
    }
}
