using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Code.Misc;
using DG.Tweening;
using FMODUnity;
using UnityEngine;
using UnityEngine.Serialization;

public class BellController : MonoBehaviour
{
    public string metaName;
    public bool isDream = false;
    private Interactable _interactable;
    private Animator _animator;
    public Transform bellMesh;
    public ParticleSystem stoneParticles;
    public GameObject lockObject;
    public Transform talismanSpawnRotator;

    public List<Renderer> bellMaterialRenderers;
    public CustomPointLight _readyLight;
    private Material _haloMaterial;
    private CinemachineVirtualCamera _virtualCamera;
    private Transform _sideCameraStart;
    private Transform _sideCameraEnd;
    private Transform _frontCameraStart;
    private Transform _frontCameraEnd;
    
    private Transform _ringCameraStart;
    private Transform _ringCameraEnd;
    
    
    public CustomPointLight light;
    private Color _lightColor;
    public Transform armHolder;
    private Vector3 _startPosition;
    private bool _subroutineFinished;
    public static event Action OnBellRing;
    private Material _lockGlowMaterial;
    
    public static event Action OnPlayerNearbyRungBell;

    public static event Action<BellController> OnTalismanMove; 
    public static event Action<BellController, Vector3, Vector3, float, float, float> OnTalismanRing; 
    public static event Action<BellController> OnTalismanRingComplete; 
    
    public const string StonePersistentEventSuffix = "-bell-stone";
    public const string CompleteEventSuffix = "-bell-complete";

    private List<Transform> _talismanPositions;
    
    public EventReference _ringEventReference;


    private CanvasGroup _canvasGroup;
    private List<TalismanCanvasItem> _talismanCanvasItems;
    private int collectedTalismanCount;
    

    private enum BellState
    {
        Init,
        Stone,
        Ready,
        Complete,
    }

    private void Awake()
    {
        _interactable = GetComponentInChildren<Interactable>();
        _haloMaterial = transform.Find("Halo").GetComponent<Renderer>().material;
        armHolder.gameObject.SetActive(false);
        _interactable.SetEnabled(true);
        _lightColor = light.Color;
        _canvasGroup = GetComponentInChildren<CanvasGroup>();
        collectedTalismanCount = 0;
        _readyLight.gameObject.SetActive(false);
        _lockGlowMaterial = lockObject.transform.Find("Halo").GetComponent<Renderer>().material;
        _lockGlowMaterial.SetFloat("_Alpha", 0);

    }

    // Start is called before the first frame update
    void Start()
    {
        _startPosition = transform.position;
        _animator = GetComponentInChildren<Animator>();
        _virtualCamera = GetComponentInChildren<CinemachineVirtualCamera>();
        Util.ReplaceAnimatorTrigger(_animator, "Idle");
        _virtualCamera.transform.SetParent(null);
        _sideCameraStart = transform.Find("SideCameraStart");
        _sideCameraEnd = transform.Find("SideCameraEnd");
        
        _frontCameraStart = transform.Find("FrontCameraStart");
        _frontCameraEnd = transform.Find("FrontCameraEnd");
        
        _ringCameraStart = transform.Find("RingCameraStart");
        _ringCameraEnd = transform.Find("RingCameraEnd");

        _talismanPositions = new List<Transform>();
        _talismanCanvasItems = new List<TalismanCanvasItem>();
        var talismanCanvasItemPrefab = Resources.Load("Prefab/TalismanCanvasItem") as GameObject;
        var holder = _canvasGroup.transform.Find("Holder");
        
        for (int i = 0; i < transform.Find("TalismanPositions").childCount; i++)
        {
            _talismanPositions.Add(transform.Find("TalismanPositions").GetChild(i));
            var talismanCanvasItemObj = Instantiate(talismanCanvasItemPrefab, holder);
            _talismanCanvasItems.Add(talismanCanvasItemObj.GetComponent<TalismanCanvasItem>());
        }
        
        

        if (SaveSystem.GetPersistentEventCompleted(metaName + CompleteEventSuffix))
        {
            // complete
            
        } else if (SaveSystem.GetPersistentEventCompleted(metaName + StonePersistentEventSuffix))
        {
            // stone

            _canvasGroup.alpha = 0f;
            _interactable.SetEnabled(false);
            var talismanPrefab = Resources.Load("Prefab/Talisman") as GameObject;
            light.Color = Color.black;
            int i = 0;
            foreach (var talismanPosition in _talismanPositions)
            {

                var obj = Instantiate(talismanPrefab, talismanPosition.position, Quaternion.identity);
                var talisman = obj.GetComponent<Talisman>();
                talisman.metaName = talismanPosition.name;
                talisman.destination = talismanPosition.position;
                talisman.bell = this;
                talisman.rotationOffset = (float)i / _talismanPositions.Count;
                i++;
                
                if (SaveSystem.GetPersistentEventCompleted(talisman.GetPersistentEvent()))
                {
                    talisman.MakeCollected();
                }
                else
                {
                    talisman.MakeCollectible();
                }
            }
            
            
            SetShaderWeights(0f, 1f, 1f, 0f);
        }
        else
        {
            // init
            _canvasGroup.alpha = 0f;
            lockObject.SetActive(false);
            SetShaderWeights(0f, 0.15f, 0f, 1f);
        }


    }

