using System.Collections.Generic;
using UnityEngine;

public class AdvancementController : MonoBehaviour
{

    public int InstancePositionId = 0;

    public List<string> AdvancementEvents = new List<string>(); 
    
    void Start()
    {
        for (int i = 0; i < AdvancementEvents.Count; i++)
        {
            if (i < InstancePositionId && !SaveSystem.GetPersistentEventCompleted(AdvancementEvents[i]))
            {
                Destroy(transform.parent.gameObject);  // if condition for any previous position is not met, destroy (instance hasnt arrived here yet)
                return;
            }
            
            if (i == InstancePositionId && SaveSystem.GetPersistentEventCompleted(AdvancementEvents[i]))
            {
                Destroy(transform.parent.gameObject); // if condition for current position is met, destroy (instance has advanced from here)
                return;
            }
            
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
