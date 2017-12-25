using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
using RedHomestead.Economy;
using System.Collections.Generic;
using RedHomestead.Simulation;
using RedHomestead.Persistence;
using System.Linq;

public enum TerminalProgram { Finances, Colony, News, Market }
public enum MarketTab { Buy, EnRoute, Sell }
public enum BuyTab { ByResource, BySupplier, Checkout }

[Serializable]
/// bindings and display logic for all the stuff under the colony app
public struct ColonyFields
{
    public Text ColonyNameText, MDIText, SolsText, StructuresText, CraftedText, MatterText, RepairsText;

    internal void FillColonyScreen()
    {
        ColonyNameText.text = Base.Current.Name;
        MDIText.text =        Game.Current.GetScore().ToString();
        SolsText.text =       Game.Current.Environment.CurrentSol.ToString();
        StructuresText.text = Game.Current.Score.ModulesBuilt.ToString();
        CraftedText.text =    Game.Current.Score.ItemsCrafted.ToString();
        MatterText.text = String.Format("\r\n{0}\r\n{1}\r\n{2}", Game.Current.Score.MatterMined, Game.Current.Score.MatterRefined, Game.Current.Score.MatterSold);
        RepairsText.text =    Game.Current.Score.RepairsEffected.ToString();
    }
}

[Serializable]
/// bindings and display logic for all the stuff under the finance app
public struct FinanceFields
{
    public Text DaysUntilPaydayText, PaydayAmountText, BankAccountText;
    public Image DaysUntilPaydayVisualization;

    internal void Fill()
    {
        PaydayAmountText.text = EconomyManager.Instance.NextPayDayAmount.ToString("C") + " this week";
    }
}

[Serializable]
public struct EnRouteFields
{
    public RectTransform EnRouteTemplate;
    public Sprite[] DeliverySprites;

    internal void FillEnRoute(List<Order> enroutes)
    {
        int i = 0;
        foreach (Transform t in EnRouteTemplate.parent)
        {
            if (enroutes != null && i < enroutes.Count)
            {
                Order o = enroutes[i];
                t.GetChild(0).GetComponent<Image>().sprite = DeliverySprites[(int)o.Via];
                Text tex = t.GetChild(1).GetComponent<Text>();
                tex.text = o.TimeUntilETA();
                tex.transform.GetChild(1).GetComponent<Image>().fillAmount = o.DeliveryWaitPercentage();
                Transform childItemParent = t.GetChild(2);

                int j = 0;
                Matter[] orderedMatter = o.GetKeyArray();
                foreach(Transform li in childItemParent)
                {
                    if (o.LineItemUnits != null && orderedMatter != null && j < o.LineItemUnits.Count)
                    {
                        li.GetComponent<Text>().text = orderedMatter[j].ToString();
                        li.GetChild(0).GetComponent<Image>().sprite = orderedMatter[j].Sprite();
                        li.GetChild(1).GetComponent<Text>().text = String.Format("{0} <size=6>m3</size>", o.LineItemUnits[orderedMatter[j]] * orderedMatter[j].CubicMetersPerUnit());

                        li.gameObject.SetActive(true);
                    }
                    else
                    {
                        li.gameObject.SetActive(false);
                    }

                    j++;
                }

                t.gameObject.SetActive(true);
            }
            else
            {
                t.gameObject.SetActive(false);
            }

            i++;
        }
    }
}


[Serializable]
/// bindings and display logic for all the stuff under the Market > Buy tabs
public struct BuyFields
{
    public RectTransform[] BuyTabs;
    public RectTransform BySupplierSuppliersTemplate, BySuppliersStockTemplate, BySuppliersSelectSupplierButton, CheckoutStockParent, CheckoutDeliveryButtonParent, BySupplierButton, ByResourceButton, ByResourceResourceListParent, ByResourceVendorListParent;
    public Text CheckoutVendorName, CheckoutWeight, CheckoutVolume, CheckoutAccount, CheckoutGoods, CheckoutShippingCost, CheckoutTotal;
    public Text[] DeliveryTimeLabels;

    internal BuyTab Tab;

