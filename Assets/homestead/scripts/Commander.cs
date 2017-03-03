using UnityEngine;
using System.Collections;
using RedHomestead.Persistence;
using System;

//namespace RedHomestead.Commands
//{
//    public enum CommandType { AddCrate }

//    public struct Command
//    {

//    }
//}

public class Commander : MonoBehaviour {
    public static Commander Instance;
    public bool Autosaving = false;

    private Base currentBase;
    private float AutsaveSeconds = 2 * 60;

    void Awake () {
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
	}

	// Use this for initialization
	void Start () {
        StartCoroutine(Autosave());
	}

    private IEnumerator Autosave()
    {
        while(isActiveAndEnabled && Autosaving)
        {
            yield return new WaitForSeconds(AutsaveSeconds);
            Save();
        }
    }

    //public void Do()
    //{

    //}

    public void Save()
    {
        PersistentDataManager.SaveBase(currentBase);
    }
}
