using UnityEngine;
using System.Collections;
using System;

public interface ITriggerSubscriber
{
    Transform transform { get; }
    void OnChildTriggerEnter(TriggerForwarder child, Collider c, IMovableSnappable res);
}

public class TriggerForwarder : MonoBehaviour {
    private ITriggerSubscriber dad;
    public bool OnlyMovableSnappables = true;

    void Start()
    {
        if (this.transform.parent != null)
            this.dad = this.transform.parent.GetComponent<ITriggerSubscriber>();

        if (this.dad == null)
            this.dad = this.transform.root.GetComponent<ITriggerSubscriber>();

        //if (!OnlyMovableSnappables)
        //{
        //    Debug.LogWarningFormat("{0} with dad {1} is using !OnlyMovableSnappables", this.transform.name, this.dad.ToString());
        //}
    }

    public void SetDad(ITriggerSubscriber newDad)
    {
        this.dad = newDad;
    }


    void OnTriggerEnter(Collider other)
    {
        IMovableSnappable res = other.GetComponent<IMovableSnappable>();

        bool hasMovableSnappable = res != null;
        if (dad != null && (hasMovableSnappable || !OnlyMovableSnappables))
        {
            if (hasMovableSnappable && res.IsSnapped)
                return;

            this.dad.OnChildTriggerEnter(this, other, res);
        }
    }
}
