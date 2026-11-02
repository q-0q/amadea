using System;
using UnityEngine;

public class Benjamin : MonoBehaviour
{

    private DialogueController _dialogueController;

    private void Awake()
    {
        _dialogueController = GetComponentInChildren<DialogueController>();
    }

    private void OnEnable()
    {
        SaveSystem.OnSaveDataUpdated += OnSaveDataUpdated;
    }

    private void OnDisable()
    {
        SaveSystem.OnSaveDataUpdated -= OnSaveDataUpdated;
    }

    private void OnSaveDataUpdated(SaveSystem.SaveData obj)
    {
        if (SaveSystem.GetPersistentEventCompleted("glyph-travel-gate") && _dialogueController.currentDialogueIndex < 2) _dialogueController.currentDialogueIndex = 2;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
