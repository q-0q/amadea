using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using Cinemachine;
using Code.Fsm.TrialCollectibleFSM;
using Code.PlayerComponents;
using DG.Tweening;
using FMOD.Studio;
using JetBrains.Annotations;
using Unity.Mathematics;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.PlayerLoop;
using UnityEngine.Serialization;
using Wasp;
using Util = Code.Misc.Util;

public partial class PlayerFsm : GravityFsm
{
    public class PlayerFsmState : GravityFsmState
    {
        public static int Idle;
        public static int IdleLong;
        public static int IdleLongLoop;
        public static int StepStart;
        public static int StepEnd;
        
        public static int GroundMove;
        public static int Jumpsquat;
        public static int Landsquat;
        public static int Jump;
        public static int Fall;
        public static int HardTurn;
        public static int Vault;
        public static int Wallstep;
        public static int Wallsquat;
        public static int ForceWallRotation;
        public static int SlowVaultHang;
        public static int MediumVaultHang;
        public static int SlowVaultFinish;
        public static int Dashsquat;
        public static int Grapple;
        public static int HardLand;
        public static int HardLandRoll;
        public static int Wallrun;
        public static int WallrunJumpsquat;
        public static int ImpaleGround;
        public static int ImpaleAir;
        public static int GrappleStartup;
        public static int GrappleFlipsquat;
        public static int GrappleFlip;
        public static int VaultHang;
        public static int LockMomentum;
        public static int WallInteractable;
        public static int Landable;
        public static int AirControl;
        public static int WalkToPosition;
        public static int WalkToSwitchPosition;
        public static int InteractWithSwitch; // interactpull
        public static int InteractPullRecovery;
        public static int Dash;
        public static int FallAfterDash;
        public static int Skipsquat;
        public static int Skip;
        public static int LandsquatAfterDash;
        public static int DashVault;
        public static int Dialogue;
        public static int WalkToDialoguePosition;
        public static int Interactable;
        public static int Climb;
        public static int PitonHoming;
        public static int Pitonsquat;
        public static int PitonFlipsquat;
        public static int PitonFlip;
        public static int PitonInteractable;
        public static int FallAfterPitonHoming;

        public static int Slide;
        public static int SlideLateral;
        public static int SlideDown;
        public static int SlideInteractable;
        
        public static int TightropeMove;
        public static int GroundMoveAfterVault;

        public static int CutsceneWary;
        public static int CutsceneIdle;
        public static int Updraft;

        public static int TrialTeleport;
        public static int Dying;
        public static int Dying2;
        public static int Dead;
        public static int Respawn;
        
        public static int FallAfterSlide;
        public static int FallAfterSlideLateral;

        public static int Swim;
        public static int SwimSurfaceRise;
        public static int SwimSurface;
        public static int DiveFall;
        public static int Drown;

        public static int RopeSwingInteractable;
        public static int RopeSwing;
        public static int RopeSwingHoming;
        public static int RopeSwingJumpsquat;
        public static int RopeSwingJump;
        public static int CutsceneHardLand;

        public static int SurgeStartup;
        public static int SurgeDash;
        public static int SurgeDashStartup;
        public static int LandsquatAfterSurge;
        
        public static int Press;

        public static int LongFall;
        public static int KeyItemCollect;

        public static int Inventory;
        public static int ItemSlowdown;

        public static int UseIncenseBurner;
        public static int TerminalNodeInteract;
        
        public static int MinorLeylineInteractable;
        public static int MinorLeylineStartup;
        public static int MinorLeylineActive;
        public static int WalkToTerminalNodePosition;
        public static int UseMap;
        public static int InventorySlowdown;
        public static int MapSlowdown;

        public static int Tinsica;
        public static int TinsicaUsable;
        public static int TinsicaJump;
        public static int TinsicaJumpsquat;
        
        public static int WalkToRotationDaisPosition;
        public static int RotationDaisInteract;
        public static int DisplaceFoliage;
        public static int WalkToChestPosition;
        public static int ChestInteract;
        public static int SentryImmune;
        public static int WalkToTravelPosition;
        public static int Travel;
        
    }

