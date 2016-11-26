using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class OutsideVisuals : MonoBehaviour {
    
    private static List<ParticleSystem> AllParticles = new List<ParticleSystem>();
    
    public static void ToggleAllParticles(bool state)
    {
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
	void Start () {
        ParticleSystem sys = this.GetComponent<ParticleSystem>();
        if (sys != null)
            AllParticles.Add(sys);
	}
	
}
