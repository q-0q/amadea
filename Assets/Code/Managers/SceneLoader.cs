using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using FMOD.Studio;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Util = Code.Misc.Util;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Singleton;
    private CanvasGroup _canvasGroup;
    private RectTransform _rawImageRectTransform;

    private const float InDuration = 0.5f;
    private const float HoldDuration = 1f;
    private const float OutDuration = 0.3f;

    private bool _mutex;
    private Material _bellMaterial;

    private void Awake()
    {
        Singleton = this;
        _mutex = false;
        

    }

    private void Start()
    {
        _canvasGroup = GetComponentInChildren<CanvasGroup>();
        _canvasGroup.alpha = 0f;
        _rawImageRectTransform = transform.Find("Canvas").Find("RawImage").GetComponent<RectTransform>();
        _bellMaterial = transform.Find("Canvas").Find("Bell").GetComponent<RawImage>().material;
        var data = SaveSystem.LoadCachedSaveData();
        if (data.playerInGamePositionId == "Bell") StartCoroutine(OnBellStart());
        else StartCoroutine(OnStandardStart());
    }

    public void LoadScene(string scene)
    {
        // FMODUnity.RuntimeManager.GetBus("Bus:/").stopAllEvents(STOP_MODE.IMMEDIATE);
        StartCoroutine(LoadYourAsyncScene(scene));
    }

    public void ReloadCurrentScene()
    {
        LoadScene(SceneManager.GetActiveScene().name);
    }

    IEnumerator OnStandardStart()
    {
        _canvasGroup.alpha = 1f;
        _rawImageRectTransform.anchorMin = Vector2.zero;
        _rawImageRectTransform.anchorMax = Vector2.one;
        _rawImageRectTransform.offsetMin = Vector2.zero;
        _rawImageRectTransform.offsetMax = Vector2.zero;
        Vector2 startPos = _rawImageRectTransform.anchoredPosition;
        float width = _rawImageRectTransform.rect.width;
        Vector2 endPos = startPos - new Vector2(width, 0);

        float elapsed = 0f;
        while (elapsed < HoldDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < OutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Util.SmoothLerp01(elapsed / OutDuration);
            _rawImageRectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }

        _rawImageRectTransform.anchoredPosition = endPos;
        _canvasGroup.alpha = 0f;
    }
    
    IEnumerator OnBellStart()
    {
        _canvasGroup.alpha = 1f;
        _rawImageRectTransform.anchorMin = Vector2.zero;
        _rawImageRectTransform.anchorMax = Vector2.one;
        _rawImageRectTransform.offsetMin = Vector2.zero;
        _rawImageRectTransform.offsetMax = Vector2.zero;
        Vector2 startPos = _rawImageRectTransform.anchoredPosition;
        float width = _rawImageRectTransform.rect.width;
        Vector2 endPos = startPos - new Vector2(width, 0);
        _rawImageRectTransform.anchoredPosition = endPos;
        
        
        _bellMaterial.SetFloat("_Alpha", 1f);

        yield return new WaitForSeconds(0.75f);
        
        var t = 0f;
        var d = 2f;

        while (t < d)
        {
            _bellMaterial.SetFloat("_Alpha", 1f - (t / d));
            t += Time.deltaTime;
            yield return null;
        }
        
        _bellMaterial.SetFloat("_Alpha", 0f);
        _canvasGroup.alpha = 0f;
    }
    
    IEnumerator LoadYourAsyncScene(string scene)
    {
        if (_mutex) yield break;
        _mutex = true;
        _canvasGroup.alpha = 1f;
        float width = _rawImageRectTransform.rect.width;
        Vector2 startPos = new Vector2(width, 0);
        Vector2 endPos = startPos - new Vector2(width, 0);
        float elapsed = 0f;
        while (elapsed < InDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Util.SmoothLerp01(elapsed / InDuration);
            _rawImageRectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }

        _rawImageRectTransform.anchoredPosition = endPos;
        
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(scene);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        _canvasGroup.alpha = 0f;
        _mutex = false;
    }



    public void LoadSceneBell(string scene, Vector3 screenPos)
    {
        StartCoroutine(LoadBellAsync(scene));

    }

    IEnumerator LoadBellAsync(string scene)
    {
        if (_mutex) yield break;
        _mutex = true;
        _canvasGroup.alpha = 1f;

        _bellMaterial.SetFloat("_Alpha", 0f);
        var t = 0f;
        var d = 2f;

        while (t < d)
        {
            _bellMaterial.SetFloat("_Alpha", (t / d));
            t += Time.deltaTime;
            yield return null;
        };
        _bellMaterial.SetFloat("_Alpha", 1f);
        
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(scene);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        _canvasGroup.alpha = 0f;
        _mutex = false;
    }

    private void OnEnable()
    {
        BellController.OnBellRingAtPosition += OnRing;
    }

    private void OnDisable()
    {
        BellController.OnBellRingAtPosition -= OnRing;
    }

    private void OnRing(Vector3 obj)
    {
        var screenPosition = Camera.main.WorldToScreenPoint(obj);
        _bellMaterial.SetVector("_Center", new Vector2(screenPosition.x, screenPosition.y));
        
    }
}
