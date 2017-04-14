using UnityEngine;
using System.Collections;
using RedHomestead.Economy;
using System;
using RedHomestead.Simulation;

public class EconomyManager : MonoBehaviour
{
    public delegate void EconomyHandler();

    public static EconomyManager Instance;

    public event EconomyHandler OnBankAccountChange;
    
    public LandingZone LandingZone;

#warning todo: make crate/vessel the same prefab, just swap out meshes
    public Transform ResourceCratePrefab, ResourceVesselPrefab, ResourceTankPrefab;

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

    public int BasePaydayAmount = 1000000;

    void Awake()
    {
        Instance = this;
    }

    // Use this for initialization
    void Start()
    {
        SunOrbit.Instance.OnHourChange += OnHourChange;
        SunOrbit.Instance.OnSolChange += OnSolChange;
    }

    void OnDestroy()
    {
        SunOrbit.Instance.OnHourChange -= OnHourChange;
        SunOrbit.Instance.OnSolChange -= OnSolChange;
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

        foreach (Order candidate in RedHomestead.Persistence.Game.Current.Player.EnRouteOrders.ToArray())
        {
            SolsAndHours future = now.SolHoursIntoFuture(candidate.ETA);

            if (future.Sol <= 1 && future.Hour <= 1)
            {
                Deliver(candidate);
                RedHomestead.Persistence.Game.Current.Player.EnRouteOrders.Remove(candidate);
            }
        }
    }

    private void Deliver(Order order)
    {
        GuiBridge.Instance.ComputerAudioSource.PlayOneShot(this.IncomingDelivery);
        switch (order.Via)
        {
            default:
                GuiBridge.Instance.ShowNews(NewsSource.DroppodHere);
                LandingZone.Deliver(order);
                break;
        }
    }

    private void Payday()
    {
        RedHomestead.Persistence.Game.Current.Player.BankAccount += BasePaydayAmount;

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
