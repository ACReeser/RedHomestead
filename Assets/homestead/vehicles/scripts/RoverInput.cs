using UnityEngine;
using System.Collections;
using UnityStandardAssets.CrossPlatformInput;
using RedHomestead.Persistence;
using System;

namespace RedHomestead.Rovers
{ 
    [RequireComponent(typeof(SixWheelCarController))]
    public class RoverInput : MonoBehaviour, IDataContainer<RoverData>
    {
        private SixWheelCarController m_Car; // the car controller we want to use

        public RoverData data;
        public RoverData Data { get { return data; } set { data = value; } }

        internal bool AcceptInput;

        private void Awake()
        {
            // get the car controller
            m_Car = GetComponent<SixWheelCarController>();
        }

        private void FixedUpdate()
        {
            if (AcceptInput)
            {
                // pass the input to the car!
                float h = CrossPlatformInputManager.GetAxis("Horizontal");
                float v = CrossPlatformInputManager.GetAxis("Vertical");
    #if !MOBILE_INPUT
                float handbrake = CrossPlatformInputManager.GetAxis("Jump");
                m_Car.Move(h, v, v, handbrake);
    #else
                m_Car.Move(h, v, v, 0f);
    #endif
            }
        }
    }
}
