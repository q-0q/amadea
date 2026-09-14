using System;
using System.Collections;
using Code.Misc;
using Code.TriggerParams;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

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

    public static event Action<string> OnBarrierSwitch;

    private void Awake()
    {
        _interactable = GetComponentInChildren<Interactable>();
        _particleSystem = GetComponentInChildren<ParticleSystem>();
        _animator = GetComponentInChildren<Animator>();
        _light = GetComponentInChildren<CustomPointLight>();
        Util.ReplaceAnimatorTrigger(_animator, "Down");

        
        
    }
    
    

    private void OnEnable()
    {
        _interactable.OnInteracted += OnInteracted;
        _interactable.OnHardInteracted += OnHardInteracted;
    }

    private void OnHardInteracted()
    {
        Util.ReplaceAnimatorTrigger(_animator, "Rising");
        _interactable.SetEnabled(false);
        StartCoroutine(Coroutine());
        IEnumerator Coroutine()
        {
            yield return new WaitForSeconds(1.125f);
            Curtain.SetActive(false);
            OnBarrierSwitch?.Invoke(metaName);
            _particleSystem.Play();
            SaveSystem.WritePersistentEvent(metaName);
            _light.gameObject.SetActive(false);
            vibratorA.transform.DOShakePosition(0.4f, 0.2f, 20);
            vibratorB.transform.DOShakePosition(0.4f, 0.2f, 20);
            vibratorC.transform.DOShakePosition(0.4f, 0.2f, 20);
            
        }
    }

    private void OnDisable()
    {
        _interactable.OnInteracted -= OnInteracted;
        _interactable.OnHardInteracted -= OnHardInteracted;
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
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
