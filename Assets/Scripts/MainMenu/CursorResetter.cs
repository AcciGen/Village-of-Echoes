using UnityEngine;

public class CursorResetter : MonoBehaviour
{
    void Awake()
    {
        UnlockCursor();
    }

    void Start()
    {
        UnlockCursor();
    }

    void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Time.timeScale = 1f;
    }
}
