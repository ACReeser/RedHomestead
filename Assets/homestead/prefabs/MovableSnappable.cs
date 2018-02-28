using RedHomestead.Simulation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public interface IMovableSnappable
{
    bool IsSnapped { get; }
    ICrateSnapper SnappedTo { get; }
    Transform transform { get; }
    Rigidbody movableRigidbody { get; }
    FixedJoint snapJoint { get; }
    void SnapCrate(ICrateSnapper snapParent, Vector3 snapPosition, Rigidbody jointRigid = null, Quaternion? globalRotation = null);
    void UnsnapCrate();
    void OnPickedUp();
    string GetText();
    float Progress { get; }
}

public abstract class MovableSnappable : MonoBehaviour, IMovableSnappable {
    private const float LerpTime = 0.15f;
    public AudioClip BangSoundClip;

    /// <summary>
    /// Is this snapped to something?
    /// </summary>
    public bool IsSnapped { get; protected set; }
    /// <summary>
    /// Thing that cratelike is snapped to. Optional. May be null.
    /// </summary>
    public ICrateSnapper SnappedTo { get; protected set; }
    /// <summary>
    /// Rigidbody of this snappable. Is not null.
    /// </summary>
    public Rigidbody movableRigidbody { get; protected set; }
    /// <summary>
    /// Joint that was created for this snap. Optional. May be null.
    /// </summary>
    public FixedJoint snapJoint { get; protected set; }

    public virtual float Progress
    {
        get { return -1f; }
    }
    
    void Awake()
    {
        movableRigidbody = GetComponent<Rigidbody>();
        if (movableRigidbody == null)
            movableRigidbody = transform.GetChild(0).GetComponent<Rigidbody>();
    }

    public void SnapCrate(ICrateSnapper snapParent, Vector3 snapPosition, Rigidbody jointRigid = null, Quaternion? globalRotation = null)
    {
        this.SnappedTo = snapParent;
        this.SnapCrate(snapParent.transform, snapPosition, jointRigid, globalRotation);
    }

    private Coroutine snapping;
    public void SnapCrate(Transform parent, Vector3 snapPosition, Rigidbody jointRigid = null, Quaternion? globalRotation = null)
    {
        this.IsSnapped = true;
        PlayerInput.Instance.DropObject();
        snapping = StartCoroutine(LerpToSnap(parent, snapPosition, jointRigid, globalRotation));
    }

    private IEnumerator LerpToSnap(Transform parent, Vector3 toPosition, Rigidbody jointRigid, Quaternion? globalRotation)
    {
        if (jointRigid != null)
        {
            movableRigidbody.velocity = Vector3.zero;
        }
        else
        {
            movableRigidbody.useGravity = false;
            movableRigidbody.isKinematic = true;
        }

        Vector3 fromPosition = transform.position;
        Quaternion fromRotation = transform.rotation;
        Quaternion toRotation = globalRotation ?? parent.rotation;

        float time = 0f;
        while (time < LerpTime)
        {
            transform.position = Vector3.Lerp(fromPosition, toPosition, time/LerpTime);
            transform.rotation = Quaternion.Lerp(fromRotation, toRotation, time/LerpTime);
            time += Time.deltaTime;
            yield return null;
        }

        transform.position = toPosition;
        transform.rotation = toRotation;

        if (jointRigid != null)
        {
            movableRigidbody.velocity = Vector3.zero;
            snapJoint = gameObject.AddComponent<FixedJoint>();
            snapJoint.connectedBody = jointRigid;
            snapJoint.breakForce = Mathf.Infinity;
            snapJoint.breakTorque = Mathf.Infinity;
            snapJoint.enableCollision = false;
        }
        
        PlayerInput.Instance.PlayInteractionClip(toPosition, BangSoundClip);
        OnSnap();
        snapping = null;
        print("I'm done moving to snap");
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
        if (this.SnappedTo != null)
        {
            this.SnappedTo.DetachCrate(this);
            this.SnappedTo = null;
        }

        if (this.snapJoint != null)
        {
            GameObject.Destroy(snapJoint);
        }
        movableRigidbody.isKinematic = false;
        movableRigidbody.useGravity = true;
        IsSnapped = false;

        PlayerInput.Instance.PlayInteractionClip(transform.position, BangSoundClip);
        OnDetach();
    }

    public abstract string GetText();

    public virtual void OnPickedUp() { }
}
