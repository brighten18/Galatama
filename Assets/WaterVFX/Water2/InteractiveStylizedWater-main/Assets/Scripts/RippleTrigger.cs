using UnityEngine;
using StarterAssets;
using System.Collections.Generic;

public class RippleTrigger : MonoBehaviour
{
    [SerializeField] private ParticleSystem ripple;
    [SerializeField] private float validationInterval = 0.25f;

    private readonly HashSet<Collider> activeWaterInteractors = new HashSet<Collider>();
    private Collider waterTrigger;
    private float validationTimer;

    private void Awake()
    {
        waterTrigger = GetComponent<Collider>();
        StopRipple(clearParticles: true);
    }

    private void Update()
    {
        validationTimer += Time.deltaTime;
        if (validationTimer < validationInterval)
        {
            return;
        }

        validationTimer = 0f;
        ValidateActiveInteractors();
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

    private void ValidateActiveInteractors()
    {
        if (activeWaterInteractors.Count == 0)
        {
            StopRipple(clearParticles: true);
            return;
        }

        activeWaterInteractors.RemoveWhere(interactor =>
            interactor == null ||
            !interactor.enabled ||
            !interactor.gameObject.activeInHierarchy ||
            !IsPlayerInteractor(interactor) ||
            !IsInsideWaterTrigger(interactor));

        if (activeWaterInteractors.Count == 0)
        {
            StopRipple(clearParticles: true);
        }
    }

    private bool IsInsideWaterTrigger(Collider interactor)
    {
        if (waterTrigger == null || interactor == null)
        {
            return false;
        }

        return waterTrigger.bounds.Intersects(interactor.bounds);
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