    internal void FillCheckout(Vendor v)
    {
        SetTab(BuyTab.Checkout);

        CheckoutVendorName.text = v.Name;
        CheckoutAccount.text = String.Format("${0:n0}", RedHomestead.Persistence.Game.Current.Player.BankAccount);
        foreach(DeliveryType delivery in Enum.GetValues(typeof(DeliveryType)))
        {
            DeliveryTimeLabels[(int)delivery].text = SolsAndHours.SolHoursFromNow(delivery.ShippingTimeHours(v.DistanceFromPlayerKilometersRounded)).ToString();
        }
        FillCheckoutStock(v);
    }
    internal void SetTab(BuyTab tab)
    {
        Tab = tab;
        RefreshBuyTabsTabs();
    }
    internal void RefreshBuyTabsTabs()
    {
        BySupplierButton.gameObject.SetActive(Tab != BuyTab.Checkout);
        ByResourceButton.gameObject.SetActive(Tab != BuyTab.Checkout);
    }
    internal void RefreshMassVolumeMoney(Order o)
    {
        CheckoutWeight.text = String.Format("{0}/{1}<size=6>kg</size>", o.TotalMass, o.Via.MaximumMass());
        CheckoutVolume.text = String.Format("{0}/{1}<size=6>m3</size>", o.TotalVolume, o.Via.MaximumVolume());
        CheckoutGoods.text = String.Format("-{0:n0}", o.MatterCost);
        CheckoutShippingCost.text = String.Format("-{0:n0}", o.ShippingCost);
        CheckoutTotal.text = String.Format("-{0:n0}", o.GrandTotal);

        if (o.TotalMass == o.Via.MaximumMass() || o.TotalVolume == o.Via.MaximumVolume())
        {
            this.SetCheckoutMoreButtons(false);
        }
        else
        {
            this.SetCheckoutMoreButtons(true);
        }
    }

    private Button[] checkoutMoreButtons;
    private Text[] checkoutMoreButtonsText;
    private void SetCheckoutMoreButtons(bool interactable)
    {
        if (checkoutMoreButtons == null)
        {
            List<Button> tempButtons = new List<Button>();
            List<Text> tempText = new List<Text>();
            foreach (Transform t in CheckoutStockParent)
            {
                Transform button = t.GetChild(0).GetChild(3);
                tempButtons.Add(button.GetComponent<Button>());
                tempText.Add(button.GetChild(0).GetComponent<Text>());
            }
            checkoutMoreButtons = tempButtons.ToArray();
            checkoutMoreButtonsText = tempText.ToArray();
        }

        for (int i = 0; i < checkoutMoreButtons.Length; i++)
        {
            checkoutMoreButtons[i].interactable = interactable;
            checkoutMoreButtonsText[i].enabled = interactable;
        }
    }

    private void FillCheckoutStock(Vendor v)
    {
        SetStock(v, CheckoutStockParent, (Transform t, int i) =>
        {
            Transform group = t.GetChild(0);
            group.GetChild(0).GetComponent<Image>().sprite = v.Stock[i].Matter.Sprite();
            group.GetChild(1).GetComponent<Text>().text = v.Stock[i].Name;
            group.GetChild(2).GetComponent<Text>().text = string.Format(
                "{0} @ ${1}  {2}<size=6>kg</size> {3}<size=6>m3</size>", v.Stock[i].StockAvailable, v.Stock[i].ListPrice, v.Stock[i].Matter.KgPerUnit(), v.Stock[i].Matter.CubicMetersPerUnit());
            group.GetChild(5).GetComponent<Text>().text = "0";
        });

        this.checkoutMoreButtons = null;
    }
    internal void SetBySuppliersStock(Vendor v)
    {
        SetStock(v, BySuppliersStockTemplate.parent, (Transform t, int i) =>
        {
            t.GetChild(0).GetComponent<Image>().sprite = v.Stock[i].Matter.Sprite();
            t.GetChild(1).GetComponent<Text>().text = v.Stock[i].Name;
            t.GetChild(2).GetComponent<Text>().text = v.Stock[i].StockAvailable + " @ $" + v.Stock[i].ListPrice;
        });

        BySuppliersSelectSupplierButton.gameObject.SetActive(v != null);
    }
    private void SetStock(Vendor v, Transform stockParent, Action<Transform, int> bind)
    {
        int i = 0;
        foreach (Transform t in stockParent)
        {
            if (v != null && i < v.Stock.Count)
            {
                bind(t, i);
                t.gameObject.SetActive(true);
            }
            else
            {
                t.gameObject.SetActive(false);
            }

            i++;
        }
    }
    internal void FillBySuppliers(List<Vendor> vendors)
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

    internal void FillByResource()
    {
        HashSet<Matter> matterOnSale = new HashSet<Matter>();
        foreach(Vendor v in Corporations.Wholesalers)
        {
            foreach(var stock in v.Stock)
            {
                matterOnSale.Add(stock.Matter);
            }
        }

        Matter[] matters = matterOnSale.ToArray();
        int i = 1;
        foreach (RectTransform button in ByResourceResourceListParent)
        {
            if (i < matters.Length)
            {
                button.GetChild(0).GetComponent<Image>().sprite = matters[i].Sprite();
                button.GetChild(1).GetComponent<Text>().text = matters[i].ToString();
                button.name = Convert.ToInt32(matters[i]).ToString();
                button.gameObject.SetActive(true);
            }
            else
            {
                button.gameObject.SetActive(false);
            }

            i++;
        }
    }

