using UnityEngine;
using System.Collections;
using UnityStandardAssets.CrossPlatformInput;
using RedHomestead.Persistence;
using System;
using RedHomestead.Simulation;
using System.Collections.Generic;
using RedHomestead.Electricity;
using RedHomestead.Industry;

namespace RedHomestead.Rovers
{ 
    [RequireComponent(typeof(SixWheelCarController))]
    public class RoverInput : MonoBehaviour, IDataContainer<RoverData>, ICrateSnapper, ITriggerSubscriber, IBattery, ISink
    {
        public const float RoverDrainWattsPerSecond = 100f;
        private SixWheelCarController m_Car; // the car controller we want to use
        private Rigidbody carRigid;

        private RoverData data = null;
        public RoverData Data { get { return data; } set { data = value; } }
        internal bool AcceptInput;
        internal bool CanDrive { get { return !this.HasPowerGrid(); } }
        public bool CanMalfunction { get { return true; } }
        public SpriteRenderer flowAmountRenderer, dashboardPowerRenderer;

        public FailureAnchors failureEffectAnchors;
        public FailureAnchors FailureEffectAnchors { get { return failureEffectAnchors; } }
        public float FaultedPercentage { get { return data.FaultedPercentage; } set { data.FaultedPercentage = value; } }

        #region power
        public EnergyContainer EnergyContainer { get { return data.EnergyContainer; } }
        public PowerVisualization powerViz;
        public PowerVisualization PowerViz { get { return powerViz; } }
        public string PowerGridInstanceID { get; set; }
        public string PowerableInstanceID { get { return data.PowerableInstanceID; } }

        public List<IPumpable> AdjacentPumpables { get; private set; }
        #endregion

        #region camera members
        public Transform[] CameraMounts;
        public Camera RoverCam;
        public TextMesh roverCamTextMesh;
        private int cameraMountIndex = 0;
        #endregion

        #region hatch members
        public Transform Hatch;
        private const float HatchOpenDegrees = 142;
        private const float HatchClosedRotationX = 0f;
        private const float HatchOpenRotationX = -HatchOpenDegrees;
        private float HatchDegrees = 0f;
        #endregion

        public Light[] Lights;

        void Awake()
        {
            // get the car controller
            m_Car = GetComponent<SixWheelCarController>();
            carRigid = GetComponent<Rigidbody>();
        }

        void Start()
        {
            if (this.data == null)
            {
                this.data = new RoverData()
                {
                    EnergyContainer = new EnergyContainer()
                    {
                        TotalCapacity = ElectricityConstants.WattHoursPerBatteryBlock * 3
                    },
                    PowerableInstanceID = Guid.NewGuid().ToString(),
                    Oxygen = new ResourceContainer(Matter.Oxygen, .5f, ContainerSize.Full)
                };
            }
            this.AdjacentPumpables = new List<IPumpable>();
            this.InitializePowerVisualization();

            HatchDegrees = GetRotationXFromHatchState();
            Hatch.localRotation = Quaternion.Euler(HatchDegrees, 0f, 0f);
            ToggleLights(false);
            updateOxygen = StartCoroutine(UpdateOxygenBar());
        }

        private IEnumerator UpdateOxygenBar()
        {
            while (isActiveAndEnabled)
            {
                yield return new WaitForSeconds(.5f);
                
                flowAmountRenderer.transform.localScale = new Vector3(1, this.Data.Oxygen.UtilizationPercentage, 1);
                dashboardPowerRenderer.transform.localScale = new Vector3(1, this.Data.EnergyContainer.UtilizationPercentage * 0.7f, 1);

                if (AcceptInput)
                    this.RefreshVisualization();
            }
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

        private bool LightsOn = true;
        internal void ToggleLights(bool? state = null)
        {
            if (!state.HasValue)
                state = !LightsOn;

            LightsOn = state.Value;

            foreach(Light l in Lights)
            {
                l.enabled = state.Value;
            }
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
            if (AcceptInput && Data.EnergyContainer.CurrentAmount > 0f)
            {
                // pass the input to the car!
                float h = CrossPlatformInputManager.GetAxis("Horizontal");
                float v = CrossPlatformInputManager.GetAxis("Vertical");
    #if !MOBILE_INPUT
                float brake = CrossPlatformInputManager.GetAxis("Jump");
                m_Car.Move(h, v, v, -brake);
                Data.EnergyContainer.Pull(RoverDrainWattsPerSecond * Time.fixedDeltaTime * Mathf.Abs(v));
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

        public void DetachCrate(IMovableSnappable detaching)
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

        private IMovableSnappable[] attachedCrates = new IMovableSnappable[4];

        private Coroutine detachTimer;
        private Coroutine updateOxygen;

        public void OnChildTriggerEnter(TriggerForwarder child, Collider c, IMovableSnappable res)
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

        private void SnapToLatch(TriggerForwarder child, IMovableSnappable res, float offset)
        {
            res.SnapCrate(this, child.transform.position + child.transform.TransformDirection(new Vector3(0, 0, -offset)), carRigid);
        }

        public ResourceContainer Get(Matter c)
        {
            if (c == Matter.Oxygen)
                return Data.Oxygen;
            else
                return null;
        }

        public bool HasContainerFor(Matter c)
        {
            return c == Matter.Oxygen;
        }

        public void OnAdjacentChanged()
        {
            
        }
    }
}
