using UnityEngine;

public class ReverbConfig : MonoBehaviour
{
    
    public string ReverbType = "LargeOutdoors";
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        FMODUnity.RuntimeManager.StudioSystem.setParameterByNameWithLabel("WorldReverbType", ReverbType);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
