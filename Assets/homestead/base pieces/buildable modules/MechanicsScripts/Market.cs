using RedHomestead.Buildings;
using RedHomestead.Simulation;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarketRow : ResourceRow<Market>
{
    public MarketRow(Market parent, Vector3 rowCenter) : base(parent, rowCenter)
    {
    }

    public bool Release(ResourceComponent res, out MarketRow row)
    {
        ResourceRow<Market> fromParent = null;
        bool released = _Release(res, out fromParent);
        row = fromParent as MarketRow;
        return released;
    }
}

public class Market : ResourcelessGameplay, ICrateSnapper, ITriggerSubscriber, IWarehouse
{
    static Market()
    {
        GlobalResourceList = new ResourceList(OnResourcesDepleted);
        Markets = new List<Market>();
    }
    public static readonly List<Market> Markets;
    public static readonly ResourceList GlobalResourceList;
    public static void OnResourcesDepleted(List<ResourceComponent> resources)
    {
        foreach (ResourceComponent res in resources)
        {
            foreach (Market w in Markets)
            {
                w.DetachCrate(res);
            }
            GameObject.Destroy(res.gameObject);
        }
    }

    private MarketRow left, right;
    private Dictionary<MarketRow, Coroutine> CrateInterferenceTimers = new Dictionary<MarketRow, Coroutine>();
    private const float SnapInterferenceTimerSeconds = 1.25f;

    public override float WattsConsumed
    {
        get
        {
            return 0f;
        }
    }

    // Use this for initialization
    protected override void OnStart()
    {
        Markets.Add(this);
        left = new MarketRow(this, transform.GetChild(1).position);
        right = new MarketRow(this, transform.GetChild(2).position);
    }

    void OnDestroy()
    {
        Markets.Remove(this);
    }


    public void OnChildTriggerEnter(TriggerForwarder child, Collider other, IMovableSnappable res)
    {
        MarketRow whichRow = GetRow(child.name);
        if (whichRow != null && res is ResourceComponent && !CrateInterferenceTimers.ContainsKey(whichRow))
        {
            whichRow.Capture(this, res as ResourceComponent);
        }
    }

    private MarketRow GetRow(string childName)
    {
        switch (childName)
        {
            case "leftRow":
                return left;
            case "rightRow":
                return right;
            //case "centerRow":
            //    return middle;
        }

        return null;
    }

    public void DetachCrate(IMovableSnappable detaching)
    {
        MarketRow wasAttachedTo = null;
        //we don't know which row we are detaching from! so...
        //we have to search them all possibly
        //short circuit hack = if one of them returns true, the remaining calls will not be made
        if (this.left.Release(detaching as ResourceComponent, out wasAttachedTo) ||
           //this.middle.Release(detaching as ResourceComponent, out wasAttachedTo) ||
           this.right.Release(detaching as ResourceComponent, out wasAttachedTo))
        {
            this.CrateInterferenceTimers.Add(wasAttachedTo, StartCoroutine(CrateInterferenceCountdown(wasAttachedTo)));
        }
    }

    private IEnumerator CrateInterferenceCountdown(MarketRow row)
    {
        yield return new WaitForSeconds(SnapInterferenceTimerSeconds);

        this.CrateInterferenceTimers.Remove(row);
    }

    public override void OnAdjacentChanged() { }

    public override void Tick() { }

    public override void Report() { }

    public override void InitializeStartingData()
    {
        this.Data = new ResourcelessModuleData()
        {
            ModuleInstanceID = Guid.NewGuid().ToString(),
            ModuleType = GetModuleType()
        };
    }

    public override Module GetModuleType()
    {
        return Module.Market;
    }

    public void AddToGlobalList(ResourceComponent res)
    {
        Market.GlobalResourceList.Add(res);
    }

    public void RemoveFromGlobalList(ResourceComponent res)
    {
        Market.GlobalResourceList.Remove(res);
    }
}
