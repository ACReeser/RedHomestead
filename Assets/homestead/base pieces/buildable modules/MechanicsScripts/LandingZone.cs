using UnityEngine;
using System.Collections;
using System;
using RedHomestead.Economy;
using System.Collections.Generic;
using RedHomestead.Simulation;
using RedHomestead.Crafting;
using RedHomestead.Persistence;

[Serializable]
public class LandingZoneData: FacingData
{
    public string LZInstanceID;
}

public class LandingZone : MonoBehaviour, IDeliveryScript, IDataContainer<LandingZoneData> {
    public static LandingZone Instance;
    public Transform bouncePrefab, landerPrefab;
    public Light[] spotlights;

    private Transform currentLander;

    public LandingZoneData Data { get; set; }
    internal CargoLander Cargo { get; private set; }

    void Awake () {
        Instance = this;
	}

    void Start()
    {
        if (Data == null)
        {
            this.Data = new LandingZoneData()
            {
                Transform = this.transform,
                LZInstanceID = Guid.NewGuid().ToString()
            };
        }

        ToggleLights(false);
    }

    public void ToggleLights(bool isOn)
    {
        foreach (var light in spotlights)
        {
            light.gameObject.SetActive( isOn );
        }
    }

    public void Deliver(Order o)
    {
        switch (o.Via)
        {
            case DeliveryType.Drop:
                Transform bouncer = GameObject.Instantiate<Transform>(bouncePrefab);
                bouncer.position = this.transform.position + Vector3.up * 800f;
                bouncer.GetComponent<BounceLander>().Deliver(o);
                GuiBridge.Instance.ShowNews(NewsSource.IncomingBounce);
                SunOrbit.Instance.ResetToNormalTime();
                break;
            case DeliveryType.Lander:
            case DeliveryType.Rover:
                if (this.Cargo == null)
                {
                    Transform lander = GameObject.Instantiate<Transform>(landerPrefab);
                    lander.position = this.transform.position;
                    this.Cargo = lander.GetComponent<CargoLander>();
                }
                Cargo.Deliver(o, this);
                break;
        }
    }

    public void Deliver(Dictionary<Matter, int> supplies, Dictionary<Craftable, int> craftables)
    {
        int totalN = 0;
        foreach(KeyValuePair<Matter, int> supply in supplies)
        {
            float totalAmountToSpawnVolume = supply.Key.CubicMetersPerUnit() * supply.Value;
            while (totalAmountToSpawnVolume > 0)
            {
                Vector3 position = GetSpiralPosition(totalN);
                float amountToSpawnVolume = Mathf.Min(1f, totalAmountToSpawnVolume);
                BounceLander.CreateCratelike(supply.Key, amountToSpawnVolume, position);

                totalN++;
                totalAmountToSpawnVolume -= amountToSpawnVolume;
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
