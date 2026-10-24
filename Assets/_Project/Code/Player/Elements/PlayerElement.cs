using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.InputSystem;

// The element this player plays. The host sets it from the player's character.
// F6 changes it in development builds, to test reactions without another player.
[RequireComponent(typeof(PlayerCharacter))]
public class PlayerElement : NetworkBehaviour
{
    [SerializeField] private ElementLibrary _library;

    private readonly SyncVar<ElementType> _element = new();

    private PlayerCharacter _character;
    private InputService _inputService;

    // The last element shown to the owner, so each change is announced once.
    private ElementType _announced;

    public ElementType Current => _element.Value;

    private void Awake()
    {
        _character = GetComponent<PlayerCharacter>();
        _element.OnChange += OnElementChanged;

        if (_library == null)
            Debug.LogError("[PlayerElement] Element library is not assigned.", this);

        // RequireComponent only adds it to new objects: an older prefab needs it added by hand.
        if (_character == null)
            Debug.LogError("[PlayerElement] The player has no PlayerCharacter: it gets no element.", this);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        // The character is set before Spawn, and a value set here still travels inside the spawn message.
        CharacterDefinition character = _character != null ? _character.Definition : null;
        _element.Value = character != null ? character.Element : ElementType.None;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!IsOwner)
            return;

        Announce();

        // A test key for editor and development builds: the character stays, only the element changes.
        if (Debug.isDebugBuild && GameServices.Instance != null)
        {
            _inputService = GameServices.Instance.InputService;
            _inputService.CycleElement.performed += OnCycleElement;
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();

        if (_inputService != null)
            _inputService.CycleElement.performed -= OnCycleElement;

        _inputService = null;

        // Pooled players come back with the value of their previous life.
        _announced = ElementType.None;
    }

    private void OnCycleElement(InputAction.CallbackContext context)
    {
        ServerCycleElement();
    }

    [ServerRpc]
    private void ServerCycleElement()
    {
        if (!Debug.isDebugBuild || _library == null)
            return;

        _element.Value = _library.GetNext(_element.Value);
        ObserversElementCycled();
    }

    // Every client writes it, so each launcher knows the match had a cheat in it.
    [ObserversRpc]
    private void ObserversElementCycled()
    {
        LauncherEvents.CheatUsed("element_cycle", IsOwner);
    }

    private void OnElementChanged(ElementType previous, ElementType next, bool asServer)
    {
        // The first value can arrive before OnStartClient, where IsOwner isn't reliable yet.
        // OnStartClient announces that one.
        if (asServer || !IsClientInitialized || !IsOwner)
            return;

        Announce();
    }

    private void Announce()
    {
        if (_element.Value == _announced || _library == null || GameServices.Instance == null)
            return;

        // Later changes come from the test key. The first one, from the character, is written by
        // LauncherEventReporter right after match_joined, so it falls inside the match.
        bool first = _announced == ElementType.None;
        if (!first)
            LauncherEvents.ElementSet(_element.Value, "debug_key");

        _announced = _element.Value;

        ElementDefinition element = _library.Get(_element.Value);
        string name = element != null ? element.DisplayName : _element.Value.ToString();

        // The first toast says who you play; the ones from the test key, only the element.
        CharacterDefinition character = _character != null ? _character.Definition : null;
        string text = first && character != null
            ? $"Juegas con {character.DisplayName} · {name}"
            : $"Tu elemento: {name}";
        GameServices.Instance.ToastService.Show(text, ToastType.Info);
    }
}
