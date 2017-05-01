using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RedHomestead.Scoring{

    public static class ScoringConstants
    {
        internal const int PerSol = 100;
        internal const int PerModule = 50;
        internal const int PerRepair = 10;
        internal const int PerMatterMined = 2;
        internal const int PerMatterRefined = 3;
        internal const int PerMatterSold = 4;
    }

    [Serializable]
	public struct GameScore
    {
        public int ModulesBuilt;
        public int RepairsEffected;
        public float MatterMined;
        public float MatterRefined;
        public int MatterSold;
        //public int Matter sold
        //public int Suit upgrades made/bought
        //public float Extra Training completed
        //public int Solar Flares / Dust Storms survived
        //public int Science data collected
        //public float Additional Colonist time

        public int GetScore(int sol, int hour)
        {
            float rawScore = (sol + hour / SunOrbit.MartianHoursPerDay) * ScoringConstants.PerSol;

            rawScore += ModulesBuilt    * ScoringConstants.PerModule;
            rawScore += RepairsEffected * ScoringConstants.PerRepair;
            rawScore += MatterMined     * ScoringConstants.PerMatterMined;
            rawScore += MatterRefined   * ScoringConstants.PerMatterRefined;
            rawScore += MatterSold      * ScoringConstants.PerMatterSold;

            return Mathf.RoundToInt(Mathf.Floor(rawScore));
        }
    }
}
