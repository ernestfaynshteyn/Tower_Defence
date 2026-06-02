using System.Collections;
using UnityEngine;

public class FlashFade : MonoBehaviour
{
    public float fadeDuration = 3f;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    private void OnEnable()
    {
        StartCoroutine(FadeOut());
    }

    private IEnumerator FadeOut()
    {
        if (spriteRenderer == null)
        {
            Debug.LogWarning("FlashFade could not find a SpriteRenderer.");
            yield break;
        }

        float timer = 0f;

        Color color = spriteRenderer.color;
        color.a = 1f;
        spriteRenderer.color = color;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;

            float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);

            color.a = alpha;
            spriteRenderer.color = color;

            yield return null;
        }

        Destroy(gameObject);
    }
}