using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Code.TriggerParams;
using DG.Tweening;
using FMOD.Studio;
using FMODUnity;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using Util = Code.Misc.Util;

public partial class CrabGuardFsm
{
    private Interactable _interactable;
    private DialogueController _dialogueController;
    private const string PersistentEvent = "c1-snail-quest";
    private bool _triggerActive;
    private float _turnAmount;
    private Transform _questDestination;
    public int questCompleteDialogueIndex = 0;
    
    private void OnDialogueProgressed(int textIndex)
    {

    }
    
    private void OnTriggerProxyStay(Collider obj)
    {
        _triggerActive = true;

    }

    private void OnTriggerProxyExit(Collider obj)
    {
        _triggerActive = false;
    }

    private void OnChannel()
    {
        Machine.Jump(CrabGuardFsmState.Channel);
        transform.rotation = Quaternion.identity;
        StartCoroutine(Coroutine());   
        IEnumerator Coroutine()
        {
            
            yield return new WaitForSeconds(0.35f);
            
            Vector3 destination = PlayerFsm.Singleton.transform.position + (transform.position - PlayerFsm.Singleton.transform.position).normalized * 3f;
            Vector3 origin = transform.position;

            transform.rotation = Quaternion.LookRotation(origin - destination, Vector3.up);
            
            var t = 0f;
            var d = 0.35f;

            while (t < d)
            {
                var w = t / d;
                transform.position = Util.LerpWithArc(origin, destination, w, 4f, Vector3.up);
                t += Time.deltaTime;
                yield return null;
            }

            transform.DOShakePosition(1f, 1f, 30);
        }
    }
}