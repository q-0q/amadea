using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using FMOD.Studio;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using Util = Code.Misc.Util;

public class SaveSystem : MonoBehaviour
{

    private float _timeSinceLastSave;
    
    private static SaveSystem _singleton;

    public static event Action<SaveData> OnSaveDataUpdated; 
    private static SaveSystem Singleton
    {
        get
        {
            if (_singleton == null)
            {
                var go = new GameObject("SaveSystem");
                _singleton = go.AddComponent<SaveSystem>();
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

        _cachedSaveData = null;
        _singleton = this;
        _timeSinceLastSave = 0;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        _timeSinceLastSave += Time.deltaTime;
    }

    [System.Serializable]
    public class TrialCompletionEntry
    {
        public string metaName;
        public float time;
        public float goldTime;
    }

    private const bool DeleteOutdatedSaves = true;

    [System.Serializable]
    public class SaveData
    {
        public string scene;
        public float[] playerInGamePosition;
        public string playerInGamePositionId;
        public float playerInGameYAngle;
        public List<string> persistentEvents;
        public List<string> items;
        public List<TrialCompletionEntry> trialCompletions;
        public List<string> lemonCollections;
        public List<string> bells;
        public int bellCount;
        public int bitCount;
        public int incenseAmount;
        public int cultLocationCampId;
        public List<string> bitDeposits;
        public List<string> terminalNodes;
        public List<string> areas;
        public string currentNearestTerminalNode;
        public float currentNeatestTerminalNodeT;
        public string checkpointPath;
        public List<string> tricks;
        public float playTime;
        public string gameVersion;
        public int rhealTrickLearnedPositionInstance;
        
            

        public SaveData()
        {
            scene = "00-Tutorial";
            playerInGamePosition = null;
            playerInGameYAngle = 0f;
            playerInGamePositionId = "";
            persistentEvents = new List<string>();
            items = new List<string>();
            trialCompletions = new List<TrialCompletionEntry>();
            lemonCollections = new List<string>();
            bells = new List<string>();
            terminalNodes = new List<string>();
            areas = new List<string>();
            currentNearestTerminalNode = "";
            currentNeatestTerminalNodeT = 0;
            checkpointPath = "";
            bitCount = 0;
            bellCount = 0;
            playTime = 0;
            incenseAmount = 0;
            cultLocationCampId = 0;
            bitDeposits = new List<string>();
            tricks = new List<string>();
            gameVersion = "";
            rhealTrickLearnedPositionInstance = -1;

        }
    }

    private SaveData _cachedSaveData;
    
    private static string GetDirectory()
    {
        return Path.Combine(Application.persistentDataPath, "saves");
    }

    private static string GetCurrentSaveIdPath()
    {
        var id = MetaSaveSystem.LoadCachedMetaSaveData().saveId;
        return Path.Combine(GetDirectory(), id + ".json");
    }
    
    private static string GetCurrentSaveIdImagePath()
    {
        var id = MetaSaveSystem.LoadCachedMetaSaveData().saveId;
        return Path.Combine(GetDirectory(), id + ".png");
    }
    
    public static string GetImagePathFromSaveId(int id)
    {
        return Path.Combine(GetDirectory(), id + ".png");
    }
    
    public static void WritePlayerInGamePosition(Vector3 gamePosition, string gamePositionId, float yAngle)
    {
        if (CultTrialManager.Singleton.isCurseEnabled) return;
        SaveData data = LoadCachedSaveData();
        data.playerInGamePosition = new []{ gamePosition.x, gamePosition.y, gamePosition.z};
        data.playerInGamePositionId = gamePositionId;
        data.playerInGameYAngle = yAngle;
        WriteSaveData(data);
    }
    
    public static void ClearPlayerInGamePosition()
    {
        SaveData data = LoadCachedSaveData();
        data.playerInGamePosition = null;
        data.playerInGamePositionId = "";
        data.playerInGameYAngle = 0;
        WriteSaveData(data);
    }
    
    public static void WritePersistentEvent(string persistentEvent)
    {
        if (persistentEvent == "")
        {
            Debug.LogError("Tried to write empty persistent event");
            return;
        }
        SaveData data = LoadCachedSaveData();
        if (data.persistentEvents.Contains(persistentEvent)) return;
        data.persistentEvents.Add(persistentEvent);
        WriteSaveData(data);
    }
    
    public static void WriteItem(string item)
    {
        if (item == "")
        {
            Debug.LogError("Tried to write empty item");
            return;
        }
        SaveData data = LoadCachedSaveData();
        if (data.items.Contains(item)) return;
        data.items.Add(item);
        WriteSaveData(data);
    }
    
    public static void RemoveItem(string item)
    {
        if (item == "")
        {
            Debug.LogError("Tried to remove empty item");
            return;
        }
        SaveData data = LoadCachedSaveData();
        if (!data.items.Contains(item)) return;
        data.items.Remove(item);
        WriteSaveData(data);
    }
    
    public static void WriteBell(string metaName)
    {
        if (metaName == "")         
        {
            Debug.LogError("Tried to write empty bell");
            return;
        }
        SaveData data = LoadCachedSaveData();
        if (data.bells.Contains(metaName)) return;
        data.bellCount++;
        data.bells.Add(metaName);
        WriteSaveData(data);
    }
    
    public static void ReduceBellCount(int amount)
    {
        SaveData data = LoadCachedSaveData();
        data.bellCount -= amount;
        WriteSaveData(data);
    }

    public static void WriteTrialCompletion(string metaName, float time, float goldTime)
    {
        if (metaName == "")
        {
            Debug.LogError("Tried to write empty trial");
            return;
        };
        
        SaveData data = LoadCachedSaveData();

        var entry = data.trialCompletions.FirstOrDefault(e => e.metaName == metaName);

        if (entry != null)
        {
            if (entry.time < time) return;
            entry.time = time;
        }
        else
        {
            data.trialCompletions.Add(new TrialCompletionEntry
            {
                metaName = metaName,
                time = time,
                goldTime = goldTime
            });
        }

        WriteSaveData(data);
    }

    
    public static bool GetTrialCompletion(string metaName, out float playerRecordTime)
    {
        playerRecordTime = -1f;
        SaveData data = LoadCachedSaveData();
        var entry = data.trialCompletions.FirstOrDefault(e => e.metaName == metaName);
        if (entry != null) playerRecordTime = entry.time;
        return entry != null;
    }
    
    public static bool GetTrialGolded(string metaName)
    {
        SaveData data = LoadCachedSaveData();
        var entry = data.trialCompletions.FirstOrDefault(e => e.metaName == metaName);
        if (entry == null) return false;
        return entry.time < entry.goldTime;
    }
    
    public static void WriteLemonCollection(string metaName)
    {
        if (metaName == "")
        {
            Debug.LogError("Tried to write empty lemon");
            return;
        };
        
        SaveData data = LoadCachedSaveData();
        var entry = data.lemonCollections.FirstOrDefault(e => e == metaName);
        if (entry != null) return;
        data.lemonCollections.Add(metaName);
        WriteSaveData(data);
    }
    
    public static void AddBit()
    {
        SaveData data = LoadCachedSaveData();
        data.bitCount++;
        Singleton._cachedSaveData = data;
    }
    
    public static void RemoveBit(int amount)
    {
        SaveData data = LoadCachedSaveData();
        data.bitCount -= amount;
        Singleton._cachedSaveData = data;
    }
    
    public static void AddIncenseAmount(int amount)
    {
        SaveData data = LoadCachedSaveData();
        data.incenseAmount += amount;
        WriteSaveData(data);
    }
    
    public static int GetIncenseAmount()
    {
        SaveData data = LoadCachedSaveData();
        return data.incenseAmount;
    }
    
    public static void AdvanceCultLocationCampId()
    {
        SaveData data = LoadCachedSaveData();
        data.cultLocationCampId ++;
        WriteSaveData(data);
    }
    
    public static int GetCultLocationCampId()
    {
        SaveData data = LoadCachedSaveData();
        return data.cultLocationCampId;
    }
    
    public static void CollectBitDeposit(string metaName)
    {
        if (metaName == "") 
        {
            Debug.LogError("Tried to write empty bit deposit");
            return;
        };
        
        SaveData data = LoadCachedSaveData();
        var entry = data.bitDeposits.FirstOrDefault(e => e == metaName);
        if (entry != null) return;
        data.bitDeposits.Add(metaName);
        Singleton._cachedSaveData = data;
    }
    
    public static bool GetBitDeposit(string metaName, int id)
    {
        SaveData data = LoadCachedSaveData();
        var entry = data.bitDeposits.FirstOrDefault(e => e == metaName);
        return entry != null;
    }
    
    public static bool GetLemonCollection(string metaName)
    {
        SaveData data = LoadCachedSaveData();
        var entry = data.lemonCollections.FirstOrDefault(e => e == metaName);
        return entry != null;
    }
    
    public static List<string> GetAllItems()
    {
        SaveData data = LoadCachedSaveData();
        return data.items;
    }

    public static bool GetPersistentEventCompleted(string persistentEvent)
    {
        var data = LoadCachedSaveData();
        return data.persistentEvents.Contains(persistentEvent);
    }
    
    public static bool GetBell(string metaName)
    {
        var data = LoadCachedSaveData();
        return data.bells.Contains(metaName);
    }

    public static int GetBitCount()
    {
        var data = LoadCachedSaveData();
        return data.bitCount;
    }

    public static void WriteTerminalNode(string metaName)
    {
        if (metaName == "") 
        {
            Debug.LogError("Tried to write empty major leyline node");
            return;
        };
        
        SaveData data = LoadCachedSaveData();
        if (data.terminalNodes.Contains(metaName)) return;
        data.terminalNodes.Add(metaName);
        Singleton._cachedSaveData = data;
        WriteSaveData(data);
    }
    
    public static void WriteArea(string metaName)
    {
        if (metaName == "") 
        {
            Debug.LogError("Tried to write empty area");
            return;
        };
        
        SaveData data = LoadCachedSaveData();
        if (data.areas.Contains(metaName)) return;
        data.areas.Add(metaName);
        Singleton._cachedSaveData = data;
        WriteSaveData(data);
    }
    
    public static bool GetArea(string metaName)
    {
        SaveData data = LoadCachedSaveData();
        if (data.areas == null) return false;
        return data.areas.Contains(metaName);
    }

    public static bool GetTerminalNode(string metaName)
    {
        SaveData data = LoadCachedSaveData();
        if (data.terminalNodes == null) return false;
        return data.terminalNodes.Contains(metaName);
    }

    public static void WriteNearestTerminalNode(string node, float t)
    {
        SaveData data = LoadCachedSaveData();
        data.currentNearestTerminalNode = node;
        data.currentNeatestTerminalNodeT = t;
        WriteSaveData(data);
    }

    public static string GetNearestTerminalNode(out float t)
    {
        SaveData data = LoadCachedSaveData();
        t = data.currentNeatestTerminalNodeT;
        return data.currentNearestTerminalNode;
    }

    public static void WriteCheckpointPath(string path)
    {
        SaveData data = LoadCachedSaveData();
        data.checkpointPath = path;
        WriteSaveData(data);
    }

    public static string GetCheckpointPath()
    {
        SaveData data = LoadCachedSaveData();
        return data.checkpointPath;
    }


    public static void WriteTrick(string trick)
    {
        SaveData data = LoadCachedSaveData();
        if (data.tricks.Contains(trick)) return;
        data.tricks.Add(trick);
        WriteSaveData(data);
    }

    public static bool GetTrick(string trick)
    {
        SaveData data = LoadCachedSaveData();
        if (data.tricks == null) return false;
        return data.tricks.Contains(trick);
    }

    public static void WriteRhealTrickLearnedPositionInstance(int positionInstance)
    {
        SaveData data = LoadCachedSaveData();
        data.rhealTrickLearnedPositionInstance = positionInstance;
        WriteSaveData(data);
    }

    public static int GetRhealTrickLearnedPositionInstance()
    {
        SaveData data = LoadCachedSaveData();
        return data.rhealTrickLearnedPositionInstance;
    }
    
    
    private static void WriteSaveData(SaveData saveData, bool screenCapture = true)
    {
        string directory = GetDirectory();
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);
        
        SaveData data = saveData;
        data.scene = SceneManager.GetActiveScene().name;
        data.playTime += Singleton._timeSinceLastSave;
        data.gameVersion = Application.version;
        
        Singleton._timeSinceLastSave = 0;
        Singleton._cachedSaveData = data;
        string json = JsonUtility.ToJson(data, true);
        
        if (screenCapture) UpdateScreenshot();
        File.WriteAllText(GetCurrentSaveIdPath(), json);
        
        OnSaveDataUpdated?.Invoke(data);

        Debug.Log("Saved file to: " + GetCurrentSaveIdPath());
    }

    public static void UpdateScreenshot(float delay = 0)
    {
        return;
        Singleton.StartCoroutine(CaptureScreenshot(delay));
    }
    private static IEnumerator CaptureScreenshot(float delay)
    {
        yield return new WaitForSeconds(delay);
        ScreenCapture.CaptureScreenshot(GetCurrentSaveIdImagePath(), 1);
        yield return null;
    }
    
    public static void WriteCachedSave()
    {
        WriteSaveData(Singleton._cachedSaveData, false);
    }

    public static SaveData LoadCachedSaveData()
    {
        if (Singleton._cachedSaveData == null)
        {
            string path = GetCurrentSaveIdPath();
            if (!File.Exists(path))
            {
                Singleton._cachedSaveData = new SaveData();
            }
            else
            {
                var loadedData = LoadAndValidate(path);
                Singleton._cachedSaveData = loadedData;
            }
        }
        
        return Singleton._cachedSaveData;
    }

    
    public static void ClearCache()
    {
        Singleton._cachedSaveData = null;
    }
    public static SaveData LoadSaveDataFromId(int id)
    {
        var path = Path.Combine(GetDirectory(), id + ".json");
        if (!File.Exists(path))
        {
            return null;
        }

        var loadedData = LoadAndValidate(path);
        Singleton._cachedSaveData = loadedData;
        return Singleton._cachedSaveData;
    }
    
    public static SaveData LoadAndValidate(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            string json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new Exception("File is empty.");
            }

            var data = JsonUtility.FromJson<SaveData>(json);
            if (data == null)
            {
                throw new Exception("JsonUtility returned null.");
            }

            if (data.gameVersion != "" && data.gameVersion != Application.version && DeleteOutdatedSaves)
            {
                throw new Exception("Outdated save.");
            }


            return data;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SaveSystem] Critical error loading {path}. Deleting file. \nError: {ex.Message}");
            
