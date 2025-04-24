using UnityEngine;

public class MyBagManager : MonoBehaviour
{
    public static MyBagManager Instance;
    public GameObject myBag;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}