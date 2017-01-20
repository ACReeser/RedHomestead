using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace RedHomestead.Simulation
{
    public enum Energy { Electrical, Thermal }

    public enum Compound { Unspecified = -1, Hydrogen, Oxygen, CarbonMonoxide, CarbonDioxide, Methane, Water }

    //todo: resource could be flags to allow quick "is this in requirements", only if 64 or less resources tho
    public enum Resource { Steel, SiliconWafers, Aluminium, Biomass, OrganicMeal, MealPowder, MealShake, RationMeal }

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
