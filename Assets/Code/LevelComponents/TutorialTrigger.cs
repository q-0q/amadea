using System;
using System.Reflection;

using System.Collections;
using System.Collections.Generic;
using Code.TriggerParams;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;


public class TutorialTrigger : MonoBehaviour
{
    public string action = "";
    public string text = "Tutorial text";
    public string playerHideState = "";
    private int playerHideStateInt = -1;
    private bool active = false;
    private const float SoundCooldown = 7f;
    private float _soundTimer;

    private void Start()
    {
        _soundTimer = 100f;
        FieldInfo field = typeof(PlayerFsm.PlayerFsmState).GetField(
            playerHideState,
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
        );

        if (field != null)
        {
            playerHideStateInt = (int)field.GetValue(null);
        }
    }

    private void Update()
    {
        if (active && PlayerFsm.Singleton.Machine.IsInState(playerHideStateInt))
        {
            TutorialCanvas.Singleton.HideTutorialText();
            active = false;
        }

        _soundTimer += Time.deltaTime;
    }

    private void OnTriggerStay(Collider other)
    {
        if (PlayerFsm.Singleton.Machine.IsInState(playerHideStateInt)) return;
        TutorialCanvas.Singleton.ShowTutorialText(text, action, _soundTimer > SoundCooldown);
        _soundTimer = 0f;
        active = true;
    }

    private void OnTriggerExit(Collider other)
    {
        TutorialCanvas.Singleton.HideTutorialText();
        active = false;
    }


}