    internal void FillByResourceVendors(Matter selected)
    {
        int i = 0;
        foreach(RectTransform button in ByResourceVendorListParent)
        {
            if (i < Corporations.Wholesalers.Count)
            {
                Vendor v = Corporations.Wholesalers[i];
                if (v.Shelves.ContainsKey(selected))
                {
                    Stock stock = v.Shelves[selected];
                    var rightText = button.GetChild(0).GetComponent<Text>();
                    rightText.text = v.Name;
                    rightText.transform.GetChild(0).gameObject.SetActive((v.AvailableDelivery & DeliveryType.Rover) == DeliveryType.Rover);
                    rightText.transform.GetChild(1).gameObject.SetActive((v.AvailableDelivery & DeliveryType.Lander) == DeliveryType.Lander);
                    rightText.transform.GetChild(2).gameObject.SetActive((v.AvailableDelivery & DeliveryType.Drop) == DeliveryType.Drop);
                    rightText.transform.GetChild(3).GetComponent<Text>().text = String.Format("{0} <size=6>km</size> Away", v.DistanceFromPlayerKilometersRounded);
                    button.GetChild(1).GetComponent<Text>().text = String.Format(@"${0} / unit
      {1} in stock", stock.ListPrice, stock.StockAvailable);

                    button.name = i.ToString();
                    button.gameObject.SetActive(true);
                }
                else
                {
                    button.gameObject.SetActive(false);
                }
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
    
    public RectTransform[] ProgramPanels, MarketTabs;
    public RectTransform HomePanel;
    public ColonyFields colony;
    public FinanceFields finance;
    public BuyFields buys;
    public EnRouteFields enroute;
    private RectTransform currentProgramPanel, currentMarketTab, currentBuyTab;
    internal Order CurrentOrder;
    private Transform canvasParentRect;
    private Canvas canvas;

	// Use this for initialization
	void Start ()
    {
        HideAll(ProgramPanels);
        HideAll(MarketTabs);
        HideAll(buys.BuyTabs);
        canvasParentRect = transform.parent.GetComponent<Transform>();
        canvasParentRect.localScale = new Vector3(1, 0f, 1f);
        canvas = GetComponent<Canvas>();
        canvas.enabled = false;

        SetProgram(null);
        SunOrbit.Instance.OnHourChange += OnHourChange;
        SunOrbit.Instance.OnSolChange += OnSolChange;
        EconomyManager.Instance.OnBankAccountChange += OnBankAccountChange;
        OnBankAccountChange();

        CurrentOrder = new Order()
        {
            LineItemUnits = new ResourceUnitCountDictionary()
        };
    }

    private void OnBankAccountChange()
    {
        finance.BankAccountText.text = String.Format("${0:n0}", RedHomestead.Persistence.Game.Current.Player.BankAccount);
    }

    void OnDestroy()
    {
        SunOrbit.Instance.OnHourChange -= OnHourChange;
        SunOrbit.Instance.OnSolChange -= OnSolChange;
    }

    private void OnSolChange(int newSol)
    {
        colony.ColonyNameText.text = newSol.ToString() + " Sols";
    }

    private void OnHourChange(int newSol, float newHour)
    {
        finance.DaysUntilPaydayText.text = string.Format("{0}hrs until payday", EconomyManager.Instance.HoursUntilPayday);
        finance.DaysUntilPaydayVisualization.fillAmount = EconomyManager.Instance.HoursUntilPaydayPercentage;

        if (currentMarketTab == MarketTabs[(int)MarketTab.EnRoute])
        {
            enroute.FillEnRoute(RedHomestead.Persistence.Game.Current.Player.EnRouteOrders);
        }
    }

    private void HideAll(RectTransform[] collection)
    {
        foreach (RectTransform t in collection)
        {
            t.gameObject.SetActive(false);
        }
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
        else if (currentProgramPanel == ProgramPanels[(int)TerminalProgram.Colony])
            colony.FillColonyScreen();
        else if (currentProgramPanel == ProgramPanels[(int)TerminalProgram.Finances])
        {
            finance.Fill();
            OnBankAccountChange();
        }

        if (currentProgramPanel != null)
            currentProgramPanel.gameObject.SetActive(true);
    }

    public void SwitchMarketTab(int t)
    {

        if (currentMarketTab != null)
            currentMarketTab.gameObject.SetActive(false);

        currentMarketTab = MarketTabs[t];
        
        if (currentMarketTab == MarketTabs[(int)MarketTab.Buy])
        {
            SwitchBuyTab((int)BuyTab.BySupplier);
        }
        else if (currentMarketTab == MarketTabs[(int)MarketTab.EnRoute])
        {
            enroute.FillEnRoute(RedHomestead.Persistence.Game.Current.Player.EnRouteOrders);
        }

        currentMarketTab.gameObject.SetActive(true);
    }

    public void SwitchBuyTab(int t)
    {
        if (currentBuyTab != null)
            currentBuyTab.gameObject.SetActive(false);

        currentBuyTab = buys.BuyTabs[t];

        currentBuyTab.gameObject.SetActive(true);

        BuyTab newTab = (BuyTab)t;

        buys.SetTab(newTab);

        if (newTab == BuyTab.BySupplier)
        {
            buys.FillBySuppliers(Corporations.Wholesalers);
            buys.SetBySuppliersStock(null);
        }
        else if (newTab == BuyTab.ByResource)
        {
            buys.FillByResource();
            buys.FillByResourceVendors(Matter.Unspecified);
        }
    }

    public void Checkout(int supplierIndex)
    {
        CheckoutVendor = Corporations.Wholesalers[supplierIndex];
        CurrentOrder.Vendor = CheckoutVendor;
        buys.FillCheckout(CheckoutVendor);
        buys.RefreshMassVolumeMoney(CurrentOrder);
        SwitchBuyTab((int)BuyTab.Checkout);
    }

    private Vendor CheckoutVendor = null;
    private int BySupplierVendorIndex = -1;
    public void BySupplierVendorClick()
    {
        BySupplierVendorIndex = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.transform.parent.GetSiblingIndex();
        
        buys.SetBySuppliersStock(Corporations.Wholesalers[BySupplierVendorIndex]);
    }

    public void BySupplierSelectVendorAndCheckout()
    {
        Checkout(BySupplierVendorIndex);
    }

    
    public void ByResourceVendorClick()
    {
        var vendorIndex = int.Parse(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.transform.name);

        Checkout(vendorIndex);
    }

    public void ByResourceMatterClick()
    {
        var selected = (Matter)Enum.Parse(typeof(Matter), UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.transform.name);

        buys.FillByResourceVendors(selected);
    }


    //todo: bug - selecting delivery will get around limits
    public void SelectDeliveryType(int type)
    {
        CurrentOrder.Via = (DeliveryType)type;
        
        buys.RefreshMassVolumeMoney(CurrentOrder);
        //todo: refresh delivery estimate text using CurrentOrder.DeliveryTime.ToString()
    }

    public void DeltaItem(int amount)
    {
        Transform button = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.transform;
        int stockI = button.parent.parent.GetSiblingIndex();
        Stock s = CheckoutVendor.Stock[stockI];
        CurrentOrder.TryAddLineItems(s, amount);

        RefreshAmountText(button.parent, CurrentOrder.LineItemUnits[s.Matter]);
        buys.RefreshMassVolumeMoney(CurrentOrder);
    }

    private void RefreshAmountText(Transform checkoutFieldsParent, float units)
    {
        checkoutFieldsParent.GetChild(5).GetComponent<Text>().text = units.ToString();
    }

    public void PlaceOrder()
    {
        if (RedHomestead.Persistence.Game.Current.Player.BankAccount < CurrentOrder.GrandTotal)
            return;
        else
        {
            CurrentOrder.FinalizeOrder();
            RedHomestead.Persistence.Game.Current.Player.EnRouteOrders.Add(CurrentOrder);
            CurrentOrder = new Order()
            {
                LineItemUnits = new ResourceUnitCountDictionary()
            };
            SwitchMarketTab((int)MarketTab.EnRoute);
        }
    }

    public void CancelOrder()
    {
        CurrentOrder = new Order()
        {
            LineItemUnits = new ResourceUnitCountDictionary()
        };
        SwitchBuyTab((int)BuyTab.BySupplier);
    }

    internal void Toggle(bool isViewing)
    {
        if (openCloseCoroutine != null)
            StopCoroutine(openCloseCoroutine);

        openCloseCoroutine = StartCoroutine(OpenOrCloseTerminal(isViewing));
    }

    private Coroutine openCloseCoroutine;
    private const float openCanvasHeight = 1f;
    private IEnumerator OpenOrCloseTerminal(bool isViewing)
    {
        if (isViewing)
        {
            yield return new WaitForSeconds(.5f);
            canvas.enabled = true;
        }

        float startHeight = canvasParentRect.localScale.y;
        float targetHeight = isViewing ? openCanvasHeight : 0f;
        float time = 0f;
        float duration = .40f;

        while (canvasParentRect.localScale.y != targetHeight)
        {
            canvasParentRect.localScale = new Vector3(1f, Mathfx.Coserp(startHeight, targetHeight, time / duration), 1f);
            time += Time.deltaTime;
            yield return null;
        }

        if (!isViewing)
            canvas.enabled = false;
    }
}
