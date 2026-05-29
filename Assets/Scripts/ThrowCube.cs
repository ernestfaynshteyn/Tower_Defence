using System.Collections;
using UnityEngine;

public class ThrowCubeFadeSpawner : MonoBehaviour
{
    [Header("Fade Settings")]
    public float fadeDuration = 3f;

    public void SpawnFadeCube()
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);

        cube.transform.position = transform.position;
        cube.transform.localScale = new Vector3(3f, 3f, 3f);

        Material mat = new Material(Shader.Find("Standard"));
        mat.color = Color.white;

        // Make material transparent
        mat.SetFloat("_Mode", 3);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;

        cube.GetComponent<Renderer>().material = mat;

        StartCoroutine(FadeOutAndDestroy(cube, mat));
    }

    private IEnumerator FadeOutAndDestroy(GameObject cube, Material mat)
    {
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;

            float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            mat.color = new Color(1f, 1f, 1f, alpha);

            yield return null;
        }

        Destroy(cube);
    }
}