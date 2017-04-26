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

        public void RecalculateFunds()
        {
            StartingFunds = Mathf.RoundToInt(ChosenFinancing.StartingFunds() * PerkMultipliers.StartingFunds(ChosenPlayerTraining));
            AllocatedFunds = EconomyExtensions.HabitatCost + EconomyExtensions.RoverCost;
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