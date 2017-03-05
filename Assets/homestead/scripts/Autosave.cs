using UnityEngine;
using System.Collections;
using RedHomestead.Persistence;

public class Autosave : MonoBehaviour
{
    public static Autosave Instance;
    internal bool AutosaveEnabled = false;
    private float AutsaveSeconds = 2 * 60;
#if UNITY_EDITOR
    private bool editorAutoMadeThisGame = false;
#endif

    void Awake ()
    {
        Instance = this;
#if UNITY_EDITOR
        if (Game.Current == null)
        {
            print("Starting new game for editor session");
            PersistentDataManager.StartNewGame();
            editorAutoMadeThisGame = true;
        }
#endif
    }

    void Start()
    {
        StartCoroutine(DoAutosave());

#if UNITY_EDITOR
        if (editorAutoMadeThisGame)
            StartCoroutine(DebugAutosave());
    }


    private IEnumerator DebugAutosave()
    {
        yield return null;
        yield return null;

        Save();
    }
#else
    }
#endif

    private IEnumerator DoAutosave()
    {
        while (isActiveAndEnabled)
        {
            yield return new WaitForSeconds(AutsaveSeconds);

            if (AutosaveEnabled)
                Save();
        }
    }



    public void Save()
    {
        GuiBridge.Instance.ToggleAutosave(true);
#if UNITY_EDITOR
        print("Saving game");
#endif
        PersistentDataManager.SaveGame(Game.Current);
        GuiBridge.Instance.ToggleAutosave(false);
    }
}
