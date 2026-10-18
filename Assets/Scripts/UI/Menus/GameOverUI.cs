using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Image killerImage;
    [SerializeField] private Sprite greenSlimePortrait;
    [SerializeField] private Sprite blueSlimePortrait;
    [SerializeField] private Sprite redSlimePortrait;
    [SerializeField] private Material slimeRecolorMaterial;
    [SerializeField] private TextMeshProUGUI recordsText;
    [SerializeField] private RectTransform retryButton;

    private void Start()
    {
        if (killerImage == null)
            return;

        GameRecords.Save();

        Sprite killerPortrait = null;
        Color killerColor = Color.white;
        if (GlobalData.Instance != null)
        {
            switch (GlobalData.Instance.lastEnemyThatKilledPlayer)
            {
                case "1":
                    killerPortrait = greenSlimePortrait;
                    killerColor = new Color(0.15f, 1f, 0.25f, 1f);
                    break;
                case "2":
                    killerPortrait = blueSlimePortrait;
                    killerColor = new Color(0.35f, 0.85f, 1f, 1f);
                    break;
                case "3":
                    killerPortrait = redSlimePortrait;
                    killerColor = new Color(1f, 0.18f, 0.08f, 1f);
                    break;
                case "4":
                    killerPortrait = greenSlimePortrait;
                    killerColor = new Color(1f, 0.82f, 0.08f, 1f);
                    break;
                case "5":
                    killerPortrait = greenSlimePortrait;
                    killerColor = new Color(0.72f, 0.18f, 1f, 1f);
                    break;
                case "6":
                    killerPortrait = greenSlimePortrait;
                    killerColor = new Color(1f, 0.2f, 0.65f, 1f);
                    break;
            }
        }

        killerImage.sprite = killerPortrait;
        killerImage.color = killerColor;
        if (slimeRecolorMaterial != null)
            killerImage.material = slimeRecolorMaterial;
        killerImage.gameObject.SetActive(killerPortrait != null);
        ShowRecords();
    }

    private void ShowRecords()
    {
        if (recordsText == null && killerImage != null && killerImage.canvas != null)
        {
            GameObject labelObject = new GameObject("Run Records", typeof(RectTransform));
            labelObject.transform.SetParent(killerImage.canvas.transform, false);
            recordsText = labelObject.AddComponent<TextMeshProUGUI>();

            RectTransform rect = recordsText.rectTransform;
            rect.anchorMin = retryButton != null ? retryButton.anchorMin : new Vector2(0.5f, 0.5f);
            rect.anchorMax = retryButton != null ? retryButton.anchorMax : new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            if (retryButton != null)
            {
                rect.anchoredPosition = retryButton.anchoredPosition + Vector2.down *
                    (retryButton.rect.height * Mathf.Abs(retryButton.localScale.y) * 0.5f + 12f);
                rect.sizeDelta = new Vector2(
                    Mathf.Max(360f, retryButton.rect.width * Mathf.Abs(retryButton.localScale.x)),
                    280f);
            }
            else
            {
                rect.anchoredPosition = new Vector2(0f, -120f);
                rect.sizeDelta = new Vector2(600f, 280f);
            }

            recordsText.fontSize = 48f;
            recordsText.fontStyle = FontStyles.Bold;
            recordsText.alignment = TextAlignmentOptions.Top;
            recordsText.color = Color.black;
            recordsText.textWrappingMode = TextWrappingModes.Normal;
            recordsText.raycastTarget = false;
        }

        if (recordsText != null)
            recordsText.text = GameRecords.GetSummaryText();
    }

    public void RestartGame()
    {
        LoadGameplayScene();
    }

    public void RestartGame2()
    {
        LoadGameplayScene();
    }

    private static void LoadGameplayScene()
    {
        string activeScene = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(PrototypeSceneRoutes.IsPrototypeLoss(activeScene)
            ? PrototypeSceneRoutes.GameplayScene
            : "Game thing");
    }
}

