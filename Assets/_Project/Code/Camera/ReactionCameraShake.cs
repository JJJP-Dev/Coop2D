using Unity.Cinemachine;
using UnityEngine;

// Shakes the camera a little for each reaction this client sees, more for a duo. Presentation only.
// It goes on the Cinemachine camera, which also needs a CinemachineImpulseListener to feel it.
[RequireComponent(typeof(CinemachineImpulseSource))]
public class ReactionCameraShake : MonoBehaviour
{
    [SerializeField] private ReactionShakeSettings _settings;

    private CinemachineImpulseSource _source;
    private CombatEventService _events;
    private float _pendingStrength;
    private float _nextShakeTime;

    private void Awake()
    {
        _source = GetComponent<CinemachineImpulseSource>();

        if (_settings == null)
            Debug.LogError("[ReactionCameraShake] Shake settings are not assigned.", this);
    }

    private void OnEnable()
    {
        if (_settings == null || GameServices.Instance == null)
            return;

        _events = GameServices.Instance.CombatEventService;
        _events.ReactionTriggered += OnReactionTriggered;
    }

    private void OnDisable()
    {
        if (_events == null)
            return;

        _events.ReactionTriggered -= OnReactionTriggered;
        _events = null;
    }

    // One cast can trigger a dozen at once: only the strongest of the frame shakes.
    private void OnReactionTriggered(ReactionInfo reaction)
    {
        if (Vector2.Distance(reaction.Position, transform.position) > _settings.MaxDistance)
            return;

        float strength = reaction.IsDuo ? _settings.DuoStrength : _settings.SoloStrength;
        _pendingStrength = Mathf.Max(_pendingStrength, strength);
    }

    private void LateUpdate()
    {
        if (_pendingStrength <= 0f)
            return;

        // Reactions during the pause after a shake are dropped, not saved for later.
        if (Time.time >= _nextShakeTime)
        {
            float angle = Random.Range(0f, 2f * Mathf.PI);
            Vector3 direction = new(Mathf.Cos(angle), Mathf.Sin(angle), 0f);

            _source.GenerateImpulseWithVelocity(direction * _pendingStrength);
            _nextShakeTime = Time.time + _settings.MinInterval;
        }

        _pendingStrength = 0f;
    }
}
