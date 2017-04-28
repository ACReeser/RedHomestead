using RedHomestead.Persistence;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IRepairable
{
    float RepairProgress { get; set; }
}

public interface IElectricalRepairable: IRepairable
{
    Transform ElectricalFailureAnchor { get; }
}
public interface IPressureRepairable : IRepairable
{
    Transform PressureFailureAnchor { get; }
}

public class Gremlin : MonoBehaviour {
    private const int BaseDC = 10;
    private const int RepairableGraceCount = 5;
    /// <summary>
    /// The bonus (to avoid havok) during the late night
    /// </summary>
    private const float LateNightBonus = 5f;
    private const float LowBatteryBonus = 5f;
    private const int WealthyPlayerThreshold = 200000;
    private const float PenaltyPerRepairable = .333f;
    private const int GremlinMissesThreshold = 4;
    public Transform ElectricalFailureSparksPrefab, OutgassingFailurePrefab;
    internal Gremlin Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }
    
	void Start()
    {
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
        if (registeredRepairables.Count > 0)
        {
            int roll = UnityEngine.Random.Range(1, 21);
            int dc = GetDifficultyCheck(registeredRepairables);

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

    //todo: use map to struct with both part system transform and failType
    //and use that as currentlygremlind, with keys.count > 0 as "has victim"
    private Dictionary<IRepairable, Transform> repairableToParticleSystemMap = new Dictionary<IRepairable, Transform>();
    private List<IRepairable> currentlyGremlind = new List<IRepairable>();
    private IEnumerator CauseHavok()
    {
        IRepairable victim = registeredRepairables[UnityEngine.Random.Range(0, registeredRepairables.Count)];

        if (victim is IElectricalRepairable)
        {
            Transform particleSystem = GetParticleSystemPrefab(FailureType.Electrical);
            particleSystem.SetParent((victim as IElectricalRepairable).ElectricalFailureAnchor);
            particleSystem.transform.localPosition = Vector3.zero;
            particleSystem.transform.localRotation = Quaternion.identity;
            repairableToParticleSystemMap.Add(victim, particleSystem);
        }

        currentlyGremlind.Add(victim);

        bool hasGremlinedRepairable = true;

        while (hasGremlinedRepairable)
        {

            yield return new WaitForSeconds(1f);

            hasGremlinedRepairable = currentlyGremlind.Count > 0;
        }

        StartCoroutine(Lurk());
    }

    private List<IRepairable> registeredRepairables = new List<IRepairable>();
    internal void Register(IRepairable repairable)
    {
        registeredRepairables.Add(repairable);
    }

    internal void Deregister(IRepairable repairable)
    {
        registeredRepairables.Remove(repairable);
    }

    internal void FinishRepair(IRepairable repairable)
    {
        currentlyGremlind.Remove(repairable);
        //todo: add particle system back to pool
        //repairableToParticleSystemMap[repairable]
    }

    public enum FailureType { Electrical, Pressure }
    private Dictionary<FailureType, List<Transform>> particleSystemPool = new Dictionary<FailureType, List<Transform>>()
    {
        { FailureType.Electrical, new List<Transform>() },
        { FailureType.Pressure, new List<Transform>() }
    };

    private Transform GetParticleSystemPrefab(FailureType failType)
    {
        switch (failType)
        {
            case FailureType.Pressure:
                return OutgassingFailurePrefab;
            default:
                return ElectricalFailureSparksPrefab;
        }
    }

    internal Transform GetFailureParticleSystemFromPool(FailureType failType)
    {
        List<Transform> pool = particleSystemPool[failType];

        if (pool.Count > 0)
        {
            Transform result = pool[0];
            pool.RemoveAt(0);
            return result;
        }
        else
        {
            return (Transform)GameObject.Instantiate(GetParticleSystemPrefab(failType));
        }
    }
}
