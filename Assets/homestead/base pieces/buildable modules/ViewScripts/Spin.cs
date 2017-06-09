using UnityEngine;
using System.Collections;

public class Spin : MonoBehaviour {

    public float Speed;
    public Vector3 Axis;
    
	// Update is called once per frame
	void Update ()
    {
        OnUpdate();
    }

    protected virtual void OnUpdate()
    {
        this.transform.Rotate(Axis * Speed * Time.deltaTime, Space.Self);
    }
}
