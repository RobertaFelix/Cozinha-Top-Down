using UnityEngine;

[DefaultExecutionOrder(-10000)]
public class PersistentObjects : MonoBehaviour
{
    private static PersistentObjects instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            // Stop duplicate children before their Awake can replace live UI singletons.
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        instance = this;

        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
