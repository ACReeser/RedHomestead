using RedHomestead.Persistence;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RedHomestead.Perks
{
    public enum Perk
    {
        Administrator,
        Astronaut,
        Athlete,
        Botanist,
        Chemist,
        Engineer,
        Geologist,
        Salesperson,
    }

    public static class PerkMultipliers
    {
        public static float AirUsage { get; private set; }
        public static float RunSpeed { get; private set; }
        public static float ConstructSpeed { get; private set; }
        public static float SolarEfficiency { get; private set; }
        //public static float StartingFunds { get; private set; }
        public static float WeeklyIncome { get; private set; }
        public static float PurchasePrice { get; private set; }
        public static float ShippingCost { get; private set; }
        public static float ExcavationTime { get; private set; }
        public static float CropWaterUse { get; private set; }
        public static float CropGrowSpeed { get; private set; }
        public static float IndustrialEfficiency { get; private set; }
        public static float GeologyData { get; private set; }
        public static float ChemistryData { get; private set; }

        public static float StartingFunds(Perk selectedPerk)
        {
            return 1f + (selectedPerk == Perk.Administrator ? .15f : 0) + (selectedPerk == Perk.Astronaut ? .10f : 0);
        }

        public static void LoadFromPlayerPerkProgress()
        {
            AirUsage = Perk.Athlete.HasPerkLevel(1) ? .9f : 1f;
            RunSpeed = Perk.Athlete.HasPerkLevel(2) ? 1.15f : 1f;

            ConstructSpeed = Perk.Engineer.HasPerkLevel(1) ? .9f : 1f;
            SolarEfficiency= Perk.Engineer.HasPerkLevel(2) ? 1.1f : 1f;

            ChemistryData = Perk.Chemist.HasPerkLevel(1) ? 1.25f : 1f;
            IndustrialEfficiency = Perk.Chemist.HasPerkLevel(2) ? 1.1f : 1f;

            //StartingFunds = 1f + (Perk.Administrator.HasPerkLevel(1) ? .15f : 0) + (Perk.Astronaut.HasPerkLevel(1) ? .10f : 0);
            ShippingCost = Perk.Administrator.HasPerkLevel(2) ? .925f : 1f;

            PurchasePrice = Perk.Salesperson.HasPerkLevel(1) ? 1.05f : 1f;
            WeeklyIncome  = Perk.Salesperson.HasPerkLevel(2) ? .95f : 1f;

            GeologyData  = Perk.Geologist.HasPerkLevel(1) ? 1.25f : 1f;
            ExcavationTime  = Perk.Geologist.HasPerkLevel(2) ? .9f : 1f;

            CropWaterUse = Perk.Botanist.HasPerkLevel(1) ? .9f : 1f;
            CropGrowSpeed  = Perk.Botanist.HasPerkLevel(2) ? 1.1f : 1f;
        }
    }


    public static class PerkExtensions {
        public static bool HasPerkLevel(this Perk perk, int level)
        {
            return Game.Current.Player.PerkProgress[(int)perk] >= level;
        }
    }

    public enum PackUpgrades
    {
        OxygenPack,
        PowerPack,
        Exoskeleton
    }
}
