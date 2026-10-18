using System;
using UnityEngine;
using UnityEngine.Serialization;

public class Junk : MonoBehaviour
{
    [FormerlySerializedAs("itemName")] public string itemKey;

    private GameObject a;
    private GameObject b;
    private Interactable _interactable;

    private void Awake()
    {
        a = transform.Find("junk").Find("JunkA").gameObject;
        b = transform.Find("junk").Find("JunkB").gameObject;
        _interactable = GetComponentInChildren<Interactable>();
        
        if (SaveSystem.GetAllItems().Contains(itemKey) || SaveSystem.GetAllItems().Contains("Bandana") ) MakeComplete();
    }

    private void OnEnable()
    {
        _interactable.OnInteracted += OnInteracted;
    }

    private void OnDisable()
    {
        _interactable.OnInteracted -= OnInteracted;
    }

    private void OnInteracted()
    {
        PlayerFsm.Singleton.CollectItem(itemKey);
        MakeComplete();
    }
    
    private void MakeComplete()
    {
        a.SetActive(false);
        b.SetActive(false);
        
        _interactable.SetEnabled(false);
    }
}
