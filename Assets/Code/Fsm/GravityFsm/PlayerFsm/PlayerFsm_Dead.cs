using System;
using System.Collections;
using Code.Misc;
using DG.Tweening;
using UnityEngine;

public partial class PlayerFsm
{
    public static event Action OnPlayerRespawn;

    private void DyingOnUpdate()
    {
        Shader.SetGlobalFloat("_PlayerTintWeight", TimeInCurrentState() / 0.3f);
    }
    
    private void RespawnOnUpdate()
    {
        Shader.SetGlobalFloat("_PlayerTintWeight", Mathf.InverseLerp(0.5f, 0, TimeInCurrentState()));
    }

    private IEnumerator ResetAfterDelay()
    {
        yield return new WaitForSeconds(0.275f);
        // Reset();
        
        
    }
    
    private void DeathConfigure()
    {
        Machine.Configure(PlayerFsmState.Dying)
            .Permit(PlayerFsmTrigger.Timeout, PlayerFsmState.Dead)
            .SubstateOf(PlayerFsmState.DisplaceFoliage)
            .SubstateOf(PlayerFsmState.SentryImmune)
            .OnEntry(_ =>
            {
                FMODUnity.RuntimeManager.PlayOneShot(deathFmodEvent);
                Time.timeScale = 1f;
                YVelocity = 0;
                
                Shader.SetGlobalColor("_PlayerTintColor", Color.white);
                Animator.StartPlayback();
                Animator.enabled = false;
                isSprinting = false;
                _isSurgeQueued = false;
                _deathParticles.PlayDeath();
                // _impulse.GenerateImpulse();

                
                EndSurge();
                
                StartCoroutine(SoundCoroutine());
                IEnumerator SoundCoroutine()
                {
                    
                    yield return new WaitForSeconds(0.75f);
                    
                    var t = 0f;
                    var d = 0.75f;
                    while (t < d)
                    {
                        FMODUnity.RuntimeManager.StudioSystem.setParameterByName("RespawnAmbientAmount", t / d);
                        t += Time.deltaTime;
                        yield return null;
                    }

                    // yield return new WaitForSeconds(0.35f);
                    
                    t = 0f;
                    d = 2f;
                    while (t < d)
                    {
                        FMODUnity.RuntimeManager.StudioSystem.setParameterByName("RespawnAmbientAmount", 1f - (t / d));
                        t += Time.deltaTime;
                        yield return null;
                    }
                    
                    FMODUnity.RuntimeManager.StudioSystem.setParameterByName("RespawnAmbientAmount", 0f);
                }
                
            });
        
        Machine.Configure(PlayerFsmState.Dead)
            .Permit(FsmTrigger.Timeout, PlayerFsmState.Respawn)
            .SubstateOf(PlayerFsmState.SentryImmune)
            .OnEntry(_ =>
            {
                YVelocity = 0;
                // _skinnedMeshRenderer.transform.DOShakePosition(0.35f, 0.25f, 20);
                _deathParticles.transform.position = transform.position;
                MakeAllRenderersInvisible();
            });
        
        
        Machine.Configure(PlayerFsmState.Respawn)
            .Permit(FsmTrigger.Timeout, PlayerFsmState.Idle)
            .SubstateOf(GravityFsmState.Grounded)
            .SubstateOf(PlayerFsmState.SentryImmune)
            .OnEntry(_ =>
            {
                _timeSinceRespawn = 0;
                _momentum = 0;
                LastUpwardsY = transform.position.y;
                // _skinnedMeshRenderer.transform.DOComplete();
                MakeAllRenderersVisible();
                Animator.StopPlayback();
                Animator.enabled = true;
                Animator.SetLayerWeight(1, 0);
                Shader.SetGlobalFloat("_PlayerTintWeight", 1f);
                Shader.SetGlobalFloat("_PlayerEvaporateClip", 1f);

                var initialPosition = transform.position;
                transform.position = _safeGroundPosition;
                OnPlayerTeleported?.Invoke(transform.position - initialPosition);
                

                
                
                
                
                IEnumerator ClipCoroutine()
                {
                    yield return new WaitForSeconds(0.1f);
                    // Util.InvokeSphereEffect(transform.position, Vector3.one * 4f, 1.25f, 0.8f, -0.5f);
                    yield return new WaitForSeconds(0.1f);
                    var t = 0f;
                    var d = 0.5f;
                    while (t < d)
                    {
                        Shader.SetGlobalFloat("_PlayerEvaporateClip", 1f - Util.SmoothLerp01(t / d));
                        t += Time.deltaTime;
                        yield return null;
                    }
                }
                
                IEnumerator TintCoroutine()
                {
                    yield return new WaitForSeconds(0.1f);
                    // Util.InvokeSphereEffect(transform.position, Vector3.one * 4f, 1.25f, 0.8f, -0.5f);
                    yield return new WaitForSeconds(0.1f);
                    var t = 0f;
                    var d = 1f;
                    while (t < d)
                    {
                        Shader.SetGlobalFloat("_PlayerTintWeight", 1f - Util.SmoothLerp01(t / d));
                        t += Time.deltaTime;
                        yield return null;
                    }
                }
                
                StartCoroutine(TintCoroutine());
                StartCoroutine(ClipCoroutine());
            })
            .OnExit(_ =>
            {
                Shader.SetGlobalFloat("_PlayerTintWeight", 0);
                
            });
    }
}