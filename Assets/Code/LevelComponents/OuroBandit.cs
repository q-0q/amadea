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
        OnSaveDataUpdated(null);
        
        if (SaveSystem.GetAllItems().Contains("Bandana")) Destroy(gameObject);
    }
    protected override void OnEnable()
    {
        base.OnEnable();
        SaveSystem.OnSaveDataUpdated += OnSaveDataUpdated;
        _dialogue.OnPreCompleted += OnDialogue;
    }

    private void OnDialogue()
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
        }
        
    }

    private void OnSaveDataUpdated(SaveSystem.SaveData obj)
    {
        
        foreach (var item in items)
        {
            if (!SaveSystem.GetAllItems().Contains(item)) return;
        }

        if (SaveSystem.GetPersistentEventCompleted(expositionEvent)) _dialogue.currentDialogueIndex = 3;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        SaveSystem.OnSaveDataUpdated -= OnSaveDataUpdated;
        _dialogue.OnPreCompleted -= OnDialogue;
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
