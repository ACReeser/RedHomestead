using UnityEngine;
using System.Collections;
using RedHomestead.Economy;
using System;
using RedHomestead.Simulation;
using RedHomestead.Persistence;
using System.Collections.Generic;

public class EconomyManager : MonoBehaviour
{
    public delegate void EconomyHandler();

    public static EconomyManager Instance;

    public event EconomyHandler OnBankAccountChange;
    
    public LandingZone LandingZone;

#warning todo: make crate/vessel the same prefab, just swap out meshes
    public Transform ResourceCratePrefab, ResourceVesselPrefab, ResourceTankPrefab;
    public Transform[] CraftablePrefabs;
    public Transform[] DepositPrefabs;
    public Texture2D[] DepositAbundanceTextures; 
    public Terrain terrain;


    public float MinutesUntilPayday = SunOrbit.MartianMinutesPerDay * 7f;
    public AudioClip IncomingDelivery, BuyerFoundForGoods;

    public int HoursUntilPayday
    {
        get
        {
            return Mathf.RoundToInt(MinutesUntilPayday / 60);
        }
    }

    public float HoursUntilPaydayPercentage
    {
        get
        {
            return (float)HoursUntilPayday / MinutesUntilPayday;
        }
    }

    void Awake()
    {
        Instance = this;
    }
    
    void Start()
    {
        SunOrbit.Instance.OnHourChange += OnHourChange;
        SunOrbit.Instance.OnSolChange += OnSolChange;

        if (Base.Current.InitialMatterPurchase != null)
        {
            LandingZone.Deliver(Base.Current.InitialMatterPurchase, Base.Current.InitialCraftablePurchase);
            Base.Current.InitialCraftablePurchase = null;
            Base.Current.InitialMatterPurchase = null;
        }

        randomIncomeGenerator = new System.Random(Game.Current.Player.WeeklyIncomeSeed);
        NextPayDayAmount = FastForward(Game.Current.Environment.CurrentSol);

        if (Game.Current.IsNewGame)
        {
            SeedDeposits();
        }
    }


    void OnDestroy()
    {
        SunOrbit.Instance.OnHourChange -= OnHourChange;
        SunOrbit.Instance.OnSolChange -= OnSolChange;
    }

    private void SeedDeposits()
    {
        int numDepositTypes = DepositPrefabs.Length;
        int numDeposits = 50;
        float xScale = terrain.terrainData.size.x / 128;
        float yScale = terrain.terrainData.size.y / 128;
        for (int i = 0; i < numDeposits; i++)
        {
            int depositIndex = UnityEngine.Random.Range(0, numDepositTypes);
            int x, y;
            GetXY(depositIndex, out x, out y);
            Vector3 worldspaceLocation = new Vector3(
                UnityEngine.Random.Range(x * xScale, (x + 1) * xScale),
                terrain.terrainData.size.y,
                UnityEngine.Random.Range(y * yScale, (y + 1) * yScale)
            ) + terrain.transform.position;
            //http://answers.unity3d.com/questions/18397/get-height-of-terrain-in-script.html
            worldspaceLocation.y = terrain.SampleHeight(worldspaceLocation);

            //we're trying not to use raycasts...
            //RaycastHit hitInfo;
            //if (Physics.Raycast(worldspaceLocation, Vector3.down, out hitInfo) && hitInfo.collider.transform.CompareTag("terrain"))
            //{
            //    GameObject.Instantiate(DepositPrefabs[i], hitInfo.point, hitInfo.no)
            //}

            Transform newlyBornDeposit = GameObject.Instantiate(DepositPrefabs[depositIndex], worldspaceLocation, Quaternion.identity);
            //http://answers.unity3d.com/questions/8867/how-to-get-quaternionfromtorotation-and-hitnormal.html
            Vector3 sample = SampleNormal(worldspaceLocation);
            Vector3 proj = newlyBornDeposit.forward - (Vector3.Dot(newlyBornDeposit.forward, sample)) * sample;
            newlyBornDeposit.rotation = Quaternion.LookRotation(proj, sample);
        }
    }
    private Vector3 SampleNormal(Vector3 position)
    {
        var terrainLocalPos = position - terrain.transform.position;
        var normalizedPos = new Vector2(
            Mathf.InverseLerp(0f, terrain.terrainData.size.x, terrainLocalPos.x),
            Mathf.InverseLerp(0f, terrain.terrainData.size.z, terrainLocalPos.z)
        );
        var terrainNormal = terrain.terrainData.GetInterpolatedNormal(normalizedPos.x, normalizedPos.y);

        return terrainNormal;
    }

