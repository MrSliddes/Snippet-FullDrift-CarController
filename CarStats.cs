using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FD.Car
{
    /// <summary>
    /// Contains the stats of the car //TODO make a system that does this automaticly based on CarSO
    /// </summary>
    [System.Serializable]
    public class CarStats
    {
        [Range(0, 1)]
        public float difficulty;
        [Range(0, 1)]
        public float driftControl;
        [Range(0, 1)]
        public float speed;
        [Range(0, 1)]
        public float maxSpeed;
        [Range(0, 1)]
        public float steering;
        [Range(0, 1)]
        public float acceleration;
        [Range(0, 1)]
        public float breaking;
        [Range(0, 1)]
        public float grip;
        // Weight
    }
}