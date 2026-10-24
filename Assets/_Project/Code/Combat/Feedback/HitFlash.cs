using UnityEngine;

// Turns this object's sprites white for a moment each time it takes a hit.
// It reacts to the hits the host confirmed, on every client, the host's included, so everyone sees
// the same flashes. Only the picture changes: it swaps materials and touches nothing else.
[RequireComponent(typeof(Health))]
public class HitFlash : MonoBehaviour
{
    [SerializeField] private FeedbackProfile _profile;

    [Tooltip("A material with the Coop2D/SpriteFlash shader. Shared by everything that flashes.")]
    [SerializeField] private Material _flashMaterial;

    [Tooltip("The sprites that turn white. Empty: every SpriteRenderer in its children.")]
    [SerializeField] private SpriteRenderer[] _renderers;

    private Health _health;
    private CombatEventService _events;
    private Material[] _originalMaterials;
    private float _flashEndsAt;
    private bool _isFlashing;

    private void Awake()
    {
        _health = GetComponent<Health>();

        if (_renderers == null || _renderers.Length == 0)
            _renderers = GetComponentsInChildren<SpriteRenderer>(true);

        _originalMaterials = new Material[_renderers.Length];

        if (_profile == null || _flashMaterial == null)
            Debug.LogError("[HitFlash] Feedback profile or flash material is not assigned.", this);
    }

    private void OnEnable()
    {
        if (GameServices.Instance == null)
            return;

        _events = GameServices.Instance.CombatEventService;
        _events.HitConfirmed += OnHitConfirmed;
    }

    // Pooled objects and those that leave mid-flash come back with their own material.
    private void OnDisable()
    {
        if (_events != null)
            _events.HitConfirmed -= OnHitConfirmed;

        _events = null;
        StopFlash();
    }

    private void Update()
    {
        // Real time: it's only the picture, and it shouldn't stretch if time is ever slowed.
        if (_isFlashing && Time.unscaledTime >= _flashEndsAt)
            StopFlash();
    }

    // Every hit reaches every HitFlash, so each one keeps only its own.
    private void OnHitConfirmed(HitInfo hit)
    {
        if (hit.Target != _health || _profile == null || _flashMaterial == null || _profile.HitFlashDuration <= 0f)
            return;

        // A second hit during the flash only makes it last longer.
        _flashEndsAt = Time.unscaledTime + _profile.HitFlashDuration;

        if (_isFlashing)
            return;

        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] == null)
                continue;

            // sharedMaterial, not material: reading .material would make a copy per sprite.
            _originalMaterials[i] = _renderers[i].sharedMaterial;
            _renderers[i].sharedMaterial = _flashMaterial;
        }

        _isFlashing = true;
    }

    private void StopFlash()
    {
        if (!_isFlashing)
            return;

        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null)
                _renderers[i].sharedMaterial = _originalMaterials[i];

            _originalMaterials[i] = null;
        }

        _isFlashing = false;
    }
}
