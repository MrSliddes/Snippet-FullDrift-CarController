using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FD.Car
{
    /// <summary>
    /// Contains all values of a car type
    /// </summary>
    [System.Serializable]
    [CreateAssetMenu(fileName = "CarSO_Mustang", menuName = "FD/Car/Car SO")]
    public class CarSO : ScriptableObject
    {
        [Header("Box Collider")]
        [Tooltip("The Physics Material of the car box collider")]
        public PhysicMaterial boxColliderPhyMat;
        [Tooltip("The local position center of the car box collider")]
        public Vector3 boxColliderCenter = new Vector3(0f, 0.105031386f, 0f);
        [Tooltip("The size of the car box collider")]
        public Vector3 boxColliderSize = new Vector3(1.5f, 0.789937258f, 3.8499999f);

        [Header("Rigidbody")]
        [Tooltip("The mass of the rigidbody")]
        public float rbMass = 1;
        [Tooltip("The drag of the rigidbody")]
        public float rbDrag = 2;
        [Tooltip("The angular of the rigidbody")]
        public float rbAngularDrag = 2;

        [Header("Car")] // This has to be swapped so that car controller is above car
        [Header("Car Controller")]
        [Tooltip("How much traction is applied to the car on the x axis")]
        public float tractionAxisX = 40;
        [Tooltip("How fast the car can gain speed / slow down")]
        public float carSpeedChange = 6f;
        [Tooltip("How fast the car breaks")]
        public float breakSpeed = 30f;
        [Tooltip("The steering amount of the wheel applied to rotating the car")]
        public float steering = 5;
        [Tooltip("The mass the car receives while in the air")]
        public float inAirMass = 100;
        [Tooltip("The rb drag while the car is in the air")]
        public float inAirDrag = 0.0f;
        [Tooltip("The angular drag of the car while in the air")]
        public float inAirAngularDrag = 0.01f;
        [Tooltip("The extra gravity force applied to the car while in air")]
        public float inAirGravityForce = 9;

        [Header("Motor")]
        [Tooltip("The current rpm of the motor")]
        public float motorRPM = 1000;
        [Tooltip("The max rpm the motor can achieve")]
        public float motorRPMMax = 8000;
        [Tooltip("The minimum the engine can be before stalling")]
        public float motorRPMMin = 1000;
        [Tooltip("How fast the motor gains rpm")]
        public float motorRPMIncreaseSpeed = 2000;
        [Tooltip("How fast the motor loses rpm")]
        public float motorRPMDecreaseSpeed = 3000;

        [Header("Wheels")]
        [Tooltip("What layermasks the wheel can contact with")]
        public LayerMask layerMaskWheel;
        [Tooltip("The radius of the wheel expressed in meters")]
        public float wheelRadius = 0.57f;
        [Tooltip("The higher the kmh the more control steering gives")]
        public AnimationCurve steerControl;
        [Tooltip("How far the suspension is allowed to raycast down")]
        public float maxSuspensionDistance = 0.6f;
        [Tooltip("The force that gets added to the suspension")]
        public float suspensionUpwardForce = 5;

        [Header("Drifting")]
        [Tooltip("The minimum speed the car needs to go in order to start a drift")]
        public float minKMHForDrift = 15;
        [Tooltip("How fast the currentDriftForce can be changed by player input")]
        public float driftChangeForce = 2f;
        [Tooltip("The minimum drift force applied while drifting")]
        public float minDriftForce = 0.5f;
        [Tooltip("The max drift force that can be applied while drifting")]
        public float maxDriftForce = 2;
        [Tooltip("When the car comes out of drift the current drift force bleeds back to < 0 with this amount")]
        public float comingOutDriftReducer = 0.5f;
        [Tooltip("If car gets counter steerd too long while drifting it exits drift\nShould be smaller than driftChangeForce")]
        public float counterSteerTooLongTime = 1.5f;
        [Tooltip("The max gear the car will go into while in drift transition")]
        public int transitionDriftMaxGear = 4;

        [Header("Transition")]
        [Tooltip("The current transition of the car")]
        public CarTransition transition = CarTransition.drift;
        [Tooltip("The gear values of the car")]
        public CarGear[] gears = new CarGear[] { new CarGear("Reverse", 6, new Vector2(11.5f, 0), new Vector2(20, 0)), new CarGear("Neutral", 0, Vector2.zero, Vector2.zero),
                                                new CarGear("1st", 12, new Vector2(11.5f, 0), new Vector2(20, 0)), new CarGear("2nd", 10, new Vector2(17.5f, 0), new Vector2(30, 20)),
                                                new CarGear("3rd", 8, new Vector2(29, 17.5f), new Vector2(50, 30)), new CarGear("4th", 6, new Vector2(46.5f, 29), new Vector2(80, 50)),
                                                new CarGear("5th", 4, new Vector2(64, 46.5f), new Vector2(110, 80)), new CarGear("6th", 2, new Vector2(90, 64), new Vector2(156, 110))};

        [Header("Car Visuals")]
        [Tooltip("The car model")]
        public GameObject model;
        [Tooltip("The car model effects")]
        public GameObject modelEffects;
        [Tooltip("The collision fitted to the car model")]
        public GameObject modelCollision;
        [Tooltip("The display name of the car")]
        public string displayName;
        [Tooltip("The icon of the car")]
        public Sprite icon;

        [Header("Car Transforms")]
        [Tooltip("The local position of the rb center of mass transform")]
        public Vector3 transformRBCenterOfMass = new Vector3(0f, -0.340000004f, 0f);
        [Tooltip("The local position where the accelerating and breaking is applied to the rb")]
        public Vector3 transformAcceleratingBreaking = new Vector3(0, -0.379999995f, 0.549000025f);

        [Header("Drift Collider")]
        [Tooltip("The Physics Material of the car drift box collider")]
        public PhysicMaterial driftColliderPhyMat;
        [Tooltip("The local position center of the car drift box collider")]
        public Vector3 driftColliderCenter = new Vector3(0f, 0.105031386f, 0f);
        [Tooltip("The size of the car drift box collider")]
        public Vector3 driftColliderSize = new Vector3(1.5f, 0.789937258f, 3.8499999f);

        [Header("Car Audio")]
        [Tooltip("The FMOD event name used for this car")]
        public string fmodEventName = "event:/Car/Tyres Rolling Test";

        [Header("Car Stats")]
        public CarStats stats;
    }
}