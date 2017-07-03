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
        float BiomassProductionPerTickInUnits { get; }
        float HarvestThresholdInUnits { get; }
        float CurrentBiomassInUnits { get; }
        void Harvest();
        bool CanHarvest { get; }
    }

    public abstract class FarmConverter : Converter, IFarm, IPowerConsumer, IToggleReceiver
    {
        public abstract bool IsOn { get; set; }
        public abstract float WaterConsumptionPerTickInUnits { get; }
        public abstract float BiomassProductionPerTickInUnits { get; }
        public abstract float HarvestThresholdInUnits { get; }
        public Transform HeatToggle;

        /// <summary>
        /// Coefficient that is multiplied against BiomassProductionPerTickInUnits and subtracted every tick that power is not on
        /// </summary>
        public const float FrostBiomassDamageCoefficient = .5f;

        public abstract void OnEmergencyShutdown();
        public float CurrentBiomassInUnits
        {
            get
            {
                return Data.Containers[Matter.Biomass].CurrentAmount;
            }
        }

        protected virtual ISink WaterIn { get; set; }

        public bool CanHarvest
        {
            get
            {
                return Data.Containers[Matter.Biomass].CurrentAmount > HarvestThresholdInUnits;
            }
        }

        protected override void OnStart()
        {
            base.OnStart();
            ToggleMap.ToggleLookup[this.HeatToggle] = this;
        }

        private int ticksFrozen = 0;
        protected virtual void FarmTick()
        {
            if (this.HasPower && this.IsOn)
            {
                ticksFrozen = 0;

                if (this.WaterIn != null)
                {
                    if (Data.Containers[Matter.Water].CurrentAmount < this.WaterConsumptionPerTickInUnits)
                    {
                        Data.Containers[Matter.Water].Push(WaterIn.Get(Matter.Water).Pull(this.WaterConsumptionPerTickInUnits));
                    }

                    if (Data.Containers[Matter.Water].CurrentAmount >= this.WaterConsumptionPerTickInUnits)
                    {
                        Data.Containers[Matter.Water].Pull(WaterConsumptionPerTickInUnits);
                        Data.Containers[Matter.Biomass].Push(BiomassProductionPerTickInUnits);
                    }
                }
            }
            else
            {
                if (this.Data.Containers[Matter.Biomass].CurrentAmount > 0f)
                {
                    Data.Containers[Matter.Biomass].Pull(BiomassProductionPerTickInUnits * FrostBiomassDamageCoefficient);

                    if (ticksFrozen == 0)
                    {
                        //GuiBridge.Instance.ComputerAudioSource.PlayOneShot(GreenhouseFreezing)
                        ticksFrozen++;
                    }
                    else if (ticksFrozen > 60 * 2)
                    {
                        ticksFrozen = 0;
                    }
                    else
                    {
                        ticksFrozen++;
                    }
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

        public void Harvest()
        {
            this.OnHarvest(Data.Containers[Matter.Biomass].Pull(HarvestThresholdInUnits));
        }

        protected abstract void OnHarvest(float harvestAmountUnits);

        public void Toggle(Transform toggleHandle)
        {
            this.IsOn = !this.IsOn;
            this.OnPowerChanged();
            this.RefreshVisualization();
        }
    }

    public static class AgricultureExtensions
    {
        public static float HarvestPercentage(this IFarm farm)
        {
            return farm.CurrentBiomassInUnits / farm.HarvestThresholdInUnits;
        }

        public static string HarvestPercentageString(this IFarm farm)
        {
            return String.Format("{0:0.#}%", farm.HarvestPercentage() * 100f);
        }
    }

}
