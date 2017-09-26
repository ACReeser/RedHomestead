using UnityEngine;
using System.Collections;

public interface ITriggerSubscriber
{
    void OnChildTriggerEnter(TriggerForwarder child, Collider c, IMovableSnappable res);
}

//only used for warehouse so far
public class TriggerForwarder : MonoBehaviour {
    private ITriggerSubscriber dad;
    public bool OnlyMovableSnappables = true;

    void Start()
    {
        this.dad = this.transform.parent.GetComponent<ITriggerSubscriber>();

        if (this.dad == null)
            this.dad = this.transform.root.GetComponent<ITriggerSubscriber>();

        if (!OnlyMovableSnappables)
        {
            Debug.LogWarningFormat("{0} with dad {1} is using !OnlyMovableSnappables", this.transform.name, this.dad.ToString());
        }
    }

    void OnTriggerEnter(Collider other)
    {
        IMovableSnappable res = other.GetComponent<IMovableSnappable>();

        bool hasMovableSnappable = res != null;
        if (hasMovableSnappable || !OnlyMovableSnappables)
        {
            if (hasMovableSnappable && res.IsSnapped)
                return;

            this.dad.OnChildTriggerEnter(this, other, res);
        }
    }
}
