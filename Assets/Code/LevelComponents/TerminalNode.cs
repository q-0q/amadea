using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using Code.LevelComponents;
using FMOD.Studio;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Splines;
using UnityEngine.UI;
using Util = Code.Misc.Util;

public class TerminalNode : MonoBehaviour
{
    public string metaName;
    public float cameraSplineFollowDurationMultiplier = 1.0f;
    [FormerlySerializedAs("cameraSpline")] public bool intakeCutscene = false;
    
    private SplineContainer _visualSplineContainer;
    private SplineContainer _intakeVisualSplineContainer;
    private SplineContainer _intakeCameraSplineContainer;
    private Material _intakeVisualSplineMaterial;
    private Interactable _interactable;
    private DialogueController _dialogueController;
    private Material _visualSplineMaterial;
    public Renderer _arc1Renderer;

    
    private CinemachineVirtualCamera _intakeCamera;
    private Transform _intakeCameraLookAt;
    private Transform _intakeCameraFollow;
    
    private Transform _nodeTransform;

    private bool _waitingForIntakeCamera;

    public float IntakeCameraSplineDistance = 40f;

    public float mapSplineTMin = 0f;
    public float mapSplineTMax = 1f;

    private bool _isActive;
    public Canvas _mainCanvas;
    public Image _interactableImage;
    public TextMeshProUGUI _idTmp;

    private const string intakeEventPath = "event:/TerminalIntake";
    private const string accessEventPath = "event:/TerminalAccess";
    private const string ambientAEventPath = "event:/TerminalAmbientA";
    private const string ambientBEventPath = "event:/TerminalAmbientB";

    private EventInstance ambientAEventInstance;
    private EventInstance ambientBEventInstance;
    

    private void Awake()
    {
        _visualSplineContainer = transform.Find("VisualSpline").GetComponent<SplineContainer>();
        _intakeCameraSplineContainer = transform.Find("IntakeCameraSpline").GetComponent<SplineContainer>();

        _intakeCameraLookAt = transform.Find("IntakeCameraLookAt");
        _intakeCameraFollow = transform.Find("IntakeCameraFollow");
        _intakeCameraFollow.position = _intakeCameraSplineContainer.EvaluatePosition(0);
        _intakeCameraLookAt.position = _visualSplineContainer.EvaluatePosition(0);

        _visualSplineMaterial = _visualSplineContainer.GetComponent<MeshRenderer>().material;
        _interactable = transform.Find("NodeRedux").Find("Interactable").GetComponent<Interactable>();
        _dialogueController = GetComponentInChildren<DialogueController>();
        
        _intakeCamera = transform.Find("TerminalIntakeCamera").GetComponent<CinemachineVirtualCamera>();
        _intakeCamera.Priority = -20;

        _nodeTransform = transform.Find("NodeRedux");
        
        _visualSplineMaterial.SetFloat("_FillWeight", 0f);
        _waitingForIntakeCamera = false;
        _isActive = false;


        ConfigureDialogueController();
        _mainCanvas.transform.localScale = new Vector3(1f, 0f, 1f);
        
        OnSaveDataUpdated(SaveSystem.LoadCachedSaveData());
        _idTmp.text = GlyphController.TerminalRegistry[metaName].displayId;
        
        
        
        
        

    }
    

    private void ConfigureDialogueController()
    {
        var count = 0;
        foreach (var terminal in SaveSystem.LoadCachedSaveData().terminalNodes)
        {
            var data = GlyphController.TerminalRegistry[terminal];
            if (data.contributeToCount) count++;
        }
        var countString = count == 1 ? "There is now <color=red>1</color> Terminal" : "There are now <color=red>" + count + "</color> Terminals";

        var lore = GlyphController.TerminalRegistry[metaName].loreDialogue;

        
        var primaryTexts = new List<string>()
        {
            metaName == "tutorial-0" ? "Terminal online. Leyline signal successfully bootstrapped from source.": "Terminal online. Leyline signal has been extended.", 
            countString + " in the network."
        };
        
        if (GlyphController.TerminalRegistry[metaName].isEndCap) primaryTexts.Add("This leyline branch is saturated.");
        
        primaryTexts.Add(lore.Count == 0 ? "No additional data has been logged at this station." : "There are additional logs available on this station.");
        
        
        var secondaryTexts = new List<string>()
        {
            lore.Count == 0
                ? "Terminal is online. No additional data has been logged at this station. Exiting."
                : "Accessing Terminal logs..."
        };

        foreach (var l in lore)
        {
            secondaryTexts.Add("<color=red>"  + l + "</color>");
        }
        
        if (lore.Count != 0) secondaryTexts.Add("This is all of the available logs. Exiting.");


        _dialogueController.dialogues = new List<Dialogue>()
        {
            new Dialogue()
            {
                advanceDialogueIndex = true,
                texts = primaryTexts
            },

            new Dialogue()
            {
                advanceDialogueIndex = false,
                texts = secondaryTexts
            },
        };
    }