    public class PlayerFsmTrigger : GravityFsmTrigger
    {
        public static int Jump;
        public static int HardTurn;
        public static int NoMomentum;
        public static int FaceLedge;
        public static int FaceHighLedge;
        public static int FaceWall;
        public static int FaceWallStrict;
        public static int FaceOpen;
        public static int FlankWall;
        public static int FlankOpen;
        public static int Dash;
        public static int Attack;
        public static int ContactHitboxTrigger;
        public static int InteractWithSwitch;
        public static int ArriveAtWalkToPositionTarget;
        public static int ArriveAtWalkToPositionTargetRanged;
        public static int StartDialogue;
        public static int EndDialogue;
        
        public static int StartUpdraft;
        public static int EndUpdraft;
        
        public static int EnterPitonTrigger;


        public static int Accelerating;
        public static int IdleMomentumThresholdPassedDecelerating;

        public static int VaultHangFarFromLedge;
        
        public static int SoftTurnLeft;
        public static int SoftTurnRight;

        public static int SwimTriggerRaycastHit;
        public static int IsAboveWater;

        public static int EnterRopeSwingTrigger;
        public static int SurgePedestalInteracted;
        public static int Press;
        
        public static int IsAboveLongFall;

        public static int Inventory;
        public static int UseIncenseBurner;

        public static int MinorLeylineTrigger;
        public static int Map;
        
        public static int Trick;
        public static int PullCompleted;
    }
    
    protected override void OnAwake()
    {
        Singleton = this;
        
        
        respawnAmbientInstance =
            FMODUnity.RuntimeManager.CreateInstance(
                FMODUnity.RuntimeManager.PathToEventReference("event:/RespawnAmbient"));
        
        FMODUnity.RuntimeManager.StudioSystem.setParameterByName("RespawnAmbientAmount", 0f);
        
        SetPositionFromSaveData();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        QualitySettings.vSyncCount = 0; // Set vSyncCount to 0 so that using .targetFrameRate is enabled.
        Application.targetFrameRate = MetaSaveSystem.LoadCachedMetaSaveData().fpsCap;
        
        Shader.SetGlobalFloat("_PlayerTintWeight", 0);
        Shader.SetGlobalFloat("_PlayerEvaporateClip", 0f);
        
    }

    public void SetPositionFromSaveData()
    {
        var initialPosition = transform.position;
        var saveData = SaveSystem.LoadCachedSaveData();
        if (saveData != null )
        {

            var positionIdTransform = Util.FindGamePositionById(saveData.playerInGamePositionId, out var cameraRotationOffset);
            if (positionIdTransform != null)
            {
                transform.position = positionIdTransform.position;
                transform.rotation = positionIdTransform.rotation;
            }
            
            else if (saveData.playerInGamePosition != null)
            {
                if (saveData.playerInGamePosition.Length != 0)
                {
                    transform.position = new Vector3(saveData.playerInGamePosition[0], saveData.playerInGamePosition[1],
                        saveData.playerInGamePosition[2]);

                    transform.rotation = Quaternion.Euler(0, saveData.playerInGameYAngle, 0);
                }
            }
        }

        OnPlayerTeleported?.Invoke(transform.position - initialPosition);
    }

