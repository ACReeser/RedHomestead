using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using RedHomestead.Simulation;

public class GasStorage : SingleResourceSink, ICrateSnapper {
    public MeshFilter MeshFilter;
    public Mesh[] CompoundUVSet = new Mesh[6];
    public Mesh UnspecifiedUV;
    public Color[] CompoundColors = new Color[6];
    public Transform SnapAnchor, PumpHandle;
    public SpriteRenderer IconRenderer;
    public Sprite[] PumpSprites;
    public Color OnColor, OffColor;
    public AudioClip HandleChangeClip;

    internal enum PumpStatus { PumpOff, PumpIn, PumpOut }
    internal PumpStatus CurrentPumpStatus;

    public const float PumpPerSecond = .25f;
    public const float PumpUpdateIntervalSeconds = .25f;
    public const float PumpPerUpdateInterval = PumpPerSecond / PumpUpdateIntervalSeconds;
    private const float SnapInterferenceTimerSeconds = 1.25f;

    public override float WattRequirementsPerTick
    {
        get
        {
            return 0;
        }
    }

    // Use this for initialization
    protected override void OnStart()
    {
        base.OnStart();

        if (this.SinkType != Matter.Unspecified)
            _SpecifyCompound(this.SinkType);
        else
            SyncMeshToCompoundType();

        RefreshPumpState();
    }

    private void SyncMeshToCompoundType()
    {
        RefreshMeshToCompound();
        SetValveTagsToCompound(this.transform);
    }

    //todo: bug
    //assumes that all interaction will be valves
    private void SetValveTagsToCompound(Transform t)
    {
        foreach(Transform child in t){
            //8 == interaction
            if (child.gameObject.layer == 8)
            {
                child.tag = PlayerInput.GetValveFromCompound(this.SinkType);
            }

            SetValveTagsToCompound(child);
        }
    }

    private void RefreshMeshToCompound()
    {
        if (this.SinkType == Matter.Unspecified)
        {
            this.MeshFilter.mesh = UnspecifiedUV;
            flowAmountRenderer.color = CompoundColors[0];
        }
        else
        {
            int index = (int)this.SinkType + 6;
            if (index < CompoundUVSet.Length && CompoundUVSet[index] != null)
            {
                this.MeshFilter.mesh = CompoundUVSet[index];
            }
            flowAmountRenderer.color = CompoundColors[index];
        }
    }

    // Update is called once per frame
    void Update()
    {
        float percentage = 0f;
        if (Container != null)
        {
            percentage = Container.UtilizationPercentage;
        }

        flowAmountRenderer.transform.localScale = new Vector3(1, percentage, 1);
    }

    public void SpecifyCompound(Matter c)
    {
        if (this.SinkType == Matter.Unspecified)
        {
            _SpecifyCompound(c);
        }
        else
        {
            print("cannot set this storage to compound type "+c.ToString());
        }
    }

    private void _SpecifyCompound(Matter c)
    {
        this.SinkType = c;
        SyncMeshToCompoundType();

        if (c == Matter.Unspecified)
        {
            this.Container = null;
        }
        else
        {
            this.Container = new ResourceContainer(StartAmount)
            {
                TotalCapacity = Capacity,
                MatterType = this.SinkType
            };
        }
    }

    public override void Report()
    {
        throw new NotImplementedException();
    }

    public override void OnAdjacentChanged()
    {
        base.OnAdjacentChanged();

        if (Adjacent.Count == 0 && Container != null && Container.UtilizationPercentage <= 0f)
        {
            _SpecifyCompound(Matter.Unspecified);
        }
    }

    private ResourceComponent capturedResource, lastCapturedResource;
    private Coroutine Pumping, CrateInterferenceTimer;

    void OnTriggerEnter(Collider other)
    {
        ResourceComponent res = other.GetComponent<ResourceComponent>();

        if (res != null &&
            this.SinkType != Matter.Unspecified &&
            this.SinkType == res.Info.ResourceType &&
            capturedResource == null &&
            (res != lastCapturedResource || CrateInterferenceTimer == null))
        {
            CaptureResource(other, res);
        }
    }

    private void CaptureResource(Collider other, ResourceComponent res)
    {
        capturedResource = res;
        res.SnapCrate(this, SnapAnchor.position);
        RefreshPumpState();
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

            Pumping = StartCoroutine(Pump(false));
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

            Pumping = StartCoroutine(Pump(true));
        }

        RefreshPumpState();
    }

    private IEnumerator Pump(bool pumpIn)
    {
        SoundSource.Play();

        while(isActiveAndEnabled && CurrentPumpStatus != PumpStatus.PumpOff && capturedResource != null)
        {
            if (pumpIn)
            {
                if (capturedResource.Info.Quantity < PumpPerUpdateInterval)
                    break;
                else
                {
                    capturedResource.Info.Quantity -= PumpPerUpdateInterval;

                    float excess = Container.Push(PumpPerUpdateInterval);

                    if (excess > 0)
                    {
                        capturedResource.Info.Quantity = excess;
                        break;
                    }
                }
            }
            else
            {
                float amount = Container.Pull(PumpPerUpdateInterval);

                capturedResource.Info.Quantity += amount;

                if (amount <= 0f)
                    break;
            }

            yield return new WaitForSeconds(PumpUpdateIntervalSeconds);
        }

        SoundSource.Stop();
        CurrentPumpStatus = PumpStatus.PumpOff;
        RefreshPumpState();
    }

    private void RefreshPumpState()
    {
        switch (this.CurrentPumpStatus)
        {
            case PumpStatus.PumpOff:
                this.IconRenderer.flipY = false;
                this.PumpHandle.localRotation = Quaternion.Euler(0, 90, 0);
                break;
            case PumpStatus.PumpIn:
                this.IconRenderer.flipY = true;
                this.PumpHandle.localRotation = Quaternion.Euler(0, 180, 0);
                break;
            case PumpStatus.PumpOut:
                this.IconRenderer.flipY = false;
                this.PumpHandle.localRotation = Quaternion.identity;
                break;
        }

        this.IconRenderer.sprite = this.CurrentPumpStatus == PumpStatus.PumpOff ? this.PumpSprites[0] : this.PumpSprites[1];
        this.IconRenderer.color = this.CurrentPumpStatus == PumpStatus.PumpOff ? this.OffColor : this.OnColor;

        if (capturedResource != null)
        {
            capturedResource.transform.tag = this.CurrentPumpStatus == PumpStatus.PumpOff ? "movable" : "Untagged";
            PumpHandle.tag = "pumpHandle";
        }
        else
        {
            PumpHandle.tag = "Untagged";
        }
    }

    public void DetachCrate(ResourceComponent detaching)
    {
        this.lastCapturedResource = this.capturedResource;
        this.capturedResource = null;
        PumpHandle.tag = "Untagged";
        this.CrateInterferenceTimer = StartCoroutine(CrateInterferenceCountdown());
    }

    private IEnumerator CrateInterferenceCountdown()
    {
        yield return new WaitForSeconds(SnapInterferenceTimerSeconds);
        this.CrateInterferenceTimer = null;
    }
}