            try 
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (IOException ioEx)
            {
                Debug.LogError($"[SaveSystem] Could not delete corrupt file: {ioEx.Message}");
            }

            return null;
        }
    }
}

public class MetaSaveSystem : MonoBehaviour
{
    public static event Action<MetaSaveSystem.MetaSaveData> OnMetaSaveDataUpdated;
    private MetaSaveData _cachedMetaSaveData;

    private static MetaSaveSystem _singleton;
    private static MetaSaveSystem Singleton
    {
        get
        {
            if (_singleton == null)
            {
                var go = new GameObject("MetaSaveSystem");
                _singleton = go.AddComponent<MetaSaveSystem>();
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

        _cachedMetaSaveData = null;
        _singleton = this;
        DontDestroyOnLoad(gameObject);
    }
    
        
    [System.Serializable]
    public class MetaSaveData
    {

        public int saveId;
        public int cameraSensitivityModifier;
        public bool enableAmbientParticles;
        public bool enableFpsDisplay;
        public bool autoCamEnabled;
        public int foliageRenderDistanceLevel;
        public string cameraUpdateMode;
        public int fpsCap;

        public MetaSaveData()
        {
            this.saveId = 0;
            this.cameraSensitivityModifier = 10;
            this.enableAmbientParticles = true;
            this.enableFpsDisplay = true;
            this.autoCamEnabled = false;
            this.foliageRenderDistanceLevel = 1;
            this.cameraUpdateMode = "SmartUpdate";
            this.fpsCap = 120;
        }
    }
    
    private static string GetDirectory()
    {
        return Path.Combine(Application.persistentDataPath);
    }

    public static string GetPath()
    {
        return Path.Combine(GetDirectory(), "meta.json");
    }

    public static void WriteSaveId(int saveId)
    {
        var data = LoadCachedMetaSaveData();
        data.saveId = saveId;
        WriteMetaSaveData(data);
    }
    
    public static void WriteCameraSensitivityModifier(int cameraSensitivityModifier)
    {
        MetaSaveData data = LoadCachedMetaSaveData();
        data.cameraSensitivityModifier = cameraSensitivityModifier;
        WriteMetaSaveData(data);
    }
    
    public static void WriteEnableFpsDisplay(bool enableFpsDisplay)
    {
        MetaSaveData data = LoadCachedMetaSaveData();
        data.enableFpsDisplay = enableFpsDisplay;
        WriteMetaSaveData(data);
    }
    
    public static void WriteEnableAutocam(bool enableAutocam)
    {
        MetaSaveData data = LoadCachedMetaSaveData();
        data.autoCamEnabled = enableAutocam;
        WriteMetaSaveData(data);
    }
    
    public static void WriteFoliageRenderDistance(int foliageRenderDistance)
    {
        MetaSaveData data = LoadCachedMetaSaveData();
        data.foliageRenderDistanceLevel = foliageRenderDistance;
        WriteMetaSaveData(data);
    }

    private static void WriteMetaSaveData(MetaSaveData data)
    {
        string directory = GetDirectory();
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);
        
        Singleton._cachedMetaSaveData = data;
        string json = JsonUtility.ToJson(data, true);

        File.WriteAllText(GetPath(), json);
        OnMetaSaveDataUpdated?.Invoke(data);
        Debug.Log("Saved file to: " + GetPath());
    }

    public static MetaSaveData LoadCachedMetaSaveData()
    {
        if (Singleton._cachedMetaSaveData == null)
        {
            string path = GetPath();
            if (!File.Exists(path))
            {
                Singleton._cachedMetaSaveData = new MetaSaveData();
            }
            else
            {
                string json = File.ReadAllText(path);
                Singleton._cachedMetaSaveData = Util.LoadAndValidate<MetaSaveData>(path);
                if (Singleton._cachedMetaSaveData == null) Singleton._cachedMetaSaveData = new MetaSaveData();
            }
        }

        return Singleton._cachedMetaSaveData;
    }

    public static void WriteAmbientParticlesEnabled(bool enableAmbientParticles)
    {
        MetaSaveData data = LoadCachedMetaSaveData();
        data.enableAmbientParticles = enableAmbientParticles;
        WriteMetaSaveData(data);
    }
}