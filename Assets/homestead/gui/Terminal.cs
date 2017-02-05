using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
using RedHomestead.Economy;
using System.Collections.Generic;

public enum TerminalProgram { Finances, Colony, News, Market }
public enum MarketTab { Buy, EnRoute, Sell }
public enum BuyTab { ByResource, BySupplier, Checkout }

[Serializable]
public struct ColonyFields
{
    public Text ColonyDayText;
}

[Serializable]
public struct FinanceFields
{
    public Text DaysUntilPaydayText, BankAccountText;
    public Image DaysUntilPaydayVisualization;
}

[Serializable]
public struct BuyFields
{
    public RectTransform[] BuyTabs;
    public RectTransform BySupplierSuppliersTemplate, BySuppliersStockTemplate, CheckoutStockParent, CheckoutDeliveryButtonParent;
    public Text CheckoutVendorName, CheckoutWeight, CheckoutVolume, CheckoutAccount, CheckoutGoods, CheckoutShippingCost, CheckoutTotal;

    internal void SetBySuppliersStock(Vendor v)
    {
        int i = 0;
        foreach(Transform t in BySuppliersStockTemplate.parent)
        {
            if (v != null && i < v.Stock.Count)
            {
                //t.GetChild(0).GetComponent<Image>().sprite = v.Stock[i].Sprite;
                t.GetChild(1).GetComponent<Text>().text = v.Stock[i].Name;
                t.GetChild(2).GetComponent<Text>().text = v.Stock[i].StockAvailable + " @ $" + v.Stock[i].ListPrice;
                t.gameObject.SetActive(true);
            }
            else
            {
                t.gameObject.SetActive(false);
            }

            i++;
        }
    }
    internal void SetBySuppliers(List<Vendor> vendors)
    {
        int i = 0;
        foreach (Transform t in BySupplierSuppliersTemplate.parent)
        {
            Transform button = t.GetChild(0);
            if (i < vendors.Count)
            {
                button.GetChild(0).gameObject.SetActive(vendors[i].AvailableDelivery.IsSet(DeliveryType.Rover));
                button.GetChild(1).gameObject.SetActive(vendors[i].AvailableDelivery.IsSet(DeliveryType.Lander));
                button.GetChild(2).gameObject.SetActive(vendors[i].AvailableDelivery.IsSet(DeliveryType.Drop));
                button.GetChild(3).GetComponent<Text>().text = vendors[i].Name;
                button.GetChild(4).GetComponent<Text>().text = string.Format("{0} Units\n{1}<size=6>km</size> Away", vendors[i].TotalUnits, vendors[i].DistanceFromPlayerKilometersRounded);
                button.gameObject.SetActive(true);
            }
            else
            {
                button.gameObject.SetActive(false);
            }

            i++;
        }
    }
}

public class Terminal : MonoBehaviour {

    public RectTransform[] ProgramPanels, MarketTabs, BuyTabs;
    public RectTransform HomePanel;
    public ColonyFields colony;
    public FinanceFields finance;
    public BuyFields buys;

    private RectTransform currentProgramPanel, currentMarketTab, currentBuyTab;
    internal Order CurrentOrder;

	// Use this for initialization
	void Start ()
    {
        HideAll(ProgramPanels);
        HideAll(MarketTabs);
        HideAll(BuyTabs);

        SetProgram(null);
        SunOrbit.Instance.OnHourChange += OnHourChange;
        SunOrbit.Instance.OnSolChange += OnSolChange;
        EconomyManager.Instance.OnBankAccountChange += OnBankAccountChange;

        CurrentOrder = new Order();
    }

    private void OnBankAccountChange()
    {
        finance.BankAccountText.text = String.Format("${0:n0}", EconomyManager.Instance.Player.BankAccount);
    }

    void Destroy()
    {
        SunOrbit.Instance.OnHourChange -= OnHourChange;
        SunOrbit.Instance.OnSolChange -= OnSolChange;
    }

    private void OnSolChange(int newSol)
    {
        colony.ColonyDayText.text = newSol.ToString() + " Sols";
    }

    private void OnHourChange(int newSol, float newHour)
    {
        finance.DaysUntilPaydayText.text = string.Format("{0}hrs until payday", EconomyManager.Instance.HoursUntilPayday);
        finance.DaysUntilPaydayVisualization.fillAmount = EconomyManager.Instance.HoursUntilPaydayPercentage;
    }

    private void HideAll(RectTransform[] collection)
    {
        foreach (RectTransform t in collection)
        {
            t.gameObject.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update () {
	
	}

    public void SwitchProgram(int p)
    {
        SetProgram(ProgramPanels[p]);
    }

    public void CloseProgram()
    {
        SetProgram(null);
    }

    private void SetProgram(RectTransform panel)
    {
        HomePanel.gameObject.SetActive(panel == null);

        if (currentProgramPanel != null)
            currentProgramPanel.gameObject.SetActive(false);

        currentProgramPanel = panel;

        if (currentProgramPanel == ProgramPanels[(int)TerminalProgram.Market])
            SwitchMarketTab((int)MarketTab.Buy);

        if (currentProgramPanel != null)
            currentProgramPanel.gameObject.SetActive(true);
    }

    public void SwitchMarketTab(int t)
    {

        if (currentMarketTab != null)
            currentMarketTab.gameObject.SetActive(false);

        currentMarketTab = MarketTabs[t];

        if (currentMarketTab == MarketTabs[(int)MarketTab.Sell])
            SwitchBuyTab((int)BuyTab.BySupplier);

        currentMarketTab.gameObject.SetActive(true);
    }

    public void SwitchBuyTab(int t)
    {
        if (currentBuyTab != null)
            currentBuyTab.gameObject.SetActive(false);

        currentBuyTab = BuyTabs[t];

        currentBuyTab.gameObject.SetActive(true);

        if (currentBuyTab == BuyTabs[(int)BuyTab.BySupplier])
        {
            buys.SetBySuppliers(Corporations.Wholesalers);
            buys.SetBySuppliersStock(null);
        }
    }

    public void Checkout(int supplierIndex)
    {
        CheckoutVendor = Corporations.Wholesalers[supplierIndex];
        FillCheckoutStock();
        SwitchBuyTab((int)BuyTab.Checkout);
    }

    private void FillCheckoutStock()
    {
        throw new NotImplementedException();
    }

    private Vendor CheckoutVendor = null;
    private int BySupplierVendorIndex = -1;
    public void BySupplierVendorClick()
    {
        BySupplierVendorIndex = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.transform.GetSiblingIndex();
        buys.SetBySuppliersStock(Corporations.Wholesalers[BySupplierVendorIndex]);
    }

    public void BySupplierSelectVendorAndCheckout()
    {
        Checkout(BySupplierVendorIndex);
    }

    public void SelectDeliveryType(int type)
    {

    }

    public void AddItem()
    {
        Transform button = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.transform;
        RefreshAmountText(button.parent);
    }

    private void RefreshAmountText(Transform checkoutFieldsParent)
    {

    }

    public void SubtractItem()
    {
        Transform button = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.transform;
        int stockI = button.parent.parent.GetSiblingIndex();

        RefreshAmountText(button.parent);
    }

    public void PlaceOrder()
    {

    }
}
