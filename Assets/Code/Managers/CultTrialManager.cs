using System;
using System.Collections;
using Code.Misc;
using DG.Tweening;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class CultTrialManager : MonoBehaviour
{
    public static CultTrialManager Singleton;
    
        
   [NonSerialized]
    public DialogueController dialogueNoItem;
   [NonSerialized]
    public DialogueController dialogueItem;
   [NonSerialized]
    public DialogueController dialogueFirstTimeUse1;
   [NonSerialized]
    public DialogueController dialogueFirstTimeUse2;
   [NonSerialized]
    public DialogueController dialogueFirstTimeUse3;
   [NonSerialized]
    public DialogueController dialogueFirstTimeUse4;

    private const string FirstTimeUsePersistentEvent = "CultTrialUsed";

    private Material _curseHaloMaterial;
    private Transform _curseHalo;
    private Image _curseCanvasImage;
    private Material _curseHudTextMaterial;

    private CanvasGroup _markHudCanvasGroup;
    public bool isCurseEnabled;

    public bool isCurseTicking;
    private float _curseDuration;
    private CustomFogController _activeFogController;
    private CustomFogController _curseFogController;
    private TextMeshProUGUI _markHudTmp;
    private TextMeshProUGUI _timerTmp;
    
    private const float CurseMaximumDuration = 6f;

    private Color _activeFogControllerBaseColor;
    private float _activeFogControllerDepthMin;
    private float _activeFogControllerDepthMax;

    public static event Action<CultTrialFsm> OnTrialActive;
    public static event Action<CultTrialFsm> OnCurseApplied;
    public static event Action OnCurseRemoved;

    private Transform _activeHalo;
    private Material _activeHaloMaterial;
    
    private bool _timerTicking;
    private float _timer;

    private void OnEnable()
    {
        PlayerFsm.OnPlayerCultTrialDeath += OnPlayerCultTrialDeath;
    }
    
    private void OnDisable()
    {
        PlayerFsm.OnPlayerCultTrialDeath -= OnPlayerCultTrialDeath;
    }

    private void Awake()
    {
        Singleton = this;
        _curseHalo = transform.Find("CurseHalo");
        _curseHaloMaterial = _curseHalo.GetComponent<Renderer>().material;
        _curseCanvasImage = transform.Find("CurseCanvas").GetComponentInChildren<Image>();
        _markHudCanvasGroup = transform.Find("Canvas").Find("MarkHud").GetComponent<CanvasGroup>();
        _markHudTmp = _markHudCanvasGroup.transform.Find("Text").GetComponent<TextMeshProUGUI>();
        _timerTmp = _markHudCanvasGroup.transform.Find("Timer").GetComponent<TextMeshProUGUI>();
        
        _curseHudTextMaterial = _markHudTmp.fontMaterial;
        _activeFogController = transform.Find("ActiveFogController")
            .GetComponent<CustomFogController>();
        _activeFogControllerBaseColor = _activeFogController.Color;
        _activeFogControllerDepthMin = _activeFogController.DepthMin;
        _activeFogControllerDepthMax = _activeFogController.DepthMax;
        
        _curseFogController = transform.Find("CurseFogController")
            .GetComponent<CustomFogController>();

        _activeHalo = transform.Find("ActiveHalo");
        _activeHaloMaterial = _activeHalo.GetComponent<Renderer>().material;

        dialogueNoItem = transform.Find("DialogueNoItem").GetComponent<DialogueController>();
        dialogueItem = transform.Find("DialogueItem").GetComponent<DialogueController>();
        dialogueFirstTimeUse1 = transform.Find("DialogueFirstTimeUse1").GetComponent<DialogueController>();
        dialogueFirstTimeUse2 = transform.Find("DialogueFirstTimeUse2").GetComponent<DialogueController>();
        dialogueFirstTimeUse3 = transform.Find("DialogueFirstTimeUse3").GetComponent<DialogueController>();
        dialogueFirstTimeUse4 = transform.Find("DialogueFirstTimeUse4").GetComponent<DialogueController>();
        
        DisableCurse();
    }

    private void Update()
    {

        _activeFogController.transform.position = PlayerFsm.Singleton.transform.position;
        _curseFogController.transform.position = PlayerFsm.Singleton.transform.position;
        
        if (_timerTicking)
        {
            _timer += Time.deltaTime;
            _timerTmp.text = _timer.ToString("F2");
        }
        
        _activeHalo.position = Camera.main.transform.position;
        _activeFogController.transform.position = PlayerFsm.Singleton.transform.position;
        _curseHudTextMaterial.SetFloat("_BarGlowMultiply",Mathf.Lerp(_curseHudTextMaterial.GetFloat("_BarGlowMultiply"), isCurseTicking ? 2.5f : 0f, Time.deltaTime * 4f));
        _activeFogController.LerpStrengthMultiplier = isCurseTicking ? 1f : 100f;
        if (!isCurseTicking)
        {
            _activeFogController.LerpStrengthMultiplier = 1f;
            _activeFogController.Color = _activeFogControllerBaseColor;
            _activeFogController.DepthMin = _activeFogControllerDepthMin;
            _activeFogController.DepthMax = _activeFogControllerDepthMax;
            return;
        };
        
        _curseDuration -= Time.deltaTime;
        var w = _curseDuration / CurseMaximumDuration;
        _curseHudTextMaterial.SetFloat("_BarAmount", w);
        _activeFogController.LerpStrengthMultiplier = 20f;
        _activeFogController.Color = Color.Lerp(_activeFogControllerBaseColor, Color.black, (1f - w));

        var depthOffset = Mathf.Lerp(0, -50f, 1f - w);
        // _activeFogController.DepthMin = _activeFogControllerDepthMin;
        _activeFogController.DepthMax = _activeFogControllerDepthMax + depthOffset;
        _activeHaloMaterial.SetFloat("_Alpha", Mathf.Lerp(0f,1f,1f-w));

        
        
        if (w < 0) OnCurseExpire();
    }

    private void OnCurseExpire()
    {
        PlayerFsm.Singleton.InvokePlayerDeath();
    }

    public void SetCurseEffects(float timeInCurrentState, bool fov = true)
    {
        _curseHalo.position = PlayerFsm.Singleton.transform.position + Vector3.up * 2f;
        _curseCanvasImage.transform.parent.position = _curseHalo.position;

        if (fov) {
            var strength = 2f;
            var freeLook = PlayerCinemachineFreeLook.Singleton.GetFreeLook();
            freeLook.m_Lens.FieldOfView =
                Mathf.Lerp(freeLook.m_Lens.FieldOfView, 55f, Time.deltaTime * strength);

            var offset = freeLook.transform.GetComponent<CinemachineCameraOffset>();
            offset.m_Offset = Vector3.Lerp(offset.m_Offset,
                new Vector3(0, 0, -7f), Time.deltaTime * strength);
        }
        
        _curseHalo.localScale = Vector3.one * Mathf.Lerp(10f, 2f, Mathf.InverseLerp(0, 2.5f, timeInCurrentState));
        var w = Mathf.InverseLerp(0, 2f, timeInCurrentState);
        _curseHaloMaterial.SetFloat("_Multiply", w * Mathf.InverseLerp(2.5f, 2f, timeInCurrentState));
        _curseHaloMaterial.SetFloat("_Radius", Mathf.Lerp(0.1f, 1f, w));

        _curseCanvasImage.transform.localScale = Vector3.one * Mathf.Lerp(1.5f, 0.5f, Mathf.InverseLerp(1.5f, 3.5f, timeInCurrentState));
        _curseCanvasImage.transform.Rotate(new Vector3(0, 0, Time.deltaTime * 100f));
        var c = _curseCanvasImage.color;
        c.a = Mathf.InverseLerp(0f, 1.5f, timeInCurrentState) * Mathf.InverseLerp(2.25f, 1.5f, timeInCurrentState);
        _curseCanvasImage.color = c;
    }

    public void ReplenishCurseDuration(bool sphereEffect = true)
    {
        _curseDuration = CurseMaximumDuration;
        _curseHudTextMaterial.SetFloat("_BarAmount", 1f);
        _activeHaloMaterial.SetFloat("_Alpha", 0f);
        if (sphereEffect) Util.InvokeSphereEffect(PlayerFsm.Singleton.transform.position + Vector3.up, Vector3.one * 5f, 1.5f, 1f, 0 );
        HudTmpPunchPosition();
    }

    private void HudTmpPunchPosition()
    {
        _markHudTmp.transform.parent.DOComplete();
        _markHudTmp.transform.parent.DOPunchPosition(Vector3.right * 10f, 0.5f, 10, 1f);

        IEnumerator PanelGlowCoroutine()
        {
            var t = 0f;
            var d = 0.15f;
            while (t < d)
            {
                var w = Util.SmoothLerp01(t / d);
                t += Time.deltaTime;
                
                yield return null;
            }
        }
    }

    public void EnableActiveFog()
    {
        _activeFogController.Priority = 10;
    }
    
    public void DisableActiveFog()
    {
        _activeFogController.Priority = -10;
    }

    public void EnableCurse(CultTrialFsm fsm)
    {
        _curseHudTextMaterial.SetFloat("_BarGlowMultiply", 0f);
        _markHudCanvasGroup.alpha = 1f;
        _curseDuration = CurseMaximumDuration;
        isCurseEnabled = true;
        HudTmpPunchPosition();
        OnCurseApplied?.Invoke(fsm);
    }
    
    public void DisableCurse()
    {
        _markHudCanvasGroup.alpha = 0f;
        isCurseEnabled = false;
        isCurseTicking = false;
        _activeHaloMaterial.SetFloat("_Alpha", 0);
        _activeFogController.Color = _activeFogControllerBaseColor;
        _activeFogController.DepthMax = _activeFogControllerDepthMax;
        OnCurseRemoved?.Invoke();
    }

    public void StartCurseTicking(CultTrialFsm fsm)
    {
        isCurseTicking = true;
        OnTrialActive?.Invoke(fsm);
        HudTmpPunchPosition();
        // StartCoroutine(ActiveHaloMaterialCoroutine());
        //
        // IEnumerator ActiveHaloMaterialCoroutine()
        // {
        //     var t = 0f;
        //     var d = 0.15f;
        //     while (t < d)
        //     {
        //         var w = Util.SmoothLerp01(t / d);
        //         t += Time.deltaTime;
        //         _activeHaloMaterial.SetFloat("_Alpha", w);
        //         yield return null;
        //     }
        // }
    }

    private void OnPlayerCultTrialDeath()
    {
        _activeHaloMaterial.SetFloat("_Alpha", 0);
    }

    public void ClearTimer()
    {
        _timerTmp.text = "";
        _timer = 0;
    }

    public void StartTimer()
    {
        _timerTicking = true;
        _timer = 0;
    }

    public void StopTimer()
    {
        _timerTicking = false;
    }

    public void EnableCurseFog()
    {
        _curseFogController.Priority = 20;
    }

    public void DisableCurseFog()
    {
        _curseFogController.Priority = -10;
    }
}
