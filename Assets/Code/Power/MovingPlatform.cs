using System;
using FMOD.Studio;
using UnityEngine;
using Random = UnityEngine.Random;

public class MovingPlatform : MonoBehaviour
{
    [Header("Cycle Settings")]
    [SerializeField] private float cycleDuration = 2f;
    [Range(0f, 1f)]
    [SerializeField] private float cycleOffset = 0f;
    [SerializeField] private float cycleSnapping = 0f;
    [SerializeField] private bool loop = true;

    [Header("Movement (Per Cycle Delta)")]
    [SerializeField] private Vector3 positionPerCycle = Vector3.zero;
    [SerializeField] private Vector3 rotationPerCycle = Vector3.zero;

    private float transitionTime = 0f;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private PowerConnector _powerConnector;

    private static string eventPath = "event:/StoneGrind";
    private EventInstance _eventInstance;

    public bool Mute = false;

    // --- Added ---
    private float _previousCycleValue;
    private float _previousFrameTime;
    // -------------

    public static event Action<Transform, Vector3, Quaternion> OnMoved;

    public void JumpToEnd()
    {
        transitionTime = cycleDuration;
    }

    private void Start()
    {
        _eventInstance =
            FMODUnity.RuntimeManager.CreateInstance(FMODUnity.RuntimeManager.PathToEventReference(eventPath));
        FMODUnity.RuntimeManager.AttachInstanceToGameObject(_eventInstance, gameObject);
        _eventInstance.setTimelinePosition(Random.Range(0, 5000));
        _eventInstance.start();

        startPosition = transform.localPosition;
        startRotation = transform.localRotation;
        _powerConnector = GetComponentInChildren<PowerConnector>();

        _previousFrameTime = Time.time;
    }

    private void OnDisable()
    {
        _eventInstance.stop(STOP_MODE.ALLOWFADEOUT);
    }

    private void Update()
    {
        if (HitstopManager.Singleton.IsHitstopActive()) return;
        if (cycleDuration <= 0f) return;

        var startPosition = transform.position;
        var startRotation = transform.rotation;

        if (_powerConnector is null)
        {
            if (loop)
            {
                float time = Time.time + cycleOffset * cycleDuration;
                float cycles = time / cycleDuration;

                ApplyContinuousMotion(cycles);
                UpdateStoneGrindParameter(cycles);
            }
            else
            {
                float time = Time.time + cycleOffset * cycleDuration;
                float normalizedTime = (time % cycleDuration) / cycleDuration;
                float sine = Mathf.Sin(normalizedTime * Mathf.PI * 2f);
                float t = (sine * 0.5f) + 0.5f;

                ApplyLerpedMotion(t);
                UpdateStoneGrindParameter(t);
            }
        }
        else
        {
            var isForward = _powerConnector.IsPowered();

            transitionTime += (isForward ? 1f : -1f) * Time.deltaTime;
            transitionTime = Mathf.Clamp(transitionTime, 0f, cycleDuration);

            float t = transitionTime / cycleDuration;
            ApplyLerpedMotion(t);
            UpdateStoneGrindParameter(t);
        }

        var positionDelta = transform.position - startPosition;
        var rotationDelta = transform.rotation * Quaternion.Inverse(startRotation);
        OnMoved?.Invoke(transform, positionDelta, rotationDelta);
    }

    // --- Added ---
    private void UpdateStoneGrindParameter(float currentValue)
    {
        if (Mute)
        {
            _eventInstance.setParameterByName("StoneGrindAmount", 0);
            return;
        }
        
        currentValue = SharpSymmetric(currentValue, cycleSnapping);
        float deltaTime = Time.time - _previousFrameTime;
        if (deltaTime <= 0f) return;

        float rate = Mathf.Abs(currentValue - _previousCycleValue) / deltaTime;

        // Normalize rate so 1 full cycle per second = 1.0
        float normalizedRate = Mathf.Clamp01(rate * cycleDuration);

        _eventInstance.setParameterByName("StoneGrindAmount", normalizedRate);

        _previousCycleValue = currentValue;
        _previousFrameTime = Time.time;
    }
    // -------------

    private void ApplyContinuousMotion(float cycles)
    {
        Vector3 targetPos = positionPerCycle * cycles;
        Quaternion targetRot = Quaternion.Euler(rotationPerCycle * cycles);

        transform.localPosition = startPosition + targetPos;
        transform.localRotation = startRotation * targetRot;
    }

    private void ApplyLerpedMotion(float t)
    {
        float shapedT = SharpSymmetric(t, cycleSnapping);

        Vector3 targetPos = Vector3.Lerp(Vector3.zero, positionPerCycle, shapedT);
        Quaternion targetRot = Quaternion.Lerp(
            Quaternion.identity,
            Quaternion.Euler(rotationPerCycle),
            shapedT
        );

        transform.localPosition = startPosition + targetPos;
        transform.localRotation = startRotation * targetRot;
    }

    private float SharpSymmetric(float t, float snapping)
    {
        if (snapping <= 0f) return t;

        float k = Mathf.Lerp(0.1f, 60f, snapping);
        float x = (t - 0.5f) * 2f;
        float y = 1f / (1f + Mathf.Exp(-k * x));

        float min = 1f / (1f + Mathf.Exp(k));
        float max = 1f / (1f + Mathf.Exp(-k));

        return Mathf.InverseLerp(min, max, y);
    }
}