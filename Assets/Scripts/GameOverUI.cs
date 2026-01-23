using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    public TextMeshProUGUI killedByText;
    public Image image;

    void Start()
    {
        killedByText.text =
            "Killed by: " + GlobalData.Instance.lastEnemyThatKilledPlayer;

    }
}
