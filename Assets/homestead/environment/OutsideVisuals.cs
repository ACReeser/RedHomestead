using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(ParticleSystem))]
public class OutsideVisuals : MonoBehaviour {
    
    private static List<ParticleSystem> AllParticles = new List<ParticleSystem>();

    private ParticleSystem myParticleSystem;
    public Transform follow;
    private float followInterval;
    private static bool particlesActive = false;

    public IEnumerator Follow()
    {
        while(isActiveAndEnabled)
        {
            yield return new WaitForSeconds(followInterval);

            if (particlesActive)
                this.transform.position = new Vector3(follow.position.x, this.transform.position.y, follow.position.z);
        }
    }

    public static void ToggleAllParticles(bool state)
    {
        particlesActive = state;
        foreach(ParticleSystem sys in AllParticles)
        {
            var emission = sys.emission;
            emission.enabled = state;

            if (state)
                sys.Play();
            else
                sys.Stop();
        }
    }

	// Use this for initialization
	void Awake() {
        myParticleSystem = this.GetComponent<ParticleSystem>();

        if (AllParticles.Count > 0)
            AllParticles.Clear();
    }

    void Start()
    {
        if (myParticleSystem != null)
        {
            AllParticles.Add(myParticleSystem);
        }

        if (follow != null)
            StartCoroutine(this.Follow());
    }

}
