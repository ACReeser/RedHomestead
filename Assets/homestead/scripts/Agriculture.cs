using RedHomestead.Electricity;
using RedHomestead.Industry;
using RedHomestead.Simulation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using RedHomestead.Persistence;

namespace RedHomestead.Agriculture{
    
    public interface IFarm: ISink
    {
        float WaterConsumptionPerTickInUnits { get; }
        float BiomassProductionPerTickInUnits { get; }
        float HarvestThresholdInUnits { get; }
        float CurrentBiomassInUnits { get; }
        float OxygenProductionPerTickInUnits { get; }
        void Harvest();
        bool CanHarvest { get; }
        ISink WaterIn { get; }
    }
    
    [Serializable]
    public class FarmFlexData
    {
        public bool IsHeatOn;
        public bool IsWaterOn;
    }

    public abstract class FarmConverter : Converter, IFarm, IPowerConsumer, IToggleReceiver, IFlexDataContainer<MultipleResourceModuleData, FarmFlexData>
    {
        public abstract bool IsOn { get; set; }
        public abstract float WaterConsumptionPerTickInUnits { get; }
        public abstract float BiomassProductionPerTickInUnits { get; }
        public abstract float OxygenProductionPerTickInUnits { get; }
        public abstract float HarvestThresholdInUnits { get; }
        public Transform HeatToggle, WaterToggle;
        public UnityEngine.TextMesh DisplayText;
        public SpriteRenderer heatSprite, waterSprite;
        public FarmFlexData FlexData { get; set; }

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

        public virtual ISink WaterIn { get; protected set; }

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
            this.RefreshFarmVisualization();
            this.RefreshIcons();
        }

        private void RefreshIcons()
        {
            //this.heatSprite.color = this.HasPower && this.FlexData.IsHeatOn;
        }

        protected abstract void RefreshFarmVisualization();

        private float oldBiomass;
        private int ticksFrozen = 0;
        protected virtual void FarmTick()
        {
            if (this.HasPower && this.IsOn)
            {
                ticksFrozen = 0;

                if (this.WaterIn != null && !this.CanHarvest)
                {
                    if (Data.Containers[Matter.Water].CurrentAmount < this.WaterConsumptionPerTickInUnits)
                    {
                        Data.Containers[Matter.Water].Push(WaterIn.Get(Matter.Water).Pull(this.WaterConsumptionPerTickInUnits));
                    }

                    if (Data.Containers[Matter.Water].CurrentAmount >= this.WaterConsumptionPerTickInUnits)
                    {
                        oldBiomass = Data.Containers[Matter.Biomass].CurrentAmount;
                        Data.Containers[Matter.Water].Pull(WaterConsumptionPerTickInUnits);
                        Data.Containers[Matter.Biomass].Push(BiomassProductionPerTickInUnits);
                        CheckForAmountChange();
                    }
                }
            }
            else
            {
                if (this.Data.Containers[Matter.Biomass].CurrentAmount > 0f)
                {
                    oldBiomass = Data.Containers[Matter.Biomass].CurrentAmount;

                    Data.Containers[Matter.Biomass].Pull(BiomassProductionPerTickInUnits * FrostBiomassDamageCoefficient);

                    CheckForAmountChange();

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

            UpdateText();
        }

        private void CheckForAmountChange()
        {
            if (Mathf.RoundToInt(oldBiomass * 10f) != Mathf.RoundToInt(Data.Containers[Matter.Biomass].CurrentAmount * 10f))
            {
                this.RefreshFarmVisualization();
            }
        }

        private void UpdateText()
        {            
            DisplayText.text = String.Format("Hydroponics\n\nHarvest:\n{0}\n\nWater Supply:\n<size=22>{1} Days</size>\n\nYield:\n{2} Meals\n\nOxygen:\n{3}kg a Day", this.HarvestDays(), this.WaterSupplyDays(), this.YieldMeals(), this.OxygenADayKgs());
        }

        public override void OnSinkConnected(ISink s)
        {
            if (s.HasContainerFor(Matter.Water))
                this.WaterIn = s;

            this.RefreshIcons();
        }

        public override void ClearHooks()
        {
            this.WaterIn = null;
            this.RefreshIcons();
        }

        public override void InitializeStartingData()
        {
            base.InitializeStartingData();
            this.FlexData = new FarmFlexData();
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
            this.RefreshFarmVisualization();
        }

        protected abstract void OnHarvest(float harvestAmountUnits);

        public void Toggle(Transform toggleHandle)
        {
            if (toggleHandle == HeatToggle)
            {
                this.IsOn = !this.IsOn;
                this.OnPowerChanged();
                this.RefreshVisualization();
            }
            else if (toggleHandle == WaterToggle)
            {

            }
        }
    }

    public static class AgricultureExtensions
    {
        public static string HarvestDays(this IFarm farm)
        {
            return farm.CanHarvest ? "<size=32><color=green>READY</color></size>" : farm.Get(Matter.Biomass).CurrentAmount > 0 ? String.Format("<size=32>{0:0.#} Days</size>", (farm.HarvestThresholdInUnits - farm.Get(Matter.Biomass).CurrentAmount) / farm.BiomassProductionPerTickInUnits / SunOrbit.GameSecondsPerGameDay) : "<size=32>NEVER</size>";
        }

        public static string WaterSupplyDays(this IFarm farm)
        {
            return farm.WaterIn == null ? "0" : String.Format("{0:#.#}", farm.WaterIn.Get(Matter.Water).CurrentAmount / farm.WaterConsumptionPerTickInUnits / SunOrbit.GameSecondsPerGameDay);
        }

        public static string YieldMeals(this IFarm farm)
        {
            return String.Format("{0:0.#}", Matter.Biomass.MealsPerCubicMeter() * farm.Get(Matter.Biomass).CurrentAmount);
        }

        public static string OxygenADayKgs(this IFarm farm)
        {
            return String.Format("{0:0.##}", Matter.Oxygen.Kilograms() * farm.OxygenProductionPerTickInUnits * SunOrbit.GameSecondsPerGameDay);
        }
    }

}
