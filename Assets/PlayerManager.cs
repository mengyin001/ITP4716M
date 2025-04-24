using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerManager : MonoBehaviour
{
    private static PlayerManager instance;

    public GameObject playerPrefab; // 玩家预制件
    private GameObject playerInstance; // 玩家实例

    private void Awake()
    {
        // 确保只存在一个 PlayerManager 实例
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject); // 保持 PlayerManager 在场景切换时不被销毁

        // 加载初始场景中的玩家
        LoadPlayer();
    }

    private void LoadPlayer()
    {
        // 检查当前场景是否有玩家对象
        playerInstance = GameObject.FindGameObjectWithTag("Player");
        if (playerInstance == null)
        {
            // 如果没有，实例化新玩家
            playerInstance = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
            playerInstance.tag = "Player"; // 设置玩家标签
            Debug.Log("玩家对象已实例化。");
        }
        else
        {
            Debug.Log("玩家对象已存在。");
        }
    }

    public void SwitchScene(string sceneName)
    {
        // 当需要切换场景时调用
        SceneManager.LoadScene(sceneName);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 在新场景加载后检查玩家对象
        LoadPlayer();
    }

    private void OnEnable()
    {
        // 注册场景加载事件
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        // 注销场景加载事件
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}