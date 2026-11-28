// AI assistance: Codex helped revise settings and scene flow. Ernest directed the design.
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{

    public string selectedWeapon; // or enum / ScriptableObject

    private void Awake()
    {
        // Bind the active title button directly so an outdated saved OnClick
        // reference cannot leave Start Game without its scene-loading action.
        Button startButton = GetComponent<Button>();
        if (startButton != null)
        {
            startButton.onClick = new Button.ButtonClickedEvent();
            startButton.onClick.AddListener(ChangeToArmoryPrototypeSelection);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameObject quitButton = GameObject.Find("QuitGame");
        if (quitButton != null && quitButton.TryGetComponent(out Button quit))
        {
            quit.onClick = new Button.ButtonClickedEvent();
            quit.onClick.AddListener(QuitGame);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void changeToScenelevelThing(int index)       
    {
        SceneManager.LoadScene(index);
    }

    public void ChangeToArmoryPrototypeSelection()
    {
        SceneManager.LoadScene(PrototypeSceneRoutes.SelectionScene);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

}
