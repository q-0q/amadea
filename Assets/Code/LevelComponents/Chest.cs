using System;
using System.Collections;
using Code.Misc;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

public class Chest : MonoBehaviour
{

    private Animator _animator;
    private Interactable _interactable;

    public string itemId;
    public Transform vibratorA;
    public Transform vibratorB;
    public Transform item;

    private Vector3 itemStartPosition;
    private Vector3 itemStartScale;

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        _interactable = GetComponentInChildren<Interactable>();

        itemStartPosition = item.localPosition;
        itemStartScale = item.localScale;
        item.localScale = Vector3.zero;

        if (SaveSystem.GetAllItems().Contains(itemId))
        {
            _interactable.SetEnabled(false);
            Util.ReplaceAnimatorTrigger(_animator, "Open");
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    private void OnEnable()
    {
        _interactable.OnInteracted += OnInteracted;
    }

    private void OnDisable()
    {
        _interactable.OnInteracted -= OnInteracted;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void DoOpen()
    {
        Util.ReplaceAnimatorTrigger(_animator,"Opening");
    }

    private void OnInteracted()
    {
        PlayerFsm.Singleton.Machine.Jump(PlayerFsm.PlayerFsmState.WalkToChestPosition);
        PlayerFsm.Singleton.walkToPositionTarget = _interactable.transform.position;
        PlayerFsm.Singleton.walkToPositionArrivalDistanceModifier = _interactable.arrivalDistanceModifier;
        
        _interactable.SetEnabled(false);

        StartCoroutine(Coroutine());
        IEnumerator Coroutine()
        {

            item.localPosition = itemStartPosition;
            item.localScale = Vector3.zero;
            
            while (PlayerFsm.Singleton.Machine.IsInState(PlayerFsm.PlayerFsmState.WalkToChestPosition))
                yield return null;
            
            DoOpen();
            
            FMODUnity.RuntimeManager.PlayOneShotAttached(FMODUnity.RuntimeManager.PathToEventReference("event:/ChestOpen"), gameObject);

            yield return new WaitForSeconds(0.4f);

            vibratorA.transform.DOShakePosition(0.25f, 0.00125f, 20);
            vibratorB.transform.DOShakePosition(0.25f, 0.00125f, 20);

            yield return new WaitForSeconds(1.25f);
            
            
            var t = 0f;
            var d = 0.75f;
            while (t < d)
            {
                var w = Util.SmoothLerp01(t / d);
                item.localPosition = Vector3.Lerp(itemStartPosition, itemStartPosition + Vector3.up * 3f, w);
                item.localScale = Vector3.Lerp(Vector3.zero, itemStartScale, w * 2.5f);
                t += Time.deltaTime * Mathf.Lerp(1f, 0.5f, Mathf.InverseLerp(0.5f, 1f, t));
                yield return null;
            }

            yield return new WaitForSeconds(0.5f);
            
            PlayerFsm.Singleton.CollectItem(itemId);
            
            Util.InvokeSphereEffect(item.position - Vector3.up, Vector3.one * 6f, 1.25f, 0.8f, -0.5f);
            t = 0f;
            d = 0.25f;
            while (t < d)
            {
                var w = Util.SmoothLerp01(t / d);
                item.localScale = Vector3.Lerp(itemStartScale, Vector3.zero, w);
                t += Time.deltaTime;
                yield return null;
            }
            
            
        }
        
    }
}
