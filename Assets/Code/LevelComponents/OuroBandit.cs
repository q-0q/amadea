using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class OuroBandit : SimpleNpcController
{
    public const string expositionEvent = "ouro-bandit-exposition";
    
    private static readonly List<string> items = new List<string>()
    {
        "OuroJunk0",
        "OuroJunk1",
        "OuroJunk2",
        "OuroJunk3",
    };

    private DialogueController _dialogue;

    protected override void Awake()
    {
        base.Awake();

        _dialogue = GetComponentInChildren<DialogueController>();
        CheckForQuestCompleted(null);
    }
    protected override void OnEnable()
    {
        base.OnEnable();
        _dialogue.OnPreCompleted += OnDialoguePreCompleted;
        _dialogue.OnCompleted += OnDialogueCompleted;
        SaveSystem.OnSaveDataUpdated += CheckForQuestCompleted;
    }

    private void OnDialoguePreCompleted()
    {
        if (_dialogue.currentDialogueIndex == 1)
        {
            SaveSystem.WritePersistentEvent(expositionEvent);
        }
        
        if (_dialogue.currentDialogueIndex == 3 && !SaveSystem.GetAllItems().Contains("Bandana"))
        {
            foreach (var item in items)
            {
                SaveSystem.RemoveItem(item);
            }
            
            PlayerFsm.Singleton.CollectItem("Bandana");
            SaveSystem.WritePersistentEvent("ouro-bandit-completed");
        }
        
        
    }
    
    private void OnDialogueCompleted()
    {
        CheckForQuestCompleted(null);
    }

    private void CheckForQuestCompleted(SaveSystem.SaveData _)
    {
        
        foreach (var item in items)
        {
            if (!SaveSystem.GetAllItems().Contains(item)) return;
        }

        if (SaveSystem.GetPersistentEventCompleted(expositionEvent) && _dialogue.currentDialogueIndex == 2) _dialogue.currentDialogueIndex = 3;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        SaveSystem.OnSaveDataUpdated -= CheckForQuestCompleted;
        _dialogue.OnPreCompleted -= OnDialoguePreCompleted;
        _dialogue.OnCompleted -= OnDialogueCompleted;
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public static string ItemUseDescription()
    {
        return SaveSystem.GetPersistentEventCompleted(expositionEvent) ? "Makepeace Marlow tasked you with finding 4 of these." : 
            "It doesn't do anything, but it might be valued by someone who doesn't know better.";
    }
}
