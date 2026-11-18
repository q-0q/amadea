using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using DG.Tweening;
using FMOD.Studio;
using UnityEngine;
using Util = Code.Misc.Util;

public class BellDoorController : MonoBehaviour
{

    private Animator _animator;
    private CinemachineVirtualCamera _passiveCamera;
    public static event Action OnPlayerNearbyUnopenedBellDoor;
    public string persistentEvent;
    
    private float _playerLookAwayTimer;
    private const float LookawayTime = 0.75f;

    private List<GameObject> _lightObjects;

    private const float lightPlacementRadius = 15f;
    private const float lightPlacementSeparationAngle = 25f;

    private const string _readyEventPath = "event:/BellDoorReady";
    private const string _openEventPath = "event:/BellDoorOpen";
    private EventInstance _readyInstance;
    private Interactable _interactable;
    private Collider _collider;

    public bool DontWritePersistentEvent = false;

    private CinemachineVirtualCamera _openCamera;

    private void Awake()
    {
        _playerLookAwayTimer = 0f;
        _animator = GetComponentInChildren<Animator>();
        _passiveCamera = transform.Find("BellDoorPassiveCamera").GetComponentInChildren<CinemachineVirtualCamera>();
        _openCamera = transform.Find("OpenCamera").Find("BellDoorOpenCamera").GetComponentInChildren<CinemachineVirtualCamera>();
        _interactable = GetComponentInChildren<Interactable>();
        _interactable.SetEnabled(false);
        TryGetComponent(out _collider);

        
        var lightPrefab = Resources.Load("Prefab/BellDoorLight") as GameObject;
        var lightHolder = transform.Find("LightHolder");
        _lightObjects = new List<GameObject>();

        var bellRequirement = 3;
        for (int i = 0; i < bellRequirement; i++)
        {
            var angle = lightPlacementSeparationAngle * (i - (bellRequirement - 1) * 0.5f);
            var offset = Quaternion.Euler(angle, 0f, 0f) * Vector3.up * lightPlacementRadius;
            var lightObject = Instantiate(lightPrefab, lightHolder);
            lightObject.transform.SetLocalPositionAndRotation(offset, Quaternion.identity);
            _lightObjects.Add(lightObject);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        if (SaveSystem.GetPersistentEventCompleted(persistentEvent))
        {
            _readyInstance.stop(STOP_MODE.ALLOWFADEOUT);
            for (int i = 0; i < _lightObjects.Count; i++)
            {
                Util.InvokeSphereEffect(_lightObjects[i].transform.position - Vector3.up, Vector3.one * 8f, 1.25f, 1f, -3f);
                _lightObjects[i].transform.Find("Mesh").Find("Tetra").GetComponent<Renderer>().material.SetFloat("_Weight", 1);
                _lightObjects[i].transform.Find("Mesh").Find("Halo").GetComponent<Renderer>().material.SetFloat("_Weight", 0);
            }
        
            _interactable.SetEnabled(false);
            Util.ReplaceAnimatorTrigger(_animator, "Opened");
            _collider.enabled = false;
        }

        else
        {
            UpdatePlayerBellStatus();
        }

    }

    private void UpdatePlayerBellStatus()
    {
        
        var playerBellCount = 0;
        if (SaveSystem.GetPersistentEventCompleted(BellController.FirstBellEvent)) playerBellCount++;
        if (SaveSystem.GetPersistentEventCompleted(BellController.SecondBellEvent)) playerBellCount++;
        if (SaveSystem.GetPersistentEventCompleted(BellController.ThirdBellEvent)) playerBellCount++;
        
        for (int i = 0; i < _lightObjects.Count; i++)
        {
            var weight = i < playerBellCount ? 1f : 0f;
            _lightObjects[i].transform.Find("Mesh").Find("Tetra").GetComponent<Renderer>().material.SetFloat("_Weight", weight);
            _lightObjects[i].transform.Find("Mesh").Find("Halo").GetComponent<Renderer>().material.SetFloat("_Weight", weight);
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Vector3.Angle(PlayerFsm.Singleton.transform.forward, transform.forward) < 90f)
        {
            _playerLookAwayTimer += Time.deltaTime;
        }
        else
        {
            _playerLookAwayTimer = 0;
        }
        
        if (_passiveCamera.Priority > 0) OnPlayerNearbyUnopenedBellDoor?.Invoke();
    }

    private void OnTriggerStay(Collider other)
    {
        _passiveCamera.Priority = _playerLookAwayTimer >= LookawayTime ? -20 : 20;
    }

    private void OnTriggerExit(Collider other)
    {
        _passiveCamera.Priority = -20;
    }

    private void OnInteracted()
    {
        for (int i = 0; i < _lightObjects.Count; i++)
        {
            _lightObjects[i].transform.Find("Mesh").Find("Halo").GetComponent<Renderer>().material.SetFloat("_Weight", 0);
            Util.InvokeSphereEffect(_lightObjects[i].transform.position - Vector3.up, Vector3.one * 8f, 1.25f, 1f, -3f);
        }
        
        _interactable.SetEnabled(false);
        
        if (!DontWritePersistentEvent) SaveSystem.WritePersistentEvent(persistentEvent);

        StartCoroutine(Coroutine());
        IEnumerator Coroutine()
        {
            _openCamera.Priority = 30;
            CutsceneManager.Singleton.SetPseudoCutsceneActive();
            yield return new WaitForSeconds(0.25f);
            FMODUnity.RuntimeManager.PlayOneShotAttached(FMODUnity.RuntimeManager.PathToEventReference(_openEventPath), gameObject);
            yield return new WaitForSeconds(0.25f);
            var vibrator = transform.Find("bell-door").Find("BellDoorArmature");
            vibrator.DOShakePosition(0.75f, 0.2f, 25);
            Util.ReplaceAnimatorTrigger(_animator, "Open");
            yield return new WaitForSeconds(1.0f);
            _readyInstance.stop(STOP_MODE.ALLOWFADEOUT);
            var dolly = _openCamera.GetCinemachineComponent<CinemachineTrackedDolly>();
            var t = 0f;
            var d = 3f;
            while (t < d)
            {
                dolly.m_PathPosition = Util.SmoothLerp01(t / d);
                t += Time.deltaTime;
                yield return null;
            }

            _collider.enabled = false;
            _passiveCamera.Priority = -20;
            _openCamera.Priority = -20;
            CutsceneManager.Singleton.ClearPseudoCutsceneActive();
        }
    }

    private void OnEnable()
    {
        BellController.OnBellRing += UpdatePlayerBellStatus;
        _readyInstance = FMODUnity.RuntimeManager.CreateInstance(FMODUnity.RuntimeManager.PathToEventReference(_readyEventPath));
        FMODUnity.RuntimeManager.AttachInstanceToGameObject(_readyInstance, _interactable.gameObject);
        _interactable.OnInteracted += OnInteracted;

    }
    
    private void OnDisable()
    {
        BellController.OnBellRing -= UpdatePlayerBellStatus;
        _interactable.OnInteracted -= OnInteracted;
        _readyInstance.stop(STOP_MODE.ALLOWFADEOUT);
    }
}
