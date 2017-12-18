using RedHomestead.Perks;
using System;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Utility;
using Random = UnityEngine.Random;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
public class CustomFPSController : MonoBehaviour
{
    [SerializeField]
    public bool m_IsWalking;
    [SerializeField]
    public bool m_IsOnLadder, m_IsTransitioningLadder, m_PostTransitionLadderState;
    [SerializeField]
    private float m_WalkSpeed;
    [SerializeField]
    internal float m_RunSpeed;
    [SerializeField]
    [Range(0f, 1f)]
    private float m_RunstepLenghten;
    [SerializeField]
    private float m_JumpSpeed;
    [SerializeField]
    private float m_StickToGroundForce;
    [SerializeField]
    private float m_GravityMultiplier;
    [SerializeField]
    public MouseLook MouseLook;
    [SerializeField]
    private bool m_UseFovKick;
    [SerializeField]
    private CustomFOVKick m_FovKick = new CustomFOVKick();
    [SerializeField]
    private bool m_UseHeadBob;
    [SerializeField]
    private CurveControlledBob m_HeadBob = new CurveControlledBob();
    [SerializeField]
    private LerpControlledBob m_JumpBob = new LerpControlledBob();
    [SerializeField]
    private float m_StepInterval;
    [SerializeField]
    private AudioClip[] footstepsDust;
    [SerializeField]
    private AudioClip[] footstepsInterior;
    [SerializeField]
    private AudioClip m_JumpSound;           // the sound played when character leaves the ground.
    [SerializeField]
    private AudioClip m_LandSound;           // the sound played when character touches back on ground.

    private AudioClip[] activeFootsteps;

    private Camera m_Camera;
    private bool m_Jump, m_Thrusting;
    private float m_YRotation;
    private Vector2 m_Input;
    private Vector3 moveDirection = Vector3.zero;
    private Vector3 jumpDirection = Vector3.zero;

    private Vector3 moveToLadderPos, moveFromLadderPos;
    private float ladderMoveTime = 0f;
    private const float ladderMoveDuration = .25f;

    private CharacterController m_CharacterController;
    internal CharacterController CharacterController { get { return m_CharacterController; } }

    internal float SurvivalSpeedMultiplier = 1f;

    private CollisionFlags m_CollisionFlags;
    private bool m_PreviouslyGrounded;
    private Vector3 m_OriginalCameraPosition;
    private float m_StepCycle;
    private float m_NextStep;
    private bool m_Jumping;
    private AudioSource m_AudioSource;

    public Transform jetpackHarness;
    public ParticleSystem[] jetpacks;
    public bool FreezeMovement = false;
    public bool FreezeLook = false;

    // Use this for initialization
    private void Start()
    {
        m_CharacterController = GetComponent<CharacterController>();
        m_Camera = Camera.main;
        m_OriginalCameraPosition = m_Camera.transform.localPosition;
        m_FovKick.Setup(m_Camera);
        m_HeadBob.Setup(m_Camera, m_StepInterval);
        m_StepCycle = 0f;
        m_NextStep = m_StepCycle / 2f;
        m_Jumping = false;
        m_AudioSource = GetComponent<AudioSource>();
        m_RunSpeed *= PerkMultipliers.RunSpeed;
        //alex
        activeFootsteps = footstepsDust;
        this.InitializeMouseLook();
    }


    // Update is called once per frame
    private void Update()
    {
        if (!FreezeLook)
            RotateView();

        // the jump state needs to read here to make sure it is not missed
        if (!m_Jump && !m_Jumping)
        {
            m_Jump = CrossPlatformInputManager.GetButtonDown("Jump");
        }

        if (!m_PreviouslyGrounded && m_CharacterController.isGrounded)
        {
            //land
            StartCoroutine(m_JumpBob.DoBobCycle());
            PlayLandingSound();
            moveDirection.y = 0f;
            m_Jumping = false;
            m_Thrusting = false;
            if (jetpacks[0].isPlaying)
            {
                jetpacks[0].Stop();
                jetpacks[1].Stop();
            }
        }
        if (!m_CharacterController.isGrounded)
        {
            if (m_Jumping)
            {
                m_Thrusting = CrossPlatformInputManager.GetButton("Jump");
                if (m_Thrusting && !jetpacks[0].isPlaying)
                {
                    jetpacks[0].Play();
                    jetpacks[1].Play();
                }
                else if (!m_Thrusting && jetpacks[0].isPlaying)
                {
                    jetpacks[0].Stop();
                    jetpacks[1].Stop();
                }
            }
            else if (m_PreviouslyGrounded)
                moveDirection.y = 0f;
        }

        m_PreviouslyGrounded = m_CharacterController.isGrounded;
    }


