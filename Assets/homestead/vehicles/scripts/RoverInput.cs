using UnityEngine;
using System.Collections;
using UnityStandardAssets.CrossPlatformInput;
using RedHomestead.Persistence;
using System;
using RedHomestead.Simulation;
using System.Collections.Generic;

namespace RedHomestead.Rovers
{ 
    [RequireComponent(typeof(SixWheelCarController))]
    public class RoverInput : MonoBehaviour, IDataContainer<RoverData>, ICrateSnapper, ITriggerSubscriber
    {
        private SixWheelCarController m_Car; // the car controller we want to use
        private Rigidbody carRigid;

        public RoverData data;
        public RoverData Data { get { return data; } set { data = value; } }

        internal bool AcceptInput;

        private void Awake()
        {
            // get the car controller
            m_Car = GetComponent<SixWheelCarController>();
            carRigid = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            if (AcceptInput)
            {
                // pass the input to the car!
                float h = CrossPlatformInputManager.GetAxis("Horizontal");
                float v = CrossPlatformInputManager.GetAxis("Vertical");
    #if !MOBILE_INPUT
                float brake = CrossPlatformInputManager.GetAxis("Jump");
                m_Car.Move(h, v, -brake, 0f);
    #else
                m_Car.Move(h, v, v, 0f);
    #endif
            }
        }

        public void ExitBrake()
        {
            StartCoroutine(BrakeABit());
        }

        private IEnumerator BrakeABit()
        {
            float duration = 2f;
            float time = 0f;
            while(time < duration)
            {
                time += Time.deltaTime;
                carRigid.velocity = Vector3.zero;
                yield return null;
            }
        }

        public void DetachCrate(ResourceComponent detaching)
        {
            for (int i = 0; i < attachedCrates.Length; i++)
            {
                if (attachedCrates[i] == detaching)
                {
                    attachedCrates[i] = null;
                    break;
                }
            }
            detachTimer = StartCoroutine(DetachTimer());
        }

        private IEnumerator DetachTimer()
        {
            yield return new WaitForSeconds(1f);
            detachTimer = null;
        }

        private ResourceComponent[] attachedCrates = new ResourceComponent[4];

        private Coroutine detachTimer;
        public void OnChildTriggerEnter(TriggerForwarder child, Collider c, ResourceComponent res)
        {
            if (detachTimer == null && res != null)
            {
                if (child.name == "LeftLatch")
                {
                    int index = 0;
                    if (attachedCrates[index] != null)
                        index = 1;

                    attachedCrates[index] = res;

                    SnapToLatch(child, res, (index * -1));
                }
                else
                //if (childName == "RightLatch")
                {
                    int index = 2;
                    if (attachedCrates[index] != null)
                        index = 3;

                    attachedCrates[index] = res;

                    SnapToLatch(child, res, (index % 2 * -1));
                }
            }
        }

        private void SnapToLatch(TriggerForwarder child, ResourceComponent res, int offset)
        {
            res.SnapCrate(this, new Vector3(0, 0, .5f - offset), child.transform);
        }
    }
}
