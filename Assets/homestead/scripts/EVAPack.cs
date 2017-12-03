using UnityEngine;
using System.Collections;
using System;
using RedHomestead.Persistence;
using RedHomestead.Simulation;

namespace RedHomestead.EVA
{
    public enum EVAUpgrade
    {
        None     = 0,
        Oxygen   = 1,
        Battery  = 2,
        Toolbelt = 4,
        Jetpack  = 8
    }

    [Serializable]
    public class PackResourceData
    {
        public Container Container;
        
        public float ConsumptionPerSecond;
        public float DeprivationSeconds;

        public PackResourceData() { }
        public PackResourceData(float max, float consumption)
        {
            this.Container = new Container(max, max);
            this.ConsumptionPerSecond = consumption;
        }
        public PackResourceData(Container container, float consumption)
        {
            this.Container = container;
            this.ConsumptionPerSecond = consumption;
        }
    }


    [Serializable]
    public class PackData : RedHomesteadData
    {
        public PackResourceData Oxygen;
        public PackResourceData Power;
        public PackResourceData Water;
        public PackResourceData Food;
        public string CurrentHabitatModuleInstanceID;
        public EVAUpgrade Upgrades;

        public bool HasUpgrade(EVAUpgrade queryUpgrade)
        {
            return (Upgrades & queryUpgrade) != 0;
        }

        public void SetUpgrade(EVAUpgrade newUpgrade)
        {
            Upgrades |= newUpgrade;

            switch(newUpgrade)
            {
                case EVAUpgrade.Battery:
                    EVA.UpgradePower(this);
                    break;
                case EVAUpgrade.Oxygen:
                    EVA.UpgradeOxygen(this);
                    break;
                case EVAUpgrade.Toolbelt:
                    PlayerInput.Instance.Loadout.UpgradeToBigToolbelt();
                    break;
            }
        }

        public bool IsInDeprivationMode()
        {
            return (Oxygen.DeprivationSeconds > 0f || Power.DeprivationSeconds > 0f || Food.DeprivationSeconds > 0f || Water.DeprivationSeconds > 0f);
        }

        protected override void BeforeMarshal(Transform t)
        {
        }

        public override void AfterDeserialize(Transform t = null)
        {
        }
    }

    public enum ConsumptionPeriod { Daily, Hourly }

    public static class EVA{
        public static class Constants
        {
            public const float KilogramsOxygenPerHour = 0.0972f;
            public const float BasePackOxygenKilograms = KilogramsOxygenPerHour * 4f;
            public const float UpgradedPackOxygenKilograms = KilogramsOxygenPerHour * 8f;

            public const float CaloriesPerDay = 2400;
            public const float WaterPerDayKilograms = 3f;
            public const float SuitHeatingWattsPerHour = 1000f;
            public const float SuitHeatingWattsPerSecond = SuitHeatingWattsPerHour / 60f / 60f;

            public const float BasePackPowerWatts = SuitHeatingWattsPerHour * 6f;
            public const float UpgradedPackPowerWatts = SuitHeatingWattsPerHour * 10f;
        }

        public static PackData GetDefaultPackData()
        {
            return new PackData()
            {
                Oxygen = new PackResourceData(Constants.BasePackOxygenKilograms, GetConsumptionPerSecond(ConsumptionPeriod.Hourly, Constants.KilogramsOxygenPerHour)),
                Water = new PackResourceData(Constants.WaterPerDayKilograms / 2, GetConsumptionPerSecond(ConsumptionPeriod.Daily, Constants.WaterPerDayKilograms)),
                Food = new PackResourceData(Constants.CaloriesPerDay, GetConsumptionPerSecond(ConsumptionPeriod.Daily, Constants.CaloriesPerDay)),
                Power = new PackResourceData(Constants.BasePackPowerWatts, GetConsumptionPerSecond(ConsumptionPeriod.Hourly, Constants.SuitHeatingWattsPerHour))
            };
        }

        public const float OxygenResupplySeconds = 4f;
        public const float PowerResupplySeconds = 4f;

        public static float OxygenResupplyKilogramsPerUnit = GetConsumptionPerSecond(ConsumptionPeriod.Hourly, Constants.KilogramsOxygenPerHour) * OxygenResupplySeconds / Simulation.Matter.Oxygen.Kilograms();
        public static float PowerResupplyWattsPerSecond = GetConsumptionPerSecond(ConsumptionPeriod.Hourly, Constants.SuitHeatingWattsPerHour) * PowerResupplySeconds;

        public static void UpgradePower(PackData data)
        {
            data.Power.Container.TotalCapacity = Constants.UpgradedPackPowerWatts;
        }

        public static void UpgradeOxygen(PackData data)
        {
            data.Oxygen.Container.TotalCapacity = Constants.UpgradedPackOxygenKilograms;
        }

        private static float GetConsumptionPerSecond(ConsumptionPeriod period, float amountPerPeriod)
        {
            switch (period)
            {
                case ConsumptionPeriod.Daily:
                    return amountPerPeriod / SunOrbit.MartianMinutesPerDay * SunOrbit.GameSecondsPerMartianMinute;
                case ConsumptionPeriod.Hourly:
                default:
                    return amountPerPeriod / 60 * SunOrbit.GameSecondsPerMartianMinute;
            }
        }
    }
}
