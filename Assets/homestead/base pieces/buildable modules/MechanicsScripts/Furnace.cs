using RedHomestead.Simulation;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RedHomestead.Buildings;
using RedHomestead.Electricity;
using RedHomestead.Persistence;
using RedHomestead.Industry;

[Serializable]
public class FurnaceFlexData
{
    public EnergyContainer Heat;
}

public class Furnace : Converter, ITriggerSubscriber, ICrateSnapper, IPowerConsumerToggleable, IFlexDataContainer<MultipleResourceModuleData, FurnaceFlexData>
{
    public Transform[] lifts;
    public Transform platform, lever;
    public TriggerForwarder oreSnap, powderSnap;
    public ParticleSystem oreParticles;

    private float[] liftMax = new float[]
    {
        .9635f,
        1.734288f,
        2.531137f,
        3.349197f
    };
    private float platformMax = 3.795f;
    private const float noTiltX = -90f, tiltX = -160f;
    private const float leverPlatformDownY = -30f, leverPlatformUpY = -90f;
    
    private bool isPlatformUp = false;
    private bool isRaisingAndDumping = false;
    private bool isTiltingAndLowering = false;

    private Coroutine lerpHydro;

    private Formula formula;
    
    protected override void OnStart () {
        this.ToggleHydraulics(false);
        
        formula = new Formula(Data.Containers, new FormulaComponent()
        {
            Matter = Matter.CarbonDioxide,
            ReactionRate = .001f
        }, new FormulaComponent()
        {
            Matter = Matter.Hydrogen,
            ReactionRate = .001f
        });

        base.OnStart();
	}

    internal void ToggleHydraulicLiftLever()
    {
        if (this.capturedOre != null && !isRaisingAndDumping && !isTiltingAndLowering)
        {
            this.lever.localRotation = Quaternion.Euler(isPlatformUp ? leverPlatformUpY : leverPlatformDownY, 90f, 90f);
            this.ToggleHydraulics();
        }
    }

    internal void ToggleHydraulics(bool? newPlatformUp = null)
    {
        if (!newPlatformUp.HasValue)
            newPlatformUp = !this.isPlatformUp;

        this.isRaisingAndDumping = newPlatformUp.Value; //old down, new up
        this.isTiltingAndLowering = !newPlatformUp.Value; //old up, new down

        if (this.lerpHydro != null)
            StopCoroutine(this.lerpHydro);

        this.lerpHydro = StartCoroutine(LerpHydraulic());
    }

    private IEnumerator LerpHydraulic()
    {
        if (this.isTiltingAndLowering)
        {
            yield return Tilt(true);
        }

        float duration = 2f;
        float time = 0f;

        while (time < duration)
        {
            for (int i = 0; i < lifts.Length; i++)
            {
                Transform t = lifts[i];
                t.localPosition = new Vector3(
                    t.localPosition.x, 
                    Mathf.Lerp(isTiltingAndLowering ? liftMax[i] : 0f, isTiltingAndLowering ? 0f : liftMax[i], time / duration), 
                    t.localPosition.z);
            }
            platform.localPosition = new Vector3(
                platform.localPosition.x, 
                Mathf.Lerp(isTiltingAndLowering ? platformMax : 0.624f, isTiltingAndLowering ? 0.624f : platformMax, time / duration), 
                platform.localPosition.z);
            yield return null;

            time += Time.deltaTime;
        }

        if (this.isRaisingAndDumping)
        {
            yield return Tilt(false);

            oreParticles.Play();

            this.Data.Containers[Matter.IronOre].Push(capturedOre.Data.Container.Pull(1f));

            yield return new WaitForSeconds(oreParticles.main.duration);
        }
        
        this.isPlatformUp = this.isRaisingAndDumping;
        this.isRaisingAndDumping = false;
        this.isTiltingAndLowering = false;
    }

    private IEnumerator Tilt(bool isTiltToStraight)
    {
        float duration = .5f;
        float time = 0f;

        while (time < duration)
        {
            platform.localRotation = Quaternion.Euler(Mathf.Lerp(isTiltToStraight ? tiltX : noTiltX, isTiltToStraight ? noTiltX : tiltX, time / duration), 90f, 90f);
            yield return null;
            time += Time.deltaTime;
        }
    }

