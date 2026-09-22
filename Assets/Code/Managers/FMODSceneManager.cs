using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using STOP_MODE = FMOD.Studio.STOP_MODE;

public class FMODSceneManager : MonoBehaviour
{
    public enum FMODSceneEvent
    {
        
        // DO NOT CHANGE THE ORDER OF THESE! ONLY ADD TO THE END OF THE LIST!!!
        // OTHERWISE ALL EXISTING AUDIO REQUESTORS WILL BE MESSED UP!
        Ch1Music,
        CogsMusic,
        TimeGoesOnMusic,
        WindAmbience,
        CaveAmbience,
        SnailMusic,
        AwakeningMusic
        
    }
    
    private static Dictionary<FMODSceneEvent, EventInstance> _eventInstances;
    
    private static FMODSceneManager _singleton;
    public static FMODSceneManager Singleton
    {
        get
        {
            if (_singleton == null)
            {
                var go = new GameObject("FMODSceneManager");
                _singleton = go.AddComponent<FMODSceneManager>();
            }
            return _singleton;
        }
    }

    void Awake()
    {
        if (_singleton != null && _singleton != this)
        {
            Destroy(gameObject);
            return;
        }

        _singleton = this;
        DontDestroyOnLoad(gameObject);

        _eventInstances = new Dictionary<FMODSceneEvent, EventInstance>
        {
            [FMODSceneEvent.Ch1Music] = RuntimeManager.CreateInstance(FMODUnity.RuntimeManager.PathToEventReference("event:/CH1_Music")),
            [FMODSceneEvent.CogsMusic] = RuntimeManager.CreateInstance(FMODUnity.RuntimeManager.PathToEventReference("event:/Music2")),
            [FMODSceneEvent.TimeGoesOnMusic] = RuntimeManager.CreateInstance(FMODUnity.RuntimeManager.PathToEventReference("event:/Music3")),
            [FMODSceneEvent.SnailMusic] = RuntimeManager.CreateInstance(FMODUnity.RuntimeManager.PathToEventReference("event:/SnailMusic")),
            [FMODSceneEvent.WindAmbience] = RuntimeManager.CreateInstance(FMODUnity.RuntimeManager.PathToEventReference("event:/WindAmbience")),
            [FMODSceneEvent.CaveAmbience] = RuntimeManager.CreateInstance(FMODUnity.RuntimeManager.PathToEventReference("event:/CaveAmbience")),
            [FMODSceneEvent.AwakeningMusic] = RuntimeManager.CreateInstance(FMODUnity.RuntimeManager.PathToEventReference("event:/M_Awakening")),
        };
    }

    public void Play(FMODSceneEvent fmodSceneEvent)
    {
        if (PlaybackState(_eventInstances[fmodSceneEvent]) == PLAYBACK_STATE.PLAYING) return;
        _eventInstances[fmodSceneEvent].start();
    }
    
    public void Stop(FMODSceneEvent fmodSceneEvent)
    {
        _eventInstances[fmodSceneEvent].stop(STOP_MODE.ALLOWFADEOUT);
    }

    public void StopAll()
    {
        foreach (var (_, instance) in _eventInstances)
        {
            instance.stop(STOP_MODE.ALLOWFADEOUT);
        }
    }
    
    
    
    FMOD.Studio.PLAYBACK_STATE PlaybackState(FMOD.Studio.EventInstance instance) 
    {
        FMOD.Studio.PLAYBACK_STATE pS;
        instance.getPlaybackState(out pS);
        return pS;
    }
}


