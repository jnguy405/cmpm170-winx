using UnityEngine;

public class PauseManager : MonoBehaviour
{
    void Start()
    {
        
    }
    // listen for the escape key to pause the game and open the pause menu
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // toggle the pause menu
        }
    }
}