    private void GetXY(int i, out int x, out int y)
    {
        bool acceptable = false;
        do
        {
            x = UnityEngine.Random.Range(0, 128);
            y = UnityEngine.Random.Range(0, 128);

            //threshold of 
            float threshold = DepositAbundanceTextures[i].GetPixel(x, y).grayscale;
            if (threshold > 0f)
            {
                float roll = UnityEngine.Random.Range(0f, 1f);
                acceptable =  roll < threshold;

                if (acceptable)
                    print("threshold of " + threshold + " at " + x + "," + y);
                else
                    print("reroll: "+roll+"<"+threshold);
            }
        } while (!acceptable);
    }

    private void OnSolChange(int sol)
    {
        MinutesUntilPayday -= 40;
    }

    private void OnHourChange(int sol, float hour)
    {
        MinutesUntilPayday -= 60f;

        if (MinutesUntilPayday <= 0)
        {
            Payday();
        }

        CheckOrdersForArrival();
    }

    private void CheckOrdersForArrival()
    {
        SolHourStamp now = SolHourStamp.Now();
        List<Order> ToBeDelivered = new List<Order>(); 

        foreach (Order candidate in RedHomestead.Persistence.Game.Current.Player.EnRouteOrders.ToArray())
        {
            SolsAndHours future = now.SolHoursIntoFuture(candidate.ETA);

            if (future.Sol == 0 && future.Hour < 1)
            {
                ToBeDelivered.Add(candidate);
                RedHomestead.Persistence.Game.Current.Player.EnRouteOrders.Remove(candidate);
            }
            
        }
        StartCoroutine(DeliveryProcess(ToBeDelivered));
    }

    IEnumerator DeliveryProcess(List<Order> ToBeDelivered)
    {
        foreach(Order k in ToBeDelivered)
        {
            Deliver(k);
            yield return new WaitForSeconds(8f);
        }
        
    }

    private void Deliver(Order order)
    {
        GuiBridge.Instance.ComputerAudioSource.PlayOneShot(this.IncomingDelivery);
        switch (order.Via)
        {
            default:
                LandingZone.Deliver(order);
                break;
        }
    }

    public int NextPayDayAmount { get; private set; }
    private System.Random randomIncomeGenerator;

    private int FastForward(int gameSol)
    {
        int currentSol = 0;
        int payday = getNextPayday();
        while(currentSol < gameSol)
        {
            payday = getNextPayday();
        }
        return payday;
    }

    private int getNextPayday()
    {
        FinancerData finData = Game.Current.Player.Financing.Data();
        int round = 1000;
        if (finData.MaxWeekly != finData.MinWeekly)
        {
            int rounded = randomIncomeGenerator.Next(finData.MinWeekly / round, (finData.MaxWeekly / round) + 1);
            return rounded * round;
        }
        else
        {
            return finData.MaxWeekly;
        }
    }

    private void Payday()
    {
        Game.Current.Player.BankAccount += NextPayDayAmount;
        NextPayDayAmount = getNextPayday();

        if (this.OnBankAccountChange != null)
            this.OnBankAccountChange();
    }

    internal void Purchase(int amount)
    {
        RedHomestead.Persistence.Game.Current.Player.BankAccount -= amount;

        if (this.OnBankAccountChange != null)
            this.OnBankAccountChange();
    }

    internal Transform GetResourceCratePrefab(Matter m)
    {
        if (m.IsPressureVessel())
        {
            return ResourceVesselPrefab;
        }
        else if (m.IsTankVessel())
        {
            return ResourceTankPrefab;
        }
        else
        {
            return ResourceCratePrefab;
        }
    }
}
