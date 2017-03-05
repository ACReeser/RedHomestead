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

    void Awake () {
        Instance = this;
	}

	// Use this for initialization
	void Start () {
	}

    //public void Do()
    //{

    //}
}
