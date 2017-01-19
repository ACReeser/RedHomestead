using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using RedHomestead.Construction;
using RedHomestead.Simulation;

public abstract class ModuleGameplay : MonoBehaviour
{
    public SpriteRenderer PowerIndicator;

    public LocalEnergyHistory EnergyHistory = new LocalEnergyHistory();
    public LocalCompoundHistory CompoundHistory = new LocalCompoundHistory();

    private bool _hasPower;
    public bool HasPower {
        get { return _hasPower; }
        set {
            _hasPower = value;
            if (PowerIndicator != null) {
                PowerIndicator.enabled = !_hasPower;
            }
        }
    }

    public abstract float WattRequirementsPerTick { get; }
    protected List<ModuleGameplay> Adjacent = new List<ModuleGameplay>();
	
    public void LinkToModule(ModuleGameplay adjacent)
    {
        Adjacent.Add(adjacent);
        OnAdjacentChanged();
    }

    public void UnlinkFromModule(ModuleGameplay adjacent)
    {
        Adjacent.Remove(adjacent);
        OnAdjacentChanged();
    }

    void Start()
    {
        this.OnStart();
    }

    protected virtual void OnStart() { }
    public abstract void OnAdjacentChanged();
    public abstract void Tick();
}

public abstract class PowerSupply : ModuleGameplay
{
    public abstract float WattsPerTick { get; }
}

public abstract class Sink : ModuleGameplay
{
    protected List<Sink> SimilarAdjacentSinks = new List<Sink>();
    
    protected override void OnStart()
    {
        FlowManager.Instance.Sinks.Add(this);
    }

    public override void Tick()
    {
        this.Equalize();
    }

    public bool HasContainerFor(Compound c)
    {
        return Get(c) != null;
    }

    public abstract ResourceContainer Get(Compound c);
    public abstract void Equalize();
}

public abstract class SingleResourceSink : Sink
{
    public Compound SinkType;
    public ResourceContainer Container;
    public SpriteRenderer flowAmountRenderer;
    public float StartAmount, Capacity;

    public override ResourceContainer Get(Compound c)
    {
        if (SinkType == c)
        {
            return Container;
        }
        return null;
    }

    public override void Equalize()
    {
        //float totalAmount; int sinkCount;
        //foreach(Sink s in SimilarAdjacentSinks)
        //{
        //    ResourceContainer rc = s.Get(SinkType);
        //}

        if (flowAmountRenderer != null && Container != null)
            flowAmountRenderer.transform.localScale = new Vector3(1, Container.UtilizationPercentage, 1);
    }

    public override void OnAdjacentChanged()
    {
        this.SimilarAdjacentSinks.Clear();

        foreach(ModuleGameplay m in Adjacent)
        {
            if (m is Sink)
            {
                if ((m as Sink).HasContainerFor(SinkType))
                {
                    this.SimilarAdjacentSinks.Add(m as Sink);
                }
            }
        }
    }
}

public abstract class Converter : ModuleGameplay
{
    protected override void OnStart()
    {
        FlowManager.Instance.Converters.Add(this);
    }

    public override void Tick()
    {
        this.Convert();
    }

    public abstract void Convert();

    public override void OnAdjacentChanged()
    {
        ClearHooks();

        foreach(ModuleGameplay m in Adjacent)
        {
            if (m is Sink)
            {
                OnSinkConnected(m as Sink);
            }
        }
    }

    public abstract void ClearHooks();
    public virtual void OnSinkConnected(Sink s) { }
}


public abstract class MultipleResourceConverter : Converter
{
    public Dictionary<Compound, ResourceContainer> Holding = new Dictionary<Compound, ResourceContainer>();
}


public class ResourceContainer
{
    public ResourceContainer() { }
    public ResourceContainer(float initialAmount)
    {
        this.Amount = initialAmount;
    }

    public Compound SimpleCompoundType;
    public Resource ComplexResourceType;

    public float TotalCapacity = 1f;
    protected float Amount;
    public float UtilizationPercentage
    {
        get
        {
            if (TotalCapacity <= 0)
                return 0;

            return Amount / TotalCapacity;
        }
    }
    public string UtilizationPercentageString()
    {
        return (int)(UtilizationPercentage * 100) + "%";
    }

    public float AvailableCapacity
    {
        get
        {
            return TotalCapacity - Amount;
        }
    }

    public float Push(float pushAmount)
    {
        if (AvailableCapacity > 0)
        {
            Amount += pushAmount;

            if (Amount > TotalCapacity)
            {
                float overage = Amount - TotalCapacity;

                Amount = TotalCapacity;

                return overage;
            }
            else
            {
                return 0;
            }
        }
        else
        {
            return pushAmount;
        }
    }

    public float Pull(float pullAmount)
    {
        if (Amount <= 0)
        {
            return 0;
        }
        else
        {
            if (Amount > pullAmount)
            {
                Amount -= pullAmount;

                return pullAmount;
            }
            else
            {
                float allToGive = Amount;

                Amount = 0;

                return allToGive;
            }
        }
    }
}

public class SumContainer : ResourceContainer
{
    public float LastTickRateOfChange;
    public float CurrentAmount
    {
        get
        {
            return Amount;
        }
    }

    public SumContainer() { }
    public SumContainer(float initialAmount): base(initialAmount) { }
}
