using RedHomestead.Persistence;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Graveyard : MonoBehaviour {

    public static Graveyard Instance;
    public Transform GraveTemplate;

	// Use this for initialization
	void Start () {
        Instance = this;
        GraveTemplate.gameObject.SetActive(false);
        SurvivalTimer.Instance.OnPlayerDeath += Instance_OnPlayerDeath;
        if (Base.Current.Graves != null && Base.Current.Graves.Length > 0)
        {
            foreach(var grave in Base.Current.Graves)
            {
                AddGrave(grave);
            }
        }
	}

    private void Instance_OnPlayerDeath(GraveData newGrave)
    {
        AddGrave(newGrave);
    }

    int numberOfGraves = 0;
    private void AddGrave(GraveData grave)
    {
        Transform newGrave = GameObject.Instantiate(GraveTemplate, this.transform);
        newGrave.Translate(Vector3.left * 1.5f * numberOfGraves, Space.Self);

        string text = grave.PlayerName + "\nSol " + grave.StartSol + " - Sol " + grave.DeathSol + "\n" + grave.DeathReason;
        newGrave.GetChild(0).GetComponent<TextMesh>().text = text;
        newGrave.GetChild(1).GetComponent<TextMesh>().text = text;

        newGrave.gameObject.SetActive(true);
        numberOfGraves++;
    }

    public Vector3 GetGlobalLastGraveStareAtLocation()
    {
        return this.transform.TransformPoint(Vector3.left * 1.5f * numberOfGraves + Vector3.forward);
    }
}
