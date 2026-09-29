using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Image killerImage;
    [SerializeField] private Sprite greenSlimePortrait;
    [SerializeField] private Sprite blueSlimePortrait;
    [SerializeField] private Sprite redSlimePortrait;

    private void Start()
    {
        if (killerImage == null)
            return;

        Sprite killerPortrait = null;
        if (GlobalData.Instance != null)
        {
            switch (GlobalData.Instance.lastEnemyThatKilledPlayer)
            {
                case "1":
                    killerPortrait = greenSlimePortrait;
                    break;
                case "2":
                    killerPortrait = blueSlimePortrait;
                    break;
                case "3":
                    killerPortrait = redSlimePortrait;
                    break;
            }
        }

        killerImage.sprite = killerPortrait;
        killerImage.gameObject.SetActive(killerPortrait != null);
    }

    public void RestartGame()
    {
        SceneManager.LoadScene("Game thing");
    }

    public void RestartGame2()
    {
        SceneManager.LoadScene("Game thing");
    }
}

