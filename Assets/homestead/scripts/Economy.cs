using UnityEngine;
using System.Collections;
using System;

public class Economy : MonoBehaviour {
    public delegate void EconomyHandler();

    public static Economy Instance;

    public event EconomyHandler OnBankAccountChange;

    public int HoursUntilPayday;
    public int PaydayAmount;

    void Awake()
    {
        Instance = this;
    }

	// Use this for initialization
	void Start () {
        StartCoroutine(EconomyTick());
	}

    private IEnumerator EconomyTick()
    {
        while (isActiveAndEnabled)
        {
            yield return null;
        }
    }
}
