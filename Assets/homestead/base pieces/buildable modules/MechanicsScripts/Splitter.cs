using UnityEngine;
using System.Collections;
using System;
using RedHomestead.Simulation;
using RedHomestead.Buildings;
using System.Linq;
using RedHomestead.Industry;

//acts like a sink
//but actually converts internal amount to half amounts
public class Splitter : Converter, ISink
{
    internal float FlowPerSecond = .1f;

    public override float WattsConsumed
    {
        get
        {
            return 0f;
        }
    }

    private ModuleGameplay Input, OutputOne, OutputTwo;

    private Matter SplitterMatterType
    {
        get
        {
            if (Data.Containers.Keys.Count == 1)
            {
                return Data.Containers.Values.First().MatterType;
            }
            else
            {
                return Matter.Unspecified;
            }
        }
    }
    
    private ResourceContainer Container
    {
        get
        {
            if (Data.Containers.Keys.Count == 1)
            {
                return Data.Containers.Values.First();
            }
            else
            {
                return null;
            }

        }
    }

    private bool IsFullyConnected
    {
        get
        {
            return Input != null && OutputOne != null && OutputTwo != null;
        }
    }

    private bool IsPartiallyConnected
    {
        get
        {
            return Input != null && (OutputOne != null || OutputTwo != null);
        }
    }

    public override void Convert()
    {
        if (IsFullyConnected)
        {
            if (PullFromTank())
            {
                PushSplit();
            }
        }
        else if (IsPartiallyConnected)
        {
            if (PullFromTank())
            {
                PushSingle();
            }
        }
    }

    private float matterBuffer = 0f;
    private bool PullFromTank()
    {
        if (Container != null)
        {
            float newWater = Container.Pull(FlowPerSecond * Time.fixedDeltaTime);
            matterBuffer += newWater;

            float matterThisTick = FlowPerSecond * Time.fixedDeltaTime;

            if (matterBuffer >= matterThisTick)
            {
                matterBuffer -= matterThisTick;
                return true;
            }
        }

        return false;
    }

    protected override void OnStart()
    {
        base.OnStart();
    }

    private void PushSplit()
    {
        Matter m = SplitterMatterType;
        OutputOne.Get(m).Push(FlowPerSecond / 2f * Time.fixedDeltaTime);
        OutputTwo.Get(m).Push(FlowPerSecond / 2f * Time.fixedDeltaTime);
    }

    private void PushSingle()
    {
        Matter m = SplitterMatterType;

        if (OutputOne != null)
            OutputOne.Get(m).Push(FlowPerSecond * Time.fixedDeltaTime);
        else if (OutputTwo != null)
            OutputTwo.Get(m).Push(FlowPerSecond * Time.fixedDeltaTime);
    }

    public override void ClearHooks()
    {
        OutputOne = OutputTwo = null;
    }

    public override void OnAdjacentChanged()
    {
        if (Adjacent.Count == 0)
        {
            this.Data.Containers.Clear();
            RefreshValveTags();
        }

        base.OnAdjacentChanged();
    }

    private void RefreshValveTags()
    {
    }

    public override void OnSinkConnected(ISink s)
    {
        if (this.Data.Containers.Keys.Count == 0)
        {
            if (s is SingleResourceModuleGameplay)
            {
                this.Data.Containers.Add((s as SingleResourceModuleGameplay).Data.Container.MatterType, new ResourceContainer()
                {
                    MatterType = (s as SingleResourceModuleGameplay).Data.Container.MatterType,
                    TotalCapacity = 1f
                });
            }

            RefreshValveTags();
        }
        else
        {
            if (s.HasContainerFor(this.SplitterMatterType) && s is ModuleGameplay)
            {
                if (OutputOne == null)
                    OutputOne = s as ModuleGameplay;
                else if (OutputTwo == null)
                    OutputTwo = s as ModuleGameplay;
            }
        }
    }

    public override void Report()
    {
        throw new NotImplementedException();
    }
    

    public override Module GetModuleType()
    {
        return Module.Splitter;
    }

    public override ResourceContainerDictionary GetStartingDataContainers()
    {
        return new ResourceContainerDictionary()
        {

        };
    }
}
