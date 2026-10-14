using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Image killerImage;
    [SerializeField] private Sprite greenSlimePortrait;
    [SerializeField] private Sprite blueSlimePortrait;
    [SerializeField] private Sprite redSlimePortrait;
    [SerializeField] private Material slimeRecolorMaterial;

    private void Start()
    {
        if (killerImage == null)
            return;

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

