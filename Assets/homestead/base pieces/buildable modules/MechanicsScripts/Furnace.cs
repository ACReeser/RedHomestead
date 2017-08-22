using RedHomestead.Simulation;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Furnace : MonoBehaviour, ITriggerSubscriber, ICrateSnapper
{

    public Transform[] lifts;
    public Transform platform;

    private float[] liftMax = new float[]
    {
        .9635f,
        1.734288f,
        2.531137f,
        3.349197f
    };
    private float platformMax = 3.795f;

	// Use this for initialization
	void Start () {
        this.ToggleHydraulics();
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    private bool platformUp = true;
    private Coroutine lerpHydro;

    internal void ToggleHydraulics()
    {
        this.platformUp = !this.platformUp;
        if (this.lerpHydro != null)
            StopCoroutine(this.lerpHydro);

        this.lerpHydro = StartCoroutine(LerpHydraulic(this.platformUp));
    }

    private IEnumerator LerpHydraulic(bool up)
    {
        float duration = 2f;
        float time = 0f;

        while (time < duration)
        {
            for (int i = 0; i < lifts.Length; i++)
            {
                Transform t = lifts[i];
                t.position = new Vector3(t.position.x, Mathf.Lerp(up ? liftMax[i] : 0f, up ? 0f : liftMax[i], time /duration), t.position.z);
            }
            platform.position = new Vector3(platform.position.x, Mathf.Lerp(up ? platformMax : 0.624f, up ? 0.624f : platformMax, time / duration), platform.position.z);
            yield return null;

            time += Time.deltaTime;
        }
    }

    private ResourceComponent capturedOre, capturedPowder;
    private Coroutine unsnapTimer;

    public void OnChildTriggerEnter(TriggerForwarder child, Collider c, IMovableSnappable movesnap)
    {
        var res = c.GetComponent<ResourceComponent>();
        if (res != null && res.Data.Container.MatterType.IsRawMaterial() && capturedOre == null)
        {
            res.SnapCrate(this, child.transform.position);
            res.transform.SetParent(platform);
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
        unsnapTimer = StartCoroutine(UnsnapTimer());
    }

    private IEnumerator UnsnapTimer()
    {
        yield return new WaitForSeconds(2f);
        unsnapTimer = null;
    }

    private bool matches(Matter ore, Matter powder)
    {
        return Convert.ToInt32(ore) + 9 == Convert.ToInt32(powder);
    }
}
