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
        Canvas,
        Iron
    }

    public class ResourceEntry
    {
        public Matter Type { get; set; }
        public float Count { get; set; }

        public ResourceEntry(float count, Matter type)
        {
            this.Type = type;
            this.Count = count;
        }

        public override string ToString()
        {
            return String.Format("{0} x{1:0.##}", this.Type, this.Count);
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

        public static float KgPerMeal(this Matter meal)
        {
            float denominator = meal.MealsPerCubicMeter();

            if (denominator != 0)
            {
                return meal.Kilograms() / denominator;
            }
            else
            {
                return 0;
            }
        }

        public static float MealsPerCubicMeter(this Matter meal, float cubicMeters = 1f)
        {
            switch (meal)
            {
                case Matter.MealShake:
                case Matter.MealPowder:
                    return 36f;
                case Matter.OrganicMeal:
                case Matter.RationMeal:
                case Matter.Biomass:
                    return 18f;
                default:
                    return 0;
            }
        }

        public static float CubicMetersPerMeal(this Matter meal)
        {
            float denominator = meal.MealsPerCubicMeter();

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
                case Matter.MealShake:
                    return 600f;
                case Matter.OrganicMeal:
                case Matter.RationMeal:
                    return 1200f;
                default:
                    return 0f;
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
}
