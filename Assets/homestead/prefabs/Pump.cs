using RedHomestead.Industry;
using RedHomestead.Persistence;
using RedHomestead.Simulation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Pump : MovableSnappable, ICrateSnapper, ITriggerSubscriber, IDataContainer<FacingData>, ISink
{
    private FacingData data;
    public FacingData Data { get { return data; } set { data = value; } }

    public List<IPumpable> AdjacentPumpables { get; set; }

    public Transform CrateAnchor, PumpHandle;
    public SpriteRenderer IconRenderer;
    public Sprite[] PumpSprites;
    public Color OnColor, OffColor;
    public AudioClip HandleChangeClip;
    public AudioSource SoundSource;

    public Mesh[] matterTypeMeshes;
    public MeshFilter pumpFilter;

    internal enum PumpStatus { PumpOff, PumpIn, PumpOut, PumpOpen }
    internal PumpStatus CurrentPumpStatus;
    internal PromptInfo CurrentPromptBasedOnPumpStatus;
    public bool PumpMode { get { return connectedSink != null && connectedSink.HasContainerFor(this.valveType); } }
    public bool ValveMode { get { return !PumpMode; } }

    private IPumpable connectedPumpable;
    private ISink connectedSink;
    private Matter valveType = Matter.Unspecified;

    public const float PumpPerSecond = .04f;
    public const float PumpUpdateIntervalSeconds = .25f;
    public const float PumpPerUpdateInterval = PumpPerSecond / PumpUpdateIntervalSeconds;
    private const float SnapInterferenceTimerSeconds = 1.25f;
    private Coroutine detachTimer;
    private ResourceComponent capturedResource;
    private Coroutine Pumping;

    public override string GetText()
    {
        return "Pump";
    }

    void Start()
    {
        this.AdjacentPumpables = new List<IPumpable>();
        RefreshPumpState();
    }

    //handled by AttachTo
    //protected override void OnSnap() {}

    protected override void OnDetach()
    {
        if (connectedPumpable != null)
        {
            IndustryExtensions.RemoveAdjacentPumpable(this, connectedPumpable);
            connectedPumpable = null;
            connectedSink = null;
        }
        PumpHandle.tag = "Untagged";
        CurrentPumpStatus = PumpStatus.PumpOff;
        RefreshPumpState();

        if (capturedResource != null)
            capturedResource.UnsnapCrate();
    }

    public void DetachCrate(IMovableSnappable detaching)
    {
        capturedResource = null;
        detachTimer = StartCoroutine(DetachCrateTimer());
        PumpHandle.tag = "Untagged";
        CurrentPumpStatus = PumpStatus.PumpOff;
        RefreshPumpState();

        if (connectedPumpable != null)
            connectedPumpable.OnAdjacentChanged(); //trigger a recalculate for the connected thing
    }

    private IEnumerator DetachCrateTimer()
    {
        yield return new WaitForSeconds(1f);
        detachTimer = null;
    }

    public void OnChildTriggerEnter(TriggerForwarder child, Collider c, IMovableSnappable res)
    {
        if (res == null && child.transform.name == "PumpSnap")
        {
            string valveTag = c.tag.ToLower();
            if (valveTag.EndsWith("valve"))
            {
                AttachTo(c, valveTag);
            }
        }
        else if (detachTimer == null && res is ResourceComponent)
        {
            if (ResourceMatchesCurrentPumpable(res as ResourceComponent))
            {
                capturedResource = res as ResourceComponent;
                capturedResource.SnapCrate(this, CrateAnchor.position);

                if (valveType == Matter.Unspecified)
                {
                    if (connectedPumpable is GasStorage && (connectedPumpable as GasStorage).Data.Container.CurrentAmount <= 0f)
                    {
                        this.valveType = capturedResource.Data.Container.MatterType;
                        (connectedPumpable as GasStorage).SpecifyCompound(capturedResource.Data.Container.MatterType);
                        this.SyncMeshesToMatterType();
                    }
                }

                RefreshPumpState();
            }
        }
    }

    private void AttachTo(Collider c, string valveTag)
    {
        connectedPumpable = c.transform.root.GetComponent<IPumpable>();
        connectedSink = connectedPumpable as ISink;
        print("valve mode: " + ValveMode);
        print("pump mode: " + PumpMode);

        if (connectedPumpable != null)
        {
            valveType = MatterExtensions.FromValveTag(valveTag);
            SyncMeshesToMatterType();
            this.SnapCrate(c.transform, c.transform.position + (c.transform.TransformDirection(Vector3.forward) * 1.3f));
            IndustryExtensions.AddAdjacentPumpable(this, connectedPumpable);
            RefreshPumpState();
        }
    }

    private void SyncMeshesToMatterType()
    {
        pumpFilter.mesh = matterTypeMeshes[Math.Abs((int)this.valveType)];
    }

    private bool ResourceMatchesCurrentPumpable(ResourceComponent resourceComponent)
    {
        return (connectedPumpable != null) && ((resourceComponent.Data.Container.MatterType == valveType) || (valveType == Matter.Unspecified));
    }

    public void OnAdjacentChanged()
    {
        if (AdjacentPumpables.Count > 0)
            connectedPumpable = AdjacentPumpables[0];
        else
            connectedPumpable = null;
    }

    public void StartPumpingOut()
    {
        if (CurrentPumpStatus == PumpStatus.PumpIn)
        {
            CurrentPumpStatus = PumpStatus.PumpOff;
        }
        else if (CurrentPumpStatus == PumpStatus.PumpOff)
        {
            CurrentPumpStatus = PumpStatus.PumpOut;
            if (Pumping != null)
                StopCoroutine(Pumping);

            Pumping = StartCoroutine(DoPump(false));
        }

        RefreshPumpState();
    }

    public void StartPumpingIn()
    {
        if (CurrentPumpStatus == PumpStatus.PumpOut)
        {
            CurrentPumpStatus = PumpStatus.PumpOff;
        }
        else if (CurrentPumpStatus == PumpStatus.PumpOff)
        {
            CurrentPumpStatus = PumpStatus.PumpIn;

            if (Pumping != null)
                StopCoroutine(Pumping);

            Pumping = StartCoroutine(DoPump(true));
        }

        RefreshPumpState();
    }

    public void ToggleValve()
    {
        if (CurrentPumpStatus == PumpStatus.PumpOpen)
        {
            CurrentPumpStatus = PumpStatus.PumpOff;

            if (connectedPumpable != null)
                connectedPumpable.OnAdjacentChanged(); //trigger a recalculate for the connected thing
        }
        else if (CurrentPumpStatus == PumpStatus.PumpOff)
        {
            CurrentPumpStatus = PumpStatus.PumpOpen;

            if (connectedPumpable != null)
                connectedPumpable.OnAdjacentChanged(); //trigger a recalculate for the connected thing
        }

        RefreshPumpState();
    }

    private IEnumerator DoPump(bool pumpIn)
    {
        SoundSource.Play();

        while (isActiveAndEnabled && CurrentPumpStatus != PumpStatus.PumpOff && capturedResource != null && connectedPumpable != null)
        {
            if (pumpIn)
            {
                if (capturedResource.Data.Container.CurrentAmount < PumpPerUpdateInterval)
                    break;
                else
                {
                    float amountFromCrate = capturedResource.Data.Container.Pull(PumpPerUpdateInterval);
                    float excessToAttached = PushToAttached(amountFromCrate);

                    if (excessToAttached > 0f)
                        capturedResource.Data.Container.Push(excessToAttached);

                    if (amountFromCrate <= 0f)
                    {
                        break;
                    }
                }
            }
            else
            {
                float amountFromAttached = PullFromAttached(PumpPerUpdateInterval);
                float excessToCrate = capturedResource.Data.Container.Push(amountFromAttached);

                if (excessToCrate > 0)
                    PushToAttached(excessToCrate);

                if (amountFromAttached <= 0f)
                    break;
            }

            yield return new WaitForSeconds(PumpUpdateIntervalSeconds);
        }

        SoundSource.Stop();
        CurrentPumpStatus = PumpStatus.PumpOff;
        RefreshPumpState();
    }

    private float PullFromAttached(float amount)
    {
        if (this.connectedSink != null)
            return this.connectedSink.Get(valveType).Pull(amount);
        else
            return 0f;
    }

    private float PushToAttached(float amount)
    {
        if (this.connectedSink != null)
            return this.connectedSink.Get(valveType).Push(amount);
        else
            return 0f;
    }

    private void RefreshPumpState()
    {
        switch (this.CurrentPumpStatus)
        {
            case PumpStatus.PumpOff:
                this.PumpHandle.localRotation = Quaternion.Euler(0, 0, 0);
                this.IconRenderer.transform.localRotation = Quaternion.Euler(0, -90, 0);
                this.IconRenderer.sprite = this.PumpSprites[0];
                CurrentPromptBasedOnPumpStatus = this.ValveMode ? Prompts.OpenPumpHint : Prompts.TurnPumpOnHint;
                break;
            case PumpStatus.PumpIn:
                this.PumpHandle.localRotation = Quaternion.Euler(0, 90, 0);
                this.IconRenderer.transform.localRotation = Quaternion.Euler(0, -90, -90);
                this.IconRenderer.sprite = this.PumpSprites[1];
                CurrentPromptBasedOnPumpStatus = Prompts.StopPumpingInHint;
                break;
            case PumpStatus.PumpOut:
                this.PumpHandle.localRotation = Quaternion.Euler(0, -90, 0);
                this.IconRenderer.transform.localRotation = Quaternion.Euler(0, -90, 90);
                this.IconRenderer.sprite = this.PumpSprites[1];
                CurrentPromptBasedOnPumpStatus = Prompts.StopPumpingInHint;
                break;
            case PumpStatus.PumpOpen:
                this.PumpHandle.localRotation = Quaternion.Euler(0, 90, 0);
                this.IconRenderer.transform.localRotation = Quaternion.Euler(0, -90, 0);
                this.IconRenderer.sprite = this.PumpSprites[2];
                CurrentPromptBasedOnPumpStatus = Prompts.ClosePumpHint;
                break;
        }
        
        this.IconRenderer.color = this.CurrentPumpStatus == PumpStatus.PumpOff ? this.OffColor : this.OnColor;

        if (connectedPumpable != null && capturedResource != null)
        {
            capturedResource.transform.tag = this.CurrentPumpStatus == PumpStatus.PumpOff ? "movable" : "Untagged";
            PumpHandle.tag = "pumpHandle";
        }
        else
        {
            PumpHandle.tag = "Untagged";
        }
    }

    public ResourceContainer Get(Matter c)
    {
        if (HasContainerFor(c))
            return capturedResource.data.Container;
        else
            return null;
    }

    public bool HasContainerFor(Matter c)
    {
        return capturedResource != null && capturedResource.data.Container.MatterType == c && CurrentPumpStatus == PumpStatus.PumpOpen;
    }
}
