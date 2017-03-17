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

        public Transform Hatch;
        public Transform[] CameraMounts;
        public Camera RoverCam;
        public TextMesh roverCamTextMesh;
        private int cameraMountIndex = 0;

        internal bool AcceptInput;
        private const float HatchOpenDegrees = 142;
        private const float HatchClosedRotationX = 0f;
        private const float HatchOpenRotationX = -HatchOpenDegrees;
        private float HatchDegrees = 0f;

        void Awake()
        {
            // get the car controller
            m_Car = GetComponent<SixWheelCarController>();
            carRigid = GetComponent<Rigidbody>();
        }

        void Start()
        {
            HatchDegrees = GetRotationXFromHatchState();
            Hatch.localRotation = Quaternion.Euler(HatchDegrees, 0f, 0f);
        }

        private float GetRotationXFromHatchState()
        {
            return Data.HatchOpen ? HatchOpenRotationX : HatchClosedRotationX;
        }

        private Coroutine hatchMovement;
        internal void ToggleHatchback(bool? state = null)
        {
            if (!state.HasValue)
                state = !Data.HatchOpen;

            Data.HatchOpen = state.Value;

            if (hatchMovement == null)
                hatchMovement = StartCoroutine(MoveHatch());
        }

        internal void ChangeCameraMount()
        {
            cameraMountIndex++;
            if (cameraMountIndex > CameraMounts.Length - 1)
                cameraMountIndex = 0;

            RoverCam.transform.SetParent(CameraMounts[cameraMountIndex]);
            RoverCam.transform.localPosition = Vector3.zero;
            RoverCam.transform.localRotation = Quaternion.identity;
            roverCamTextMesh.text = CameraMounts[cameraMountIndex].name;
        }

        private IEnumerator MoveHatch()
        {
            //we aren't using hatch.rotate here because reading from localRotation.eulerAngles is super unreliable
            while (HatchDegrees != GetRotationXFromHatchState())
            {
                HatchDegrees += Data.HatchOpen ? -1 : 1;
                Hatch.localRotation = Quaternion.Euler(HatchDegrees, 0f, 0f);
                yield return null;
            }
            hatchMovement = null;
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
                    int index = 1;
                    if (attachedCrates[index] != null)
                        index = 0;

                    attachedCrates[index] = res;

                    SnapToLatch(child, res, (index * -1)+.5f);
                }
                else
                //if (childName == "RightLatch")
                {
                    int index = 3;
                    if (attachedCrates[index] != null)
                        index = 2;

                    attachedCrates[index] = res;

                    SnapToLatch(child, res, (index % 2 * -1)+.5f);
                }
            }
        }

        private void SnapToLatch(TriggerForwarder child, ResourceComponent res, float offset)
        {
            res.SnapCrate(this, child.transform.position + child.transform.TransformDirection(new Vector3(0, 0, -offset)), carRigid);
        }
    }
}