    protected override void OnStart()
    {
        
        base.OnStart();
        TryGetComponent(out Animator);
        Singleton = this;
        InitState = PlayerFsmState.IdleLongLoop;
        isSprinting = false;
        _isSurgeQueued = false;
        Time.timeScale = 1f;
        _timeSinceBoostStarted = 100f;
        _movementAnimationMirror = false;
        TryGetComponent(out _playerInput);
        _inputBuffer = new InputBuffer(_playerInput, 0.275f);
        _inputBuffer.InitInput("Jump");
        _inputBuffer.InitInput("Dash");
        _inputBuffer.InitInput("Attack");
        _inputBuffer.InitInput("Interact", false, true);
        _inputBuffer.InitInput("Inventory");
        _inputBuffer.InitInput("Map");
        _inputBuffer.InitInput("Trick");
        _camera = Camera.main;
        _previousWallrunSide = FlankType.None;
        _checkpointVector3 = transform.position;
        _checkpointQuaternion = transform.rotation;
        _kiIndicatorParticles = transform.Find("Armature").GetComponentsInChildren<ParticleSystem>().Where(d => d.name == "PlayerFootParticles").ToList();
        _teleportParticles = transform.Find("TeleportParticles").GetComponent<ParticleSystem>();
        _deathParticles = transform.Find("DeathParticles").GetComponent<PlayerDeathParticles>();
        _splashParticles = transform.Find("SplashParticles").GetComponent<ParticleSystem>();
        _splashParticles.transform.SetParent(null);
        _teleportParticles.transform.SetParent(null);
        _deathParticles.transform.SetParent(null);
        _teleportCamera = transform.parent.Find("PlayerTeleportCamera").GetComponent<CinemachineVirtualCamera>();
        _teleportCameraLookAt = transform.parent.Find("PlayerTeleportCameraLookAt");
        _playerDashParticles = GetComponentInChildren<PlayerDashParticles>();
        _playerDashParticles.transform.SetParent(null);
        
        _playerTrickParticles = GetComponentInChildren<PlayerTrickParticles>();
        _playerTrickParticles.transform.SetParent(null);
        _surgeStartupCamera = transform.parent.Find("SurgeStartupCamera").GetComponent<CinemachineVirtualCamera>();
        _playerSurgeHalo = GetComponentInChildren<PlayerSurgeHalo>();
        _playerSurgeHalo.transform.SetParent(null);
        _speedLinesParticles = Camera.main.transform.Find("SpeedLinesParticles").GetComponent<ParticleSystem>();
        _speedLinesParticlesTransform = _speedLinesParticles.transform;
        _speedLinesParticlesTransform.SetParent(null);
        _minorLeylineHalo = GetComponentInChildren<PlayerMinorLeylineHalo>();
        _minorLeylineHalo.transform.SetParent(null);
        
        ApplyMetaSaveData(MetaSaveSystem.LoadCachedMetaSaveData());
        // transform.Find("KiIndicatorParticles").SetParent(null);
        _renderers = GetComponentsInChildren<Renderer>().ToList();
        _skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        _material = GetComponentInChildren<SkinnedMeshRenderer>().material;
        _impulse = GetComponentInChildren<CinemachineImpulseSource>();
        
        UpdateMusicDistanceAttenuation(true);



        SaveSystem.UpdateScreenshot(0.3f);
        SnapToGround();
        StartCoroutine(SurgeTrailCoroutine());
    }
    
    protected override void OnStartComplete()
    {
        base.OnStartComplete();
        
        ReplaceAnimatorTrigger("IdleLongLoop");
        Animator.SetLayerWeight(1, 0f);
    }


