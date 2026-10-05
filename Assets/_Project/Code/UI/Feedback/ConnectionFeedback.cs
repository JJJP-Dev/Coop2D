using UnityEngine;

public class ConnectionFeedback : MonoBehaviour
{
    private void Start()
    {
        GameServices.Instance.ConnectionService.ClientConnectionStateChanged += OnConnectionStateChanged;
    }

    private void OnDestroy()
    {
        GameServices.Instance.ConnectionService.ClientConnectionStateChanged -= OnConnectionStateChanged;
    }

    private void OnConnectionStateChanged(SessionState state)
    {
        Debug.Log($"[ConnectionFeedback] Received state: {state}");

        switch (state)
        {
            case SessionState.Connected:
                GameServices.Instance.ToastService.Show("Conectado", ToastType.Success);
                break;

            case SessionState.Failed:
                HandleConnectionFailed();
                break;

            case SessionState.Lost:
                GameServices.Instance.ToastService.Show("El host ha cerrado la partida", ToastType.Error);
                break;

            case SessionState.Disconnected:
                GameServices.Instance.ToastService.Show("Has salido de la partida", ToastType.Info);
                break;
        }
    }

    private void HandleConnectionFailed()
    {
        switch (GameServices.Instance.ConnectionService.LastFailReason)
        {
            case FailReason.Timeout:
            case FailReason.Unreachable:

                GameServices.Instance.ToastService.Show(
                    "No se encontró el host",
                    ToastType.Error
                );

                break;

            case FailReason.ServerFailed:

                GameServices.Instance.ToastService.Show(
                    "No se pudo hospedar: el puerto está ocupado",
                    ToastType.Error
                );

                break;
        }
    }
}