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
        ExperimentStatus status = experiment.Status;
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
        DayText.text = experiment.ProgressText;
    }
}

public class ScienceTerminal : BaseTerminal {
    public SciTerminalDetail UI;

    private ScienceLab myLab;

    public enum DetailPage { Geology, Biology }
    private DetailPage CurrentPage = DetailPage.Biology;

    internal BiologyScienceExperiment[] BiologyMissionList;
    internal GeologyScienceExperiment[] GeologyMissionList;

    protected override void DoBeforeOpenTerminal()
    {
        BiologyMissionList = Science.GetAvailableBiologyExperiments();
        ReplaceTemplateWithRunningExperiment(myLab.FlexData.CurrentBioExperiment, BiologyMissionList);
        GeologyMissionList = Science.GetAvailableGeologyExperiments();
        ReplaceTemplateWithRunningExperiment(myLab.FlexData.CurrentGeoExperiment, GeologyMissionList);
        GeologyTabClick();
    }

    /// <summary>
    /// We get a list of all missions, but the science lab may have a running experiment
    /// so we replace the template with the running experiment
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="currentExperiment"></param>
    /// <param name="missions"></param>
    private void ReplaceTemplateWithRunningExperiment<T>(T currentExperiment, T[] missions)
    {
        if (currentExperiment != null)
        {
            for (int i = 0; i < missions.Length; i++)
            {
                if (missions[i].Equals(currentExperiment))
                {
                    missions[i] = currentExperiment;
                    break;
                }
            }
        }
    }

    // Use this for initialization
    protected override void Start () {
        base.Start();
        myLab = this.transform.root.GetComponent<ScienceLab>();
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    private IScienceExperiment detailedExperiment;
    public void ClickOrHover(int index)
    {
        switch (CurrentPage)
        {
            case DetailPage.Biology:
                detailedExperiment = BiologyMissionList[index];
                UI.FillDetail(detailedExperiment);
                break;
            case DetailPage.Geology:
                detailedExperiment = GeologyMissionList[index];
                UI.FillDetail(detailedExperiment);
                break;
        }
    }

    public void GeologyTabClick()
    {
        CurrentPage = DetailPage.Geology;
        UI.FillDetail(GeologyMissionList.FirstOrDefault());
        UI.FillList(GeologyMissionList.Cast<IScienceExperiment>(), CurrentPage);
    }
    public void BiologyTabClick()
    {
        CurrentPage = DetailPage.Biology;
        UI.FillDetail(BiologyMissionList.FirstOrDefault());
        UI.FillList(BiologyMissionList.Cast<IScienceExperiment>(), CurrentPage);
    }
    
    public void AcceptExperiment()
    {
        myLab.AcceptExperiment(detailedExperiment);
        UI.FillDetail(detailedExperiment);
    }

    public void CancelExperiment()
    {
        myLab.CancelExperiment(detailedExperiment);
        UI.FillDetail(detailedExperiment);
    }

    public void CompleteExperiment()
    {
        myLab.CompleteExperiment(detailedExperiment);
        UI.FillDetail(detailedExperiment);
    }
}
