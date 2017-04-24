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
        public const float AirUsage = 1f;
        public const float RunSpeed = 1f;
    }


    public static class PerkExtensions {
        public static bool HasPerkLevel(this Perk perk, int level)
        {
            return Game.Current.Player.PerkProgress[(int)perk] >= level;
        }
    }
}