    private void PlayLandingSound()
    {
        m_AudioSource.clip = m_LandSound;
        m_AudioSource.Play();
        m_NextStep = m_StepCycle + .5f;
        PlaceBootprint();
        PlaceBootprint();
    }

    Vector3 currentHitNormal, currentHitPosition, JetpackPropulsion = new Vector3(0, 9f, 0f);

    private void FixedUpdate()
    {
        float speed;
        GetInput(out speed);

        if (m_IsTransitioningLadder || m_IsOnLadder)
        {
            if (m_IsTransitioningLadder)
            {
                this.transform.position = Vector3.Lerp(moveFromLadderPos, moveToLadderPos, ladderMoveTime / ladderMoveDuration);

                ladderMoveTime += Time.fixedDeltaTime;

                if (ladderMoveTime > ladderMoveDuration)
                {
                    this.transform.position = moveToLadderPos;
                    m_IsTransitioningLadder = false;
                    m_IsOnLadder = m_PostTransitionLadderState;
                }
            }
            else if (transform.position.y + m_Input.y < maximumLadderY)
            {
                m_CharacterController.Move(new Vector3(0, m_Input.y, 0) * Time.fixedDeltaTime * speed / 2f);
            }
        }
        else
        {
            // always move along the camera forward as it is the direction that it being aimed at
            Vector3 desiredMove =
                m_Jumping && !m_Thrusting ?
                            jumpDirection
                            :
                            transform.forward * m_Input.y + transform.right * m_Input.x;

            // get a normal for the surface that is being touched to move along it
            RaycastHit hitInfo;
            Physics.SphereCast(transform.position, m_CharacterController.radius, Vector3.down, out hitInfo,
                                m_CharacterController.height / 2f, ~0, QueryTriggerInteraction.Ignore);

            desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;

            currentHitNormal = hitInfo.normal;
            currentHitPosition = hitInfo.point;

            moveDirection.x = desiredMove.x * speed;
            moveDirection.z = desiredMove.z * speed;

            if (m_CharacterController.isGrounded)
            {
                moveDirection.y = -m_StickToGroundForce;

                if (m_Jump)
                {
                    moveDirection.y = m_JumpSpeed;
                    jumpDirection = desiredMove;
                    PlayJumpSound();
                    m_Jump = false;
                    m_Jumping = true;
                }
            }
            else
            {
                if (m_Thrusting)
                {
                    moveDirection += JetpackPropulsion * Time.deltaTime;
                    //jetpackHarness.localRotation = Quaternion.Euler(-moveDirection.z*45f, 0f, -moveDirection.x * 45f);
                    jumpDirection = moveDirection;
                }

                moveDirection += Physics.gravity * m_GravityMultiplier * Time.fixedDeltaTime;
            }

            if (!FreezeMovement)
            {
                m_CollisionFlags = m_CharacterController.Move(moveDirection * Time.fixedDeltaTime);

                ProgressStepCycle(speed);
                UpdateCameraHeadBob(speed);
            }
        }

        //alex
        //m_MouseLook.UpdateCursorLock();
    }


    private void PlayJumpSound()
    {
        m_AudioSource.clip = m_JumpSound;
        m_AudioSource.Play();
    }


    private void ProgressStepCycle(float speed)
    {
        if (m_CharacterController.velocity.sqrMagnitude > 0 && (m_Input.x != 0 || m_Input.y != 0))
        {
            m_StepCycle += (m_CharacterController.velocity.magnitude + (speed * (m_IsWalking ? 1f : m_RunstepLenghten))) *
                            Time.fixedDeltaTime;
        }

        if (!(m_StepCycle > m_NextStep))
        {
            return;
        }

        m_NextStep = m_StepCycle + m_StepInterval;

        PlayFootStepAudio();
    }


    private void PlayFootStepAudio()
    {
        if (!m_CharacterController.isGrounded)
        {
            return;
        }
        // pick & play a random footstep sound from the array,
        // excluding sound at index 0
        int n = Random.Range(1, activeFootsteps.Length);
        m_AudioSource.clip = activeFootsteps[n];
        m_AudioSource.PlayOneShot(m_AudioSource.clip);
        // move picked sound to index 0 so it's not picked next time
        activeFootsteps[n] = activeFootsteps[0];
        activeFootsteps[0] = m_AudioSource.clip;

        PlaceBootprint();
    }


