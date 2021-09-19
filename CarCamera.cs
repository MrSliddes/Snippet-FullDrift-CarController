using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CarCamera : MonoBehaviour
{
    public static CarCamera Instance;

    /// <summary>
    /// The cinemachine camera for the car
    /// </summary>
    public CinemachineFreeLook cinemachineCarCamera;
    /// <summary>
    /// Particle effect that gets shown while drifting
    /// </summary>
    public ParticleSystem particleDriftEffect;

    private CinemachineBasicMultiChannelPerlin cmPerlin;

    private void Awake()
    {
        // Set
        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        // Get
        if(cinemachineCarCamera == null) cinemachineCarCamera = FindObjectOfType<CinemachineFreeLook>();
        if(cinemachineCarCamera == null) Debug.LogWarning("[CarCamera] CM Car Camera not found");
        cmPerlin = cinemachineCarCamera.GetRig(1).GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>(); // Only grab the middle rig //IMPROVE add for all rigs

        // Set
        particleDriftEffect.Stop();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// Shakes the camera
    /// </summary>
    /// <param name="intensity">How intensive the shake is</param>
    /// <param name="duration">How long the shake lasts</param>
    /// <remarks>If a shake is happening and another is called it is overridden</remarks>
    public static void Shake(Vector2 intensity, float duration)
    {
        Instance.StopCoroutine(Instance.ShakeAsync(Vector2.zero, 0));
        Instance.StartCoroutine(Instance.ShakeAsync(intensity, duration));
    }

    private IEnumerator ShakeAsync(Vector2 intensity, float duration)
    {
        // Set
        cmPerlin.m_AmplitudeGain = intensity.x;
        cmPerlin.m_FrequencyGain = intensity.y;
        yield return new WaitForSeconds(duration);
        // Reset
        cmPerlin.m_AmplitudeGain = 0;
        cmPerlin.m_FrequencyGain = 0;
        yield break;
    }
}
