using UnityEngine;
using System.Collections;
using RedHomestead.Persistence;
using RedHomestead.GameplayOptions;
using UnityEngine.SceneManagement;

public class Autosave : MonoBehaviour
{
    public static Autosave Instance;
    internal bool AutosaveEnabled = false;
    private float AutsaveSeconds = 2 * 60;

    void Awake ()
    {
        Instance = this;
#if UNITY_EDITOR
        if (Game.Current == null)
        {
            print("Starting new game for editor session");
            var boughtMatter = new BoughtMatter();
            bool isTutorial = SceneManager.GetActiveScene().buildIndex == 2;

            if (!isTutorial)
            {
                boughtMatter.Set(RedHomestead.Simulation.Matter.Water, 1);
                boughtMatter.Set(RedHomestead.Simulation.Matter.Oxygen, 1);
                boughtMatter.Set(RedHomestead.Simulation.Matter.Hydrogen, 2);
            }

            PersistentDataManager.StartNewGame(new RedHomestead.GameplayOptions.NewGameChoices() {
                PlayerName = "Ares",
                ChosenLocation = new RedHomestead.Geography.BaseLocation()
                {
                    Region = RedHomestead.Geography.MarsRegion.meridiani_planum
                },
                ChosenFinancing = RedHomestead.Economy.BackerFinancing.Government,
                BuyRover = true,
                ChosenPlayerTraining = RedHomestead.Perks.Perk.Athlete,
                RemainingFunds = 1000000,
                BoughtMatter = boughtMatter,
                BoughtCraftables = new System.Collections.Generic.Dictionary<RedHomestead.Crafting.Craftable, int>(),
                IsTutorial = isTutorial
            });
        }
#endif
    }

    void Start()
    {
        StartCoroutine(DoAutosave());
    }

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
