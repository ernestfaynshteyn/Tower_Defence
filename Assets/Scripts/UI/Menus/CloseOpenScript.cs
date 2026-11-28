// AI assistance: Codex helped revise menu behavior and layouts. Ernest directed the design.
using UnityEngine;
using UnityEngine.UI;

public class CloseOpenScript : MonoBehaviour
{
    public GameObject uiPanel;

    public GameObject openButton;
    public GameObject closeButton;

    private void Awake()
    {
        // The shop also has a legacy copy of this component. Only the
        // controller beside the two buttons should initialize the tree UI.
        if (uiPanel == null || openButton == null || closeButton == null || uiPanel == gameObject)
            return;

        // This canvas keeps the buttons clickable above the shop's canvas.
        Canvas buttonCanvas = GetComponent<Canvas>();
        if (buttonCanvas == null)
            buttonCanvas = gameObject.AddComponent<Canvas>();
        buttonCanvas.overrideSorting = true;
        buttonCanvas.sortingOrder = 251;
        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        uiPanel.SetActive(false);
        openButton.SetActive(true);
        closeButton.SetActive(false);
    }

    public void OpenUI()
    {
        BackgroundMusic.TabOpened();
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
