using RedHomestead.Persistence;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IRepairable
{

}

public class Gremlin : MonoBehaviour {
    private const int BaseDC = 10;
    private const int RepairableGraceCount = 5;
    private const float LowBatteryBonus = 5;
    /// <summary>
    /// The bonus (to avoid havok) during the late night
    /// </summary>
    private const float LateNightBonus = 5f;
    private const int WealthyPlayerThreshold = 200000;
    private const float PenaltyPerRepairable = .333f;
    private const int GremlinMissesThreshold = 4;
    public Transform ElectricalFailureSparksPrefab;

	// Use this for initialization
	void Start () {
        StartCoroutine(Lurk());
	}

    private float lurkTime;
    private IEnumerator Lurk()
    {
        lurkTime = UnityEngine.Random.Range(1f * 60f, 6f * 60f);

        yield return new WaitForSeconds(lurkTime);

        Plot();
    }

    private void Plot()
    {
        List<IRepairable> repairables = GetAllRegisteredRepairables();

        if (repairables.Count > 0)
        {
            int roll = UnityEngine.Random.Range(1, 21);
            int dc = GetDifficultyCheck(repairables);

            if (roll > dc)
            {
                Game.Current.Player.GremlinMissStreak++;
                StartCoroutine(Lurk());
            }
            else
                StartCoroutine(CauseHavok());
        }
        else
        {
            StartCoroutine(Lurk());
        }
    }
    
    private int GetDifficultyCheck(List<IRepairable> repairables)
    {
        float dc = BaseDC;

        //if you have a lot of things that can go wrong
        //give a penalty that increases linearly
        if (repairables.Count > RepairableGraceCount)
            dc += (repairables.Count - RepairableGraceCount) * PenaltyPerRepairable;

        //if it's late at night, things are less likely to go wrong
        if (Game.Current.Environment.CurrentHour < 5 || Game.Current.Environment.CurrentHour > 20)
        {
            dc -= LateNightBonus;
        }

        //if your battery is low (percentage wise) then give the player a break
        if (FlowManager.Instance.PowerGrids.LastGlobalTickData.CurrentBatteryWatts < FlowManager.Instance.PowerGrids.LastGlobalTickData.CurrentCapacityWatts / 4f)
        {
            dc -= LowBatteryBonus;
        }

        //if the player is wealthy, and thus more able to take the challenge, give a penalty linearly with wealth
        if (Game.Current.Player.BankAccount > WealthyPlayerThreshold)
        {
            dc += ((Game.Current.Player.BankAccount - WealthyPlayerThreshold) / 50000) + 1;
        }

        //if the player has missed a lot of gremlin attacks, give a random penalty
        if (Game.Current.Player.GremlinMissStreak > GremlinMissesThreshold)
        {
            dc += UnityEngine.Random.Range(1, Game.Current.Player.GremlinMissStreak - GremlinMissesThreshold+1);
        }

        return Mathf.RoundToInt(dc);
    }

    private List<IRepairable> currentlyGremlind = new List<IRepairable>();
    private IEnumerator CauseHavok()
    {
        yield return new WaitForSeconds(1f);
    }

    private List<IRepairable> registeredRepairables = new List<IRepairable>();

    private List<IRepairable> GetAllRegisteredRepairables()
    {
        return registeredRepairables;
    }
}
