using RedHomestead.Electricity;
using RedHomestead.Industry;
using RedHomestead.Simulation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace RedHomestead.Agriculture{
    
    public interface IFarm: ISink
    {
        float WaterConsumptionPerTickInUnits { get; }
    }

    public abstract class FarmConverter : Converter, IFarm, IPowerConsumer
    {
        public abstract bool IsOn { get; set; }
        public abstract float WaterConsumptionPerTickInUnits { get; }
        public abstract float BiomassProductionPerTickInUnits { get; }
        public abstract float HarvestThresholdInUnits { get; }

        public abstract void OnEmergencyShutdown();

        protected virtual ISink WaterIn { get; set; }

        protected virtual void FarmTick()
        {
            if (this.WaterIn != null)
            {
                if (Data.Containers[Matter.Water].CurrentAmount < this.WaterConsumptionPerTickInUnits)
                {
                    Data.Containers[Matter.Water].Push(WaterIn.Get(Matter.Water).Pull(this.WaterConsumptionPerTickInUnits));
                }

                if (Data.Containers[Matter.Water].CurrentAmount > this.WaterConsumptionPerTickInUnits)
                {

                }
            }
        }

        public override void OnSinkConnected(ISink s)
        {
            if (s.HasContainerFor(Matter.Water))
                this.WaterIn = s;
        }

        public override void ClearHooks()
        {
            this.WaterIn = null;
        }

        public override ResourceContainerDictionary GetStartingDataContainers()
        {
            return new ResourceContainerDictionary()
            {
                { Matter.Water, new ResourceContainer(0f) },
                { Matter.Biomass, new ResourceContainer(0f) }
            };
        }
    }

    public static class AgricultureExtensions
    {
    }

}
