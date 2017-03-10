using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using RedHomestead.Simulation;
using RedHomestead.Buildings;

public class GasStorage : SingleResourceModuleGameplay, ICrateSnapper {
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
        
        SyncObjectsToCompound();
        RefreshPumpState();
    }

    private void SyncObjectsToCompound()
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
                child.tag = PlayerInput.GetValveFromCompound(this.Data.Container.MatterType);
            }

            SetValveTagsToCompound(child);
        }
    }

    private void RefreshMeshToCompound()
    {
        if (this.ResourceType == Matter.Unspecified)
        {
            this.MeshFilter.mesh = UnspecifiedUV;
            flowAmountRenderer.color = CompoundColors[0];
        }
        else
        {
            int index = (int)this.ResourceType + 6;
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
        if (this.Data.Container != null)
        {
            percentage = this.Data.Container.UtilizationPercentage;
        }

        flowAmountRenderer.transform.localScale = new Vector3(1, percentage, 1);
    }

    public void SpecifyCompound(Matter c)
    {
        if (this.ResourceType == Matter.Unspecified)
        {
            this.Data.Container.MatterType = c;
            SyncObjectsToCompound();
        }
        else
        {
            print("cannot set this storage to compound type "+c.ToString());
        }
    }

    public override void Report()
    {
        throw new NotImplementedException();
    }

    public override void OnAdjacentChanged()
    {
        if (Adjacent.Count == 0 && this.Data.Container != null && this.Data.Container.UtilizationPercentage <= 0f)
        {
            this.Data.Container.MatterType = Matter.Unspecified;
            SyncObjectsToCompound();
        }
    }

    private ResourceComponent capturedResource, lastCapturedResource;
    private Coroutine Pumping, CrateInterferenceTimer;

    void OnTriggerEnter(Collider other)
    {
        ResourceComponent res = other.GetComponent<ResourceComponent>();

        if (res != null &&
            this.ResourceType != Matter.Unspecified &&
            this.ResourceType == res.Data.ResourceType &&
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
                if (capturedResource.Data.Quantity < PumpPerUpdateInterval)
                    break;
                else
                {
                    capturedResource.Data.Quantity -= PumpPerUpdateInterval;

                    float excess = this.Data.Container.Push(PumpPerUpdateInterval);

                    if (excess > 0)
                    {
                        capturedResource.Data.Quantity = excess;
                        break;
                    }
                }
            }
            else
            {
                float amount = this.Data.Container.Pull(PumpPerUpdateInterval);

                capturedResource.Data.Quantity += amount;

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

    public override void Tick()
    {
    }

    public override Module GetModuleType()
    {
        return Module.SmallGasTank;
    }

    public override ResourceContainer GetStartingDataContainer()
    {
        if (this.Data == null)
            return new ResourceContainer()
            {
                MatterType = Matter.Unspecified,
                TotalCapacity = 10f
            };
        else
            return this.Data.Container;
    }
}
