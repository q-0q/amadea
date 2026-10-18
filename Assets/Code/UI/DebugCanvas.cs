using UnityEngine;

public class DebugCanvas : MonoBehaviour
{
    public GameObject children;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        children.SetActive(!PhotoManager.Singleton.IsActive());
    }
}
