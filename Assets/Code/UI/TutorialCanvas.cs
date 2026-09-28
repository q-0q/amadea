using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class TutorialCanvas : MonoBehaviour
{
    
    private CanvasGroup _canvasGroup;
    private TextMeshProUGUI _tmpText;
    public static TutorialCanvas Singleton;
    private bool _open = false;
    private int _currentTextIndex = 0;
    private string action;

    public Image Image;
    private TextMeshProUGUI _tapTmp;

    private void Awake()
    {
        Singleton = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _tmpText = transform.Find("LayoutGroup").Find("Text").GetComponent<TextMeshProUGUI>();
        _canvasGroup.alpha = 0;
        _tapTmp = transform.Find("LayoutGroup").Find("TapText").GetComponent<TextMeshProUGUI>();
    }

    // Update is called once per frame
    void Update()
    {
        // return;
        Image.sprite = InputTypeManager.Singleton.GetSpriteForAction(action);
        _tapTmp.gameObject.SetActive(action != "Move" && action != "Look");
        
        if (_open && !GameMenu.Singleton.IsMenuOpen() && !CutsceneManager.Singleton.IsCutsceneTutorialCanvasHidden() && !PhotoManager.Singleton.IsActive())
        {
            _canvasGroup.alpha = Mathf.Lerp(_canvasGroup.alpha, 1, Time.unscaledDeltaTime * 10f);
        }
        else
        {
            _canvasGroup.alpha = Mathf.Lerp(_canvasGroup.alpha, 0, Time.unscaledDeltaTime * 5f);
        }
    }

    public void HideTutorialText()
    {
        _open = false;
    }

    public void ShowTutorialText(string text, string action)
    {
        if (CutsceneManager.Singleton.IsCutsceneTutorialCanvasHidden()) return;
        
        
        this.action = action;
        Image.gameObject.SetActive(action != "");
        if (!_open)
        {
            FMODUnity.RuntimeManager.PlayOneShotAttached(
                FMODUnity.RuntimeManager.PathToEventReference("event:/TutorialPrompt"), gameObject);
        }
        _open = true;
        _tmpText.text = text;
        
        _tmpText.CalculateLayoutInputHorizontal();
        
        // Force the parent to reposition the icon and text based on new width
        LayoutRebuilder.ForceRebuildLayoutImmediate(_tmpText.transform.parent.GetComponent<RectTransform>());
        
        
    }
    
    public string GetCurrentAction()
    {
        return action;
    }

    public bool IsOpen()
    {
        return _open;
    }
    
}
