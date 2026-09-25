using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using STOP_MODE = FMOD.Studio.STOP_MODE;

public class FMODSceneManager : MonoBehaviour
{
    public enum MusicEvent
    {
        Sustain,
        Off,
        Awakening,
        Glyph,
        Steppe
    }

    public enum AmbientEvent
    {
        Wind,
        Cave
    }
    
    private static Dictionary<MusicEvent, EventInstance> _musicInstances;
    private static Dictionary<AmbientEvent, EventInstance> _ambientInstances;
    
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

    private EventInstance _reverbControllerInstance;
    
    void Awake()
    {
        if (_singleton != null && _singleton != this)
        {
            Destroy(gameObject);
            return;
        }

        _singleton = this;
        DontDestroyOnLoad(gameObject);

        _musicInstances = new Dictionary<MusicEvent, EventInstance>
        {
            [MusicEvent.Awakening] = RuntimeManager.CreateInstance(FMODUnity.RuntimeManager.PathToEventReference("event:/M_Awakening")),
            [MusicEvent.Glyph] = RuntimeManager.CreateInstance(FMODUnity.RuntimeManager.PathToEventReference("event:/M_Glyph")),
            [MusicEvent.Steppe] = RuntimeManager.CreateInstance(FMODUnity.RuntimeManager.PathToEventReference("event:/M_Steppe")),
        };
        
        _ambientInstances = new Dictionary<AmbientEvent, EventInstance>
        {
            [AmbientEvent.Cave] = RuntimeManager.CreateInstance(FMODUnity.RuntimeManager.PathToEventReference("event:/CaveAmbience")),
            [AmbientEvent.Wind] = RuntimeManager.CreateInstance(FMODUnity.RuntimeManager.PathToEventReference("event:/WindAmbience")),
        };

        _reverbControllerInstance =
            FMODUnity.RuntimeManager.CreateInstance(
                FMODUnity.RuntimeManager.PathToEventReference("event:/SYS_ReverbController"));

        _reverbControllerInstance.start();
    }


    public void SetAmbientEvents(List<AmbientEvent> ambientEvents)
    {
        foreach (var (key, instance) in _ambientInstances)
        {
            if (!ambientEvents.Contains(key))
            {
                instance.stop(STOP_MODE.ALLOWFADEOUT);
                continue;
            }
            
            if (PlaybackState(instance) == PLAYBACK_STATE.PLAYING) continue;
            instance.start();
        }
    }

    public void StopMusic()
    {
        foreach (var (key, instance) in _musicInstances)
        {
            instance.stop(STOP_MODE.ALLOWFADEOUT);
        }
    }

    public void SetMusicEvent(MusicEvent musicEvent, float musicProgression)
    {
        if (musicEvent == MusicEvent.Sustain) return;
        if (musicEvent == MusicEvent.Off)
        {
            StopMusic();
            return;
        };
        
        var isMusicMaskActive = IsMusicMaskActive();
        
        foreach (var (key, instance) in _musicInstances)
        {
            if (key != musicEvent)
            {
                instance.stop(STOP_MODE.ALLOWFADEOUT);
                continue;
            }
            
            if (isMusicMaskActive) continue;
            if (PlaybackState(instance) == PLAYBACK_STATE.PLAYING) continue;
            instance.start();
            FMODUnity.RuntimeManager.StudioSystem.setParameterByName("MusicProgression", musicProgression);
            print("set: 0");
        }
    }
    
    

    public void StopAll()
    {
        foreach (var (_, instance) in _musicInstances)
        {
            instance.stop(STOP_MODE.ALLOWFADEOUT);
        }
        
        foreach (var (_, instance) in _ambientInstances)
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

    private bool IsMusicMaskActive()
    {
        return Physics.CheckSphere(PlayerFsm.Singleton.transform.position, 3f,
            LayerMask.GetMask("AudioRequestorMask"));
    }
}


