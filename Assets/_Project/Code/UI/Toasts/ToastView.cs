using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ToastView : MonoBehaviour
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private TMP_Text _messageText;
    [SerializeField] private Image _background;

    public void SetMessage(string message)
    {
        _messageText.text = message;
    }

    public void SetBackgroundColor(Color color)
    {
        _background.color = color;
    }

    public void SetAlpha(float alpha)
    {
        _canvasGroup.alpha = alpha;
    }
}