    // Update is called once per frame
    void Update()
    {
        // transform.position = _startPosition + Vector3.up * (Mathf.Sin(Time.time * 1f) * 0.4f);

        foreach (var renderer in bellMaterialRenderers)
        {
            renderer.material.SetVector("_LockPosition", lockObject.transform.position);
        }
    }

    private void OnEnable()
    {
        _interactable.OnInteracted += OnInteracted;
        Talisman.OnTalismanCollected += OnTalismanCollected;
    }

    

    private void OnDisable()
    {
        _interactable.OnInteracted -= OnInteracted;
        Talisman.OnTalismanCollected -= OnTalismanCollected;
    }

    private void OnInteracted()
    {
        
        
        if (SaveSystem.GetPersistentEventCompleted(metaName + StonePersistentEventSuffix))
            StartCoroutine(RingCoroutine());
        
        else StartCoroutine(StoneCoroutine());

        IEnumerator RingCoroutine()
        {
            StartCoroutine(RingCameraCoroutine());
            var t = 0f;
            var d = 4f;
            while (t < d)
            {
                var lerpSpeed = Mathf.Lerp(0.5f, 10f, t / d);
                var oribitSpeed = Mathf.Lerp(50f, 200f, Mathf.InverseLerp(0.5f, 0.75f, t / d));
                oribitSpeed = Mathf.Lerp(200f, 50f, Mathf.InverseLerp(0.75f, 1f, t / d));
                var radius = Mathf.Lerp(7f, 2.75f, Mathf.InverseLerp(0.5f, 1f, t / d));
                OnTalismanRing?.Invoke(this, lockObject.transform.position + (transform.forward * 2f), transform.forward, lerpSpeed, oribitSpeed, radius);
                t += Time.deltaTime;
                yield return null;
            }
            
            lockObject.transform.DOComplete();
            lockObject.transform.DOPunchPosition(-transform.up * 0.002f, 0.5f, 20);
            OnTalismanRingComplete?.Invoke(this);
            

            t = 0;
            d = 3f;
            while (t < d)
            {
                SetBellMaterialFloat("_StoneWeight", Mathf.Lerp(1f, 0.6f, Mathf.InverseLerp(1f,2.75f, t)));
                t += Time.deltaTime;
                yield return null;
            }
            
            stoneParticles.Play();
            _animator.transform.parent.DOComplete();
            _animator.transform.parent.DOShakePosition(3f, 0.2f, 5);
            lockObject.SetActive(false);
            SetBellMaterialFloat("_StoneWeight", 0f);
            
            yield return new WaitForSeconds(1.5f);
            
            Util.ReplaceAnimatorTrigger(_animator, "Ring");
            
            yield return new WaitForSeconds(3.75f);
            
            _animator.transform.parent.DOComplete();
            _animator.transform.parent.DOShakePosition(2.5f, 0.15f, 25);
            
            
            yield break;
        }
        
        IEnumerator StoneCoroutine()
        {
            StartCoroutine(StoneCameraCoroutine());
            SetBellMaterialFloat("_StoneWeight", 0);
            light.Color = _lightColor;
            armHolder.gameObject.SetActive(false);
            lockObject.SetActive(false);
            Util.ReplaceAnimatorTrigger(_animator, "Stone");
            _interactable.SetEnabled(false);
            
            SaveSystem.WritePersistentEvent(metaName + StonePersistentEventSuffix);
            
            var t = 0f;
            var d = 3.5f;
            
            while (t < d)
            {
                var value = Mathf.Lerp(0, 0.45f, Util.SmoothLerp01(t/d));
                light.Color = Color.Lerp(_lightColor, Color.black, value);
                SetBellMaterialFloat("_StoneWeight", value);
                _haloMaterial.SetFloat("_Weight", 1f - value);
                
                t += Time.deltaTime;
                yield return null;
            }
            
            t = 0f;
            d = 0.5f;


            StartCoroutine(StoneSubroutine());
            
            while (t < d)
            {
                var value = Mathf.Lerp(0.45f, 1f, Util.SmoothLerp01(t/d));
                light.Color = Color.Lerp(_lightColor, Color.black, value);
                SetBellMaterialFloat("_StoneWeight", value);
                _haloMaterial.SetFloat("_Weight", 1f - value);
                
                t += Time.deltaTime;
                yield return null;
            }
            


            

        }
        
        IEnumerator StoneCameraCoroutine()
        {
            CutsceneManager.Singleton.SetPseudoCutsceneActive();
            _virtualCamera.transform.position = _sideCameraStart.position;
            _virtualCamera.transform.rotation = _sideCameraStart.rotation;
            _virtualCamera.Priority = 20;

            yield return new WaitForSeconds(0.5f);
            
            var t = 0f;
            var d = 3.5f;
            
            while (t < d)
            {
                var value = Util.SmoothLerp01(t / d);
                _virtualCamera.transform.position = Vector3.Lerp(_sideCameraStart.position, _sideCameraEnd.position, value);
                _virtualCamera.transform.rotation = Quaternion.Lerp(_sideCameraStart.rotation, _sideCameraEnd.rotation, value);
                t += Time.deltaTime;
                yield return null;
            }
            
            yield return new WaitForSeconds(1.5f);
            
            t = 0f;
            d = 1.5f;
            
            while (t < d)
            {
                var value = Util.SmoothLerp01(t / d);
                _virtualCamera.transform.position = Vector3.Lerp(_frontCameraStart.position, _frontCameraEnd.position, value);
                _virtualCamera.transform.rotation = Quaternion.Lerp(_frontCameraStart.rotation, _frontCameraEnd.rotation, value);
                t += Time.deltaTime;
                yield return null;
            }
            
            yield return new WaitForSeconds(3f);

            while (!_subroutineFinished) yield return null;
            
            
            CutsceneManager.Singleton.ClearPseudoCutsceneActive();
            _virtualCamera.transform.position = _sideCameraStart.position;
            _virtualCamera.transform.rotation = _sideCameraStart.rotation;
            _virtualCamera.Priority = -10;


            yield return new WaitForSeconds(1f);
            
            t = 0;
            d = 2f;
            while (t < d)
            {
                // _canvasGroup.alpha = t / d;
                t += Time.deltaTime;
                yield return null;
            }

            // _canvasGroup.alpha = 1f;
        }

        IEnumerator VibrateAfterDelay(float delay, float duration, float strength, int vibrato)
        {
            yield return new WaitForSeconds(delay);
            _animator.transform.parent.DOComplete();
            _animator.transform.parent.DOShakePosition(duration, strength, vibrato);
        }
        
        IEnumerator PlayParticlesAfterDelay(float delay, ParticleSystem particleSystem)
        {
            yield return new WaitForSeconds(delay);
            particleSystem.Play();
        }

        IEnumerator StoneSubroutine()
        {
            _subroutineFinished = false;
            yield return new WaitForSeconds(0.2f);
            stoneParticles.Play();
            _animator.transform.parent.DOComplete();
            _animator.transform.parent.DOShakePosition(3f, 0.2f, 5);
            lockObject.SetActive(true);
            _lockGlowMaterial.SetFloat("_Alpha", 1f);
            
            yield return new WaitForSeconds(3.25f);

            var talismanPrefab = Resources.Load("Prefab/Talisman") as GameObject;
            var talismanCount = _talismanPositions.Count;
            
            int spawnedCount = 0;
            float angleStep = 360f / talismanCount;
            float totalAngleRotated = 0f;
            float targetAngle = 360f;
            
            float elapsedTime = 0f;

            int i = 0;
            while (totalAngleRotated < targetAngle)
            {
                elapsedTime += Time.deltaTime;
                float currentSpeed = Mathf.Lerp(220f, 5f, Mathf.InverseLerp(0.7f, 1f, totalAngleRotated / targetAngle));
                
                // Calculate this frame's rotation angle increment
                float frameAngle = currentSpeed * Time.deltaTime;
                totalAngleRotated += frameAngle;
                
                // Apply rotation incrementally around the local up axis
                talismanSpawnRotator.Rotate(Vector3.up, frameAngle, Space.Self);
                
                // Spawn prefabs based on reaching equidistant angle thresholds ($0^\circ, 72^\circ, 144^\circ$, etc.)
                while (spawnedCount < talismanCount && totalAngleRotated >= spawnedCount * angleStep)
                {
                    var startPosition = talismanSpawnRotator.transform.position + (transform.up * 2.25f) +
                                        (transform.forward * 3.75f);
                    Util.InvokeSphereEffect(startPosition - Vector3.up, Vector3.one * 2.5f, 1.25f, 1.25f, -0.5f);
                    
                    var obj = Instantiate(talismanPrefab, startPosition, Quaternion.identity);
                    var talisman = obj.GetComponent<Talisman>();
                    talisman.metaName = _talismanPositions[spawnedCount].name;
                    talisman.destination = _talismanPositions[spawnedCount].position;
                    talisman.bell = this;
                    talisman.StartAlphaCoroutine();
                    talisman.outwardsPoint = talismanSpawnRotator.position;
                    talisman.rotationOffset = (float)i / _talismanPositions.Count;
                    i++;
                    obj.transform.parent = talismanSpawnRotator;
                    
                    
                    lockObject.transform.DOComplete();
                    lockObject.transform.DOShakePosition(0.5f, 0.0005f, 15);
                    
                    spawnedCount++;
                }
                
                yield return null;
            }
            
            var t = 0f;
            var d = 1.5f;
            while (t < d)
            {
                _lockGlowMaterial.SetFloat("_Alpha", 1f - (t / d));
                t += Time.deltaTime;
                yield return null;
            }
            
            _lockGlowMaterial.SetFloat("_Alpha", 0f);
            
            OnTalismanMove?.Invoke(this);

            yield return new WaitForSeconds(1.5f);
            
            _subroutineFinished = true;
        }
        
        IEnumerator RingCameraCoroutine()
        {
            CutsceneManager.Singleton.SetPseudoCutsceneActive();
            _virtualCamera.transform.position = _sideCameraStart.position;
            _virtualCamera.transform.rotation = _sideCameraStart.rotation;
            _virtualCamera.Priority = 20;

            yield return new WaitForSeconds(0.5f);
            
            var t = 0f;
            var d = 1.5f;
            
            while (t < d)
            {
                var value = Util.SmoothLerp01(t / d);
                _virtualCamera.transform.position = Vector3.Lerp(_sideCameraStart.position, _sideCameraEnd.position, value);
                _virtualCamera.transform.rotation = Quaternion.Lerp(_sideCameraStart.rotation, _sideCameraEnd.rotation, value);
                t += Time.deltaTime;
                yield return null;
            }
            
            yield return new WaitForSeconds(1f);
            
            t = 0f;
            d = 1.5f;
            
            _virtualCamera.transform.position = Vector3.Lerp(_frontCameraStart.position, _frontCameraEnd.position, 1f);
            _virtualCamera.transform.rotation = Quaternion.Lerp(_frontCameraStart.rotation, _frontCameraEnd.rotation, 1f);
            
            // while (t < d)
            // {
            //     var value = Util.SmoothLerp01(t / d);
            //     _virtualCamera.transform.position = Vector3.Lerp(_frontCameraStart.position, _frontCameraEnd.position, value);
            //     _virtualCamera.transform.rotation = Quaternion.Lerp(_frontCameraStart.rotation, _frontCameraEnd.rotation, value);
            //     t += Time.deltaTime;
            //     yield return null;
            // }
            
            yield return new WaitForSeconds(2f);
            
            t = 0f;
            d = 3.5f;
            
            while (t < d)
            {
                var value = (1f - Util.SmoothLerp01(t / d)) * 0.5f;
                _virtualCamera.transform.position = Vector3.Lerp(_sideCameraStart.position, _sideCameraEnd.position, value);
                _virtualCamera.transform.rotation = Quaternion.Lerp(_sideCameraStart.rotation, _sideCameraEnd.rotation, value);
                t += Time.deltaTime;
                yield return null;
            }


            
            // while (!_subroutineFinished) yield return null;
            
            

            yield return new WaitForSeconds(3.5f);
            
            var startRotation = _virtualCamera.transform.rotation;
            var startPosition = _virtualCamera.transform.position;
            
            t = 0f;
            d = 4f;
            
            _virtualCamera.transform.position = Vector3.Lerp(startPosition, _ringCameraEnd.position, 1f);
            _virtualCamera.transform.rotation = Quaternion.Lerp(startRotation, _ringCameraEnd.rotation, 1f); 
            
            // while (t < d)
            // {
            //     var value = Util.SmoothLerp01(t / d);
            //     _virtualCamera.transform.position = Vector3.Lerp(startPosition, _ringCameraEnd.position, value);
            //     _virtualCamera.transform.rotation = Quaternion.Lerp(startRotation, _ringCameraEnd.rotation, value); 
            //         // Quaternion.LookRotation(Vector3.ProjectOnPlane(Vector3.Lerp(_ringCameraStart.forward, _ringCameraEnd.forward, value), transform.up));
            //     t += Time.deltaTime;
            //     yield return null;
            // }

            yield return new WaitForSeconds(5.5f);
            
            CutsceneManager.Singleton.ClearPseudoCutsceneActive();
            _virtualCamera.transform.position = _sideCameraStart.position;
            _virtualCamera.transform.rotation = _sideCameraStart.rotation;
            _virtualCamera.Priority = -10;
        }
        
    }


    private void SetShaderWeights(float bellGlow, float bellNoise, float bellStone, float halo)
    {
        SetBellMaterialFloat("_GlowWeight", bellGlow);
        SetBellMaterialFloat("_NoiseWeight", bellNoise);
        SetBellMaterialFloat("_StoneWeight", bellStone);
        _haloMaterial.SetFloat("_Weight", halo);
    }


    private void SetBellMaterialFloat(string id, float value)
    {
        foreach (var renderer in bellMaterialRenderers)
        {
            renderer.material.SetFloat(id, value);
        }
    }
    
    private void OnTalismanCollected(BellController bell)
    {
        
        if (bell != this) return;
        
        _talismanCanvasItems[collectedTalismanCount].MakeComplete();
        collectedTalismanCount++;

        if (collectedTalismanCount == _talismanPositions.Count)
        {
            _interactable.SetEnabled(true);
            _readyLight.gameObject.SetActive(true);
            _lockGlowMaterial.SetFloat("_Alpha", 1f);

        }
    }
    
    
    
    
}
