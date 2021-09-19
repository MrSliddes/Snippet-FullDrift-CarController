using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FD.Car
{
    /// <summary>
    /// Makes car look fancy
    /// </summary>
    [RequireComponent(typeof(CarControllerV3))]
    public class CarEffectsV3 : MonoBehaviour //TODO this script doesnt need monobehaviour anymore, add it to CarControllerV3
    {
        [Header("Tire Marks")]
        /// <summary>
        /// The tire mark effect from a car wheel
        /// </summary>
        [Tooltip("0 = front left, 1 = front right, 2 = rear left, 3 = rear right")]
        [HideInInspector] public TrailRenderer[] tireMarks; //IMPROVE clean this op with connection to carSO / carmodeleffects

        [Header("Wheel Transforms")]
        [Tooltip("Max length of suspension, should be smaller than car wheel radius")]
        [HideInInspector] public float maxSuspensionDistance = 0.2f;
        [Tooltip("0 = front left, 1 = front right, 2 = rear left, 3 = rear right")]
        [HideInInspector] public Transform[] wheelTransforms;
        [Tooltip("The parent transform of the front 2 wheels (for rotating them)")]
        [HideInInspector] public Transform[] wheelParentTransforms;

        /// <summary>
        /// Current rotation of the wheel
        /// </summary>
        private float currentWheelRotation;
        /// <summary>
        /// The linked car controller
        /// </summary>
        private CarControllerV3 car;
        private CarCamera carCamera;

        // Start is called before the first frame update
        void Start()
        {
            // Get
            car = GetComponent<CarControllerV3>();
            carCamera = CarCamera.Instance; if(carCamera == null) Debug.LogWarning("[CarEffectsV3] no car camera found");

            // Set
            for(int i = 0; i < tireMarks.Length; i++)
            {
                tireMarks[i].emitting = false;
            }
            car.OnCarCrash += CarCrashEffects;
        }

        // Update is called once per frame
        void Update()
        {
            UpdateDriftEffects();
            UpdateTireMarks();
            UpdateWheels();
        }

        /// <summary>
        /// Called when the car crashes
        /// </summary>
        /// <param name="dir">The direction of the crash</param>
        /// <param name="kmh">The speed of the car at the crash</param>
        private void CarCrashEffects(int dir, int kmh)
        {
            if(dir == 0 || dir == 2)
            {
                // Front or back crash display camera shake
                // Above min speed
                if(kmh > 15)
                {
                    float s = MathC.Map(kmh, 0, 200, 1, 2);
                    float d = MathC.Map(kmh, 15, 200, 0.5f, 1.5f);
                    CarCamera.Shake(new Vector2(s, s), d);
                }
            }
        }

        private void UpdateDriftEffects()
        {
            if(carCamera == null) return;

            if(car.IsDrifting)
            {
                carCamera.particleDriftEffect.Play();
            }
            else
            {
                carCamera.particleDriftEffect.Stop();
            }
        }

        private void UpdateTireMarks()
        {
            if(car.IsDrifting && car.IsGrounded)
            {
                tireMarks[2].emitting = true;
                tireMarks[3].emitting = true;
            }
            else
            {
                tireMarks[2].emitting = false;
                tireMarks[3].emitting = false;
            }
        }

        private void UpdateWheels()
        {
            // Update wheel suspension position
            for(int i = 0; i < wheelParentTransforms.Length; i++) // This can be optimized
            {
                RaycastHit hit;
                if(Physics.Raycast(wheelParentTransforms[i].position, Vector3.down, out hit, maxSuspensionDistance)) { } else hit.point = wheelParentTransforms[i].position - new Vector3(0, maxSuspensionDistance, 0);
                Vector3 loc = wheelTransforms[i].TransformPoint(Vector3.zero);
                wheelTransforms[i].position = new Vector3(loc.x, hit.point.y + car.WheelRadius * 0.5f, loc.z);
                float y = wheelTransforms[i].localPosition.y > 0 ? 0 : wheelTransforms[i].localPosition.y; // Limit y
                wheelTransforms[i].localPosition = new Vector3(0, y, 0);
            }

            // Rotate wheels x axis based on car speed
            int dirZ = car.RigidbodyDirection.z > 0 ? 1 : -1;
            for(int i = 0; i < wheelTransforms.Length; i++)
            {
                wheelTransforms[i].Rotate(Vector3.right, car.CurrentRPMFromKMH * 6f * dirZ * Time.deltaTime);
            }

            // Rotate front wheels y axis
            float yAxis = 0;
            if(car.carInput.Horizontal > 0)
            {
                yAxis = 45;
            }
            else if(car.carInput.Horizontal < 0)
            {
                yAxis = -45;
            }

            if(car.IsDrifting) yAxis = car.DriftDirection > 0 ? -45 : 45;
            float refV = 0;
            currentWheelRotation = Mathf.SmoothDamp(currentWheelRotation, yAxis, ref refV, 5 * Time.deltaTime);

            for(int i = 0; i < 2; i++)
            {
                wheelParentTransforms[i].localRotation = Quaternion.Euler(0, currentWheelRotation, 0);
            }

        }


#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if(!Application.isPlaying) return; // needs to be sepperate
            if(!car.debug) return;

            // Draw wheel hit lines
            for(int i = 0; i < wheelParentTransforms.Length; i++)
            {
                RaycastHit hit;
                if(Physics.Raycast(wheelParentTransforms[i].position, Vector3.down, out hit, maxSuspensionDistance)) { } else hit.point = wheelParentTransforms[i].position - new Vector3(0, maxSuspensionDistance, 0);
                float y = -hit.distance;
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(wheelParentTransforms[i].position, hit.point);
                UnityEditor.Handles.DrawWireDisc(hit.point + new Vector3(0, car.WheelRadius * 0.5f, 0), car.transform.right, car.WheelRadius * 0.5f);
            }
        }
#endif
    }
}
