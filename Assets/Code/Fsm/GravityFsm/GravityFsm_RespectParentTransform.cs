using Code.TriggerParams;
using UnityEngine;

public abstract partial class GravityFsm
{


    public void RespectParentTransformOnUpdate()
    {

    }
    
    private void RespectParentTransformConfigure()
    {
        Machine.Configure(GravityFsmState.RespectParentTransform)
            .OnEntry(@params =>
            {

            });
    }

}