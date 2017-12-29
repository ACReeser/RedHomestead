using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtCamera : MonoBehaviour {


    public Camera m_Camera;

    public virtual void Update()
    {
        transform.LookAt(transform.position + m_Camera.transform.rotation * Vector3.forward, m_Camera.transform.rotation * Vector3.up);
    }

    // Use this for initialization
    public virtual void Start () {
        if (m_Camera == null)
            m_Camera = Camera.main;
	}
}