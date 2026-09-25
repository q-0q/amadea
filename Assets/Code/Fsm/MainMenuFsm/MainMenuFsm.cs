using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class MainMenuFsm : Fsm
{
    public class MainMenuFsmState : FsmState
    {
        public static int Home;
        public static int Options;
        public static int Saves;
        public static int Chapters;
        public static int NewGame;
    }

    public class MainMenuFsmTrigger : FsmTrigger
    {
        public static int HomePlayClicked;
        public static int HomeOptionsClicked;
        public static int BackClicked;
        public static int SaveClicked;
    }

    protected override void OnAwake()
    {
        base.OnAwake();

        _homeObject = transform.Find("Home").gameObject;
        _optionsObject = transform.Find("SettingsMenu").gameObject;
        _savesObject = transform.Find("Saves").gameObject;
        _chaptersObject = transform.Find("Chapters").gameObject;
        _backButtonObject = transform.Find("BackButton").gameObject;
        _newGameObject = transform.Find("NewGame").gameObject;
        
        TryGetComponent(out _playerInput);
    }

    protected override void OnStart()
    {
        base.OnStart();
        InitState = MainMenuFsmState.Home;
        
        _homeObject.SetActive(true);
        _optionsObject.SetActive(false);
        _savesObject.SetActive(false);
        _chaptersObject.SetActive(false);
        _backButtonObject.SetActive(false);
        Time.timeScale = 1f;
        FMODSceneManager.Singleton.StopAll();
        FMODSceneManager.Singleton.SetAmbientEvents(new List<FMODSceneManager.AmbientEvent>() { FMODSceneManager.AmbientEvent.Wind });
    }

    protected override void OnStartComplete()
    {
        Machine.Jump(MainMenuFsmState.Home);
    }
    
    public override void OnUpdate()
    {
        base.OnUpdate();

        if (Machine.IsInState(MainMenuFsmState.Home))
        {
            if (NeedToSelect(_homeObject)) _homeObject.transform.Find("Buttons").Find("Play").GetComponent<Button>().Select();
        }
        
        if (Machine.IsInState(MainMenuFsmState.Saves))
        {
            if (NeedToSelect(_savesObject)) _savesObject.transform.Find("Buttons").Find("SaveSlot1").GetComponent<Button>().Select();
        }
        
        if (Machine.IsInState(MainMenuFsmState.NewGame))
        {
            if (NeedToSelect(_savesObject)) _newGameObject.transform.Find("Buttons").Find("Back").GetComponent<Button>().Select();
        }
        
    }

    private void OnEnable()
    {
        SettingsMenu.OnSettingsMenuClosed += OnBackClicked;

    }

    private void OnDisable()
    {
        SettingsMenu.OnSettingsMenuClosed -= OnBackClicked;
    }
}
