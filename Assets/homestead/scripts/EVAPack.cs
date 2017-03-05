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

        protected override void BeforeMarshal(MonoBehaviour container) { }
    }

    public enum ConsumptionPeriod { Daily, Hourly }

    public static class EVA{
        public static class Constants
        {
            public const float KilogramsOxygenPerHour = 0.0972f;
            public const float CaloriesPerDay = 2400;
            public const float LitersOfWaterPerDay = 3f;
            public const float SuitHeatingWattsPerHour = 1000f;
        }

        public static PackData GetDefaultPackData()
        {
            return new PackData()
            {
                Oxygen = new PackResourceData(Constants.KilogramsOxygenPerHour * 4f, GetConsumptionPerSecond(ConsumptionPeriod.Hourly, Constants.KilogramsOxygenPerHour)),
                Water = new PackResourceData(Constants.LitersOfWaterPerDay / 2, GetConsumptionPerSecond(ConsumptionPeriod.Daily, Constants.LitersOfWaterPerDay)),
                Food = new PackResourceData(Constants.CaloriesPerDay, GetConsumptionPerSecond(ConsumptionPeriod.Daily, Constants.CaloriesPerDay)),
                Power = new PackResourceData(Constants.SuitHeatingWattsPerHour * 6f, GetConsumptionPerSecond(ConsumptionPeriod.Hourly, Constants.SuitHeatingWattsPerHour))
            };
        }

        public static void UpgradePower()
        {

        }

        public static void UpgradeOxygen()
        {

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