    private void OnEnable()
    {
        _interactable.OnInteracted += OnInteracted;
        _dialogueController.OnCompleted += OnDialogueCompleted;
        SaveSystem.OnSaveDataUpdated += OnSaveDataUpdated;
        GlyphController.TerminalsInScene.Add(this);
        
        ambientAEventInstance = FMODUnity.RuntimeManager.CreateInstance(ambientAEventPath);
        FMODUnity.RuntimeManager.AttachInstanceToGameObject(ambientAEventInstance, _mainCanvas.gameObject);
        ambientAEventInstance.start();

        ambientBEventInstance = FMODUnity.RuntimeManager.CreateInstance(ambientBEventPath);
        FMODUnity.RuntimeManager.AttachInstanceToGameObject(ambientBEventInstance, _mainCanvas.gameObject);
    }

    private void OnDialogueCompleted()
    {
        IEnumerator Coroutine()
        {
            var t = 0f;
            var d = 0.4f;
            while (t < d)
            {
                var w = Util.SmoothLerp01(t / d);
                t += Time.deltaTime;
                _mainCanvas.transform.localScale = Vector3.Lerp(_mainCanvas.transform.localScale, new Vector3(1f, 0f, 1f), Time.deltaTime * 9f);
                yield return null;
            }
            
            _mainCanvas.transform.localScale = new Vector3(1f, 0f, 1f);
            MakeCompleted();
        }
        StartCoroutine(Coroutine());
    }

    private void OnSaveDataUpdated(SaveSystem.SaveData data)
    {
        if (data.terminalNodes.Contains(metaName))
        {
            MakeCompleted();
        }
        else
        {
            MakeReady();
        }
    }


    private void OnDisable()
    {
        _interactable.OnInteracted -= OnInteracted;
        _dialogueController.OnCompleted -= OnDialogueCompleted;
        SaveSystem.OnSaveDataUpdated -= OnSaveDataUpdated;
        GlyphController.TerminalsInScene.Remove(this);

        ambientAEventInstance.stop(STOP_MODE.ALLOWFADEOUT);
        ambientBEventInstance.stop(STOP_MODE.ALLOWFADEOUT);
    }

    void Start()
    {
     
        if (SaveSystem.GetTerminalNode(metaName)) ambientBEventInstance.start();
        
        var curveLength = _visualSplineContainer.Spline.GetLength();
        _visualSplineMaterial.SetFloat("_SplineLength", curveLength);
        
        var startingT = (curveLength - IntakeCameraSplineDistance) / curveLength;

        
        if (intakeCutscene)
        {

            var found = false;
            foreach (var terminal in GlyphController.TerminalsInScene)
            {
                if (terminal.metaName == GlyphController.TerminalRegistry[metaName].previousNode)
                {
                    found = true;
                    _intakeVisualSplineContainer = terminal.GetVisualSplineContainer(out _intakeVisualSplineMaterial);
                    break;
                }
            }

            if (!found) intakeCutscene = false;
            else
            {
                _intakeCameraLookAt.position = _intakeVisualSplineContainer.transform.TransformPoint(_intakeVisualSplineContainer.Spline.EvaluatePosition(startingT));
                _intakeCameraFollow.position = _intakeCameraSplineContainer.transform.TransformPoint(_intakeCameraSplineContainer.Spline.EvaluatePosition(0));
            }
        }
    }
    
