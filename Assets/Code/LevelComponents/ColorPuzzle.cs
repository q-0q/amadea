using System;
using System.Collections;
using System.Collections.Generic;
using FMOD.Studio;
using UnityEngine;
using Util = Code.Misc.Util;

public class ColorPuzzle : MonoBehaviour
{
    public string persistentEvent = "color-puzzle-";
    
    [Serializable]
    public class AcceptedAnswer
    {
        public List<int> indeces;
    }
    
    public List<AcceptedAnswer> acceptedAnswers;
    public Transform door;

    private List<ColorPuzzleStation> _stations;
    private Vector3 _baseDoorPosition;
    private readonly Vector3 _doorOffset = new Vector3(0f, -26f, 0f);

    private void Awake()
    {
        var count = acceptedAnswers[0].indeces.Count;
        _stations = new List<ColorPuzzleStation>();
        var prefab = Resources.Load("Prefab/ColorPuzzleStation") as GameObject;
        var spacing = 4f;
        for (int i = 0; i < count; i++)
        {
            var obj = Instantiate(prefab);
            obj.transform.SetParent(transform.Find("Stations"));
            var offset = Vector3.left * spacing * i;
            obj.transform.localPosition = Vector3.zero + offset;
            obj.transform.localRotation = Quaternion.identity;
            var colorPuzzleStation = obj.GetComponent<ColorPuzzleStation>();
            colorPuzzleStation.owner = this;
            _stations.Add(colorPuzzleStation);
        }

        _baseDoorPosition = door.localPosition;

        if (SaveSystem.GetPersistentEventCompleted(persistentEvent))
        {
            foreach (var station in _stations)
            {
                station.MakeComplete();
            }

            door.localPosition = _baseDoorPosition + _doorOffset;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnStationColorUpdated()
    {
        var count = _stations.Count;
        foreach (var acceptedAnswer in acceptedAnswers)
        {
            var answerMatched = true;
            for (int i = 0; i < count; i++)
            {
                if (acceptedAnswer.indeces[i] != _stations[i].GetCurrentColorIndex())
                {
                    answerMatched = false;
                    break;
                }
            }

            if (answerMatched)
            {
                SaveSystem.WritePersistentEvent(persistentEvent);
                StartCoroutine(DoorCoroutine());
                foreach (var station in _stations)
                {
                    station.MakeComplete();
                }
            }
        }
    }

    private IEnumerator DoorCoroutine()
    {

        yield return new WaitForSeconds(2f);
        var t = 0f;
        var d = 3f;
        while (t < d)
        {
            var w = Util.SmoothLerp01(t / d);

            door.localPosition = Vector3.Lerp(_baseDoorPosition, _baseDoorPosition + _doorOffset, w);
            t += Time.deltaTime;
            yield return null;
        }
    }

    
}
