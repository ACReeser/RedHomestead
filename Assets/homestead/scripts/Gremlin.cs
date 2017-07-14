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
    bool CanMalfunction { get; }
    float FaultedPercentage { get; set; }
    FailureAnchors FailureEffectAnchors { get; }
    Transform transform { get; }
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
    public const string GremlindTag = "gremlind";
    private const float RepairPercentagePerSecond = .1f;
    public Transform ElectricalFailureSparksPrefab, OutgassingFailurePrefab;
    public AudioClip ElectricalFailureComputerTalk, PressureFailureComputerTalk;
    internal static Gremlin Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }
    
	void Start()
    {
        BeginLurk();
    }

    private void BeginLurk()
    {
        LurkCoroutine = StartCoroutine(Lurk());
    }

    private float lurkTime;
    private IEnumerator Lurk()
    {
        float extraBreathingRoom = Game.Current.Player.GremlinChastised ? 5f : 0f;
        lurkTime = UnityEngine.Random.Range(1f * 60f, 6f * 60f) + extraBreathingRoom;

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

#if (DEVELOPMENT_BUILD || UNITY_EDITOR)
            if (PlayerInput.DoNotDisturb) roll += 9999999;
#endif

            if (roll > dc)
            {
                Game.Current.Player.GremlinMissStreak++;
                BeginLurk();
            }
            else
            {
                CauseRandomFailure();
                StartCoroutine(WaitForRepair());
            }
        }
        else
        {
            BeginLurk();
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
            dc += ((Game.Current.Player.BankAccount - WealthyPlayerThreshold) / 50000) * .1f;
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
        public string PreviousTag;
    }
    
    private Dictionary<IRepairable, Gremlind> gremlindMap = new Dictionary<IRepairable, Gremlind>();

    private IEnumerator WaitForRepair()
    {
        bool hasGremlinedRepairable = true;

        while (hasGremlinedRepairable)
        {

            yield return new WaitForSeconds(1f);

            hasGremlinedRepairable = gremlindMap.Keys.Count > 0;

#if (DEVELOPMENT_BUILD || UNITY_EDITOR)
            if (PlayerInput.DoNotDisturb) hasGremlinedRepairable = false;
#endif
        }

        BeginLurk();
    }

    private void CauseRandomFailure()
    {
        int tries = 0;
        IRepairable victim = null;
        while (victim == null)
        {
            victim = registeredRepairables[UnityEngine.Random.Range(0, registeredRepairables.Count)];

            if (!victim.CanMalfunction)
                victim = null;
            else if (tries > 10) //include a failsafe for 10 non-victims in a row
                break;

            tries++;
        }

        if (victim == null)
        {
            //give up
            BeginLurk();
        }
        else
        {
            FailureType fail = GetFailType(victim);

            CauseFailure(victim, fail);
        }
    }

    private void CauseFailure(IRepairable victim, FailureType fail)
    {
        Transform effect = GetFailureParticleSystemFromPool(fail);

        gremlindMap.Add(victim, new Gremlind()
        {
            FailType = fail,
            Effect = effect,
            PreviousTag = victim.transform.root.tag
        });

        victim.transform.root.tag = GremlindTag;

        //if we've loaded a pre-existing fault percentage, don't undo the repair
        if (victim.FaultedPercentage == 0f)
            victim.FaultedPercentage = 1f;

        if (fail == FailureType.Electrical)
        {
            effect.SetParent(victim.FailureEffectAnchors.Electrical);
            //alert the powergrid script
            FlowManager.Instance.PowerGrids.HandleElectricalFailure(victim);
            GuiBridge.Instance.ComputerAudioSource.PlayOneShot(this.ElectricalFailureComputerTalk);
        }
        else if (fail == FailureType.Pressure)
        {
            effect.SetParent(victim.FailureEffectAnchors.Pressure);
            GuiBridge.Instance.ComputerAudioSource.PlayOneShot(this.PressureFailureComputerTalk);
        }

        effect.transform.localPosition = Vector3.zero;
        effect.transform.localRotation = Quaternion.identity;

        //alert the other scripts
        SunOrbit.Instance.CheckEmergencyReset();
        GuiBridge.Instance.ShowNews(NewsSource.GetFailureNews(victim, fail));
        PlayerInput.Instance.ToggleRepairMode(true);
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
        {
            registeredRepairables.Add(repairable);

            //should only happen after loading a game
            if (repairable.FaultedPercentage > 0f)
            {
                StopCoroutine(LurkCoroutine);
#warning bug: may lose the right kind of failure here if supports both electrical and pressure
                CauseFailure(repairable, GetFailType(repairable));
            }
        }
    }

    internal void Deregister(IRepairable repairable)
    {
        if (registeredRepairables.Contains(repairable))
            registeredRepairables.Remove(repairable);
    }

    internal void EffectRepair(IRepairable repairable)
    {
        repairable.FaultedPercentage -= RepairPercentagePerSecond * Time.deltaTime;

        if (repairable.FaultedPercentage <= 0f)
            FinishRepair(repairable);

        Game.Current.Score.RepairsEffected++;
    }

    internal void FinishRepair(IRepairable repaired)
    {
        repaired.FaultedPercentage = 0f;
        Gremlind fixing = gremlindMap[repaired];
        gremlindMap.Remove(repaired);
        fixing.Effect.SetParent(null);
        fixing.Effect.gameObject.SetActive(false);
        repaired.transform.root.tag = fixing.PreviousTag;
        particleSystemPool[fixing.FailType].Add(fixing.Effect);
        if (fixing.FailType == FailureType.Electrical)
        {
            FlowManager.Instance.PowerGrids.OnElectricalFailureChange(repaired);
        }
        Game.Current.Player.GremlinChastised = true;
        GuiBridge.Instance.ShowNews(NewsSource.MalfunctionRepaired);
        PlayerInput.Instance.ToggleRepairMode(false);
    }

    public enum FailureType { Electrical, Pressure }
    private Dictionary<FailureType, List<Transform>> particleSystemPool = new Dictionary<FailureType, List<Transform>>()
    {
        { FailureType.Electrical, new List<Transform>() },
        { FailureType.Pressure, new List<Transform>() }
    };
    private Coroutine LurkCoroutine;

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
            result.gameObject.SetActive(true);
            return result;
        }
        else
        {
            return (Transform)GameObject.Instantiate(GetParticleSystemPrefab(failType));
        }
    }
}
