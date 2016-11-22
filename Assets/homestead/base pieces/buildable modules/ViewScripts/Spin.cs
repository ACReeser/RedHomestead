using UnityEngine;
using System.Collections;

public class Spin : MonoBehaviour {

    public float Speed;
    public Vector3 Axis;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        this.transform.Rotate(Axis * Speed * Time.deltaTime, Space.Self);
	}
}
