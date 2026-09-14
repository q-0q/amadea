using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using DG.Tweening;
using FMOD.Studio;
using UnityEngine;

public class LatticeCore : MonoBehaviour
{
    public string completionEvent = "lattice-core-";
    public const float CoreHeight = 44f;
    private float _sliceHeight;
    private DialogueController _dialogue;
    
    private int _currentLatticesCompleted;

    public List<string> latticeIds = new List<string>();
    private List<GameObject> _slices;


    private CinemachineVirtualCamera _camera;
    private Transform _cameraStart;
    private Transform _cameraEnd;
    
    private const float SliceXZScale = 40f;
    
    private void Awake()
    {
        _camera = GetComponentInChildren<CinemachineVirtualCamera>();
        _cameraStart = transform.Find("Camera").Find("Start");
        _cameraEnd = transform.Find("Camera").Find("End");
        
        _dialogue = GetComponentInChildren<DialogueController>();
        _currentLatticesCompleted = 0;
        _sliceHeight = CoreHeight / latticeIds.Count;
        
        
        var slicePrefab = Resources.Load("Prefab/LatticeCoreSlice") as GameObject;
        var baseOffset = new Vector3(0f, (_sliceHeight * 0.5f) + 3f, 0f);
        _slices = new List<GameObject>();
        
        for (int i = 0; i < latticeIds.Count; i++)  
        {        
            var obj = Instantiate(slicePrefab, Vector3.zero, Quaternion.identity, transform);  
            var pos = new Vector3(0, i * _sliceHeight);  
            obj.transform.localPosition = pos + baseOffset;
            obj.transform.localScale = new Vector3(SliceXZScale * 0.9f, _sliceHeight * 0.5f, SliceXZScale * 0.9f);
            
            var slice = obj.GetComponent<LatticeCoreSlice>();
            var top = i != latticeIds.Count - 1;
            var bottom = i != 0;
            slice.SetAdjacencies(top, bottom);
            
            _slices.Add(obj);
        }    
        
        UpdateSliceCompletion(false);

    }
    
    private void UpdateDialogue()
    {
        var color = "<color=red>";

        if (_currentLatticesCompleted != latticeIds.Count)
        {
            _dialogue.dialogues[0].texts[0] = "Ouro Station Hardlight Core is not operable. Hardlight production processes are offline.";
            _dialogue.dialogues[0].texts[1] = color + _currentLatticesCompleted + "</color> lattices are calibrated. " + color +
                                              (latticeIds.Count - _currentLatticesCompleted) + "</color> lattices are in need of calibration.";
        }

        else
        {
            _dialogue.dialogues[0].texts[0] = "Station Lattice calibration complete. Hardlight Core is pending activation.";
            _dialogue.dialogues[0].texts[1] = "Final confirmation from Administrative Deck is required to begin hardlight output.";
        }
    }

    private void UpdateSliceCompletion(bool doCamera)
    {
        StartCoroutine(Coroutine());
        
        IEnumerator Coroutine()
        {
            int numCompleted = 0;
            for (int i = 0; i < latticeIds.Count; i++)
            {
                if (SaveSystem.GetPersistentEventCompleted(Lattice.eventPrefix + latticeIds[i])) numCompleted++;
            }

            if (numCompleted == 1 && doCamera)
            {
                yield return new WaitForSeconds(1f);
                CutsceneManager.Singleton.SetPseudoCutsceneActive();
                yield return new WaitForSeconds(0.5f);
                var t = 0f;
                var d = 1f;

                _camera.Priority = 50;
                _camera.transform.position = _cameraStart.position;
                _camera.transform.rotation = _cameraStart.rotation;
                
                yield return new WaitForSeconds(0.5f);
                
                while (t < d)
                {
                    var p = Vector3.Lerp(_cameraStart.position, _cameraEnd.position,
                        Code.Misc.Util.SmoothLerp01(t / d));
                    
                    var r = Quaternion.Lerp(_cameraStart.rotation, _cameraEnd.rotation,
                        Code.Misc.Util.SmoothLerp01(t / d));

                    _camera.transform.position = p;
                    _camera.transform.rotation = r;
                    t += Time.deltaTime;
                    yield return null;
                }
            }

            for (int i = 0; i < numCompleted; i++)
            {
                _slices[i].GetComponent<LatticeCoreSlice>().MakeSliceCompleted();
                var t = _slices[i].transform;
                t.localScale = new Vector3(SliceXZScale, _sliceHeight * 0.5f, SliceXZScale);
            }
            
            if (numCompleted > 0 && numCompleted < latticeIds.Count)
            {
                _slices[numCompleted - 1].GetComponent<LatticeCoreSlice>().material.SetFloat("_TopMask", 0f);
            
                if (numCompleted > 1) _slices[numCompleted - 2].GetComponent<LatticeCoreSlice>().material.SetFloat("_TopMask", 1f);
            }
            
            yield return new WaitForSeconds(2f);
            _camera.Priority = -50;
            CutsceneManager.Singleton.ClearPseudoCutsceneActive();

            _currentLatticesCompleted = numCompleted;
            UpdateDialogue();
            if (_currentLatticesCompleted == latticeIds.Count) SaveSystem.WritePersistentEvent(completionEvent);
        }
    }

    private void OnEnable()
    {
        Lattice.OnLatticeCompleted += OnLatticeCompleted;
    }

    private void OnLatticeCompleted(Lattice lattice, bool doCamera)
    {
        UpdateSliceCompletion(doCamera);
    }

    private void OnDisable()
    {
        Lattice.OnLatticeCompleted -= OnLatticeCompleted;
    }
}
