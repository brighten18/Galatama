using UnityEngine;
using StarterAssets;
using System.Collections.Generic;

public class RippleTrigger : MonoBehaviour
{
    [SerializeField] private ParticleSystem ripple;

    private readonly HashSet<Collider> activeWaterInteractors = new HashSet<Collider>();

    private void Awake()
    {
        StopRipple(clearParticles: true);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayerInteractor(other))
        {
            return;
        }

        activeWaterInteractors.Add(other);

        if (activeWaterInteractors.Count == 1)
        {
            PlayRipple();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!activeWaterInteractors.Remove(other))
        {
            return;
        }

        if (activeWaterInteractors.Count == 0)
        {
            StopRipple(clearParticles: true);
        }
    }

    private void OnDisable()
    {
        activeWaterInteractors.Clear();
        StopRipple(clearParticles: true);
    }

    private bool IsPlayerInteractor(Collider other)
    {
        return other != null &&
               (other.GetComponentInParent<ThirdPersonController>() != null ||
                other.GetComponentInParent<CharacterController>() != null);
    }

    private void PlayRipple()
    {
        if (ripple == null)
        {
            return;
        }

        ripple.gameObject.SetActive(true);

        if (!ripple.isPlaying)
        {
            ripple.Play();
        }
    }

    private void StopRipple(bool clearParticles)
    {
        if (ripple == null)
        {
            return;
        }

        ripple.Stop(true, clearParticles
            ? ParticleSystemStopBehavior.StopEmittingAndClear
            : ParticleSystemStopBehavior.StopEmitting);

        ripple.gameObject.SetActive(false);
    }
}
