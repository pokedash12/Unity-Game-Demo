using UnityEngine;

public class PersistentManager : MonoBehaviour
{
    private static PersistentManager instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // If another database already exists, destroy this new one
            Destroy(gameObject);
        }
    }
}