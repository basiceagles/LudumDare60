using UnityEngine;

public class UnlockMouseState : MonoBehaviour
{
    public void UnlockMouseStateMethod()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;  
    }
}
