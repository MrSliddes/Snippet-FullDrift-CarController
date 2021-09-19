using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FD.Car
{
    public class CarControllerV3 : MonoBehaviour
    {
        /// <summary>
        /// Is the player allowed to drive the car
        /// </summary>
        public bool AllowedToDrive { get; set; }
        /// <summary>
        /// Are all car wheels on the road?
        /// </summary>
        public bool AllWheelsOnRoad { get; private set; }
        /// <summary>
        /// Is the car currently breaking
        /// </summary>
        public bool IsBreaking { get; private set; }
        /// <summary>
        /// Is the car currently drifting?
        /// </summary>
        public bool IsDrifting { get; private set; }
        /// <summary>
        /// Is the last drift a success?
        /// </summary>
        public bool IsDriftSuccessful { get; private set; }
        /// <summary>
        /// Is the car grounded?
        /// </summary>
        public bool IsGrounded
        {
            get
            {
                return Physics.Raycast(transform.position, -transform.up, 1.5f); //TODO needs work
            }
        }
        /// <summary>
        /// Is the car grounded on 1 of its wheels?
        /// </summary>
        public bool IsGroundedOnWheel
        {
            get
            {
                for(int i = 0; i < wheelSuspensions.Length; i++)
                {
                    if(wheelSuspensions[i].compressionRatio != 0) return true;
                }
                return false;
            }
        } // TODO could be better, raycast for each wheel, value not constistent / correct
        /// <summary>
        /// Is the car currently reversing
        /// </summary>
        public bool IsReversing { get { return RigidbodyDirection.z < 0; } }
        /// <summary>
        /// The current gear the car is in
        /// </summary>
        public int CurrentGear { get; private set; }
        /// <summary>
        /// Get the car current Kilometer per hour
        /// </summary>
        public int CurrentKMH { get { return Mathf.RoundToInt(CurrentMagnitude * 3.6f); } }
        /// <summary>
        /// The current drift direction of the car
        /// </summary>
        public int DriftDirection { get; private set; }
        /// <summary>
        /// The magnitude of the car rigidbody
        /// </summary>
        /// <value>0 to +999</value>
        public float CurrentMagnitude { get { return rb.velocity.magnitude; } }
        /// <summary>
        /// The current rpm calculated from current kmh
        /// </summary>
        public float CurrentRPMFromKMH
        {
            get // https://alphons.io/2294/what-is-the-formula-to-convert-kilometers-per-hour-km-slash-h-to-revolutions-per-minute-rpm/
            {
                return (25 / (3 * Mathf.PI * so.wheelRadius)) * CurrentKMH;
            }
        }
        /// <summary>
        /// Stimulates what the rpm would be in a realistic enviroment
        /// </summary>
        public float CurrentRPMFake
        {
            get
            {
                if(CurrentGear == 1)
                {
                    // Neutral
                    return so.motorRPM;
                }
                else
                {
                    return FD.MathC.Map(CurrentKMH, 0, so.gears[CurrentGear].speedKMH.x, so.motorRPMMin, so.motorRPMMax);
                }
            }
        }
        /// <summary>
        /// The minimum rpm the motor is before stalling
        /// </summary>
        public float MotorRPMMin { get { return so.motorRPMMin; } }
        /// <summary>
        /// The max rpm the motor can be
        /// </summary>
        public float MotorRPMMax { get { return so.motorRPMMax; } }
        /// <summary>
        /// The cars wheel radius
        /// </summary>
        public float WheelRadius { get { return so.wheelRadius; } }
        /// <summary>
        /// The current local direction of the rigidbody
        /// </summary>
        public Vector3 RigidbodyDirection { get { return transform.InverseTransformDirection(rb.velocity); } }
        /// <summary>
        /// The current car so.transition
        /// </summary>
        public CarTransition CurrentCarTransition { get { return so.transition; } }

        [Header("Debugging")]
        /// <summary>
        /// Show debug values
        /// </summary>
        public bool debug;

        [Header("Car")]
        [Tooltip("The car Scriptable Object component")]
        [SerializeField] private CarSO so;

        // Wheel
        /// <summary>
        /// Contains the transforms of the box collider corners
        /// </summary>
        /// <remarks>0 = front left, 1 = front right, 2 = rear left, 3 = rear right</remarks>
        public Transform[] boxColliderCornerPoints; //KEEP


        [Header("Components")]
        /// <summary>
        /// The position where force gets added to the car. Position this lower and too the front than rbCenter for car pull up effect
        /// </summary>
        public Transform acceleratingBreakingTransform;
        /// <summary>
        /// The collider that gets activated while drifting
        /// </summary>
        public BoxCollider carDriftCollider;
        /// <summary>
        /// The forward transform based on the ground under the car
        /// </summary>
        public Transform carNormal;
        /// <summary>
        /// The model of the car transform (parent)
        /// </summary>
        public GameObject carModel;
        /// <summary>
        /// The assigned center of mass of the car rb
        /// </summary>
        public Transform rbCenterOfMassTransform;
        /// <summary>
        /// The CarAudio of the car
        /// </summary>
        public CarAudio carAudio;

        /// <summary>
        /// Called when the car crashes
        /// </summary>
        /// <remarks>
        /// int0: 0 = front crash, 1 = right crash, 2 = rear crash, 3 = left crash. int1: car speed when crashing
        /// </remarks>
        [HideInInspector] public Action<int, int> OnCarCrash;
        /// <summary>
        /// Input for the car
        /// </summary>
        [HideInInspector] public CarInputController carInput = new CarInputController();
        /// <summary>
        /// The box collider bottom corners, front left, front right, rear left, rear right
        /// </summary>
        [HideInInspector] public CarWheelSuspension[] wheelSuspensions;
        /// <summary>
        /// The CarScore
        /// </summary>
        [HideInInspector] public CarScore score;
        /// <summary>
        /// The car effects
        /// </summary>
        [HideInInspector] public CarEffectsV3 effects;
                
        /// <summary>
        /// Keeps track of the kmh of the previous frame in case a collision happens
        /// </summary>
        private int kmhPreviousFrame;
        /// <summary>
        /// The angluar drag of the car while on the ground
        /// </summary>
        private float angularDragOnGround;
        /// <summary>
        /// Checks how long the player is countersteering the car
        /// </summary>
        private float counterSteerTooLongTimer;
        /// <summary>
        /// Checks how long the player hasnt countersteerd (preventing straight drifting / reverse drift direction drifting)
        /// </summary>
        private float counterSteerNotUsedTimer;
        /// <summary>
        /// Stores the current y axis rotation of the car model
        /// </summary>
        private float currentCarModelRotation;
        /// <summary>
        /// The current drift force applied to the car while drifting
        /// </summary>
        private float currentDriftForce = 0;
        /// <summary>
        /// The current speed of the car (just a float value)
        /// </summary>
        private float currentSpeed;
        /// <summary>
        /// The current rotation of the car controller rb
        /// </summary>
        private float currentRotate;
        /// <summary>
        /// The drag of the car while on the ground
        /// </summary>
        private float dragOnGround;
        /// <summary>
        /// Used for smoothdamp and nothing more
        /// </summary>
        private float emptyRefVelocity;
        /// <summary>
        /// How efficient is the current gear? If the car is going slower then the speedMin of the gear it has a low efficiency
        /// </summary>
        /// <remarks>
        /// Ranges from 0.01 to 1
        /// </remarks>
        private float gearEfficiency;
        /// <summary>
        /// The mass the car rb has while on the ground
        /// </summary>
        private float massOnGround;
        /// <summary>
        /// The max speed the car is allowed to go
        /// </summary>
        private float maxSpeed;
        /// <summary>
        /// Used for calculating the rotation of the car rb
        /// </summary>
        private float rotate;

        /// <summary>
        /// The car boxCollider
        /// </summary>
        private BoxCollider boxCollider;        
        /// <summary>
        /// Rigidbody of the car
        /// </summary>
        private Rigidbody rb;

        private void Awake()
        {
            // Set
            score = new CarScore(this);
        }

        // Start is called before the first frame update
        void Start()
        {
            // Check if GameRules script is present
            if(FindObjectOfType<GameRules>() != null)
            {
                // Set carSO to that of game rules
                GameRules gr = FindObjectOfType<GameRules>();
                Debug.Log("[Car] detected GameRules");
                so = gr.carSO;
            }

            // Get
            boxCollider = GetComponent<BoxCollider>();
            rb = GetComponent<Rigidbody>();
            effects = GetComponent<CarEffectsV3>();

            // Set
            // Setup CarSO
            SetupCarSO();
            rb.centerOfMass = rbCenterOfMassTransform.localPosition;
            dragOnGround = rb.drag;
            massOnGround = rb.mass;
            angularDragOnGround = rb.angularDrag;
            SetupWheelSuspensions();

            AllowedToDrive = false;
            if(FindObjectOfType<CarUI>() == null) AllowedToDrive = true;
        }

        // Update is called once per frame
        void Update()
        {
            UpdateGears();
            UpdateCar();
            UpdateDrift();
            score.Update();
        }

        private void LateUpdate()
        {
            score.LateUpdate();
        }

        private void FixedUpdate()
        {
            UpdateCarFixed();
            UpdateSuspensionFixed();
        }
            
        /// <summary>
        /// Sets up the car SO
        /// </summary>
        private void SetupCarSO()
        {
            // Car box collider
            boxCollider.material = so.boxColliderPhyMat;
            boxCollider.center = so.boxColliderCenter;
            boxCollider.size = so.boxColliderSize;

            // Car rb
            rb.mass = so.rbMass;
            rb.drag = so.rbDrag;
            rb.angularDrag = so.rbAngularDrag;

            // Car model
            // Clear carModel children, then add model, effects, model collision
            foreach(Transform child in carModel.transform)
            {
                Destroy(child.gameObject);
            }
            // Car model
            CarModel carM = Instantiate(so.model, carModel.transform).GetComponent<CarModel>();
            // Set CarEffectsV3 wheel transforms
            effects.wheelTransforms = carM.wheelTransforms;
            effects.wheelParentTransforms = carM.wheelParentTransforms;
            // Car model effects
            CarModelEffects carME = Instantiate(so.modelEffects, carModel.transform).GetComponent<CarModelEffects>();
            // Set CarEffectsV3 tireMarks
            effects.tireMarks = new TrailRenderer[carME.tireMarks.Length];
            effects.tireMarks = carME.tireMarks;
            effects.maxSuspensionDistance = so.maxSuspensionDistance;
            // Car Model Collision
            Instantiate(so.modelCollision, carModel.transform);
            // RB center of mass
            rbCenterOfMassTransform.localPosition = so.transformRBCenterOfMass;
            // Accelerating / Breaking transform
            acceleratingBreakingTransform.localPosition = so.transformAcceleratingBreaking;
            // Drift collider
            carDriftCollider.material = so.driftColliderPhyMat;
            carDriftCollider.center = so.driftColliderCenter;
            carDriftCollider.size = so.driftColliderSize;
            // Car Audio
            GetComponentInChildren<FMODUnity.StudioEventEmitter>().Event = so.fmodEventName;
        }

        /// <summary>
        /// Sets the rotate amout for the car to rotate the rb
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="amount"></param>
        private void SteerCar(int direction, float amount)
        {
            // Limit so.steering by animationCurve
            rotate = (so.steerControl.Evaluate(CurrentKMH) * so.steering * direction) * amount;
        }

        /// <summary>
        /// Gets the positions of the box collider corners
        /// </summary>
        private void SetupWheelSuspensions()
        {
            wheelSuspensions = new CarWheelSuspension[4];
            for(int i = 0; i < wheelSuspensions.Length; i++)
            {
                wheelSuspensions[i] = new CarWheelSuspension();
            }
            float yUpwardsMargin = 0.01f; // By setting the y a little higher this prevents a raycast bug where the box would be on the ground, compression would be 0 so the box would stay on the ground instead of going up
            // Front left bottom
            boxColliderCornerPoints[0].localPosition = boxCollider.center + new Vector3(-boxCollider.size.x, -boxCollider.size.y + yUpwardsMargin, boxCollider.size.z) * 0.5f;
            // Front right bottom
            boxColliderCornerPoints[1].localPosition = boxCollider.center + new Vector3(boxCollider.size.x, -boxCollider.size.y + yUpwardsMargin, boxCollider.size.z) * 0.5f;
            // Rear left bottom
            boxColliderCornerPoints[2].localPosition = boxCollider.center + new Vector3(-boxCollider.size.x, -boxCollider.size.y + yUpwardsMargin, -boxCollider.size.z) * 0.5f;
            // Rear right bottom
            boxColliderCornerPoints[3].localPosition = boxCollider.center + new Vector3(boxCollider.size.x, -boxCollider.size.y + yUpwardsMargin, -boxCollider.size.z) * 0.5f;
        }

        private void UpdateCar()
        {
            if(!AllowedToDrive) return;

            // Car speed
            if(carInput.Vertical > 0 && CurrentGear != 1)
            {
                // Speed up car
                maxSpeed = so.gears[CurrentGear].speed.x;
            }
            else if(carInput.Vertical < 0 && CurrentGear != 1)
            {
                // Break the car
                maxSpeed = -so.breakSpeed;
            }
            else
            {
                maxSpeed = 0;
                if(CurrentKMH == 0) rb.velocity = new Vector3(0, rb.velocity.y, 0);
            }            

            // Steer car
            if(carInput.Horizontal != 0)
            {
                int dir = carInput.Horizontal > 0 ? 1 : -1;
                dir = RigidbodyDirection.z < 0 ? dir * -1 : dir; // Reverse has reverse control
                float amount = IsDrifting ? currentDriftForce : Mathf.Abs(carInput.Horizontal);
                // If car comes out of drifting slowly reduce drift force and use it for so.steering to allow better control when coming out of drift
                if(!IsDrifting && currentDriftForce > 1) amount = currentDriftForce;

                // Prevent car from so.steering when coming out of reverse
                if(!IsReversing || IsReversing && carInput.Vertical < 0)
                {
                    SteerCar(dir, amount); // doesnt get disabled while drifting for countersteer, not the proper way but it works
                }
            }

            float c = CurrentGear == 1 ? 1 : gearEfficiency; // Prevent NaN
            currentSpeed = Mathf.SmoothStep(currentSpeed, maxSpeed, so.gears[CurrentGear].speedIncrease * c * Time.deltaTime);
            currentRotate = Mathf.Lerp(currentRotate, rotate, Time.deltaTime * 4f); rotate = 0;

            // Check if car is breaking
            // if v speed is positive breaking is neg, else its inverted
            IsBreaking = carInput.Vertical < 0 && CurrentMagnitude > 0 && RigidbodyDirection.z > 0
                      || carInput.Vertical > 0 && CurrentMagnitude > 0 && RigidbodyDirection.z < 0;
            if(carInput.Vertical < 0 && CurrentGear == 2 && CurrentKMH == 0) IsBreaking = false;

            // Motor rpm
            // Update motor rpm
            if(carInput.Vertical > 0)
            {
                // Increase rpm
                so.motorRPM += so.motorRPMIncreaseSpeed * Time.deltaTime;
            }
            else if(so.motorRPM > so.motorRPMMin)
            {
                // Limit rpm min
                so.motorRPM -= so.motorRPMDecreaseSpeed * Time.deltaTime;
            }

            // Limit max / min
            if(so.motorRPM > so.motorRPMMax)
            {
                so.motorRPM = so.motorRPMMax;
            }
            else if(so.motorRPM < so.motorRPMMin) so.motorRPM = so.motorRPMMin;
        }

        private void UpdateCarFixed() // Check time.fixeddeltatime
        {
            // Car rb values
            rb.mass = IsGrounded ? massOnGround : so.inAirMass;
            rb.drag = IsGroundedOnWheel ? dragOnGround : so.inAirDrag;
            rb.angularDrag = IsGroundedOnWheel ? angularDragOnGround : so.inAirAngularDrag;
            // If player isnt pressing vertical reduce drag by half, to let car roll out more
            //if(carInput.Vertical == 0) rb.drag = IsGroundedOnWheel ? dragOnGround / 2 : so.inAirDrag / 2; //BUG speeds up when letting loose while accelerating, check if car isnt accelerating

            kmhPreviousFrame = CurrentKMH;

            // Car normal transform
            RaycastHit hit;
            Physics.Raycast(transform.position, Vector3.down, out hit, 2f);

            carNormal.up = Vector3.Lerp(carNormal.up, hit.normal, Time.deltaTime * 8.0f);
            carNormal.Rotate(0, transform.eulerAngles.y, 0);

            // Rotate car
            if(IsGrounded) rb.AddTorque(carNormal.up * currentRotate);
            // maybe add it via rear axis https://answers.unity.com/questions/407085/add-torque-at-position.html

            // Forward acceleration
            if(IsGrounded) rb.AddForceAtPosition(carNormal.forward * currentSpeed, acceleratingBreakingTransform.position);

            // Traction controll / slip reduction https://www.reddit.com/r/Unity3D/comments/8ms5r5/how_to_find_local_sideways_velocity_of_an_object/
            Vector3 vel = transform.InverseTransformDirection(rb.velocity);
            rb.AddRelativeForce(Vector3.right * -vel.x * so.tractionAxisX);

            // Car gravity
            if(!IsGrounded) rb.AddForce(Vector3.down * so.inAirGravityForce);
        }

        #region Drifting

        private void EnterDrift()
        {
            // Set values
            IsDrifting = true;
            IsDriftSuccessful = false;
            DriftDirection = carInput.Horizontal > 0 ? 1 : -1;
            currentDriftForce = 0;
            counterSteerTooLongTimer = so.counterSteerTooLongTime;
            counterSteerNotUsedTimer = 0;

            // Chance layer of boxCollider, turn carDriftCollider on
            boxCollider.gameObject.layer = 9; // Ignore all layer
            carDriftCollider.enabled = true;
        }

        /// <summary>
        /// Exit the car drift
        /// </summary>
        /// <param name="hardDriftCollision">If the car had collision with wall while drifting</param>
        private void ExitDrift(bool hardDriftCollision = false)
        {
            IsDrifting = false;
            IsDriftSuccessful = !hardDriftCollision;

            if(hardDriftCollision)
            {
                // Car came into contact will wall so
                // chance boxCollider to that of carDriftCollider
                transform.rotation = carDriftCollider.transform.rotation;
                carDriftCollider.transform.localRotation = Quaternion.identity;
                carModel.transform.localRotation = Quaternion.identity;
            }
            carDriftCollider.enabled = false;
            boxCollider.gameObject.layer = 8; // Car layer
            //Debug.Log("exit drifting");
        }

        /// <summary>
        /// Updates the car Drift
        /// </summary>
        private void UpdateDrift()
        {
            // Enter drift when not drifting
            if(!IsDrifting && carInput.SpaceDown && carInput.Horizontal != 0 && RigidbodyDirection.z > 0 && CurrentKMH >= so.minKMHForDrift)
            {
                EnterDrift();
            }

            if(IsDrifting)
            {
                // Prevent reverse drift direction drifting
                if(carInput.Horizontal != 0 && carInput.Horizontal != DriftDirection)
                {
                    // Is counter so.steering
                    counterSteerTooLongTimer -= Time.deltaTime;
                    if(counterSteerTooLongTimer <= 0) ExitDrift();
                    counterSteerNotUsedTimer = 0;
                }
                else
                {
                    // Prevent user from rapid tapping to still straight drift
                    if(carInput.Horizontal != DriftDirection)
                        counterSteerNotUsedTimer += Time.deltaTime;

                    if(counterSteerNotUsedTimer > 0.5f) // Hard coded global for all cars value
                    {
                        // Reset
                        counterSteerTooLongTimer = so.counterSteerTooLongTime;
                        counterSteerNotUsedTimer = 0;
                    }
                }

                // Update drift force (so.steering with drift or counter so.steering)
                if(carInput.Horizontal > 0)
                {
                    // Right
                    if(DriftDirection < 0)
                    {
                        // Decrease drift, Countersteer right
                        currentDriftForce -= so.driftChangeForce * Time.deltaTime;
                    }
                    else
                    {
                        // Increase drift, turning towords drift
                        currentDriftForce += so.driftChangeForce * Time.deltaTime;
                    }
                }
                else if(carInput.Horizontal < 0)
                {
                    // Left
                    if(DriftDirection < 0)
                    {
                        // Increase drift, turning towords drift
                        currentDriftForce += so.driftChangeForce * Time.deltaTime;
                    }
                    else
                    {
                        // Decrease drift, Countersteer right
                        currentDriftForce -= so.driftChangeForce * Time.deltaTime;
                    }
                }

                // Add continues drift force
                currentDriftForce += so.driftChangeForce * 0.5f * Time.deltaTime;

                // Clamp current drift force
                currentDriftForce = Mathf.Clamp(currentDriftForce, so.minDriftForce, so.maxDriftForce);

                // Steer car towords drift direction
                SteerCar(DriftDirection, currentDriftForce);

                // Rotate car model based on currentDriftForce
                // Map driftForce to max rotation angle the car can be
                float rotY = SLIDDES.MathC.Map(currentDriftForce, so.minDriftForce, so.maxDriftForce, 0, 45);

                currentCarModelRotation = Mathf.SmoothDamp(currentCarModelRotation, rotY * DriftDirection, ref emptyRefVelocity, 5 * Time.deltaTime);
                carModel.transform.localRotation = Quaternion.Euler(0, currentCarModelRotation, 0);

                // Rotate car drift collider to that of model
                carDriftCollider.transform.localRotation = carModel.transform.localRotation;

                // Exit drift
                if(!carInput.Space || CurrentKMH < so.minKMHForDrift)
                {
                    ExitDrift();
                }
            }
            else
            {
                // Reduce current drift force back to 0
                if(currentDriftForce > 0) currentDriftForce -= Time.deltaTime * so.comingOutDriftReducer;
                currentCarModelRotation = Mathf.SmoothDamp(currentCarModelRotation, 0, ref emptyRefVelocity, 30 * Time.deltaTime);

                // Rotate car model back to 0 smoothly
                carModel.transform.localRotation = Quaternion.Euler(0, currentCarModelRotation, 0);
            }
        }

        #endregion

        /// <summary>
        /// Updates the car wheel suspension
        /// </summary>
        private void UpdateSuspensionFixed()
        {
            // This does not take into account the wheel suspensions min/max distance causing the wheels in CarEffectsV3 to clip if the box collider is touching the ground.

            // Check if all wheels hit road, set to true and to false if 1 doesnt
            AllWheelsOnRoad = true;
            // Perform a raycast from each bottom corner from the box
            RaycastHit hit;
            for(int i = 0; i < wheelSuspensions.Length; i++)
            {
                if(Physics.Raycast(boxColliderCornerPoints[i].position, -boxColliderCornerPoints[i].up, out hit, so.maxSuspensionDistance, so.layerMaskWheel))
                {
                    wheelSuspensions[i].compressionRatio = Mathf.Abs((hit.distance / so.maxSuspensionDistance) - 1); // 0-1 -> 1-0
                    wheelSuspensions[i].surfaceImpactPoint = hit.point;
                    wheelSuspensions[i].surfaceImpactNormal = hit.normal;

                    if(hit.transform.gameObject.layer != 15) AllWheelsOnRoad = false;

                    // Add force to corner
                    rb.AddForceAtPosition(transform.up * so.suspensionUpwardForce * wheelSuspensions[i].compressionRatio, boxColliderCornerPoints[i].position);
                }
                else
                {
                    // Didnt hit ground
                    wheelSuspensions[i].compressionRatio = 0;
                    wheelSuspensions[i].surfaceImpactPoint = boxColliderCornerPoints[i].transform.position - boxColliderCornerPoints[i].up * so.maxSuspensionDistance;
                    wheelSuspensions[i].surfaceImpactNormal = boxColliderCornerPoints[i].up;
                }
            }
        }

        #region Gears

        private void UpdateGears()
        {
            // Change so.transition
            if(carInput.TDown)
            {
                // Gos to next so.transition (loops back around)
                so.transition = so.transition.Next();
            }

            // Update current so.transition
            switch(so.transition)
            {
                case CarTransition.automatic:
                    // Check if gear has to increase or decrease
                    if(carInput.Vertical > 0)
                    {
                        // Increase speed
                        // Check if we can go to next gear
                        if(so.motorRPM >= 2500 && CurrentKMH >= so.gears[CurrentGear].speedKMH.x - 5 && CurrentGear != so.gears.Length - 1)
                        {
                            ChangeGear(+1);
                        }

                        // From reverse to neutral
                        if(CurrentGear == 0 && CurrentKMH == 0)
                        {
                            CurrentGear = 1;
                        }

                        // From neutral to g1 (without rpm)
                        if(CurrentGear == 1 && CurrentKMH == 0)
                        {
                            CurrentGear = 2;
                        }

                        // If holding forward but drifting that slows the speed
                        if(CurrentKMH <= so.gears[CurrentGear].speedKMH.y && CurrentGear != 0 && CurrentGear != 1)
                        {
                            //ChangeGear(-1);
                        }
                    }
                    else if(carInput.Vertical < 0)
                    {
                        // Decrease speed / reverse
                        if(CurrentKMH <= so.gears[CurrentGear].speedKMH.y && CurrentGear != 0 || CurrentGear == 1 && CurrentKMH == 0)
                        {
                            ChangeGear(-1);
                        }

                        // If in neutral
                        if(CurrentGear == 1)
                        {
                            CurrentGear = 0;
                            IsBreaking = false;
                        }

                        // From g1 to neutral
                        if(CurrentGear == 2 && CurrentKMH == 0)
                        {
                            CurrentGear = 1;
                        }
                    }
                    else
                    {
                        // No user input, change so.gears based on speed
                        if(CurrentKMH <= so.gears[CurrentGear].speedKMH.y && CurrentGear != 1)
                        {
                            ChangeGear(-1);
                        }

                        if(Mathf.FloorToInt(CurrentKMH) == 0)
                            CurrentGear = 1;
                    }
                    break;
                case CarTransition.drift:
                    // Drift so.transition, same as auto but limits max gear for easier drift experience
                    if(carInput.Vertical > 0)
                    {
                        // Increase speed
                        // Check if we can go to next gear
                        if(so.motorRPM >= 2500 && CurrentKMH >= so.gears[CurrentGear].speedKMH.x - 5 && CurrentGear != so.gears.Length - 1  && CurrentGear < so.transitionDriftMaxGear)
                        {
                            ChangeGear(+1);
                        }

                        // From reverse to neutral
                        if(CurrentGear == 0 && CurrentKMH == 0)
                        {
                            CurrentGear = 1;
                        }

                        // From neutral to g1 (without rpm)
                        if(CurrentGear == 1 && CurrentKMH == 0)
                        {
                            CurrentGear = 2;
                        }

                        // If holding forward but drifting that slows the speed
                        if(CurrentKMH <= so.gears[CurrentGear].speedKMH.y && CurrentGear != 0 && CurrentGear != 1)
                        {
                            //ChangeGear(-1);
                        }
                    }
                    else if(carInput.Vertical < 0)
                    {
                        // Decrease speed / reverse
                        if(CurrentKMH <= so.gears[CurrentGear].speedKMH.y && CurrentGear != 0 || CurrentGear == 1 && CurrentKMH == 0)
                        {
                            ChangeGear(-1);
                        }

                        // If in neutral
                        if(CurrentGear == 1)
                        {
                            CurrentGear = 0;
                            IsBreaking = false;
                        }

                        // From g1 to neutral
                        if(CurrentGear == 2 && CurrentKMH == 0)
                        {
                            CurrentGear = 1;
                        }
                    }
                    else
                    {
                        // No user input, change so.gears based on speed
                        if(CurrentKMH <= so.gears[CurrentGear].speedKMH.y && CurrentGear != 1)
                        {
                            ChangeGear(-1);
                        }

                        if(Mathf.FloorToInt(CurrentKMH) == 0)
                            CurrentGear = 1;
                    }
                    break;
                case CarTransition.manual:
                    // Manual so.transition, player has to change so.gears himself
                    if(Input.GetKeyDown(KeyCode.R) && CurrentGear != so.gears.Length - 1)
                    {
                        // Shift up
                        ChangeGear(+1);
                    }
                    else if(Input.GetKeyDown(KeyCode.F) && CurrentGear != 0)
                    {
                        // Shift down
                        ChangeGear(-1);
                    }
                    break;
                default:
                    break;
            }

            // Update the gear effiecency (prevent NaN)
            float eff;
            if(CurrentKMH == 0 || so.gears[CurrentGear].speedKMH.y == 0)
            {
                eff = 1;
            }
            else
            {
                eff = CurrentKMH / so.gears[CurrentGear].speedKMH.y;
            }
            // Clamp value
            gearEfficiency = Mathf.Clamp(eff, 0.01f, 1);
        }

        /// <summary>
        /// Changes the current active gear
        /// </summary>
        /// <remarks>
        /// ! Important, this does not check if it is possible to change the so.gears
        /// </remarks>
        /// <param name="value"></param>
        private void ChangeGear(int value = 1)
        {
            if(value == 1)
            {
                // Increase the current gear
                CurrentGear++;
                so.motorRPM *= 0.8f; // 20%  //TODO add value of 20%-25% decrease/increase based on how close rpm is to 2500 
            }
            else
            {
                // Decrease the current gear
                CurrentGear--;
                so.motorRPM *= 1.2f; // 20% 
            }
        }
        #endregion

        private void OnCollisionEnter(Collision collision)
        {
            // Collision with road wall
            if(collision.gameObject.layer == 12)
            {
                // Shake camera
                int collisionDir = 0;
                // Detect if collision is in front or back (crash collision
                float angle = Vector3.Angle(collision.GetContact(0).normal, Vector3.forward);
                if(Mathf.Approximately(angle, 180))
                {
                    // Front
                    collisionDir = 0;
                }
                else if(Mathf.Approximately(angle, 0))
                {
                    // Back
                    collisionDir = 2;
                }
                else if(Mathf.Approximately(angle, 90))
                {
                    // Sides
                    Vector3 cross = Vector3.Cross(Vector3.forward, collision.GetContact(0).normal);
                    if(cross.y > 0)
                    {
                        // Left side
                        collisionDir = 3;
                    }
                    else
                    {
                        // Right side
                        collisionDir = 1;
                    }
                }

                // Invoke OnCarCrash (send kmh pre crash)
                OnCarCrash.Invoke(collisionDir, kmhPreviousFrame);

                // If drifting reset colliders
                if(IsDrifting) ExitDrift(true);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if(debug && Application.isPlaying)
            {
                Gizmos.color = Color.red;
                // Draw suspension lines
                for(int i = 0; i < wheelSuspensions.Length; i++)
                {
                    Gizmos.DrawLine(boxColliderCornerPoints[i].position, wheelSuspensions[i].surfaceImpactPoint);
                    UnityEditor.Handles.Label(boxColliderCornerPoints[i].position, wheelSuspensions[i].compressionRatio.ToString());
                }

                // Draw ground raycast
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, transform.position - transform.up * 1.5f);

                // All wheels road
                UnityEditor.Handles.Label(transform.position, "OnRoad: " + AllWheelsOnRoad.ToString());
            }
        }
#endif
    }
}