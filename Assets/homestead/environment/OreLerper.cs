using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OreLerper : MonoBehaviour {
    private float[] ChildrenTime;
    private Coroutine lerping;
    private bool doSpawnNew;
    private Vector3 to;
    private Vector3 from;
    private const float LerpDuration = 1.2f;

    // Use this for initialization
    void Start () {
        ChildrenTime = new float[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            ChildrenTime[i] = GetDelay(i);
        }
        foreach (Transform t in transform)
        {
            t.position = transform.position + Vector3.down;
        }
    }

    private float GetDelay(int i)
    {
        return -i - UnityEngine.Random.Range(0, .75f);
    }

    // Update is called once per frame
    void Update () {
		
	}

    internal void Toggle(bool state, Vector3 position, Vector3 cratePosition, Material childMaterial)
    {
        this.transform.position = position;
        this.from = position + Vector3.down * .25f;
        this.to = cratePosition;
        if (state)
        {
            doSpawnNew = true;
            foreach(Transform t in transform)
            {
                t.GetComponent<MeshRenderer>().material = childMaterial;
                t.position = from;
            }
            lerping = StartCoroutine(Lerp());
        }
        else
        {
            doSpawnNew = false;
        }
    }

    private IEnumerator Lerp()
    {
        int activeParticles = 0;
        do
        {
            activeParticles = 0;
            for (int i = 0; i < transform.childCount; i++)
            {
                if (ChildrenTime[i] < 0)
                {
                    if (doSpawnNew)
                        ChildrenTime[i] = Mathf.Min(0f, ChildrenTime[i] + Time.deltaTime);
                }
                else if (ChildrenTime[i] > 1f)
                {
                    transform.GetChild(i).position = to;
                    ChildrenTime[i] = GetDelay(i);
                }
                else
                {
                    activeParticles++;
                    float t = ChildrenTime[i] / LerpDuration;
                    transform.GetChild(i).position = new Vector3(Mathf.Lerp(from.x, to.x, t), from.y+ Mathf.Sin(Mathf.PI * t)*2.25f, Mathf.Lerp(from.z, to.z, t));
                    ChildrenTime[i] += Time.deltaTime;
                }
            }
            yield return null;
        }
        while (doSpawnNew || activeParticles > 0);

        foreach (Transform t in transform)
        {
            t.position = from;
        }
    }
}
