using RedHomestead.Economy;
using RedHomestead.Geography;
using RedHomestead.Perks;
using System;
using System.Linq;
using UnityEngine;

namespace RedHomestead.GameplayOptions
{
    public struct NewGameChoices
    {
        public Perk ChosenPlayerTraining;
        public string HomesteadName;
        public BackerFinancing ChosenFinancing;
        public BaseLocation ChosenLocation;
        public int StartingFunds, AllocatedFunds, RemainingFunds;
        public bool BuyRover;
        public int[] BoughtMatter;
        public int[] BoughtCraftables;

        public void Init()
        {
            BoughtMatter = new int[EconomyExtensions.StartingSupplies.Length];
            BoughtCraftables = new int[EconomyExtensions.StartingCraftables.Length];
        }

        public void RecalculateFunds()
        {
            StartingFunds = Mathf.RoundToInt(ChosenFinancing.StartingFunds() * PerkMultipliers.StartingFunds(ChosenPlayerTraining));
            AllocatedFunds = EconomyExtensions.HabitatCost + EconomyExtensions.RoverCost;

            int i = 0;
            foreach(int number in BoughtMatter)
            {
                AllocatedFunds += EconomyExtensions.StartingSupplies[i].PerUnitCost * number;
                i++;
            }

            RemainingFunds = StartingFunds - AllocatedFunds;
        }

        public float[] GetPerkProgress()
        {
            int[] perks = Enum.GetValues(typeof(Perk)).Cast<int>().ToArray();
            float[] result = new float[perks.Length];
            for (int i = 0; i < perks.Length; i++)
            {
                if ((Perk)i == ChosenPlayerTraining)
                {
                    result[i] = 2f;
                }
                else
                {
                    result[i] = 0f;
                }
            }

            return result;
        }
    }
}