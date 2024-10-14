using UnityEngine;
using UnityEngine.InputSystem;

public class PauseOnSpaceBar : MonoBehaviour
{
    void Update()
    {
        if( Keyboard.current.spaceKey.wasPressedThisFrame ) {
            Debug.Break();
        }
    }
}
