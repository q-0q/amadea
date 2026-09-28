using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class DialogueCanvas : MonoBehaviour
{
    
    private CanvasGroup _canvasGroup;
    private TextMeshProUGUI _tmpText;
    private TextMeshProUGUI _tmpName;
    private GameObject _nameBackground;
    public static DialogueCanvas Singleton;
    public DialogueController currentDialogueController;
    private int _currentTextIndex = 0;

    public float TimeSinceDialogueClosed = 100f;

    private void Awake()
    {
        Singleton = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _tmpText = transform.Find("Text").GetComponent<TextMeshProUGUI>();
        _nameBackground = transform.Find("NameBackground").gameObject;
        _tmpName = _nameBackground.GetComponentInChildren<TextMeshProUGUI>();
        _canvasGroup.alpha = 0;
    }

    // Update is called once per frame
    void Update()
    {
        TimeSinceDialogueClosed += Time.deltaTime;
        
        if (currentDialogueController != null && !GameMenu.Singleton.IsMenuOpen() && !PhotoManager.Singleton.IsActive())
        {
            _canvasGroup.alpha = Mathf.Lerp(_canvasGroup.alpha, 1, Time.unscaledDeltaTime * 15f);
            _tmpText.text = currentDialogueController.dialogues[currentDialogueController.currentDialogueIndex].texts[_currentTextIndex + currentDialogueController.textStartOffset];
            _tmpName.text = currentDialogueController.DialogueName;
            _nameBackground.SetActive(currentDialogueController.DialogueName != "");
        }
        else
        {
            _canvasGroup.alpha = Mathf.Lerp(_canvasGroup.alpha, 0, Time.unscaledDeltaTime * 40f);
        }
    }

    public void AdvanceDialogue()
    {
        PlaySound();
        _currentTextIndex++;
        if (currentDialogueController != null) currentDialogueController.ProgressionSignal(_currentTextIndex);
        if (currentDialogueController != null && _currentTextIndex + currentDialogueController.textStartOffset >= currentDialogueController.dialogues[currentDialogueController.currentDialogueIndex].texts.Count)
        {
            EndDialogue();
        }
    }

    public void EndDialogue()
    {
        if (currentDialogueController is null) return;
        currentDialogueController.Completed();
        currentDialogueController = null;
        TimeSinceDialogueClosed = 0;
        PlayerFsm.Singleton.Machine.Fire(PlayerFsm.PlayerFsmTrigger.EndDialogue);
    }

    public void StartDialogue(DialogueController controller)
    {
        StartCoroutine(Coroutine());
        PlaySound();
        IEnumerator Coroutine()
        {
            var delay = controller.canvasDelay + controller.dialogues[controller.currentDialogueIndex].canvasDelayOffset;
            StartCoroutine(PlayerCinemachineFreeLook.Singleton.PreventYRecenterForDuration(delay));
            yield return new WaitForSeconds(delay);
            currentDialogueController = controller;
            _currentTextIndex = 0;
        }
        
    }

    public Vector3 ControllerPosition()
    {
        return currentDialogueController.transform.position;
    }

    public void PlaySound()
    {
        FMODUnity.RuntimeManager.PlayOneShotAttached(FMODUnity.RuntimeManager.PathToEventReference("event:/ProgressDialogue"), gameObject);
    }
}
