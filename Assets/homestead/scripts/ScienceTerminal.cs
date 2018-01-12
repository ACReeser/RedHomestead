using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public struct SciTerminalDetail
{
    public RectTransform CancelButton, AcceptButton, FinishButton, MissionList;
    public Text DayText, RewardText, TitleDescription, CurrentPageHeaderText;
    public Image ClockFill, DetailSprite;

    public void FillList(IEnumerable<IScienceExperiment> list, ScienceTerminal.DetailPage detailPage)
    {
        int i = 0;
        foreach(var experiment in list)
        {
            if (i >= MissionList.childCount) {
                var newB = GameObject.Instantiate(MissionList.GetChild(0), MissionList);
                //newB.GetComponent<Button>().onClick
            }
            Transform child = MissionList.GetChild(i);
            child.gameObject.SetActive(true);
            child.GetChild(0).GetComponent<Image>().sprite = experiment.Experiment.Sprite();
            child.GetChild(1).GetComponent<Text>().text = experiment.Title();
            i++;
        }
        for (int j = i; j < MissionList.childCount; j++)
        {
            MissionList.GetChild(j).gameObject.SetActive(false);
        }

        CurrentPageHeaderText.text = detailPage.ToString().ToUpperInvariant();
    }

    public void FillDetail(IScienceExperiment experiment)
    {
        TitleDescription.text = string.Format("<b>{0}</b>\n<i>{1}</i>", experiment.Title().ToUpperInvariant(), experiment.Experiment.Description());
        RewardText.text = string.Format("${0} REWARD", experiment.Reward);
        DetailSprite.sprite = experiment.Experiment.Sprite();
        ExperimentStatus status = experiment.Status();
        CancelButton.gameObject.SetActive(status == ExperimentStatus.Accepted);
        AcceptButton.gameObject.SetActive(status == ExperimentStatus.Available);
        FinishButton.gameObject.SetActive(status == ExperimentStatus.Completed);
        switch (status)
        {
            case ExperimentStatus.Accepted:
                ClockFill.fillAmount = experiment.Progress;
                break;
            case ExperimentStatus.Available:
                ClockFill.fillAmount = 0f;
                break;
            case ExperimentStatus.Completed:
                ClockFill.fillAmount = 1f;
                break;
        }
        if (experiment.Progress <= 0f)
        {
            DayText.text = experiment.DurationDays + " DAY DURATION";
        }
        else
        {
            DayText.text = "Day " + experiment.Progress + " of " + experiment.DurationDays;
        }
    }
}

public class ScienceTerminal : BaseTerminal {

    public SciTerminalDetail UI;

    public enum DetailPage { Geology, Biology }
    private DetailPage CurrentPage = DetailPage.Biology;

    internal List<BiologyScienceExperiment> AvailableBiologyMissions = new List<BiologyScienceExperiment>()
    {
        new BiologyScienceExperiment()
        {
            MissionNumber = 1,
            Progress = -1,
            Reward = 5000,
            DurationDays = 1
        },
        new BiologyScienceExperiment()
        {
            MissionNumber = 2,
            Progress = -1,
            Reward = 12000,
            DurationDays = 2
        },
        new BiologyScienceExperiment()
        {
            MissionNumber = 3,
            Progress = -1,
            Reward = 17500,
            DurationDays = 3
        }
    };
    internal List<GeologyScienceExperiment> AvailableGeologyMissions = new List<GeologyScienceExperiment>()
    {
        new GeologyScienceExperiment()
        {
            MissionNumber = 1,
            Progress = -1,
            Reward = 10000,
            DurationDays = 2
        }
    };

    // Use this for initialization
    protected override void Start () {
        base.Start();
        GeologyTabClick();	
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void ClickOrHover(int index)
    {
        switch (CurrentPage)
        {
            case DetailPage.Biology:
                UI.FillDetail(AvailableBiologyMissions[index]);
                break;
            case DetailPage.Geology:
                UI.FillDetail(AvailableGeologyMissions[index]);
                break;
        }
    }

    public void GeologyTabClick()
    {
        CurrentPage = DetailPage.Geology;
        UI.FillDetail(AvailableGeologyMissions.FirstOrDefault());
        UI.FillList(AvailableGeologyMissions.Cast<IScienceExperiment>(), CurrentPage);
    }
    public void BiologyTabClick()
    {
        CurrentPage = DetailPage.Biology;
        UI.FillDetail(AvailableBiologyMissions.FirstOrDefault());
        UI.FillList(AvailableBiologyMissions.Cast<IScienceExperiment>(), CurrentPage);
    }
}
