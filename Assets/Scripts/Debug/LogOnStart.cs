using UnityEngine;

public class LogOnStart : MonoBehaviour {
    public string message;

    void Start() {
        Debug.Log( message );
    }
}
