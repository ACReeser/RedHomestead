using UnityEngine;
using System.Collections;
using RedHomestead.Economy;
using System;

public class EconomyManager : MonoBehaviour
{
    public delegate void EconomyHandler();

    public static EconomyManager Instance;

    public event EconomyHandler OnBankAccountChange;

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
        //TODO:
    }

    private void Payday()
    {
        Player.BankAccount += BasePaydayAmount;

        if (this.OnBankAccountChange != null)
            this.OnBankAccountChange();
    }

    internal void Purchase(int amount)
    {
        Player.BankAccount -= amount;

        if (this.OnBankAccountChange != null)
            this.OnBankAccountChange();
    }
}
