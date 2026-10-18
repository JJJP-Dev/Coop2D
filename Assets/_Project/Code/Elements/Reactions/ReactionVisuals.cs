using UnityEngine;

// Draws each reaction on this client from the host's events: a burst where it triggers,
// the Vapor cloud while it lasts and the Electrocution chain. Presentation only.
public class ReactionVisuals : MonoBehaviour
{
    [SerializeField] private SkillCircleVisual _circlePrefab;
    [SerializeField] private ChainVisual _chainPrefab;

    [Tooltip("Burst radius, in units, for reactions that only hit their target.")]
    [SerializeField, Min(0.1f)] private float _singleTargetBurstRadius = 0.75f;

    [Tooltip("Opacity of the cloud compared with the burst, so enemies inside stay visible.")]
    [SerializeField, Range(0f, 1f)] private float _cloudOpacity = 0.6f;

    private CombatEventService _events;

    private void OnEnable()
    {
        if (_circlePrefab == null || _chainPrefab == null)
        {
            Debug.LogError("[ReactionVisuals] Circle or chain prefab is not assigned.", this);
            return;
        }

        _events = GameServices.Instance.CombatEventService;
        _events.ReactionTriggered += OnReactionTriggered;
        _events.ChainTriggered += OnChainTriggered;
    }

    private void OnDisable()
    {
        if (_events == null)
            return;

        _events.ReactionTriggered -= OnReactionTriggered;
        _events.ChainTriggered -= OnChainTriggered;
        _events = null;
    }

    private void OnReactionTriggered(ReactionInfo reaction)
    {
        ReactionDefinition definition = reaction.Reaction;
        float burstRadius = definition.DamageRadius > 0f ? definition.DamageRadius : _singleTargetBurstRadius;

        Instantiate(_circlePrefab, reaction.Position, Quaternion.identity).Show(burstRadius, definition.Color);

        if (definition.CloudDuration > 0f)
        {
            Color cloudColor = definition.Color;
            cloudColor.a = _cloudOpacity;
            Instantiate(_circlePrefab, reaction.Position, Quaternion.identity).Show(definition.CloudRadius, cloudColor, definition.CloudDuration);
        }
    }

    private void OnChainTriggered(ReactionChain chain)
    {
        Instantiate(_chainPrefab).Show(chain.Points, chain.Color);
    }
}
