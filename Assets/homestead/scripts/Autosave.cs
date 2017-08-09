using UnityEngine;
using System.Collections;
using RedHomestead.Persistence;

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
            PersistentDataManager.StartNewGame(new RedHomestead.GameplayOptions.NewGameChoices() {
                PlayerName = "Ares",
                ChosenFinancing = RedHomestead.Economy.BackerFinancing.Government,
                BuyRover = true,
                ChosenPlayerTraining = RedHomestead.Perks.Perk.Athlete,
                RemainingFunds = 1000000,
                BoughtMatter = new System.Collections.Generic.Dictionary<RedHomestead.Simulation.Matter, int>()
                {
                    { RedHomestead.Simulation.Matter.Water, 1 },
                    { RedHomestead.Simulation.Matter.Oxygen, 1 },
                    { RedHomestead.Simulation.Matter.Hydrogen, 2 }
                },
                BoughtCraftables = new System.Collections.Generic.Dictionary<RedHomestead.Crafting.Craftable, int>()
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