    void OnInteracted()
    {
        PlayerFsm.Singleton.Machine.Jump(PlayerFsm.PlayerFsmState.WalkToTerminalNodePosition);
        PlayerFsm.Singleton.walkToPositionTarget = _interactable.transform.position;
        PlayerFsm.Singleton.walkToPositionArrivalDistanceModifier = _interactable.arrivalDistanceModifier;
        
        
        var isNew = !SaveSystem.LoadCachedSaveData().terminalNodes.Contains(metaName);
        StartCoroutine(MainCoroutine());
        

        


        IEnumerator MainCoroutine()
        {
            
            // Wait for player to be in animation
            while (PlayerFsm.Singleton.Machine.IsInState(PlayerFsm.PlayerFsmState.WalkToTerminalNodePosition))
                yield return null;
            
            float t;
            float d;

            SaveSystem.WritePlayerInGamePosition(transform.position, "", transform.rotation.eulerAngles.y);
            
            if (isNew)
            {
                if (intakeCutscene)
                {
                    _waitingForIntakeCamera = true;
                    StartCoroutine(IntakeCameraCoroutine());
                }
                while (_waitingForIntakeCamera) yield return null;
                
                yield return new WaitForSeconds(0.5f);
                
                FMODUnity.RuntimeManager.PlayOneShotAttached(FMODUnity.RuntimeManager.PathToEventReference("event:/TerminalActivate"), _mainCanvas.gameObject);

                // _visualSplineMaterial.SetFloat("_FillWeight", 1f);
                // _arc1Renderer.material.SetFloat("_GlowWeight", 1f);
            }
            
            
            
            t = 0;
            d = 0.5f;
            var current = metaName;
            while (current != "")
            {
                SaveSystem.WriteTerminalNode(current);
                var writeAdditionalKey = GlyphController.TerminalRegistry[current].writeAdditionalKey;
                if (writeAdditionalKey != "") SaveSystem.WriteTerminalNode(writeAdditionalKey);
                current = GlyphController.TerminalRegistry[current].previousNode;
            }
            
            if (isNew) ambientBEventInstance.start();
            FMODUnity.RuntimeManager.PlayOneShotAttached(FMODUnity.RuntimeManager.PathToEventReference(accessEventPath), _mainCanvas.gameObject);
            
            ConfigureDialogueController();
            while (t < d)
            {
                var w = Util.SmoothLerp01(t / d);
                t += Time.deltaTime;
                _mainCanvas.transform.localScale = Vector3.Lerp(_mainCanvas.transform.localScale, new Vector3(1f, 1f, 1f), Time.deltaTime * 9f);
                yield return null;
            }
            _mainCanvas.transform.localScale = Vector3.one;

            if (isNew) yield return new WaitForSeconds(0.25f);
            
            DialogueCanvas.Singleton.StartDialogue(_dialogueController);
            PlayerFsm.Singleton.Machine.Jump(PlayerFsm.PlayerFsmState.Dialogue);
        }
        
        
        IEnumerator IntakeCameraCoroutine()
        {
            _waitingForIntakeCamera = true;
            yield return new WaitForSeconds(0.5f);
            var splineLength = _intakeVisualSplineContainer.Spline.GetLength();
            var startingT = (splineLength - IntakeCameraSplineDistance) / splineLength;
            print(startingT);
            _intakeCamera.Priority = 30;

            FMODUnity.RuntimeManager.PlayOneShotAttached(FMODUnity.RuntimeManager.PathToEventReference(intakeEventPath), _intakeCameraLookAt.gameObject);
            
            var t = 0f;
            var d = 2f;

            while (t < d)
            {
                var fillWeight = Mathf.Lerp(startingT, 1f, t / d);
                _intakeCameraLookAt.position = _intakeVisualSplineContainer.transform.TransformPoint(_intakeVisualSplineContainer.Spline.EvaluatePosition(fillWeight));
                _intakeVisualSplineMaterial.SetFloat("_FillWeight", fillWeight);
                _intakeCameraFollow.position = _intakeCameraSplineContainer.transform.TransformPoint(_intakeCameraSplineContainer.Spline.EvaluatePosition(Util.SmoothLerp01(t / d * 0.75f)));
                t += Time.deltaTime;
                yield return null;
            }
            
            yield return new WaitForSeconds(0.75f);
            yield return new WaitForSeconds(1f);
            _waitingForIntakeCamera = false;
            
            _intakeCamera.Priority = -30;
        }
    }
    
    

    public SplineContainer GetVisualSplineContainer(out Material material)
    {
        material = _visualSplineMaterial;
        return _visualSplineContainer;
    }

    private void MakeReady()
    {
        _mainCanvas.transform.localScale = new Vector3(1f, 0f, 1f);
    }
    
    private void MakeCompleted()
    {
        _arc1Renderer.material.SetFloat("_GlowWeight", 1f);
        _visualSplineMaterial.SetFloat("_FillWeight", 1f);
        // _dialogueController.currentDialogueIndex = 1;
    }
    
}
