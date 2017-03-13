using UnityEngine;
using System.Collections;

public interface ITriggerSubscriber
{
    void OnChildTriggerEnter(TriggerForwarder child, Collider c, ResourceComponent res);
}

//only used for warehouse so far
public class TriggerForwarder : MonoBehaviour {
    private ITriggerSubscriber dad;

    void Start()
    {
        this.dad = this.transform.parent.GetComponent<ITriggerSubscriber>();
    }

    void OnTriggerEnter(Collider other)
    {
        ResourceComponent res = other.GetComponent<ResourceComponent>();

        if (res != null)
        {
            this.dad.OnChildTriggerEnter(this, other, res);
        }
    }
}
