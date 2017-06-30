using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using System;
using RedHomestead.Persistence;

public class LoadGameBridge : MonoBehaviour {
    public string playerNameToLoad;

    // Use this for initialization
    void Awake () {
        DontDestroyOnLoad(this.gameObject);
        SceneManager.sceneLoaded += OnLevelFinishedLoading;
    }

    private void OnLevelFinishedLoading(Scene scene, LoadSceneMode arg1)
    {
        if (scene.name == "main")
        {
            print("loading game from bridge");
            PersistentDataManager.LoadGame(playerNameToLoad);
            SceneManager.sceneLoaded -= OnLevelFinishedLoading;
            GameObject.Destroy(this.gameObject);
        }
    }
}
