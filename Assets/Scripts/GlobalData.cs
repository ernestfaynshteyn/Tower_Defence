using UnityEngine;
using UnityEngine.UI;

public class GlobalData : MonoBehaviour
{
    public static GlobalData Instance;

    public string lastEnemyThatKilledPlayer;
    public Image image;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
