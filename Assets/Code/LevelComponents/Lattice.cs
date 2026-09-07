using System;
using System.Collections;
using System.Collections.Generic;
using Mono.Cecil;
using UnityEngine;

public class Lattice : MonoBehaviour
{
    public const string eventPrefix = "lattice-";
    public string id = "";
    public int length = 5;
    public int width = 5;
    public int height = 5;
    public const float CellSize = 8f;
    private Transform _base;
    private const float BaseHeight = CellSize * 0.5f;
    private DialogueController _dialogue;

    public List<LatticeNodeConfig> nodeConfigs;
    private int _completedNodes;
    public static event Action<Lattice, bool> OnLatticeCompleted;

    private List<Material> _cellMaterials;

    [Serializable]
    public class LatticeNodeConfig
    {
        public int xCoordinate = 0;
        public int yCoordinate = 0;
        public int zCoordinate = 0;
    }
    
private void Awake()
{
    _base = transform.Find("Base");
    _dialogue = GetComponentInChildren<DialogueController>();
    _completedNodes = 0;
    UpdateDialogue();

    var padding = 9f;
    _base.localScale = new Vector3((length * CellSize) + padding, BaseHeight, (width * CellSize) + padding);
    _base.localPosition = new Vector3(0, BaseHeight * 0.5f, 0);
    var cellPrefab = Resources.Load("Prefab/LatticeCell") as GameObject;  
    var posOffset = new Vector3(length * CellSize * -0.5f, (height * CellSize * 0.5f), width * CellSize * -0.5f);

    _cellMaterials = new List<Material>();
    var cellPadding = 0.5f;

    var cellMaterialsByPosition = new Dictionary<(int, int), Material>();
    for (int i = 0; i < length; i++)  
    {        
        for (int j = 0; j < width; j++)  
        {            
            var obj = Instantiate(cellPrefab, Vector3.zero, Quaternion.identity, transform);  
            var pos = new Vector3(CellSize * (i + 0.5f), 0f, CellSize * (j + 0.5f));  
            obj.transform.localPosition = pos + posOffset;  
            obj.transform.localRotation = Quaternion.identity;  
            obj.transform.localScale = new Vector3(CellSize - cellPadding, (CellSize * height) - cellPadding, CellSize - 1f);
            var cellMaterial = obj.GetComponentInChildren<Renderer>().material;
            _cellMaterials.Add(cellMaterial);
            cellMaterialsByPosition[(i, j)] = cellMaterial;
        }    
    }    
    
    if (nodeConfigs == null) return;  
    
    HashSet<Vector3Int> nodePositions = new HashSet<Vector3Int>();
    foreach (var config in nodeConfigs)
    {
        nodePositions.Add(new Vector3Int(config.xCoordinate, config.yCoordinate, config.zCoordinate));
    }
  
    var nodePrefab = Resources.Load("Prefab/LatticeNode") as GameObject;  
    foreach (var nodeConfig in nodeConfigs)  
    {        
        var obj = Instantiate(nodePrefab, Vector3.zero, Quaternion.identity, transform);  
        var pos = new Vector3(CellSize * (nodeConfig.xCoordinate + 0.5f),  
            CellSize * (nodeConfig.yCoordinate + 0.5f) + BaseHeight, CellSize * (nodeConfig.zCoordinate + 0.5f));  
        obj.transform.localPosition = pos + new Vector3(posOffset.x, 0, posOffset.z);  
        obj.transform.localScale = new Vector3(CellSize, CellSize, CellSize);  
        obj.transform.localRotation = Quaternion.identity;  
        
        Vector3Int currentPos = new Vector3Int(nodeConfig.xCoordinate, nodeConfig.yCoordinate, nodeConfig.zCoordinate);
        
        bool hasLeft   = nodePositions.Contains(currentPos + Vector3Int.left);    // (-1,  0,  0)
        bool hasRight  = nodePositions.Contains(currentPos + Vector3Int.right);   // ( 1,  0,  0)
        bool hasTop    = nodePositions.Contains(currentPos + Vector3Int.up);      // ( 0,  1,  0)
        bool hasBottom = nodePositions.Contains(currentPos + Vector3Int.down);    // ( 0, -1,  0)
        bool hasFront  = nodePositions.Contains(currentPos + Vector3Int.forward); // ( 0,  0,  1)
        bool hasBack   = nodePositions.Contains(currentPos + Vector3Int.back);    // ( 0,  0, -1)


        var latticeNode = obj.GetComponent<LatticeNode>();
        latticeNode.SetCellMaterial(cellMaterialsByPosition[(nodeConfig.xCoordinate, nodeConfig.zCoordinate)]);
        latticeNode.SetLattice(this);
        ConfigureNodeAdjacency(obj, hasLeft, hasRight, hasTop, hasBottom, hasFront, hasBack);
    }



}

private void Start()
{
    if (SaveSystem.GetPersistentEventCompleted(eventPrefix + id))
    {
        OnLatticeCompleted?.Invoke(this, false);
        StartCoroutine(CellCompleteCoroutine());
    }
}


private void ConfigureNodeAdjacency(GameObject instantiatedNode, bool left, bool right, bool top, bool bottom, bool front, bool back)
    {
        instantiatedNode.GetComponent<LatticeNode>().SetAdjacencies(left, right, top, bottom, front, back);
    }



    public void IncrementCompletedNodes()
    {
        _completedNodes++;
        if (_completedNodes == nodeConfigs.Count)
        {
            SaveSystem.WritePersistentEvent(eventPrefix + id);
            OnLatticeCompleted?.Invoke(this, true);
            StartCoroutine(CellCompleteCoroutine());
        };

        UpdateDialogue();
    }

    private void UpdateDialogue()
    {
        _dialogue.dialogues[0].texts[0] = _completedNodes == nodeConfigs.Count ? "Lattice successfully calibrated." : _completedNodes + " of " + nodeConfigs.Count + " lattice nodes are currently calibrated.";
    }

    private IEnumerator CellCompleteCoroutine()
    {
        var t = 0f;
        var d = 0.075f;
        while (t < d)
        {
            foreach (var material in _cellMaterials)
            {
                material.SetFloat("_CompleteWeight", t / d);
            }
            t += Time.deltaTime;
            yield return null;
        }
    }
}