    public override void OnUpdate()
    {
        if (HitstopOnUpdate()) return;
        
        


        var isCutscenePlayerDisabled = CutsceneManager.Singleton.IsCutscenePlayerDisabled();
        _inputBuffer.OnUpdate(isCutscenePlayerDisabled);
        
        OnPlayerMomentumUpdated?.Invoke(_momentum);
        OnPlayerPositionUpdated?.Invoke(transform.position, Machine.IsInState(GravityFsmState.Grounded) ||
                                                            Machine.IsInState(PlayerFsmState.ForceWallRotation) ||
                                                            YVelocity < -6f);
        _timeSinceDashFinished += Time.deltaTime;
        _comboTimer += Time.deltaTime;
        _timeSinceLastFootstep += Time.deltaTime;
        _timeSinceRopeSwing += Time.deltaTime;

        _timeSinceBoostStarted += Time.deltaTime;
        _timeSinceMinorLeyline += Time.deltaTime;
        _timeSinceMinorLeylineUp += Time.deltaTime;
        _timeSinceRespawn += Time.deltaTime;
        
        if (_comboTimer > ComboTimeoutDuration)
        {
            ResetCombo();
        }
        
        FMODUnity.RuntimeManager.StudioSystem.setParameterByName("PlayerMomentum", ComputeMomentumWeight());
        UpdateShaderGlobals();
        HandleSlideTimer();
        UpdateMusicDistanceAttenuation();
        UpdateFmodWindRushAmount();
        HandleSpeedLines();
        HandleSteepAnimationLayers();
        HandleSurge();
        
        
        // aerialMomentumOffset = Machine.IsInState(PlayerFsmState.Fall)
        //     ? transform.forward * Mathf.Lerp(0, 1.5f, ComputeMomentumWeight())
        //     : Vector3.zero;


        var previousPosition = transform.position;


        if (Machine.IsInState(PlayerFsmState.Idle))
        {
            IdleOnUpdate();
        }

        if (Machine.IsInState(PlayerFsmState.GroundMove))
        {
            GroundMoveOnUpdate();
        }

        if ((Machine.IsInState(PlayerFsmState.Grounded)) && !Machine.IsInState(PlayerFsmState.Wallrun) && !Machine.IsInState(PlayerFsmState.WallrunJumpsquat))
        {
            if (parentTransform != null)
            {
                parentTransform.TryGetComponent(out PlayerDeathGround deathGround);
                if (deathGround != null) InvokePlayerDeath();
            }
        }
        
        if (Machine.IsInState(PlayerFsmState.StepStart))
        {
            StepStartOnUpdate();
        }
        
        if (Machine.IsInState(PlayerFsmState.StepEnd))
        {
            StepEndOnUpdate();
        }
        
        if (Machine.IsInState(PlayerFsmState.TightropeMove))
        {
            TightropeMoveOnUpdate();
        }
        
        if (Machine.IsInState(PlayerFsmState.Jump))
        {
            JumpOnUpdate();
        }
        

        if (Machine.IsInState(PlayerFsmState.VaultHang))
        {
            VaultHangOnUpdate();
        }
        if (Machine.IsInState(PlayerFsmState.SlowVaultFinish))
        {
            SlowVaultFinishOnUpdate();
        }
        
        if (Machine.IsInState(PlayerFsmState.Vault))
        {
            VaultOnUpdate();
        }
        
        if (Machine.IsInState(PlayerFsmState.Wallsquat))
        {
            WallsquatOnUpdate();
        }
        
        if (Machine.IsInState(PlayerFsmState.Wallrun))
        {
            WallrunOnUpdate();
        }
        
        if (Machine.IsInState(PlayerFsmState.LockMomentum))
        {
            LockMomentumOnUpdate();
        }

        if (Machine.IsInState(GravityFsmState.Aerial))
        {
            AerialOnUpdate();
        }
        
        if (Machine.IsInState(PlayerFsmState.AirControl))
        {
            AirControlOnUpdate();
        }
        
        if (Machine.IsInState(PlayerFsmState.HardTurn))
        {
            HardTurnOnUpdate();
        }
        
        if (Machine.IsInState(PlayerFsmState.HardLand))
        {
            HardLandOnUpdate();
        }
        
        if (Machine.IsInState(PlayerFsmState.HardLandRoll))
        {
            HardLandRollOnUpdate();
        }
        
        if (Machine.IsInState(PlayerFsmState.ForceWallRotation))
        {
            ForceWallRotationOnUpdate();
        }

        if (Machine.IsInState(PlayerFsmState.Dashsquat))
        {
            DashsquatOnUpdate();
        }
        if (Machine.IsInState(PlayerFsmState.Dash))
        {
            DashOnUpdate();
        }
        if (Machine.IsInState(PlayerFsmState.Grapple))
        {
            GrappleOnUpdate();
        }
        
        if (Machine.IsInState(PlayerFsmState.ImpaleGround))
        {
            ImpaleGroundOnUpdate();
        } 
        else if (Machine.IsInState(PlayerFsmState.ImpaleAir))
        {
            ImpaleAirOnUpdate();
        } else
        {
            Animator.SetLayerWeight(2, Mathf.Lerp(Animator.GetLayerWeight(2), 0, Time.deltaTime * 10f));
        }

        if (Machine.IsInState(PlayerFsmState.GrappleStartup))
        {
            GrappleStartupOnUpdate();
        }
        
        if (Machine.IsInState(PlayerFsmState.WalkToPosition))
        {
            WalkToPositionOnUpdate();
        }
        
        if (Machine.IsInState(PlayerFsmState.InteractWithSwitch))
        {
            InteractWithSwitchOnUpdate();
        }
        
        if (Machine.IsInState(PlayerFsmState.FallAfterDash))
        {
            FallAfterDashOnUpdate();
        }
        
        if (Machine.IsInState(PlayerFsmState.Skipsquat))
        {
            SkipsquatOnUpdate();
        }
        
        if (Machine.IsInState(PlayerFsmState.Skip))
        {
            SkipOnUpdate();
        }

        if (Machine.IsInState(PlayerFsmState.Interactable) && !isCutscenePlayerDisabled)
        {
            InteractableOnUpdate();
        }
        else
        {
            currentPotentialInteractable = null;
        }
        
        if (Machine.IsInState(PlayerFsmState.Dialogue))
        {
            DialogueOnUpdate();
        }
        
        if (Machine.IsInState(PlayerFsmState.SlideLateral))
        {
            SlideLateralOnUpdate();
        }
        
        if (Machine.IsInState(PlayerFsmState.SlideDown))
        {
            SlideDownOnUpdate();
        }
        
        if (Machine.IsInState(PlayerFsmState.Slide))
        {
            SlideOnUpdate();
        }
        
        if (Machine.IsInState(PlayerFsmState.FallAfterSlideLateral))
        {
            FallAfterSlideLateralOnUpdate();
        }
        
        if (Machine.IsInState(PlayerFsmState.Updraft))
        {
            UpdraftOnUpdate();
        }

        if (Machine.IsInState(PlayerFsmState.TrialTeleport))
        {
            TrialTeleportOnUpdate();
        }

        if (Machine.IsInState(PlayerFsmState.Climb))
        {
            ClimbOnUpdate();
        }

        if (Machine.IsInState(PlayerFsmState.PitonHoming))
        {
            PitonHomingOnUpdate();
        }

        if (Machine.IsInState(PlayerFsmState.Pitonsquat))
        {
            PitonsquatOnUpdate();
        }
        
        if (Machine.IsInState(PlayerFsmState.Dying))
        {
            DyingOnUpdate();
        }
        
        if (Machine.IsInState(PlayerFsmState.Respawn))
        {
            RespawnOnUpdate();
        }
        
        if (Machine.IsInState(PlayerFsmState.Swim))
        {
            SwimOnUpdate();
        }
        else if ((!Machine.IsInState(PlayerFsmState.Dying) && !Machine.IsInState(PlayerFsmState.Dead) &&
                  !Machine.IsInState(PlayerFsmState.Wallsquat) && Machine.IsInState(GravityFsmState.Grounded) &&
                  !Machine.IsInState(PlayerFsm.PlayerFsmState.Jumpsquat)) || Machine.IsInState(PlayerFsmState.VaultHang))
        {
            freezeFmodInstance.stop(STOP_MODE.ALLOWFADEOUT);
            _freezeTimer = 0;
        }
        
        if (Machine.IsInState(PlayerFsmState.SwimSurfaceRise))
        {
            SwimSurfaceRiseOnUpdate();
        }
        
        if (Machine.IsInState(PlayerFsmState.SwimSurface))
        {
            SwimSurfaceOnUpdate();
        }
        
        if (Machine.IsInState(PlayerFsmState.Drown))
        {
            DrownOnUpdate();
        }
        
        if (Machine.IsInState(PlayerFsmState.RopeSwingHoming))
        {
            RopeSwingHomingOnUpdate();
        }

        if (Machine.IsInState(PlayerFsmState.RopeSwing))
        {
            RopeSwingOnUpdate();
        }

        if (Machine.IsInState(PlayerFsmState.RopeSwingJumpsquat))
        {
            RopeSwingJumpsquatOnUpdate();
        }

        if (Machine.IsInState(PlayerFsmState.SurgeStartup))
        {
            SurgeStartupOnUpdate();
        }
        
        if (Machine.IsInState(PlayerFsmState.SurgeDash))
        {
            SurgeDashOnUpdate();
        }

        if (Machine.IsInState(PlayerFsmState.Press))
        {
            PressOnUpdate();
        }
        
        if (Machine.IsInState(PlayerFsmState.ItemSlowdown))
        {
            ItemSlowdownOnUpdate();
        }

        if (Machine.IsInState(PlayerFsmState.MinorLeylineStartup))
        {
            MinorLeylineStartupOnUpdate();
        }
        
        if (Machine.IsInState(PlayerFsmState.MinorLeylineActive))
        {
            MinorLeylineActiveOnUpdate();
        }
        
        if (Machine.IsInState(PlayerFsmState.Tinsica))
        {
            TinsicaOnUpdate();
        }
        
        if (Machine.IsInState(PlayerFsmState.TinsicaJump))
        {
            TinsicaJumpOnUpdate();
        }

        
        if (_playerInput.actions["Reset"].WasPerformedThisFrame())
        {
            InvokePlayerDeath();
        }
        
        if (Input.GetKeyDown(KeyCode.X))
        {
            SaveSystem.WritePlayerInGamePosition(transform.position, "", transform.rotation.eulerAngles.y);
        }
        
        HandleRaycastKill();

        

        // HandleSlopeTimer();
         HandleKiEffects();
        
        base.OnUpdate();
        
        _previousPositionDeltaNoTimescale = (transform.position - previousPosition) / Time.deltaTime;
    }

