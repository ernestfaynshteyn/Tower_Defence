using UnityEngine;

public class CloseOpenScript : MonoBehaviour
{
    public GameObject uiPanel;

    public GameObject openButton;
    public GameObject closeButton;

    public void OpenUI()
    {
        uiPanel.SetActive(true);
        openButton.SetActive(false);
        closeButton.SetActive(true);
    }

    public void CloseUI()
    {
        uiPanel.SetActive(false);
        openButton.SetActive(true);
        closeButton.SetActive(false);
    }
}