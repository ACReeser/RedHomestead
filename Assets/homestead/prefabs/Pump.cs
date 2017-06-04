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

    internal enum PumpStatus { PumpOff, PumpIn, PumpOut, PumpOpen }
    internal PumpStatus CurrentPumpStatus;

    private IPumpable connectedPumpable;
    private Matter valveType;

    public const float PumpPerSecond = .04f;
    public const float PumpUpdateIntervalSeconds = .25f;
    public const float PumpPerUpdateInterval = PumpPerSecond / PumpUpdateIntervalSeconds;
    private const float SnapInterferenceTimerSeconds = 1.25f;
    private Coroutine detachTimer;
    private ResourceComponent capturedResource;
    private Coroutine Pumping;
    private ISink connectedSink;

    public override string GetText()
    {
        return "Pump";
    }

    void Start()
    {
        this.AdjacentPumpables = new List<IPumpable>();
    }

    protected override void OnSnap()
    {
        
    }

    protected override void OnDetach()
    {
        //if (connectedPumpable != null)
        //{
        //    IndustryExtensions.RemoveAdjacentPumpable(this, connectedPumpable);
        //}     
    }

    public void DetachCrate(IMovableSnappable detaching)
    {
        capturedResource = null;
        detachTimer = StartCoroutine(DetachCrateTimer());
        PumpHandle.tag = "Untagged";
    }

    private IEnumerator DetachCrateTimer()
    {
        yield return new WaitForSeconds(1f);
        detachTimer = null;
    }

    public void OnChildTriggerEnter(TriggerForwarder child, Collider c, IMovableSnappable res)
    {
        print(child.transform.name);
        print(c.transform.name);
        print(c.transform.tag);
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
            }
        }
    }

    private void AttachTo(Collider c, string valveTag)
    {
        connectedPumpable = c.transform.root.GetComponent<IPumpable>();
        connectedSink = connectedPumpable as ISink;

        if (connectedPumpable != null)
        {
            valveType = MatterExtensions.FromValveTag(valveTag);
            IndustryExtensions.AddAdjacentPumpable(this, connectedPumpable);
            this.SnapCrate(c.transform, c.transform.TransformPoint(Vector3.forward * 2.65f));
        }
    }

    private bool ResourceMatchesCurrentPumpable(ResourceComponent resourceComponent)
    {
        return (connectedPumpable != null) && resourceComponent.Data.Container.MatterType == valveType;
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
                this.PumpHandle.localRotation = Quaternion.Euler(0, 90, 0);
                this.IconRenderer.sprite = this.PumpSprites[0];
                break;
            case PumpStatus.PumpIn:
                this.PumpHandle.localRotation = Quaternion.Euler(0, -90, 0);
                this.IconRenderer.sprite = this.PumpSprites[1];
                break;
            case PumpStatus.PumpOut:
                this.PumpHandle.localRotation = Quaternion.Euler(0, 90, 0);
                this.IconRenderer.sprite = this.PumpSprites[1];
                break;
            case PumpStatus.PumpOpen:
                this.PumpHandle.localRotation = Quaternion.Euler(0, 90, 0);
                this.IconRenderer.sprite = this.PumpSprites[2];
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
        if (capturedResource != null && capturedResource.data.Container.MatterType == c)
            return capturedResource.data.Container;
        else
            return null;
    }

    public bool HasContainerFor(Matter c)
    {
        return capturedResource != null && capturedResource.data.Container.MatterType == c;
    }
}