    private void UpdateCameraHeadBob(float speed)
    {
        Vector3 newCameraPosition;
        if (!m_UseHeadBob)
        {
            return;
        }
        if (m_CharacterController.velocity.magnitude > 0 && m_CharacterController.isGrounded)
        {
            m_Camera.transform.localPosition =
                m_HeadBob.DoHeadBob(m_CharacterController.velocity.magnitude +
                                    (speed * (m_IsWalking ? 1f : m_RunstepLenghten)));
            newCameraPosition = m_Camera.transform.localPosition;
            newCameraPosition.y = m_Camera.transform.localPosition.y - m_JumpBob.Offset();
        }
        else
        {
            newCameraPosition = m_Camera.transform.localPosition;
            newCameraPosition.y = m_OriginalCameraPosition.y - m_JumpBob.Offset();
        }
        m_Camera.transform.localPosition = newCameraPosition;
    }


    private void GetInput(out float speed)
    {
        // Read input
        float horizontal = CrossPlatformInputManager.GetAxis("Horizontal");
        float vertical = CrossPlatformInputManager.GetAxis("Vertical");

        bool waswalking = m_IsWalking;

        if (FreezeMovement)
        {
            speed = 0f;
            m_Input = new Vector2(0, 0);
        }
        else if (m_Jumping)
        {
            speed = m_IsWalking ? m_WalkSpeed : m_RunSpeed;
        }
        else
        {
#if !MOBILE_INPUT
            // On standalone builds, walk/run speed is modified by a key press.
            // keep track of whether or not the character is walking or running
            m_IsWalking = !Input.GetKey(KeyCode.LeftShift);
#endif
            // set the desired speed to be walking or running
            speed = m_IsWalking ? m_WalkSpeed : m_RunSpeed;
            m_Input = new Vector2(horizontal, vertical);

            // normalize input if it exceeds 1 in combined length:
            if (m_Input.sqrMagnitude > 1)
            {
                m_Input.Normalize();
            }
        }


        // handle speed change to give an fov kick
        // only if the player is going to a run, is running and the fovkick is to be used
        if (m_IsWalking != waswalking && m_UseFovKick && m_CharacterController.velocity.sqrMagnitude > 0)
        {
            StopAllCoroutines();
            StartCoroutine(!m_IsWalking ? m_FovKick.FOVKickUp() : m_FovKick.FOVKickDown());
        }

        speed *= SurvivalSpeedMultiplier;
    }


    private void RotateView()
    {
        MouseLook.LookRotation(transform, m_Camera.transform);
    }


    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody body = hit.collider.attachedRigidbody;
        //dont move the rigidbody if the character is on top of it
        if (m_CollisionFlags == CollisionFlags.Below)
        {
            return;
        }

        if (body == null || body.isKinematic)
        {
            return;
        }
        body.AddForceAtPosition(m_CharacterController.velocity * 0.1f, hit.point, ForceMode.Impulse);
    }

    public void GetOnLadder(Vector3 ladderPos, float maxLadderY)
    {
        if (m_IsOnLadder || m_IsTransitioningLadder)
        {
            print("can't get on ladder now");
            return;
        }
        else
        {
            this.moveFromLadderPos = this.transform.position;
            this.moveToLadderPos = new Vector3(ladderPos.x, this.transform.position.y, ladderPos.z);
            this.m_PostTransitionLadderState = true;
            this.m_IsTransitioningLadder = true;
            this.maximumLadderY = maxLadderY;
            this.ladderMoveTime = 0;
        }
    }

    public void GetOffLadder()
    {
        if (m_IsOnLadder || m_IsTransitioningLadder)
        {
            this.moveFromLadderPos = this.transform.position;
            this.moveToLadderPos = this.transform.TransformPoint(Vector3.back);
            this.ladderMoveTime = 0;
            this.m_PostTransitionLadderState = false;
            this.m_IsTransitioningLadder = true;
        }
        else
        {
            return;
        }
    }

    public SpriteRenderer[] bootSprites = new SpriteRenderer[8];

    private bool isOnLeftBoot = true;
    private int lastBootIndex = -1;

    public bool PlaceBootprints = true;
    private float maximumLadderY;

    private void PlaceBootprint()
    {
        if (this.PlaceBootprints)
        {
            lastBootIndex++;
            lastBootIndex %= bootSprites.Length;

            bootSprites[lastBootIndex].transform.position = currentHitPosition + currentHitNormal * .01f;
            bootSprites[lastBootIndex].transform.rotation = this.transform.rotation * Quaternion.Euler(currentHitNormal + Vector3.right * 90);
            bootSprites[lastBootIndex].flipX = !isOnLeftBoot;

            isOnLeftBoot = !isOnLeftBoot;
        }
    }

    internal void InitializeMouseLook()
    {
        MouseLook.Init(transform, m_Camera.transform);

        MouseLook.UpdateCursorLock();
    }

    internal void ToggleDustFootsteps(bool useDust)
    {
        activeFootsteps = useDust ? footstepsDust : footstepsInterior;
    }
}

