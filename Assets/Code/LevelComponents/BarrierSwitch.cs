using System;
using System.Collections;
using Code.TriggerParams;
using DG.Tweening;
using FMOD.Studio;
using UnityEngine;
using UnityEngine.Serialization;
using Util = Code.Misc.Util;

public class BarrierSwitch : MonoBehaviour
{
    private Interactable _interactable;
    private Animator _animator;
    public string metaName;
    public GameObject Curtain;
    private ParticleSystem _particleSystem;
    public GameObject vibratorA;
    public GameObject vibratorB;
    public GameObject vibratorC;
    private CustomPointLight _light;

    public float extraPullTime = 0f;

    public static event Action<string> OnBarrierSwitch;

    private EventInstance _ambientEventInstance;

    private void Awake()
    {
        _interactable = GetComponentInChildren<Interactable>();
        _particleSystem = GetComponentInChildren<ParticleSystem>();
        _animator = GetComponentInChildren<Animator>();
        _light = GetComponentInChildren<CustomPointLight>();
        Util.ReplaceAnimatorTrigger(_animator, "Down");

        _ambientEventInstance =
            FMODUnity.RuntimeManager.CreateInstance(
                FMODUnity.RuntimeManager.PathToEventReference("event:/MachineAmbienceA"));
        
        FMODUnity.RuntimeManager.AttachInstanceToGameObject(_ambientEventInstance, gameObject);



    }
    
    

    private void OnEnable()
    {
        _interactable.OnInteracted += OnInteracted;
        _interactable.OnHardInteracted += OnHardInteracted;
    }

    private void OnHardInteracted()
    {
        Util.ReplaceAnimatorTrigger(_animator, "Rising");
        StartCoroutine(Coroutine());
        IEnumerator Coroutine()
        {
            var t = 0f;
            var d = 1.125f + extraPullTime;
            vibratorA.transform.DOShakePosition(d, 0.025f, 15, 90f, false, false);

            var pullEvent =
                FMODUnity.RuntimeManager.CreateInstance(
                    FMODUnity.RuntimeManager.PathToEventReference("event:/BarrierSwitchPull"));
            
            FMODUnity.RuntimeManager.AttachInstanceToGameObject(pullEvent, gameObject);
            pullEvent.start();
            
            while (t < d)
            {
                if (PlayerFsm.Singleton.Machine.IsInState(PlayerFsm.PlayerFsmState.Dying) || PlayerFsm.Singleton.Machine.IsInState(PlayerFsm.PlayerFsmState.Respawn))
                {
                    Util.ReplaceAnimatorTrigger(_animator, "Down");
                    pullEvent.stop(STOP_MODE.IMMEDIATE);
                    yield break;
                };

                t += Time.deltaTime;
                yield return null;
            }
            
            pullEvent.stop(STOP_MODE.IMMEDIATE);
            _ambientEventInstance.stop(STOP_MODE.ALLOWFADEOUT);
            FMODUnity.RuntimeManager.PlayOneShotAttached(FMODUnity.RuntimeManager.PathToEventReference("event:/BarrierSwitchBreak"), gameObject);
            Util.ReplaceAnimatorTrigger(_animator, "Up");
            PlayerFsm.Singleton.Machine.Fire(PlayerFsm.PlayerFsmTrigger.PullCompleted);
            _interactable.SetEnabled(false);
            Curtain.SetActive(false);
            OnBarrierSwitch?.Invoke(metaName);
            _particleSystem.Play();
            SaveSystem.WritePersistentEvent(metaName);
            _light.gameObject.SetActive(false);
            Vibrate(0.4f, 0.2f, 20);
        }
    }

    private void Vibrate(float d, float s, int v)
    {
        vibratorA.transform.DOShakePosition(d, s, v);
        vibratorB.transform.DOShakePosition(d, s, v);
        vibratorC.transform.DOShakePosition(d, s, v);
    }

    private void OnDisable()
    {
        _interactable.OnInteracted -= OnInteracted;
        _interactable.OnHardInteracted -= OnHardInteracted;
        _ambientEventInstance.stop(STOP_MODE.ALLOWFADEOUT);
    }

    private void OnInteracted()
    {
        InteractableParam p = new InteractableParam() { Interactable = _interactable, WalkToPositionTarget =
            _interactable.transform.position};
        PlayerFsm.Singleton.Machine.Fire(PlayerFsm.PlayerFsmTrigger.InteractWithSwitch, p);
        Util.ReplaceAnimatorTrigger(_animator, "Down");
        Curtain.SetActive(true);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (SaveSystem.GetPersistentEventCompleted(metaName))
        {
            _interactable.SetEnabled(false);
            Util.ReplaceAnimatorTrigger(_animator, "Up");
            _light.gameObject.SetActive(false);
            Curtain.SetActive(false);
        }
        else
        {
            _ambientEventInstance.start();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
