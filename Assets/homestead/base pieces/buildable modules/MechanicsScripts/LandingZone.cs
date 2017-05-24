using UnityEngine;
using System.Collections;
using System;
using RedHomestead.Economy;
using System.Collections.Generic;
using RedHomestead.Simulation;
using RedHomestead.Crafting;

public class LandingZone : MonoBehaviour, IDeliveryScript {
    public Transform landerPrefab;

    private Transform currentLander;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    public void Deliver(Order o)
    {
        Transform lander = GameObject.Instantiate<Transform>(landerPrefab);
        lander.position = this.transform.position + Vector3.up * 800f;
        lander.GetComponent<BounceLander>().Deliver(o);
    }

    public void Deliver(Dictionary<Matter, int> supplies, Dictionary<Craftable, int> craftables)
    {
        int totalN = 0;
        foreach(KeyValuePair<Matter, int> supply in supplies)
        {
            int amountToSpawn = supply.Value;
            while (amountToSpawn > 0)
            {
                Vector3 position = GetSpiralPosition(totalN);
                BounceLander.CreateCratelike(supply.Key, 1f, position);

                totalN++;
                amountToSpawn--;
            }
        }
        foreach (KeyValuePair<Craftable, int> craftable in craftables)
        {
            int amountToSpawn = craftable.Value;
            while (amountToSpawn > 0)
            {
                Vector3 position = GetSpiralPosition(totalN);
                BounceLander.CreateCratelike(craftable.Key, position);

                totalN++;
                amountToSpawn--;
            }
        }
    }

    private Vector3 GetSpiralPosition(int totalN)
    {
        int height = totalN % 2 == 0 ? 0 : 1;
        int n = height == 0 ? totalN / 2 : (totalN - 1) / 2;
        Vector2 spiralPos = spiral(n);
        Vector3 position = this.transform.position + (new Vector3(spiralPos.x, height * 1.15f, spiralPos.y) * 1.7f);
        return position;
    }

    private Vector2 spiral(int n)
    {
        var k = Mathf.CeilToInt((Mathf.Sqrt(n) - 1) / 2);
        var t = 2 * k + 1;
        var m = Mathf.Pow(t, 2);
        t = t - 1;

        if (n >= m - t)
            return new Vector2(k-(m-n),-k);
        else 
            m = m - t;

        if (n >= m - t)
            return new Vector2(-k, -k + (m - n));
        else
            m = m - t;

        if (n >= m - t)
            return new Vector2(-k + (m - n), k);
        else
            return new Vector2(k,k - (m - n - t));
    }
}
