using System;
using System.Collections;
using Code.Fsm.GravityFsm.PlayerFsm;
using Code.Misc;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AcquisitionCanvas : MonoBehaviour
{
    public bool isOpen;

    public static AcquisitionCanvas Singleton;

    public CanvasGroup _backgroundCanvasGroup;
    public CanvasGroup _upperCanvasGroup;
    public CanvasGroup _middleCanvasGroup;
    public CanvasGroup _lowerCanvasGroup;

    public TextMeshProUGUI _upperSubtext;
    public TextMeshProUGUI _upperText;
    public Image _inputImage;
    public TextMeshProUGUI _inputClauseText;
    public TextMeshProUGUI _descriptionText;
    public TextMeshProUGUI _lowerText;
    
    
    private void OnEnable()
    {
        PlayerFsm.OnTrickAcquired += OnTrickAcquired;
    }

    private void OnDisable()
    {
        PlayerFsm.OnTrickAcquired -= OnTrickAcquired;
    }

    private void Awake()
    {
        Singleton = this;
        _backgroundCanvasGroup.alpha = 0;
        _upperCanvasGroup.alpha = 0;
        _middleCanvasGroup.alpha = 0f;
        _lowerCanvasGroup.alpha = 0;
    }

    private void OnTrickAcquired(string trick)
    {
        var data = MovelistRegistry.TrickMovelistRegistrations[trick];
        StartCoroutine(Coroutine( "<color=#" + MovelistRegistry.TrickColor + ">Lotus Form</color> learned", data.displayName, data.useInput, data.useClause, data.description,
            "<color=#" + MovelistRegistry.TrickColor + ">Lotus Forms</color> cost <color=#" + MovelistRegistry.TrickColor + ">ki</color> to perform."));
        if (SaveSystem.LoadCachedSaveData().tricks.Count == 1) StartCoroutine(MovelistTutorialCoroutine());
    }

    IEnumerator MovelistTutorialCoroutine()
    {
        yield return new WaitForSeconds(10f);
        TutorialCanvas.Singleton.ShowTutorialText("View movelist", "Movelist");
    }
    
    
    IEnumerator Coroutine(string upperSubtext, string upperText, string input, string inputClauseText, string description, string lowerText)
    {

        _upperSubtext.text = upperSubtext;
        _upperText.text = upperText;
        _inputImage.sprite = InputTypeManager.Singleton.GetSpriteForAction(input);
        _inputClauseText.text = inputClauseText;
        _descriptionText.text = description;
        _lowerText.text = lowerText;
        isOpen = true;

        MusicDistanceAttenuator.IsCoroutineActive = true;
        FMODUnity.RuntimeManager.StudioSystem.getParameterByName("MusicDistance", out float initialMusicDistance);
        FMODUnity.RuntimeManager.PlayOneShotAttached(FMODUnity.RuntimeManager.PathToEventReference("event:/Acquisition"), gameObject);

        var t = 0f;
        var d = 1f;

        while (t < d)
        {
            var w = Util.SmoothLerp01(t / d );
            _backgroundCanvasGroup.alpha = Mathf.Lerp(_backgroundCanvasGroup.alpha, 1f, Time.deltaTime * 7f);
            FMODUnity.RuntimeManager.StudioSystem.setParameterByName("MusicDistance", Mathf.Lerp(initialMusicDistance, 1f, w));
            t += Time.deltaTime;
            yield return null;
        }
        
        _backgroundCanvasGroup.alpha = 1f;

        
        yield return new WaitForSeconds(0.5f);
        
        t = 0f;
        d = 1f;

        while (t < d)
        {
            var w = t / d;
            _upperCanvasGroup.alpha = w;
            t += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(0.5f);
        
        t = 0f;
        d = 1f;

        while (t < d)
        {
            var w = t / d;
            _middleCanvasGroup.alpha = w;
            t += Time.deltaTime;
            yield return null;
        }
        
        t = 0f;
        d = 1f;

        while (t < d)
        {
            var w = t / d;
            _lowerCanvasGroup.alpha = w;
            t += Time.deltaTime;
            yield return null;
        }

        
        _upperCanvasGroup.alpha = 1f;
        _middleCanvasGroup.alpha = 1f;
        _lowerCanvasGroup.alpha = 1f;



        var holdTime = lowerText == "" ? 2f : 3.5f;
        yield return new WaitForSeconds(holdTime);
        
        t = 0f;
        d = 1.5f;

        while (t < d)
        {
            var w = 1f - (t / d);
            _backgroundCanvasGroup.alpha = w;
            _upperCanvasGroup.alpha = w;
            _middleCanvasGroup.alpha = w;
            _lowerCanvasGroup.alpha = w;
            FMODUnity.RuntimeManager.StudioSystem.setParameterByName("MusicDistance", Mathf.Lerp(1f, initialMusicDistance, w));
            t += Time.deltaTime;
            yield return null;
        }
        
        isOpen = false;
        
        _backgroundCanvasGroup.alpha = 0;
        _upperCanvasGroup.alpha = 0;
        _middleCanvasGroup.alpha = 0f;
        _lowerCanvasGroup.alpha = 0;
        
        MusicDistanceAttenuator.IsCoroutineActive = false;

        
        yield return null;
    }

    public void InvokeMapAcquisition()
    {
        var data = KeyItemRegistry.KeyItemRegistrations["Map"];
        StartCoroutine(Coroutine( "Acquired the", data.displayName, "Map", "Open map", data.GetUseDescription(),
            ""));
    }

    public bool IsOpen()
    {
        return isOpen;
    }
}
