using RedHomestead.Crafting;
using RedHomestead.Economy;
using RedHomestead.Geography;
using RedHomestead.Perks;
using RedHomestead.Simulation;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RedHomestead.Buildings;

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
        public Dictionary<Matter, int> BoughtMatter;
        public Dictionary<Craftable, int> BoughtCraftables;

        public void Init()
        {
            BoughtMatter = new Dictionary<Matter, int>();
            BoughtCraftables = new Dictionary<Craftable, int>();
        }

        public void RecalculateFunds()
        {
            StartingFunds = Mathf.RoundToInt(ChosenFinancing.StartingFunds() * PerkMultipliers.StartingFunds(ChosenPlayerTraining));
            AllocatedFunds = EconomyExtensions.HabitatCost + EconomyExtensions.RoverCost;

            int i = 0;
            foreach(KeyValuePair<Matter, int> kvp in BoughtMatter)
            {
                AllocatedFunds += EconomyExtensions.StartingSupplies[kvp.Key].PerUnitCost * kvp.Value;
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

        internal void AddMinimumSupplies()
        {
            if (BoughtMatter != null)
            {
                AddSuppliesFromModule(Module.Airlock);
            }
        }

        private void AddSuppliesFromModule(Module module)
        {
            foreach(ResourceEntry entry in Construction.BuildData[module].Requirements)
            {
                AddOrIncrement(BoughtMatter, entry.Type, Mathf.CeilToInt(entry.Count));
            }
        }

        private void AddOrIncrement<K>(Dictionary<K, int> dictionary, K key, int addition)
        {
            if (dictionary.ContainsKey(key))
            {
                dictionary[key] += addition;
            }
            else
            {
                dictionary[key] = addition;
            }
        }

        internal void AddBackerSupplies()
        {
            
        }
    }
}