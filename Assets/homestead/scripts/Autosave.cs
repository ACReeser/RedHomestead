using UnityEngine;
using System.Collections;
using RedHomestead.Persistence;

public class Autosave : MonoBehaviour
{
    public static Autosave Instance;
    public bool Autosaving = false;
    private float AutsaveSeconds = 2 * 60;
    
	void Awake ()
    {
        DontDestroyOnLoad(this.gameObject);
    }

    void Start()
    {
        StartCoroutine(DoAutosave());
    }

    private IEnumerator DoAutosave()
    {
        while (isActiveAndEnabled && Autosaving)
        {
            yield return new WaitForSeconds(AutsaveSeconds);
            Save();
        }
    }

    public void Save()
    {
        PersistentDataManager.SaveBase(Base.Current);
    }
}
