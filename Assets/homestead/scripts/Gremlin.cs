using RedHomestead.Persistence;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct FailureAnchors
{
    public Transform Electrical;
    public Transform Pressure;
}

public interface IRepairable
{
    float FaultedPercentage { get; set; }
    FailureAnchors FailureEffectAnchors { get; }
}

public static class RepairableExtensions
{
    public static bool CanHaveElectricalFailure(this IRepairable rep)
    {
        return rep.FailureEffectAnchors.Electrical != null;
    }

    public static bool CanHavePressureFailure(this IRepairable rep)
    {
        return rep.FailureEffectAnchors.Pressure != null;
    }
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
    internal static Gremlin Instance { get; private set; }

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

        print("Gremlin lurking for " + lurkTime + " seconds");

        yield return new WaitForSeconds(lurkTime);

        Plot();
    }

    private void Plot()
    {
        if (registeredRepairables.Count > 0)
        {
            int roll = UnityEngine.Random.Range(1, 21);
            int dc = GetDifficultyCheck(registeredRepairables);

            print("Player rolled a " + roll + " vs a Gremlin DC of " + dc);

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

    public struct Gremlind
    {
        public Transform Effect;
        public FailureType FailType;
    }
    
    private Dictionary<IRepairable, Gremlind> gremlindMap = new Dictionary<IRepairable, Gremlind>();

    private IEnumerator CauseHavok()
    {
        CauseFailure();

        bool hasGremlinedRepairable = true;

        while (hasGremlinedRepairable)
        {

            yield return new WaitForSeconds(1f);

            hasGremlinedRepairable = gremlindMap.Keys.Count > 0;
        }

        StartCoroutine(Lurk());
    }

    private void CauseFailure()
    {
        IRepairable victim = registeredRepairables[UnityEngine.Random.Range(0, registeredRepairables.Count)];

        FailureType fail = GetFailType(victim);
        Transform effect = GetFailureParticleSystemFromPool(fail);

        gremlindMap.Add(victim, new Gremlind()
        {
            FailType = fail,
            Effect = effect
        });
        victim.FaultedPercentage = 1f;

        if (fail == FailureType.Electrical)
        {
            effect.SetParent(victim.FailureEffectAnchors.Electrical);
            FlowManager.Instance.PowerGrids.HandleElectricalFailure(victim);
        }
        else if (fail == FailureType.Pressure)
        {
            effect.SetParent(victim.FailureEffectAnchors.Pressure);
        }

        effect.transform.localPosition = Vector3.zero;
        effect.transform.localRotation = Quaternion.identity;

        SunOrbit.Instance.CheckEmergencyReset();
        GuiBridge.Instance.ShowNews(NewsSource.GetFailureNews(victim, fail));
    }

    private FailureType GetFailType(IRepairable victim)
    {
        bool canElectricFailure = victim.CanHaveElectricalFailure();
        bool canPressureFailure = victim.CanHavePressureFailure();

        if (canElectricFailure && canPressureFailure)
            return UnityEngine.Random.Range(0, 2) == 0 ? FailureType.Electrical : FailureType.Pressure;
        else if (canElectricFailure)
            return FailureType.Electrical;
        else //if (canPressureFailure)
            return FailureType.Pressure;
    }

    private List<IRepairable> registeredRepairables = new List<IRepairable>();
    internal void Register(IRepairable repairable)
    {
        if (repairable.CanHaveElectricalFailure() || repairable.CanHavePressureFailure())
            registeredRepairables.Add(repairable);
    }

    internal void Deregister(IRepairable repairable)
    {
        if (registeredRepairables.Contains(repairable))
            registeredRepairables.Remove(repairable);
    }

    internal void FinishRepair(IRepairable repairable)
    {
        Gremlind fixing = gremlindMap[repairable];
        gremlindMap.Remove(repairable);
        particleSystemPool[fixing.FailType].Add(fixing.Effect);
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
