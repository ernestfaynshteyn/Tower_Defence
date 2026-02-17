using UnityEngine;
using UnityEngine.UI; // Required for UI elements

public class SkillTreeSwitch : MonoBehaviour
{
    public GameObject window1; // Drag your object here in the Inspector
    public GameObject window2; // Drag your object here in the Inspector

    // Public method to be called by the Toggle's On Value Changed event
    public void ToggleObject(int index)
    {
        switch (index)
        {
            case 0:
                window1.SetActive(true);
                window2.SetActive(false);
                break;
            case 1:
                window1.SetActive(false);
                window2.SetActive(true);
                break;
        }
    }
}
