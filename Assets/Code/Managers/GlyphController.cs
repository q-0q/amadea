using System;
using System.Collections.Generic;
using Code.Misc;
using UnityEngine;
using UnityEngine.Splines;

public class GlyphController : MonoBehaviour
{
    
    public static readonly List<TerminalNode> TerminalsInScene = new();
    private List<GameObject> _splines;
    
    
    public class TerminalData
    {
        public string previousNode = "";
        public List<string> loreDialogue = new List<string>() { };
        public string displayId;

        public int mapSplineId;
        public float mapSplineStartT;
        public float mapSplineEndT;
        public string writeAdditionalKey = "";
        public bool contributeToCount = true;
        public bool isEndCap = false;
    }

    public static readonly Dictionary<string, TerminalData> TerminalRegistry = new()
    {
        
        { "bootstrap", new TerminalData()
            {
                displayId = "",
                previousNode = "",
                loreDialogue = new List<string>() { },
                
                mapSplineId = 0,
                mapSplineStartT = 0f,
                mapSplineEndT = 0.1f,
                contributeToCount = false
            } 
        },
        
        
        { "tutorial-0", new TerminalData()
            {
                displayId = "00",
                previousNode = "bootstrap",
                loreDialogue = new List<string>() { "Signal here is drawn directly from source, making this the inception of the entire system.", "...As such, this node is a potential point of failure for entire network. Uptime is critical.", "Hardware health of this station should be monitored especially closely." },
                
                mapSplineId = 0,
                mapSplineStartT = 0.1f,
                mapSplineEndT = 0.2f
            } 
        },
        
        { "tutorial-1", new TerminalData()
            {
                displayId = "01",
                previousNode = "tutorial-0",
                loreDialogue = new List<string>() { "01 propagates signal into and through the master chambers.", "Signal density has to be within a very specific range to avoid hardware interference where the line passes indoors.", "That increased workload means this machine may require more regular defrags." },
                
                mapSplineId = 0,
                mapSplineStartT = 0.2f,
                mapSplineEndT = 0.3f
            } 
        },
        
        { "icy-canals-0", new TerminalData()
            {
                displayId = "02",
                previousNode = "tutorial-1",
                writeAdditionalKey = "icy-canals-0-split",
                loreDialogue = new List<string>() { "The Steppe's natural aquifers feed the coolant systems of the relay hub and its inner chambers.", "Additionally, the unusual mineral content of the water has been found to actually increase signal conductivity.", "The line to Ouro Station thus uses relatively low power, despite the distance and altitude gain." },
                mapSplineId = 0,
                mapSplineStartT = 0.4f,
                mapSplineEndT = 0.9f
            } 
        },
        
        { "icy-canals-0-split", new TerminalData()
            {
                displayId = "02",
                previousNode = "tutorial-1",
                loreDialogue = new List<string>() {  },
                mapSplineId = 3,
                mapSplineStartT = 0f,
                mapSplineEndT = 0.1f,
                contributeToCount = false
            } 
        },
        
        { "ouro", new TerminalData()
            {
                displayId = "03",
                previousNode = "icy-canals-0",
                loreDialogue = new List<string>() { "Ouro Station is the only solar intake for the Mountain's lower regions.", "Light captured by Ouro Station feeds hardlight systems via the leyline network.", "In case of hardlight failures, Ouro Station's leyline connection and its internal core stability should both be monitored." },
                mapSplineId = 0,
                mapSplineStartT = 0.9f,
                mapSplineEndT = 1f,
                contributeToCount = true,
                isEndCap = true
                
            } 
        }
    };
    
    
    public class AreaData
    {
        public string subtitle;
        public string title;
        public string coords;
        public string elevation;
    }

    public static readonly Dictionary<string, AreaData> AreaRegistry = new()
    {
        { "glyph", new AreaData()
            {
                subtitle = "The",
                title = "Glyph",
                coords = "76\u00b047S 64\u00b08W",
                elevation = "810 m"
                
            }
        },
        
        { "steppe", new AreaData()
            {
                subtitle = "The",
                title = "Steppe",
                coords = "77\u00b023S 72\u00b047W",
                elevation = "935 m",
            }
        },
        
        { "ouro", new AreaData()
            {
                subtitle = "",
                title = "Ouro Station",
                coords = "88\u00b072S 59\u00b012W",
                elevation = "1240 m"
            }
        },
        
                
        { "valley", new AreaData()
            {
                subtitle = "The",
                title = "Shattered Valley",
                coords = "61\u00b014S 13\u00b051W",
                elevation = "3280 m"
            }
        }
    };

    private void Awake()
    {
        var find = transform.Find("Canvas");
        
        var s = find.Find("Splines");
        _splines = new List<GameObject>();
        _splines.Add(s.Find("0").gameObject);
        _splines.Add(s.Find("1").gameObject);
        _splines.Add(s.Find("2").gameObject);
        _splines.Add(s.Find("3").gameObject);
        _splines.Add(s.Find("4").gameObject);
        // ...

        
        foreach (var spline in _splines)
        {
            
            var splineContainer = spline.GetComponent<SplineContainer>();
            var curveLength = splineContainer.Spline.GetLength();
            spline.GetComponent<Renderer>().material.SetFloat("_SplineLength", curveLength);
        }
        
        
        UpdateSplineRenderers();

    }

    private void UpdateSplineRenderers()
    {


        for (int i = 0; i < _splines.Count; i++)
        {
            var largestEndT = -1f;
            foreach (var (terminalName, terminalData) in TerminalRegistry)
            {
                if (terminalData.mapSplineId != i) continue;
                if (!SaveSystem.GetTerminalNode(terminalName)) continue;
                if (terminalData.mapSplineEndT < largestEndT) continue;
                largestEndT = terminalData.mapSplineEndT;
            }
            
            _splines[i].GetComponent<Renderer>().material.SetFloat("_FillWeight", largestEndT);
        }
    }

    private void OnEnable()
    {
        SaveSystem.OnSaveDataUpdated += OnSaveDataUpdated;
    }

    private void OnDisable()
    {
        SaveSystem.OnSaveDataUpdated -= OnSaveDataUpdated;
    }

    private void OnSaveDataUpdated(SaveSystem.SaveData saveData)
    {
        if (saveData.terminalNodes == null) return;
        UpdateSplineRenderers();
    }
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public List<GameObject> GetSplines()
    {
        return _splines;
    }
}
