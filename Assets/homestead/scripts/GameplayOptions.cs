using RedHomestead.Economy;
using RedHomestead.Geography;
using RedHomestead.Perks;
using UnityEngine;

namespace RedHomestead.GameplayOptions
{
    public struct NewGameChoices
    {
        public Perk ChosenPlayerTraining;
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
    }
}