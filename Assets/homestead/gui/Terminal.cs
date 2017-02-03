using UnityEngine;
using System.Collections;

public enum TerminalProgram { Finances, Colony, News, Market }
public enum MarketTab { Buy, EnRoute, Sell }
public enum BuyTab { ByResource, BySupplier, Checkout }

public class Terminal : MonoBehaviour {

    public RectTransform[] ProgramPanels, MarketTabs, BuyTabs;
    public RectTransform HomePanel;

    private RectTransform currentProgramPanel, currentMarketTab, currentBuyTab;

	// Use this for initialization
	void Start ()
    {
        HideAll(ProgramPanels);
        HideAll(MarketTabs);
        HideAll(BuyTabs);

        SetProgram(null);
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
    }
}
