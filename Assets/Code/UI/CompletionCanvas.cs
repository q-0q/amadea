using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CompletionCanvas : MonoBehaviour
{
    private bool _show;

    private CanvasGroup _canvasGroup;
    // Start is called before the first frame update
    void Awake()
    {
        TryGetComponent(out _canvasGroup);
        _canvasGroup.alpha = 0;
        _show = false;
    }

    private void OnCanvasTriggerEnter()
    {
        var completionProfileCanvas = GetComponentInChildren<CompletionProfileCanvas>();
        
        if(completionProfileCanvas != null) completionProfileCanvas.UpdateCompletionProfile("c1");
        _show = true;
    }
    
    private void OnCanvasTriggerExit()
    {
        _show = false;
    }

    private void OnEnable()
    {
        CompletionCanvasTrigger.OnCompletionCanvasTriggerEnter += OnCanvasTriggerEnter;
        CompletionCanvasTrigger.OnCompletionCanvasTriggerExit += OnCanvasTriggerExit;
    }

    private void OnDisable()
    {
        CompletionCanvasTrigger.OnCompletionCanvasTriggerEnter -= OnCanvasTriggerEnter;
        CompletionCanvasTrigger.OnCompletionCanvasTriggerExit -= OnCanvasTriggerExit;
    }

    // Update is called once per frame
    void Update()
    {
        _canvasGroup.alpha = Mathf.Lerp(_canvasGroup.alpha, _show ? 1f : 0f, Time.deltaTime * 5f);
    }
}
