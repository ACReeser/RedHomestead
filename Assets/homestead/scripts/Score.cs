using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RedHomestead.Scoring {

    public static class ScoringConstants
    {
        internal const int PerSol = 100;
        internal const int PerModule = 50;
        internal const int PerCrafting = 50;
        internal const int PerRepair = 10;
        internal const int PerScienceMission = 100;

        internal const int PerMatterMined = 2;
        internal const int PerMatterRefined = 3;
        internal const int PerMatterSold = 4;

        public static ScoringReward[] ScoringRewards = new ScoringReward[]{
            new ScoringReward()
            {
                Score = PerModule,
                Title = "Contractor"
            },
            new ScoringReward()
            {
                Score = PerRepair,
                Title = "Technician"
            },
            new ScoringReward()
            {
                Score = PerCrafting,
                Title = "Artisan"
            },
            new ScoringReward()
            {
                Score = PerScienceMission,
                Title = "Scientist"
            },
            new ScoringReward()
            {
                Score = PerSol,
                Title = "Survivor"
            }
        };
    }

    public struct ScoringReward
    {
        public int Score { get; set; }
        public string Title { get; set; }
    }

    public enum ScoreType
    {
        Build,
        Repair,
        Craft,
        Science,
        Survivor
    }

    [Serializable]
	public struct GameScore
    {
        public int ModulesBuilt;
        public int RepairsEffected;
        public float MatterMined;
        public float MatterRefined;
        public int MatterSold;
        public int ItemsCrafted;
        public int ScienceMissionsCompleted;
        public int TimesDied;
        //public float Extra Training completed
        //public int Solar Flares / Dust Storms survived
        //public int Science data collected
        //public float Additional Colonist time

        public int GetScore(int sol, float hour)
        {
            float rawScore = (sol + hour / SunOrbit.MartianHoursPerDay) * ScoringConstants.PerSol;

            rawScore += ModulesBuilt    * ScoringConstants.PerModule;
            rawScore += RepairsEffected * ScoringConstants.PerRepair;
            rawScore += MatterMined     * ScoringConstants.PerMatterMined;
            rawScore += MatterRefined   * ScoringConstants.PerMatterRefined;
            rawScore += MatterSold      * ScoringConstants.PerMatterSold;

            return Mathf.RoundToInt(Mathf.Floor(rawScore));
        }

        public ScoringReward AddScoringEvent(ScoreType @event, GuiBridge g)
        {
            switch (@event)
            {
                case ScoreType.Build:
                    ModulesBuilt++;
                    break;
                case ScoreType.Craft:
                    ItemsCrafted++;
                    break;
                case ScoreType.Repair:
                    RepairsEffected++;
                    break;
                case ScoreType.Science:
                    ScienceMissionsCompleted++;
                    break;
                case ScoreType.Survivor:

                    break;
            }
            var result = ScoringConstants.ScoringRewards[(int)@event];
            g.ShowScoringEvent(result);
            return result;
        }
    }
}
