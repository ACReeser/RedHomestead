using RedHomestead.Electricity;
using RedHomestead.Industry;
using RedHomestead.Simulation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using RedHomestead.Persistence;

namespace RedHomestead.Agriculture{


    public interface IHarvestable
    {
        bool CanHarvest { get; }
        float HarvestProgress { get; }
        void Harvest(float addtlProgress);
        void CompleteHarvest();
    }

    public interface IFarm: ISink, IHarvestable
    {
        float WaterConsumptionPerTickInUnits { get; }
        float BiomassProductionPerTickInUnits { get; }
        float HarvestThresholdInUnits { get; }
        float CurrentBiomassInUnits { get; }
        float OxygenProductionPerTickInUnits { get; }
        ISink WaterIn { get; }
    }
    
    [Serializable]
    public class FarmFlexData
    {
        public bool IsHeatOn;
        public bool IsWaterOn;
        public float HarvestProgress;
    }

    public abstract class FarmConverter : Converter, IFarm, IPowerConsumer, IToggleReceiver, IFlexDataContainer<MultipleResourceModuleData, FarmFlexData>
    {
        public bool IsOn { get { return FlexData.IsHeatOn; } set { FlexData.IsHeatOn = value; } }
        public float HarvestProgress { get { return FlexData.HarvestProgress; } }
        public abstract float WaterConsumptionPerTickInUnits { get; }
        public abstract float BiomassProductionPerTickInUnits { get; }
        public abstract float OxygenProductionPerTickInUnits { get; }
        public abstract float HarvestThresholdInUnits { get; }
        public Transform HeatToggle, WaterToggle;
        public UnityEngine.TextMesh DisplayText;
        public SpriteRenderer heatSprite, waterSprite;
        public FarmFlexData FlexData { get; set; }
        public Collider[] HarvestTriggers;

        protected virtual float HarvestTimeSeconds { get { return 1f; } }

        /// <summary>
        /// Coefficient that is multiplied against BiomassProductionPerTickInUnits and subtracted every tick that power is not on
        /// </summary>
        public const float FrostBiomassDamageCoefficient = .5f;
        /// <summary>
        /// Coefficient that is multiplied against BiomassProductionPerTickInUnits and subtracted every tick that water is not on
        /// </summary>
        public const float DroughtBiomassDamageCoefficient = .5f;

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

            if (this.WaterToggle != null)
            {
                ToggleMap.ToggleLookup[this.HeatToggle] = this;
                ToggleMap.ToggleLookup[this.WaterToggle] = this;
            }

            this.RefreshFarmVisualization();
            this.RefreshIconsAndHandles();
            if (this.CanHarvest)
            {
                ToggleHarvestTrigger(true);
            }
        }

        private void ToggleHarvestTrigger(bool enabled)
        {
            foreach(Collider c in HarvestTriggers)
            {
                c.enabled = enabled;
            }
        }

        protected void RefreshIconsAndHandles()
        {
            if (this.WaterToggle != null)
            {
                this.heatSprite.color = !this.HasPower ? ToggleTerminalStateData.Defaults.Invalid : this.FlexData.IsHeatOn ? ToggleTerminalStateData.Defaults.On : ToggleTerminalStateData.Defaults.Off;
                this.waterSprite.color = this.WaterIn == null ? ToggleTerminalStateData.Defaults.Invalid : this.FlexData.IsWaterOn ? ToggleTerminalStateData.Defaults.On : ToggleTerminalStateData.Defaults.Off;

                this.HeatToggle.localRotation = this.FlexData.IsHeatOn ? Quaternion.identity : ToggleTerminalStateData.Defaults.ToggleOffPosition;
                this.WaterToggle.localRotation = this.FlexData.IsWaterOn ? Quaternion.identity : ToggleTerminalStateData.Defaults.ToggleOffPosition;
            }
        }

        protected abstract void RefreshFarmVisualization();

        private float oldBiomass;
        private int cropFailureTicks = 0;
        protected virtual void FarmTick()
        {
            if (this.HasPower && this.IsOn && this.FlexData.IsWaterOn && this.FlexData.IsHeatOn)
            {
                cropFailureTicks = 0;
                
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

                        if (CanHarvest)
                        {
                            ToggleHarvestTrigger(true);
                        }
                    }
                }
            }
            else
            {
                if (this.Data.Containers[Matter.Biomass].CurrentAmount > 0f)
                {
                    float lossAmount = 0;
                    if (!FlexData.IsHeatOn)
                        lossAmount += BiomassProductionPerTickInUnits * FrostBiomassDamageCoefficient;
                    if (!FlexData.IsWaterOn)
                        lossAmount += BiomassProductionPerTickInUnits * DroughtBiomassDamageCoefficient;

                    oldBiomass = Data.Containers[Matter.Biomass].CurrentAmount;

                    Data.Containers[Matter.Biomass].Pull(lossAmount);

                    CheckForAmountChange();

                    if (cropFailureTicks == 0)
                    {
                        //GuiBridge.Instance.ComputerAudioSource.PlayOneShot(CropFailure)
                        cropFailureTicks++;
                    }
                    else if (cropFailureTicks > 59 * 2)
                    {
                        cropFailureTicks = 0;
                    }
                    else
                    {
                        cropFailureTicks++;
                    }
                }
            }

            UpdateText();
        }

        private void CheckForAmountChange()
        {
            if (Mathf.RoundToInt(oldBiomass * 100f) != Mathf.RoundToInt(Data.Containers[Matter.Biomass].CurrentAmount * 100f))
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

            this.RefreshIconsAndHandles();
        }

        public override void ClearHooks()
        {
            this.WaterIn = null;
            this.RefreshIconsAndHandles();
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

        public void Harvest(float addtlHarvest)
        {
            this.FlexData.HarvestProgress += addtlHarvest;

            if (this.FlexData.HarvestProgress >= HarvestTimeSeconds)
                this.CompleteHarvest();
        }

        public void CompleteHarvest()
        {
            this.FlexData.HarvestProgress = 0f;
            this.OnHarvestComplete(Data.Containers[Matter.Biomass].Pull(HarvestThresholdInUnits));
            ToggleHarvestTrigger(false);
            this.RefreshFarmVisualization();
        }

        protected abstract void OnHarvestComplete(float harvestAmountUnits);

        public void Toggle(Transform toggleHandle)
        {
            if (toggleHandle == HeatToggle)
            {
                this.FlexData.IsHeatOn = !this.FlexData.IsHeatOn;
                this.OnPowerChanged();
                this.RefreshVisualization();
            }
            else if (toggleHandle == WaterToggle)
            {
                this.FlexData.IsWaterOn = !this.FlexData.IsWaterOn;
            }
            RefreshIconsAndHandles();
        }

        public override void OnPowerChanged()
        {
            base.OnPowerChanged();
            RefreshIconsAndHandles();
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
