using UnityEngine;
using FMODUnity;
using UnityEngine.Serialization;

[RequireComponent(typeof(StudioEventEmitter))]
public class RandomizeFMODStart : MonoBehaviour
{

    public int OffsetRandomRangeMilliseconds = 4000;
    void Start()
    {
        int offset = Random.Range(0, OffsetRandomRangeMilliseconds);
        GetComponent<StudioEventEmitter>().EventInstance.setTimelinePosition(offset);
    }
}