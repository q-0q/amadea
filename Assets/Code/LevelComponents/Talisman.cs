using System;
using System.Collections;
using FMOD.Studio;
using UnityEngine;
using Util = Code.Misc.Util;

public class Talisman : MonoBehaviour
{

    public string metaName;
    public Vector3 destination;
    public BellController bell;
    public Vector3 outwardsPoint;
    public GameObject glow;
    private Material _glowMaterial;
    private TriggerProxy _triggerProxy;
    public float rotationOffset;
    public static event Action<BellController> OnTalismanCollected;
    private CustomPointLight _light;

    private bool _collected;
    
    public void MoveToDestination()
    {
        StartCoroutine(Coroutine());
        IEnumerator Coroutine()
        {
            var t = 0f;
            var d = 2.25f;
            var startPosition = transform.position;
            while (t < d)
            {
                var w = Util.SmoothLerp01(t / d);
                var linearPosition = Vector3.Lerp(startPosition, destination, w);
                var outWardPosition =
                    Vector3.Lerp(startPosition, startPosition + (startPosition - outwardsPoint).normalized * 40f, w);
                
                transform.position = Vector3.Lerp(outWardPosition, linearPosition, Mathf.Pow(w, 1f));
                t += Time.deltaTime;
                yield return null;
            }

            MakeCollectible();
        }
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!_collected) return;
        OrbitAroundAxis( PlayerFsm.Singleton.transform.position, PlayerFsm.Singleton.GetUpVector(), 3f, 10f, 100f);
    }

// Add this field to your MonoBehaviour class to track the rotation continuously
    private float currentAngle = 0f;

    public void OrbitAroundAxis(Vector3 center, Vector3 axis, float radius, float lerpSpeed, float orbitSpeed)
    {
        // 1. Accumulate angle using deltaTime and the current speed.
        // Changing orbitSpeed now smoothly scales the rotation rate without snapping/teleporting.
        currentAngle += orbitSpeed * Time.deltaTime;

        // 2. Combine accumulated angle with your rotation offset
        float angle = currentAngle + (360f * rotationOffset);
    
        Vector3 baseDir = Vector3.ProjectOnPlane(Vector3.forward, axis);
        if (baseDir.sqrMagnitude < 0.001f)
        {
            // Fallback if Vector3.forward happens to be parallel to 'up'
            baseDir = Vector3.ProjectOnPlane(Vector3.right, axis);
        }
        baseDir.Normalize();
    
        Vector3 offset = Quaternion.AngleAxis(angle, axis) * (baseDir * radius);
        Vector3 targetPos = center + (axis * 1.5f) + offset;

        var newPos = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * lerpSpeed);

        transform.up = (newPos - transform.position).normalized;
        transform.position = newPos;
    }

    private void Awake()
    {
        _glowMaterial = glow.GetComponent<Renderer>().material;
        _triggerProxy = GetComponentInChildren<TriggerProxy>();
        _collected = false;
        _light = GetComponentInChildren<CustomPointLight>();
    }

    private void OnEnable()
    {
        BellController.OnTalismanMove += OnTalismanMove;
        BellController.OnTalismanRing += OnTalismanRing;
        BellController.OnTalismanRingComplete += OnTalismanRingComplete;
        _triggerProxy.OnTriggerProxyStay += OnTrigger;
    }

    private void OnTalismanRingComplete(BellController obj)
    {
        if (obj != bell) return;
        Util.InvokeSphereEffect(transform.position - Vector3.up, Vector3.one * 2.5f, 1.25f, 1.25f, -0.5f);
        Destroy(gameObject);
    }


    private void OnTrigger(Collider obj)
    {
        SaveSystem.WritePersistentEvent(GetPersistentEvent());
        Util.InvokeSphereEffect(transform.position - Vector3.up, Vector3.one * 6f, 1.25f, 0.8f, -0.5f);
        MakeCollected();
    }

    private void OnDisable()
    {
        BellController.OnTalismanMove -= OnTalismanMove;
        _triggerProxy.OnTriggerProxyStay -= OnTrigger;
        BellController.OnTalismanRing -= OnTalismanRing;
        BellController.OnTalismanRingComplete -= OnTalismanRingComplete;
    }

    private void OnTalismanMove(BellController obj)
    {
        if (obj != bell) return;
        MoveToDestination();
    }

    public void MakeCollectible()
    {
        _glowMaterial.SetFloat("_Alpha", 1f);

        transform.rotation = Quaternion.identity;
        transform.position = destination;
    }

    public void MakeCollected()
    {
        _collected = true;
        _triggerProxy.gameObject.SetActive(false);
        glow.SetActive(false);
        _light.gameObject.SetActive(false);
        currentAngle = (360f * rotationOffset) + (100f * Time.time);
        OnTalismanCollected?.Invoke(bell);
    }
    
    public void StartAlphaCoroutine()
    {
        StartCoroutine(Coroutine());
        IEnumerator Coroutine()
        {
            var t = 0f;
            var d = 1.5f;
            while (t < d)
            {
                _glowMaterial.SetFloat("_Alpha", (1f - t / d) * 0.75f);
                t += Time.deltaTime;
                yield return null;
            }
            
        }
    }

    public string GetPersistentEvent()
    {
        return "talisman-" + bell.metaName + "-" + metaName;
    }
    
    private void OnTalismanRing(BellController b, Vector3 center, Vector3 axis, float lerpSpeed, float oribitSpeed, float radius)
    {
        if (b != bell) return;

        _collected = false;
        OrbitAroundAxis(center, axis, radius, lerpSpeed, oribitSpeed);
    }
}
