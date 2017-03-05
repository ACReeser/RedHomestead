using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class LoadGameBridge : MonoBehaviour {
    private int OnLevelFinishedLoading;

    // Use this for initialization
    void Awake () {
        DontDestroyOnLoad(this.gameObject);
        //SceneManager.activ += OnLevelFinishedLoading;
    }
}
