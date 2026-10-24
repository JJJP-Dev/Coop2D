using UnityEngine;

// Draws this player as its character on every PC: the sprite of its CharacterDefinition on the
// player's Visual. The character travels with the player (PlayerCharacter), so there's nothing to send.
// Polled rather than told: the id can arrive with the spawn, before or after this object wakes up,
// and checking a reference once a frame costs nothing.
// Only the sprite changes, so the hit flash, the downed fade and the colour marker keep working as
// they are. When characters get animations, this is where their animated visual will be swapped in.
[RequireComponent(typeof(PlayerCharacter))]
public class PlayerCharacterView : MonoBehaviour
{
    [Tooltip("The SpriteRenderer of the player's Visual child.")]
    [SerializeField] private SpriteRenderer _renderer;

    private PlayerCharacter _character;
    private CharacterDefinition _shown;
    private Sprite _prefabSprite;

    private void Awake()
    {
        _character = GetComponent<PlayerCharacter>();

        if (_renderer == null)
            Debug.LogError("[PlayerCharacterView] Renderer is not assigned.", this);
        else
            _prefabSprite = _renderer.sprite;
    }

    private void Update()
    {
        if (_renderer == null)
            return;

        CharacterDefinition character = _character.Definition;
        if (character == _shown)
            return;

        _shown = character;
        _renderer.sprite = character != null && character.Sprite != null ? character.Sprite : _prefabSprite;
    }
}
