// AI assistance: Codex helped revise settings and scene flow. Ernest directed the design.
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{

    public string selectedWeapon; // or enum / ScriptableObject
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        GameObject quitButton = GameObject.Find("QuitGame");
        if (quitButton != null)
            quitButton.SetActive(false);
#endif
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
