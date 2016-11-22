using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class ModuleGameplay : MonoBehaviour {
    public Dictionary<Compound, ResourceContainer> Containers = new Dictionary<Compound, ResourceContainer>();

	// Use this for initialization
	void Start () {
        FlowManager.Instance.Nodes.Add(this);
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    public abstract void Tick();
}

public enum Compound { Hydrogen, Oxygen, CarbonMonoxide, CarbonDioxide, Methane, Water }

public class ResourceContainer
{
    public Compound CompoundResourceType;

    public float TotalCapacity = 1f;
    private float Amount;
    public float UtilizationPercentage
    {
        get
        {
            if (TotalCapacity <= 0)
                return 0;

            return Amount / TotalCapacity;
        }
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