    private ResourceComponent capturedOre, capturedPowder;
    private Coroutine unsnapTimer;
    private float oreMeltPerTickUnits = .01f;
    private const float minimumHeat = .5f;
    private const float heatLossPerTickUnits = .05f;
    private const float heatProductionPerTickUnits = .1f;

    public override float WattsConsumed
    {
        get
        {
            return ElectricityConstants.WattsPerBlock * 3f;
        }
    }

    public FurnaceFlexData FlexData { get; set; }
    public MeshFilter powerCabinet;
    public MeshFilter PowerCabinet
    {
        get
        {
            return powerCabinet;
        }
    }

    public void OnChildTriggerEnter(TriggerForwarder child, Collider c, IMovableSnappable movesnap)
    {
        var res = c.GetComponent<ResourceComponent>();
        if (res != null)
        {
            bool isOre = res.Data.Container.MatterType.IsRawMaterial();
            bool isPowder = res.Data.Container.MatterType.IsFurnaceOutput();

            if (isOre && capturedOre == null && child == oreSnap)
            {
                if (capturedPowder == null || matches(res.Data.Container.MatterType, capturedPowder.Data.Container.MatterType))
                {
                    res.SnapCrate(this, child.transform.position);
                    res.transform.SetParent(platform);
                    capturedOre = res;
                }
                else
                {
                    GuiBridge.Instance.ShowNews(NewsSource.InvalidSnap);
                    return;
                }
            }
            else if (isPowder && capturedPowder == null && child == powderSnap)
            {
                if (capturedOre == null || matches(capturedOre.Data.Container.MatterType, res.Data.Container.MatterType))
                {
                    res.SnapCrate(this, child.transform.position);
                    capturedPowder = res;
                }
                else
                {
                    GuiBridge.Instance.ShowNews(NewsSource.InvalidSnap);
                    return;
                }
            }
            else
            {
                GuiBridge.Instance.ShowNews(NewsSource.InvalidSnap);
                return;
            }
        }
    }

    public void DetachCrate(IMovableSnappable detaching)
    {
        var res = detaching.transform.GetComponent<ResourceComponent>();
        if (res == capturedOre)
        {
            capturedOre = null;
            res.transform.SetParent(null);
        }
        else if (res == capturedPowder)
        {
            capturedPowder = null;
        }
        unsnapTimer = StartCoroutine(UnsnapTimer());
    }

    private IEnumerator UnsnapTimer()
    {
        yield return new WaitForSeconds(2f);
        unsnapTimer = null;
    }

    private bool matches(Matter ore, Matter powder)
    {
        return System.Convert.ToInt32(ore) + 19 == System.Convert.ToInt32(powder);
    }

    public override void Convert()
    {
        if (HasPower && IsOn && formula.TryCombine())
        {
            FlexData.Heat.Push(heatProductionPerTickUnits);
        }

        if (FlexData.Heat.CurrentAmount >= minimumHeat &&
            Data.Containers[Matter.IronOre].CurrentAmount >= oreMeltPerTickUnits)
        {
            Data.Containers[Matter.IronOre].Pull(oreMeltPerTickUnits);

            if (capturedPowder == null)
            {
#warning furnace does not create powder crate yet if none assigned
            }
            else
            {
                capturedPowder.Data.Container.Push(oreMeltPerTickUnits);
            }
        }

        if (FlexData.Heat.CurrentAmount > 0)
            FlexData.Heat.Pull(heatLossPerTickUnits);
    }

    public override void ClearSinks()
    {
        formula.ClearContainers();
    }

    public override ResourceContainerDictionary GetStartingDataContainers()
    {
        this.FlexData = new FurnaceFlexData()
        {
            Heat = new EnergyContainer(0f)
            {
                EnergyType = Energy.Thermal
            }
        };

        return new ResourceContainerDictionary()
        {
            { Matter.IronOre, new ResourceContainer(Matter.IronOre, 0f) },
            { Matter.CarbonDioxide, new ResourceContainer(Matter.CarbonDioxide, 0f) },
            { Matter.Hydrogen, new ResourceContainer(Matter.Hydrogen, 0f) }
        };
    }

    public override void Report()
    {
    }

    public override Module GetModuleType()
    {
        return Module.Furnace;
    }

    public void OnEmergencyShutdown()
    {
    }

    public override void OnSinkConnected(ISink s) {
        formula.AddSink(s, Matter.CarbonDioxide);
        formula.AddSink(s, Matter.Hydrogen);
    }
}
