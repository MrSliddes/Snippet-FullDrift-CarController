using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FD.Car
{
    /// <summary>
    /// The effects of the car model
    /// </summary>
    public class CarModelEffects : MonoBehaviour
    {
        [Header("Tire Marks")]
        /// <summary>
        /// The tire mark effect from a car wheel
        /// </summary>
        [Tooltip("0 = front left, 1 = front right, 2 = rear left, 3 = rear right")]
        public TrailRenderer[] tireMarks;
    }
}