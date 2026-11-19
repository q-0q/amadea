using UnityEngine;

public class FollowTest : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Physics.Raycast(transform.position + Vector3.up * 5f, Vector3.down, out var hit, 20f,
                Fsm.GetEnvironmentalLayermask(), QueryTriggerInteraction.Ignore))
        {
            transform.position = hit.point + Vector3.up * 4f;
        }
    }
}
