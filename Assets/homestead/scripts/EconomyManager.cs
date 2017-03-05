using UnityEngine;
using System.Collections;
using RedHomestead.Economy;
using System;

public class EconomyManager : MonoBehaviour
{
    public delegate void EconomyHandler();

    public static EconomyManager Instance;

    public event EconomyHandler OnBankAccountChange;
    
    public LandingZone LandingZone;

    public float MinutesUntilPayday = SunOrbit.MartianMinutesPerDay * 7f;

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

    public PersistentPlayer Player = new PersistentPlayer();

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

    void Destroy()
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

        foreach (Order candidate in Player.EnRouteOrders.ToArray())
        {
            SolsAndHours future = now.SolHoursIntoFuture(candidate.ETA);

            if (future.Sol <= 1 && future.Hour <= 1)
            {
                Deliver(candidate);
                Player.EnRouteOrders.Remove(candidate);
            }
        }
    }

    private void Deliver(Order order)
    {
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
}
