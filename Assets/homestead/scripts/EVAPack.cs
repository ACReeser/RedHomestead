using UnityEngine;
using System.Collections;
using System;
using RedHomestead.Persistence;

namespace RedHomestead.EVA
{
    [Serializable]
    public class PackResourceData
    {
        public float Maximum;
        public float Current;
        public float ConsumptionPerSecond;

        public PackResourceData() { }
        public PackResourceData(float max, float consumption)
        {
            this.Maximum = max;
            this.Current = max;
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
            public const float LitersOfWaterPerDay = 3f;
            public const float SuitHeatingWattsPerHour = 1000f;

            public const float BasePackPowerWatts = SuitHeatingWattsPerHour * 6f;
            public const float UpgradedPackPowerWatts = SuitHeatingWattsPerHour * 10f;
        }

        public static PackData GetDefaultPackData()
        {
            return new PackData()
            {
                Oxygen = new PackResourceData(Constants.BasePackOxygenKilograms, GetConsumptionPerSecond(ConsumptionPeriod.Hourly, Constants.KilogramsOxygenPerHour)),
                Water = new PackResourceData(Constants.LitersOfWaterPerDay / 2, GetConsumptionPerSecond(ConsumptionPeriod.Daily, Constants.LitersOfWaterPerDay)),
                Food = new PackResourceData(Constants.CaloriesPerDay, GetConsumptionPerSecond(ConsumptionPeriod.Daily, Constants.CaloriesPerDay)),
                Power = new PackResourceData(Constants.BasePackPowerWatts, GetConsumptionPerSecond(ConsumptionPeriod.Hourly, Constants.SuitHeatingWattsPerHour))
            };
        }

        public const float OxygenResupplyRatePerSecondKilograms = Constants.BasePackOxygenKilograms / 100f;
        public const float PowerResupplyRatePerSecondWatts = Constants.BasePackPowerWatts / 100f;

        public static void UpgradePower(PackData data)
        {
            data.Power.Maximum = Constants.UpgradedPackPowerWatts;
        }

        public static void UpgradeOxygen(PackData data)
        {
            data.Oxygen.Maximum = Constants.UpgradedPackOxygenKilograms;
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
