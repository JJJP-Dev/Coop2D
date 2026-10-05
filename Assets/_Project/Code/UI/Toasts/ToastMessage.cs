public readonly struct ToastMessage
{
    public readonly string Text;
    public readonly ToastType Type;

    public ToastMessage(string text, ToastType type)
    {
        Text = text;
        Type = type;
    }
}