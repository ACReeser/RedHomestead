using UnityEngine;
using System.Collections;

public enum TerminalProgram { Finances, Colony, News, Market}
public class Terminal : MonoBehaviour {

    public RectTransform[] ProgramPanels;
    public RectTransform HomePanel;

    private RectTransform currentProgramPanel;

	// Use this for initialization
	void Start () {
        foreach(RectTransform t in ProgramPanels)
        {
            t.gameObject.SetActive(false);
        }
        SetProgram(null);
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

        if (currentProgramPanel != null)
            currentProgramPanel.gameObject.SetActive(true);
    }
}
