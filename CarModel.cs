using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FD.Car
{
    /// <summary>
    /// Contains the transforms of a prefab Car Model "Carname"
    /// </summary>
    public class CarModel : MonoBehaviour
    {
        [Tooltip("0 = front left, 1 = front right, 2 = rear left, 3 = rear right")]
        public Transform[] wheelTransforms;
        [Tooltip("The parent transform of the front 2 wheels (for rotating them)")]
        public Transform[] wheelParentTransforms;
    }
}