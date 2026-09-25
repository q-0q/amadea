using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FMODSceneRequestor : MonoBehaviour
{

    public float musicProgression = 0;
    public FMODSceneManager.MusicEvent MusicEvent;
    public List<FMODSceneManager.AmbientEvent> AmbientEvents;

    private void Start()
    {
        FMODUnity.RuntimeManager.StudioSystem.setParameterByName("MusicProgression", musicProgression);
        print("set: " + musicProgression);
    }

    void Update()
    {
        FMODSceneManager.Singleton.SetAmbientEvents(AmbientEvents);
        FMODSceneManager.Singleton.SetMusicEvent(MusicEvent, musicProgression);
    }
}
