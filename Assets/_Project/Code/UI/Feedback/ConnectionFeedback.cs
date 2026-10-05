using UnityEngine;

public class ConnectionFeedback : MonoBehaviour
{
    private void Start()
    {
        ConnectionService.Instance.ClientConnectionStateChanged += OnConnectionStateChanged;
    }

    private void OnDestroy()
    {
        if (ConnectionService.Instance != null)
            ConnectionService.Instance.ClientConnectionStateChanged -= OnConnectionStateChanged;
    }

    private void OnConnectionStateChanged(SessionState state)
    {
        Debug.Log($"[ConnectionFeedback] Received state: {state}");

        switch (state)
        {
            case SessionState.Connected:
                ToastService.Show("Conectado", ToastType.Success);
                break;

            case SessionState.Failed:
                HandleConnectionFailed();
                break;

            case SessionState.Lost:
                ToastService.Show("El host ha cerrado la partida", ToastType.Error);
                break;

            case SessionState.Disconnected:
                ToastService.Show("Has salido de la partida", ToastType.Info);
                break;
        }
    }

    private void HandleConnectionFailed()
    {
        switch (ConnectionService.Instance.LastFailReason)
        {
            case FailReason.Timeout:
            case FailReason.Unreachable:

                ToastService.Show(
                    "No se encontró el host",
                    ToastType.Error
                );

                break;

            case FailReason.ServerFailed:

                ToastService.Show(
                    "No se pudo hospedar: el puerto está ocupado",
                    ToastType.Error
                );

                break;
        }
    }
}