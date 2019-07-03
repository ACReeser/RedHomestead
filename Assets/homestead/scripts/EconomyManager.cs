using UnityEngine;
using System.Collections;
using RedHomestead.Economy;
using System;
using RedHomestead.Simulation;
using RedHomestead.Persistence;
using System.Collections.Generic;
using RedHomestead.Geography;
using System.Linq;

public class EconomyManager : MonoBehaviour
{
    public delegate void EconomyHandler();

    private const int AbundantMatterDepositNumberMultiplier = 2;
    private const int AbundanceMapPixelSize = 256;
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
        PrefabCache<RedHomestead.Buildings.Module>.Cache.Clear();
        PrefabCache<RedHomestead.Interiors.Floorplan>.Cache.Clear();
        PrefabCache<RedHomestead.Interiors.Stuff>.Cache.Clear();
    }

    /// <summary>
    /// each number is the number of deposits of each type, indexed by AbundantMatter enum
    /// </summary>
    private static int[] defaultDepositsByMatterType = new int[] { 25, 20, 15, 10, 10, 10, 10 };

    private void SeedDeposits()
    {
        //used to tell how many deposit types we have
        int numDepositTypes = defaultDepositsByMatterType.Length;
        //used to calculate non-abundant penalty
        int numDeposits = defaultDepositsByMatterType.Sum();
        //xScale and yScale turn our 128 bit abundance map pixel locations into real-world positions
        float xScale = terrain.terrainData.size.x / AbundanceMapPixelSize;
        float zScale = terrain.terrainData.size.z / AbundanceMapPixelSize;

        //this array is indexed by AbundantMatter, and each value is the number of deposits of that type we will make
        int[] numberDepositsByMatterType = new int[numDepositTypes];
        defaultDepositsByMatterType.CopyTo(numberDepositsByMatterType, 0); //copy in the defaults to this array

        AbundantMatter bonusAbundanceType = Base.Current.Region.Data().AbundantMatter;
        int bonusAbundanceIndex = (int)bonusAbundanceType;
        //default abundance bonus is 100% more or 2x multiplier
        //e.g. default 20 iron deposits becomes 40
        int totalNumberOfAbundantDeposits = numberDepositsByMatterType[bonusAbundanceIndex] * AbundantMatterDepositNumberMultiplier;
        //we want to keep the total number of deposits constant for performance reasons
        //so keep track of how many deposit "slots" this doubling has used
        int numberOfStolenDeposits = totalNumberOfAbundantDeposits - numberDepositsByMatterType[bonusAbundanceIndex];
        numberDepositsByMatterType[bonusAbundanceIndex] = totalNumberOfAbundantDeposits;

        #region non-abundant rebalance      
        //calculate a number to subtract from all the other deposit types  
        //in order to keep the # of deposits the same
        int nonabundantDepositNumberPenalty = Mathf.FloorToInt(numberOfStolenDeposits / (numDepositTypes - 1));
        //note: this penalizes smaller abundance deposits more the more normally abundant the abundant matter is
        //e.g. more Water means a lot less aluminium, and still a lot of iron
        for (int i = 0; i < numberDepositsByMatterType.Length; i++)
        {
            if (i != bonusAbundanceIndex)
            {
                numberDepositsByMatterType[i] -= nonabundantDepositNumberPenalty;
            }
        }
        #endregion

        //for each type of deposit
        for (int depositTypeIndex = 0; depositTypeIndex < numDepositTypes; depositTypeIndex++)
        {
            AbundantMatter depositType = (AbundantMatter)depositTypeIndex;
            int numberDepositsOfThisType = numberDepositsByMatterType[depositTypeIndex];

            //for each number of deposits
            for (int depositIndex = 0; depositIndex < numberDepositsOfThisType; depositIndex++)
            {
                int x, y;
                GetXY(Convert.ToInt32(depositType), out x, out y);
                //turn that low-res texture coordinate into a high res one
                Vector3 worldspaceLocation = new Vector3(
                    UnityEngine.Random.Range(x * xScale, (x + 1) * xScale), //by getting a random float in the same "sector" for x
                    terrain.terrainData.size.y,
                    UnityEngine.Random.Range(y * zScale, (y + 1) * zScale) //and z
                ) + terrain.transform.position; //and make these positions relative to the terrain itself

                //then set the Y altitude by sampling the height at that location
                //http://answers.unity3d.com/questions/18397/get-height-of-terrain-in-script.html
                worldspaceLocation.y = terrain.SampleHeight(worldspaceLocation) + terrain.transform.position.y;

                //notice we are not using raycasts! hooray because those are expensive
                //RaycastHit hitInfo;
                //if (Physics.Raycast(worldspaceLocation, Vector3.down, out hitInfo) && hitInfo.collider.transform.CompareTag("terrain"))
                //{
                //    GameObject.Instantiate(DepositPrefabs[i], hitInfo.point, hitInfo.no)
                //}

                //now that we have our random worldspace coordinate, create our deposit
                Transform newlyBornDeposit = GameObject.Instantiate(DepositPrefabs[Convert.ToInt32(depositType)], worldspaceLocation, Quaternion.identity);

                //and align it to the terrain by sampling the normal
                //http://answers.unity3d.com/questions/8867/how-to-get-quaternionfromtorotation-and-hitnormal.html
                Vector3 sample = SampleNormal(worldspaceLocation);
                //and projecting it 
                Vector3 proj = newlyBornDeposit.forward - (Vector3.Dot(newlyBornDeposit.forward, sample)) * sample;
                //before turning it into a look rotation
                newlyBornDeposit.rotation = Quaternion.LookRotation(proj, sample);
            }
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

    /// <summary>
    /// Given a deposit type index, return a random x and y (from 0 to 128, the size of our abundance maps)
    /// </summary>
    /// <param name="depositAbundanceIndex"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    private void GetXY(int depositAbundanceIndex, out int x, out int y)
    {
        bool acceptable = false;
        do
        {
            x = UnityEngine.Random.Range(0, AbundanceMapPixelSize);
            y = UnityEngine.Random.Range(0, AbundanceMapPixelSize);

            //the threshold is given by the grayscale of the texture
            float threshold = DepositAbundanceTextures[depositAbundanceIndex].GetPixel(x, y).grayscale;
            //if there is a chance (it is not black)
            if (threshold > 0f)
            {
                //make a random 0 to 1 roll
                float roll = UnityEngine.Random.Range(0f, 1f);
                //and allow a deposit here if the roll is below the threshold
                //this means a white pixel (threshold 1) will always allow a deposit
                //and progressively darker pixels will allow less chance of one
                acceptable =  roll < threshold;

                //if (acceptable)
                //    print("threshold of " + threshold + " at " + x + "," + y);
                //else
                //    print("reroll: "+roll+"<"+threshold);
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
        CheckMarketsForSellOrders();
    }

    private void CheckMarketsForSellOrders()
    {
        if (Market.GlobalResourceList.CurrentAmount() > 0)
        {
            int[] slots = Market.GlobalResourceList.GetNonEmptyMatterSlots();
            if (slots.Length > 0)
            {
                int randomI = UnityEngine.Random.Range(0, slots.Length);
                Matter consuming = (Matter)((slots[randomI]) - ChemistryConstants.MinMatterOffset);
                float amount = Market.GlobalResourceList.CurrentAmount(consuming);
                Market.GlobalResourceList.Consume(consuming, amount, isCrafting:false);
                int corpI = UnityEngine.Random.Range(0, Corporations.Wholesalers.Count);
                Vendor buyer = Corporations.Wholesalers[corpI];
                int buyPrice = Corporations.MinimumBuyPrice;
                foreach (var stock in buyer.Stock)
                {
                    if (stock.Matter == consuming)
                    {
                        buyPrice = stock.ListPrice;
                        break;
                    }
                }

                int soldFor = Mathf.CeilToInt(buyPrice * amount);
                Game.Current.Player.BankAccount += soldFor;

                if (this.OnBankAccountChange != null)
                    this.OnBankAccountChange();

                GuiBridge.Instance.ShowNews(NewsSource.MarketSold.CloneWithSuffix(String.Format("{0} for ${1}", consuming.ToString(), soldFor)));
            }
        }
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

    internal void ScienceExperimentPayday(IScienceExperiment experiment)
    {
        Game.Current.Player.BankAccount += experiment.Reward;

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
