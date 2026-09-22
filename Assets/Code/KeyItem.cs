using System;
using System.Collections;
using System.Collections.Generic;
using FMOD.Studio;
using UnityEngine;
using UnityEngine.InputSystem;
using Util = Code.Misc.Util;

public class KeyItemRegistration
{
    public string displayName;
    public GameObject MeshGameObject;
    public Sprite Sprite;
    public string description;
    
    public Func<string> GetUseDescription = () => "It doesn't seem to have much use.";
    public Func<bool> GetCanUse = () => false;
    public Action onUse = null;
    public Func<string> GetUseConfirmation = () => "Use this item?";
}

public static class KeyItemRegistry
{
    public static readonly Dictionary<string, KeyItemRegistration> KeyItemRegistrations;

    static KeyItemRegistry()
    {
        KeyItemRegistrations = new Dictionary<string, KeyItemRegistration>();
        
        KeyItemRegistrations.Add("ErhuFragment1", new KeyItemRegistration()
        {
            displayName = "Erhu neck",
            description = "The stick-like neck of a delicate instrument. It's broken, but may be repaired.",
            MeshGameObject = Resources.Load("Prefab/KeyItems/UrnFragment") as GameObject,
            Sprite = null,
        });
        
        KeyItemRegistrations.Add("ErhuFragment2", new KeyItemRegistration()
        {
            displayName = "Erhu tuner",
            description = "A series of intricate tuning pegs. They're broken, but may be repaired.",
            MeshGameObject = Resources.Load("Prefab/KeyItems/UrnFragment") as GameObject,
            Sprite = null,
        });
        
        KeyItemRegistrations.Add("ErhuFragment3", new KeyItemRegistration()
        {
            displayName = "Erhu body",
            description = "The resonator body of a musical instrument. It's broken, but may be repaired.",
            MeshGameObject = Resources.Load("Prefab/KeyItems/UrnFragment") as GameObject,
            Sprite = null,
        });
        
        KeyItemRegistrations.Add("IncenseBurner", new KeyItemRegistration()
        {
            displayName = "Incense burner",
            description = "An ornately constructed censer etched with fine runes. A faint voice seems to whispering from within the cage...\n\nThe Far Cultists claim this can be used to initiate rituals at the \"Far plinths\" located across the Mountain.",
            MeshGameObject = null as GameObject,
            Sprite = null,
            GetUseDescription = () =>
            {
                var count = SaveSystem.GetIncenseAmount();
                var nearby = PlayerFsm.Singleton.GetNearbyCultTrial(out var fsm);
                var cones = count == 1 ? "cone" : "cones";
                if (count == 0 || nearby && !SaveSystem.GetPersistentEventCompleted(fsm.metaName + "-unlocked")) return count.ToString() + " " + cones + " of incense left.";
                return count.ToString() + " " + cones + " of incense left, but it wouldn't do anything right now.";
            },
            GetCanUse = () =>
            {
                var count = SaveSystem.GetIncenseAmount();
                var nearby = PlayerFsm.Singleton.GetNearbyCultTrial(out var fsm);
                return nearby && count > 0  && !SaveSystem.GetPersistentEventCompleted(fsm.metaName + "-unlocked");
            },
            onUse = () =>
            {
                PlayerFsm.Singleton.Machine.Fire(PlayerFsm.PlayerFsmTrigger.UseIncenseBurner);
            },
            GetUseConfirmation = () => "Burn a cone of incense?"
        });
        
        KeyItemRegistrations.Add("Idol", new KeyItemRegistration()
        {
            displayName = "Amadean idol",
            description = "A palm-sized depiction of Amadea, lovingly carved from stone of the Mountain.",
            MeshGameObject = Resources.Load("Prefab/KeyItems/UrnFragment") as GameObject,
            Sprite = null,
            GetUseDescription = () => "Returns you to a place of rest.",
            GetCanUse = () => true,
            onUse = () =>
            {
                SceneLoader.Singleton.ReloadCurrentScene();
            },
            GetUseConfirmation = () => "Return to a previous location?"
        });
        
        KeyItemRegistrations.Add("Map", new KeyItemRegistration()
        {
            displayName = "Glyphstone",
            description = "A Silicant artifact displaying the Glyph's symbology.\n\nAccording to Silicant Diamber, the pattern is a sacred geometric depiction of the Mountain leyline network.",
            MeshGameObject = Resources.Load("Prefab/KeyItems/UrnFragment") as GameObject,
            Sprite = null,
            GetUseDescription = () => "Shows your location in the world.",
            GetCanUse = () => true,
            onUse = () =>
            {
                PlayerFsm.Singleton.Machine.Jump(PlayerFsm.PlayerFsmState.MapSlowdown);
            },
            GetUseConfirmation = () => "Open the map?"
        });
        
        KeyItemRegistrations.Add("Lantern", new KeyItemRegistration()
        {
            displayName = "Spiritfyre Pendant",
            description = "A blush-hued gemstone affixed to a cord for wearing. A flickering swirl dances inside the stone.",
            MeshGameObject = Resources.Load("Prefab/KeyItems/UrnFragment") as GameObject,
            Sprite = null,
            GetUseDescription = () => "Illuminates dark places.",
            GetCanUse = () => false,
            onUse = () =>
            {
                
            },
            GetUseConfirmation = () => ""
        });
        
        KeyItemRegistrations.Add("Card", new KeyItemRegistration()
        {
            displayName = "Silicant Card",
            description = "A palm-sized rectangle of sleek, machined metal. Bluish lights faintly twinkle within the surface.",
            MeshGameObject = Resources.Load("Prefab/KeyItems/UrnFragment") as GameObject,
            Sprite = null,
            // GetUseDescription = () => "???",
            GetCanUse = () => false,
            onUse = () =>
            {
                
            },
            GetUseConfirmation = () => ""
        });
        
        KeyItemRegistrations.Add("Camera", new KeyItemRegistration()
        {
            displayName = "Obscura",
            description = "A finely made cylindrical wooden device. A pinpoint-sized hole at one end projects light into an image.",
            MeshGameObject = Resources.Load("Prefab/KeyItems/UrnFragment") as GameObject,
            Sprite = null,
            GetUseDescription = () => "Opens Photo mode.",
            GetCanUse = () => true,
            onUse = () =>
            {
                PhotoManager.Singleton.MakeActive();
                PlayerFsm.Singleton.Machine.Jump(PlayerFsm.PlayerFsmState.IdleLong);
            },
            GetUseConfirmation = () => "Open Photo mode?"
        });
        
        KeyItemRegistrations.Add("Mana0", new KeyItemRegistration()
        {
            displayName = "Wooden Lotus Charm",
            description = "A tiny wooden square engraved with a floral pattern.",
            MeshGameObject = Resources.Load("Prefab/KeyItems/UrnFragment") as GameObject,
            Sprite = null,
            GetUseDescription = () => "Increases Ki reserves.",
            GetCanUse = () => false,
            onUse = () =>
            {

            },
            GetUseConfirmation = () => ""
        });
        
        
        KeyItemRegistrations.Add("Rod", new KeyItemRegistration()
        {
            displayName = "Divining Rod",
            description = "A Y-shaped, petrified branch of an old, strange tree.\n\nWhen the split ends are held in both hands, the branch appears to point itself towards some unknown destination.",
            MeshGameObject = Resources.Load("Prefab/KeyItems/UrnFragment") as GameObject,
            Sprite = null,
            GetUseDescription = () => "Not implemented yet!",
            GetCanUse = () => false,
            onUse = () =>
            {

            },
            GetUseConfirmation = () => ""
        });
    }
}


