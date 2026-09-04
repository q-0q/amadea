using System;
using System.Collections.Generic;
using UnityEngine;

public class LatticeCore : MonoBehaviour
{
    public const float SliceHeight = 4f;
    private DialogueController _dialogue;
    
    private int _currentLatticesCompleted;

    public List<string> latticeIds = new List<string>();
    private List<GameObject> _slices;
    
    private void Awake()
    {
        _dialogue = GetComponentInChildren<DialogueController>();
        _currentLatticesCompleted = 0;
        
        
        var slicePrefab = Resources.Load("Prefab/LatticeCoreSlice") as GameObject;
        var baseOffset = new Vector3(0f, 5.25f, 0f);
        _slices = new List<GameObject>();
        
        for (int i = 0; i < latticeIds.Count; i++)  
        {        
            var obj = Instantiate(slicePrefab, Vector3.zero, Quaternion.identity, transform);  
            var pos = new Vector3(0, i * SliceHeight);  
            obj.transform.localPosition = pos + baseOffset;
            obj.transform.localScale = new Vector3(obj.transform.localScale.x, SliceHeight * 0.5f, obj.transform.localScale.z);
            
            var slice = obj.GetComponent<LatticeCoreSlice>();
            var top = i != latticeIds.Count - 1;
            var bottom = i != 0;
            slice.SetAdjacencies(top, bottom);
            
            _slices.Add(obj);
        }    
        
        UpdateSliceCompletion(null);

    }
    
    private void UpdateDialogue()
    {
        var status = "fully unstable";
        _dialogue.dialogues[0].texts[0] = "Hardlight core is " + status + ". All hardlight production processes are offline.";
        _dialogue.dialogues[0].texts[1] = latticeIds.Count - _currentLatticesCompleted + " lattices are in need of calibration.";
    }

    private void UpdateSliceCompletion(SaveSystem.SaveData saveData)
    {

        int numCompleted = 0;
        for (int i = 0; i < latticeIds.Count; i++)
        {
            if (SaveSystem.GetPersistentEventCompleted(Lattice.eventPrefix + latticeIds[i])) numCompleted++;
        }

        for (int i = 0; i < numCompleted; i++)
        {
            _slices[i].GetComponent<LatticeCoreSlice>().MakeSliceCompleted();
        }

        if (numCompleted > 0 && numCompleted < latticeIds.Count)
        {
            _slices[numCompleted - 1].GetComponent<LatticeCoreSlice>().material.SetFloat("_TopMask", 0f);
            
            if (numCompleted > 1) _slices[numCompleted - 2].GetComponent<LatticeCoreSlice>().material.SetFloat("_TopMask", 1f);
        }

        _currentLatticesCompleted = numCompleted;
        UpdateDialogue();
    }

    private void OnEnable()
    {
        SaveSystem.OnSaveDataUpdated += UpdateSliceCompletion;
    }

    private void OnDisable()
    {
        SaveSystem.OnSaveDataUpdated -= UpdateSliceCompletion;
    }
}
