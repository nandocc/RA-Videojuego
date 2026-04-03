using System.Collections;
using UnityEngine;

public class HitFlash : MonoBehaviour
{
    [SerializeField] private Color flashColor = new Color(1f, 0.25f, 0.1f, 1f);
    [SerializeField] private float flashDuration = 0.25f;
    [SerializeField] private float scalePunch = 0.25f;

    private Renderer[] _renderers;
    private Color[] _baseColors;
    private Vector3 _baseScale;
    private bool _running;

    private void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);
        _baseColors = new Color[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null && _renderers[i].material != null)
                _baseColors[i] = _renderers[i].material.color;
        }
        _baseScale = transform.localScale;
    }

    public void Play()
    {
        if (_running)
            return;
        StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        _running = true;
        var t = 0f;
        var targetScale = _baseScale * (1f + scalePunch);

        while (t < flashDuration)
        {
            t += Time.deltaTime;
            var k = Mathf.Clamp01(t / flashDuration);
            var s = Vector3.Lerp(targetScale, _baseScale, k);
            transform.localScale = s;

            for (int i = 0; i < _renderers.Length; i++)
            {
                var r = _renderers[i];
                if (r == null || r.material == null) continue;
                r.material.color = Color.Lerp(flashColor, _baseColors[i], k);
            }
            yield return null;
        }

        // restore
        transform.localScale = _baseScale;
        for (int i = 0; i < _renderers.Length; i++)
        {
            var r = _renderers[i];
            if (r == null || r.material == null) continue;
            r.material.color = _baseColors[i];
        }
        _running = false;
    }
}
