using RedHomestead.Simulation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public interface IMovableSnappable
{
    ICrateSnapper SnappedTo { get; }
    Transform transform { get; }
    Rigidbody movableRigidbody { get; }
    FixedJoint snapJoint { get; }
    void SnapCrate(ICrateSnapper snapParent, Vector3 snapPosition, Rigidbody jointRigid = null);
    void UnsnapCrate();
    string GetText();
}

public abstract class MovableSnappable : MonoBehaviour, IMovableSnappable {
    public AudioClip BangSoundClip;

    public ICrateSnapper SnappedTo { get; protected set; }
    public Rigidbody movableRigidbody { get; protected set; }
    public FixedJoint snapJoint { get; protected set; }
    
    void Awake()
    {
        movableRigidbody = GetComponent<Rigidbody>();
    }

    public void SnapCrate(ICrateSnapper snapParent, Vector3 snapPosition, Rigidbody jointRigid = null)
    {
        PlayerInput.Instance.DropObject();
        this.SnappedTo = snapParent;
        transform.position = snapPosition;
#warning snap crate rotation does not inherit from trigger forwarder
        transform.rotation = snapParent.transform.rotation;

        if (jointRigid != null)
        {
            movableRigidbody.velocity = Vector3.zero;
            snapJoint = gameObject.AddComponent<FixedJoint>();
            snapJoint.connectedBody = jointRigid;
            snapJoint.breakForce = Mathf.Infinity;
            snapJoint.breakTorque = Mathf.Infinity;
            snapJoint.enableCollision = false;
        }
        else
        {
            movableRigidbody.useGravity = false;
            movableRigidbody.isKinematic = true;
        }


        PlayerInput.Instance.PlayInteractionClip(snapPosition, BangSoundClip);
        OnSnap();
    }

    protected virtual void OnSnap()
    {
    }

    protected virtual void OnDetach()
    {
    }

    //called in pick up object
    public void UnsnapCrate()
    {
        this.SnappedTo.DetachCrate(this);
        this.SnappedTo = null;

        if (this.snapJoint != null)
        {
            GameObject.Destroy(snapJoint);
        }

        movableRigidbody.isKinematic = false;
        movableRigidbody.useGravity = true;
        PlayerInput.Instance.PlayInteractionClip(transform.position, BangSoundClip);
        OnDetach();
    }

    public abstract string GetText();
}
