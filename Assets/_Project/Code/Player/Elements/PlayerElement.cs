using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.InputSystem;

// The element this player plays. The host sets it from the player's slot.
[RequireComponent(typeof(PlayerIdentity))]
public class PlayerElement : NetworkBehaviour
{
    [SerializeField] private PlayerElements _slotElements;
    [SerializeField] private ElementLibrary _library;

    private readonly SyncVar<ElementType> _element = new();

    private PlayerIdentity _identity;
    private InputService _inputService;

    // The last element shown to the owner, so each change is announced once.
    private ElementType _announced;

    public ElementType Current => _element.Value;

    private void Awake()
    {
        _identity = GetComponent<PlayerIdentity>();
        _element.OnChange += OnElementChanged;

        if (_slotElements == null || _library == null)
            Debug.LogError("[PlayerElement] Slot elements or element library are not assigned.", this);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        // The slot is set before Spawn, and a value set here still travels inside the spawn message.
        if (_slotElements != null)
            _element.Value = _slotElements.GetElement(_identity.Slot);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!IsOwner)
            return;

        Announce();

        // A test key for editor and development builds, until characters exist (#162).
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

        // Later changes come from the test key. The first one, from the slot, is written by
        // LauncherEventReporter right after match_joined, so it falls inside the match.
        if (_announced != ElementType.None)
            LauncherEvents.ElementSet(_element.Value, "debug_key");

        _announced = _element.Value;

        ElementDefinition element = _library.Get(_element.Value);
        string name = element != null ? element.DisplayName : _element.Value.ToString();
        GameServices.Instance.ToastService.Show($"Tu elemento: {name}", ToastType.Info);
    }
}
