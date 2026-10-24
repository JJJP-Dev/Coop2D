public readonly struct ToastMessage
{
    public readonly string Text;
    public readonly ToastType Type;

    // Seconds on screen; 0 takes the service's default.
    public readonly float Duration;

    public ToastMessage(string text, ToastType type, float duration = 0f)
    {
        Text = text;
        Type = type;
        Duration = duration;
    }
}