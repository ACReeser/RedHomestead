using RedHomestead.Simulation;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RedHomestead.Buildings;

public class Furnace : Converter, ITriggerSubscriber, ICrateSnapper
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

	// Use this for initialization
	void Start () {
        this.ToggleHydraulics();
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    private bool platformUp = false;
    private Coroutine lerpHydro;

    internal void ToggleHydraulicLiftLever()
    {
        if (this.capturedOre != null)
        {
            this.lever.localRotation = Quaternion.Euler(platformUp ? leverPlatformUpY : leverPlatformDownY, 90f, 90f);
            this.ToggleHydraulics();
        }
    }

    internal void ToggleHydraulics()
    {
        this.platformUp = !this.platformUp;
        if (this.lerpHydro != null)
            StopCoroutine(this.lerpHydro);

        this.lerpHydro = StartCoroutine(LerpHydraulic(this.platformUp));
    }

    private IEnumerator LerpHydraulic(bool up)
    {
        if (up)
        {
            yield return Tilt(up);
        }

        float duration = 2f;
        float time = 0f;

        while (time < duration)
        {
            for (int i = 0; i < lifts.Length; i++)
            {
                Transform t = lifts[i];
                t.position = new Vector3(t.position.x, Mathf.Lerp(up ? liftMax[i] : 0f, up ? 0f : liftMax[i], time / duration), t.position.z);
            }
            platform.position = new Vector3(platform.position.x, Mathf.Lerp(up ? platformMax : 0.624f, up ? 0.624f : platformMax, time / duration), platform.position.z);
            yield return null;

            time += Time.deltaTime;
        }

        if (!up)
        {
            yield return Tilt(up);

            oreParticles.Play();
        }
    }

    private IEnumerator Tilt(bool up)
    {
        float duration = .5f;
        float time = 0f;

        while (time < duration)
        {
            platform.localRotation = Quaternion.Euler(Mathf.Lerp(up ? tiltX : noTiltX, up ? noTiltX : tiltX, time / duration), 90f, 90f);
            yield return null;
            time += Time.deltaTime;
        }
    }

    private ResourceComponent capturedOre, capturedPowder;
    private Coroutine unsnapTimer;

    public override float WattsConsumed
    {
        get
        {
            throw new NotImplementedException();
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
        return System.Convert.ToInt32(ore) + 9 == System.Convert.ToInt32(powder);
    }

    public override void Convert()
    {
    }

    public override void ClearHooks()
    {
        capturedOre = null;
    }

    public override ResourceContainerDictionary GetStartingDataContainers()
    {
        return new ResourceContainerDictionary()
        {
            { Matter.IronOre, new ResourceContainer(Matter.IronOre, 0f) }
        };
    }

    public override void Report()
    {
    }

    public override Module GetModuleType()
    {
        return Module.Furnace;
    }
}
