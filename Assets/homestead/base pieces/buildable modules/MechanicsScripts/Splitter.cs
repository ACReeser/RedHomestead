using UnityEngine;
using System.Collections;
using System;
using RedHomestead.Simulation;

//acts like a sink
//but actually converts internal amount to half amounts
public class Splitter : Converter, ISink
{
    internal float FlowPerSecond = .1f;
    internal const int ContainerBufferFlowSeconds = 4;
    internal float ContainerBufferAmount
    {
        get
        {
            return FlowPerSecond * ContainerBufferFlowSeconds;
        }
    }

    public override float WattRequirementsPerTick
    {
        get
        {
            return 0f;
        }
    }

    private ResourceContainer SplitterTank;
    private bool HaveMatterType
    {
        get
        {
            return SplitterTank != null && SplitterTank.MatterType != Matter.Unspecified;
        }
    }
    private ISink OutSinkOne, OutSinkTwo;

    private bool IsFullyConnected
    {
        get
        {
            return HaveMatterType && OutSinkOne != null && OutSinkTwo != null;
        }
    }

    private bool IsPartiallyConnected
    {
        get
        {
            return HaveMatterType && (OutSinkOne != null || OutSinkTwo != null);
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
        if (SplitterTank != null)
        {
            float newWater = SplitterTank.Pull(FlowPerSecond * Time.fixedDeltaTime);
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
        OutSinkOne.Get(SplitterTank.MatterType).Push(FlowPerSecond / 2f * Time.fixedDeltaTime);
        OutSinkTwo.Get(SplitterTank.MatterType).Push(FlowPerSecond / 2f * Time.fixedDeltaTime);
    }

    private void PushSingle()
    {
        if (OutSinkOne != null)
            OutSinkOne.Get(SplitterTank.MatterType).Push(FlowPerSecond * Time.fixedDeltaTime);
        else if (OutSinkTwo != null)
            OutSinkTwo.Get(SplitterTank.MatterType).Push(FlowPerSecond * Time.fixedDeltaTime);
    }

    public override void ClearHooks()
    {
        OutSinkOne = OutSinkTwo = null;
    }

    public override void OnAdjacentChanged()
    {
        if (Adjacent.Count == 0)
        {
            SplitterTank = null;
            RefreshValveTags();
        }

        base.OnAdjacentChanged();
    }

    private void RefreshValveTags()
    {
    }

    public override void OnSinkConnected(ISink s)
    {
        if (this.SplitterTank == null)
        {
            SplitterTank = new ResourceContainer(ContainerBufferAmount)
            {
                //???
                //how do we know what this tank holds? what this connection is?
                //we could brute force it
                //MatterType =
            };
            RefreshValveTags();
        }
        else
        {
            if (s.HasContainerFor(this.SplitterTank.MatterType))
            {
                if (OutSinkOne == null)
                    OutSinkOne = s;
                else if (OutSinkTwo == null)
                    OutSinkTwo = s;
            }
        }
    }

    public override void Report()
    {
        throw new NotImplementedException();
    }

    public ResourceContainer Get(Matter c)
    {
        if (HaveMatterType && SplitterTank.MatterType == c)
        {
            return SplitterTank;
        }
        else
        {
            return null;
        }
    }

    public bool HasContainerFor(Matter c)
    {
        if (SplitterTank == null)
            return false;
        else
            return c == SplitterTank.MatterType;
    }
}