public class KeyItem : MonoBehaviour
{

    public string Id;
    private Interactable _interactable;
    // private Transform _meshTransform;
    private bool collected = false;
    private ParticleSystem _particleSystem;

    public static event Action<string> OnKeyItemCollected;
    private const string GenericCollectionPersistentEvent = "item-collected";

    private void Awake()
    {
        _interactable = GetComponentInChildren<Interactable>();
        _particleSystem = GetComponentInChildren<ParticleSystem>();
        
        if (SaveSystem.GetPersistentEventCompleted(Id) && _interactable != null) Destroy(gameObject);
    }

    private void OnEnable()
    {
        if (_interactable != null) _interactable.OnInteracted += OnInteracted;
    }

    private void OnDisable()
    {
        if (_interactable != null) _interactable.OnInteracted -= OnInteracted;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var data = KeyItemRegistry.KeyItemRegistrations[Id];
        // _meshTransform = Instantiate(new GameObject(), transform).transform;
    }

    public void OnInteracted()
    {
        
        if (collected) return;
        collected = true;
        if (_interactable != null) _interactable.SetEnabled(false);
        Collect(Id);

         
        StartCoroutine(PositionCoroutine());
        StartCoroutine(RotationCoroutine());
        StartCoroutine(ScaleCoroutine());
        
        PlayerFsm.Singleton.Machine.Jump(PlayerFsm.PlayerFsmState.KeyItemCollect);
        
        IEnumerator PositionCoroutine()
        {
            var t = 0f;
            var d = 0.5f;

            var start = transform.position;
            var end = PlayerFsm.Singleton.transform.position + Vector3.up * 6.5f;
            while (t < d)
            {
                var w = Util.SmoothLerp01(t / d);
                transform.position = Util.LerpWithArc(start, end, w, 1f);
                // _meshTransform.localPosition = Vector3.Lerp(_meshTransform.localPosition, Vector3.zero, w);
                _particleSystem.transform.position = transform.position;
                t += Time.deltaTime;
                yield return null;
            }

            _particleSystem.Stop();
            yield return new WaitForSeconds(0.7f);
            
            t = 0f;
            d = 0.5f;
            
            start = transform.position;
            end = PlayerFsm.Singleton.transform.position + Vector3.up * 2.5f + PlayerFsm.Singleton.transform.forward;
            
            while (t < d)
            {
                var w = Util.SmoothLerp01(t / d);
                transform.position = Vector3.Lerp(start, end, w);
                t += Time.deltaTime;
                yield return null;
            }
         
            OnKeyItemCollected?.Invoke(KeyItemRegistry.KeyItemRegistrations[Id].displayName);
            
        }
        
        IEnumerator ScaleCoroutine()
        {
            var t = 0f;
            var d = 0.75f;
            yield return new WaitForSeconds(1.3f);
            while (t < d)
            {
                var w = Util.SmoothLerp01(t / d);
                // _meshTransform.localScale = Vector3.Lerp(_meshTransform.localScale, Vector3.zero, w);
                t += Time.deltaTime;
                yield return null;
            }
        }
        
        IEnumerator RotationCoroutine()
        {
            var t = 0f;
            var d = 1.5f;
            while (t < d)
            {
                var w = Util.SmoothLerp01(t / d);
                // _meshTransform.Rotate(Vector3.up, Time.deltaTime * Mathf.Lerp(1000f, 0f, Mathf.Pow(w, 2f)));
                t += Time.deltaTime;
                yield return null;
            }

        }
    }

    private void Collect(string id)
    {
        SaveSystem.WriteItem(id);
        SaveSystem.WritePersistentEvent(id);
        
        if (!SaveSystem.GetPersistentEventCompleted(GenericCollectionPersistentEvent))
        {
            StartCoroutine(Coroutine());
            IEnumerator Coroutine()
            {
                yield return new WaitForSeconds(5.75f);
                TutorialCanvas.Singleton.ShowTutorialText("Open bag", "Inventory");
            }
            SaveSystem.WritePersistentEvent(GenericCollectionPersistentEvent);
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        if (collected) return;
        // _meshTransform.Rotate(Vector3.up, Time.deltaTime * 130f);
        // _meshTransform.localPosition = new Vector3(0, (Mathf.Sin(Time.time * 2f) + 1f) * 0.5f,0);
        // _particleSystem.transform.position = _meshTransform.position;
    }

    public static void InvokeKeyItemCollected(string id)
    {
        OnKeyItemCollected?.Invoke(id);
    }
}
