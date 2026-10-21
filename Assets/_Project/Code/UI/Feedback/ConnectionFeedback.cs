using UnityEngine;

public class ConnectionFeedback : MonoBehaviour
{
    private GameServices _gameServices;

    private void Start()
    {
        _gameServices = GameServices.Instance;

        _gameServices.ConnectionService.ClientConnectionStateChanged += OnConnectionStateChanged;
    }

    private void OnDestroy()
    {
        if (_gameServices != null)
        {
            _gameServices.ConnectionService.ClientConnectionStateChanged -= OnConnectionStateChanged;
        }
    }

    private void OnConnectionStateChanged(SessionState state)
    {
        Debug.Log($"[ConnectionFeedback] Received state: {state}");

        switch (state)
        {
            case SessionState.Connected:
                _gameServices.ToastService.Show("Conectado", ToastType.Success);
                break;

            case SessionState.Failed:
                HandleConnectionFailed();
                break;

            case SessionState.Lost:
                if (_gameServices.ConnectionService.HostClosedSession)
                    _gameServices.ToastService.Show("El host ha cerrado la partida", ToastType.Error);
                else
                    _gameServices.ToastService.Show("Se ha perdido la conexión con el host", ToastType.Error);
                break;

            case SessionState.Disconnected:
                _gameServices.ToastService.Show("Has salido de la partida", ToastType.Info);
                break;
        }
    }

    private void HandleConnectionFailed()
    {
        switch (_gameServices.ConnectionService.LastFailReason)
        {
            case FailReason.Timeout:
            case FailReason.Unreachable:

                _gameServices.ToastService.Show(
                    "No se encontró el host",
                    ToastType.Error
                );

                break;

            case FailReason.ServerFailed:

                _gameServices.ToastService.Show(
                    "No se pudo hospedar: el puerto está ocupado",
                    ToastType.Error
                );

                break;

            case FailReason.VersionMismatch:

                _gameServices.ToastService.Show(
                    $"Versión distinta: el host tiene la {_gameServices.ConnectionService.HostVersion} y tú la {Application.version}",
                    ToastType.Error
                );

                break;
        }
    }
}
