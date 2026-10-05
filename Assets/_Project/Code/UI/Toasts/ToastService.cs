using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToastService : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ToastView _view;

    [Header("Colors")]
    [SerializeField] private Color _infoColor = Color.white;
    [SerializeField] private Color _successColor = Color.green;
    [SerializeField] private Color _warningColor = Color.yellow;
    [SerializeField] private Color _errorColor = Color.red;

    [Header("Timing")]
    [SerializeField] private float _fadeInDuration = 0.2f;
    [SerializeField] private float _displayDuration = 2f;
    [SerializeField] private float _fadeOutDuration = 0.2f;

    private readonly Queue<ToastMessage> _queue = new();

    private Coroutine _showCoroutine;

    private void Awake()
    {
        _view.SetAlpha(0f);
    }

    public void Show(string text, ToastType type)
    {
        Enqueue(text, type);
    }

    private void Enqueue(string text, ToastType type)
    {
        _queue.Enqueue(new ToastMessage(text, type));

        if (_showCoroutine == null)
            _showCoroutine = StartCoroutine(ProcessQueue());
    }

    private IEnumerator ProcessQueue()
    {
        while (_queue.Count > 0)
        {
            ToastMessage message = _queue.Dequeue();

            _view.SetMessage(message.Text);
            _view.SetBackgroundColor(GetColor(message.Type));

            yield return Fade(0f, 1f, _fadeInDuration);

            yield return new WaitForSecondsRealtime(_displayDuration);

            yield return Fade(1f, 0f, _fadeOutDuration);
        }

        _showCoroutine = null;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            _view.SetAlpha(to);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            _view.SetAlpha(Mathf.Lerp(from, to, t));

            yield return null;
        }

        _view.SetAlpha(to);
    }

    private Color GetColor(ToastType type)
    {
        return type switch
        {
            ToastType.Info => _infoColor,
            ToastType.Success => _successColor,
            ToastType.Warning => _warningColor,
            ToastType.Error => _errorColor,
            _ => Color.white
        };
    }
}