    private bool HitstopOnUpdate()
    {
        if (HitstopManager.Singleton.IsHitstopActive())
        {
            Animator.enabled = false;
            return true;
        }
        else
        {
            Animator.enabled = true;
        }

        return false;
    }
    
    private void OnStateChangedCompleted(TriggerParams obj)
    {
        // print(InheritableEnum.GetFieldNameByValue(Machine.State(), typeof(PlayerFsmState)));
        var trigger = StateMapConfig.AnimationTrigger.GetStrict(this);
        ReplaceAnimatorTrigger(trigger);
        if (trigger == "GroundMove") Animator.SetTrigger("GroundMoveSteep");
        // this is a workaround for what i believe is a bug inside unity animator controllers where
        // two different layers reacting to the same trigger can mess eachother up
        // so i have broken ground move into a second distinct trigger for the steepness layer
        // to prevent this from happening
    }

    private void OnEnable()
    {
        PlayerContactCollider.OnPlayerContactHitboxCollision += OnContactHitboxCollide;
        MetaSaveSystem.OnMetaSaveDataUpdated += ApplyMetaSaveData;
        PlayerFootTracker.OnPlayerFootstep += OnPlayerFootstep;
        CultTrialBoost.OnCultTrialBoostTrigger += OnCultTrialBoostTrigger;
        IntroCutsceneFsm.OnIntroCutsceneWarp += OnIntroCutsceneWarp;
        
        activeFmodInstance = FMODUnity.RuntimeManager.CreateInstance(comboActiveFmodEvent);
        slideFmodInstance = FMODUnity.RuntimeManager.CreateInstance(slideFmodEvent);
        surgeStartupFmodInstance = FMODUnity.RuntimeManager.CreateInstance(surgeStartupFmodEvent);
        
        FMODUnity.RuntimeManager.AttachInstanceToGameObject(slideFmodInstance, gameObject);
        
        slipAmbientFmodInstance = FMODUnity.RuntimeManager.CreateInstance(slipAmbientEvent);
        FMODUnity.RuntimeManager.AttachInstanceToGameObject(slipAmbientFmodInstance, gameObject);
        slipAmbientFmodInstance.start();

        windRushFmodInstance = FMODUnity.RuntimeManager.CreateInstance(windRushFmodEvent);
        windRushFmodInstance.start();
        
        freezeFmodInstance = FMODUnity.RuntimeManager.CreateInstance(freezeFmodEvent);

        FMODUnity.RuntimeManager.AttachInstanceToGameObject(respawnAmbientInstance, gameObject);
        respawnAmbientInstance.start();
    }
    
    private void OnDisable()
    {
        PlayerContactCollider.OnPlayerContactHitboxCollision -= OnContactHitboxCollide;
        MetaSaveSystem.OnMetaSaveDataUpdated -= ApplyMetaSaveData;
        PlayerFootTracker.OnPlayerFootstep -= OnPlayerFootstep;
        CultTrialBoost.OnCultTrialBoostTrigger -= OnCultTrialBoostTrigger;
        IntroCutsceneFsm.OnIntroCutsceneWarp -= OnIntroCutsceneWarp;
        
        activeFmodInstance.stop(STOP_MODE.ALLOWFADEOUT);
        slideFmodInstance.stop(STOP_MODE.ALLOWFADEOUT);
        slipAmbientFmodInstance.stop(STOP_MODE.ALLOWFADEOUT);
        windRushFmodInstance.stop(STOP_MODE.ALLOWFADEOUT);
        respawnAmbientInstance.stop(STOP_MODE.ALLOWFADEOUT);
    }
}