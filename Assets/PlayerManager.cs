using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerManager : MonoBehaviour
{
    private static PlayerManager instance;

    public GameObject playerPrefab; // ���Ԥ�Ƽ�
    private GameObject playerInstance; // ���ʵ��

    private void Awake()
    {
        // ȷ��ֻ����һ�� PlayerManager ʵ��
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject); // ���� PlayerManager �ڳ����л�ʱ��������

        // ���س�ʼ�����е����
        LoadPlayer();
    }

    private void LoadPlayer()
    {
        // ��鵱ǰ�����Ƿ�����Ҷ���
        playerInstance = GameObject.FindGameObjectWithTag("Player");
        if (playerInstance == null)
        {
            // ���û�У�ʵ���������
            playerInstance = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
            playerInstance.tag = "Player"; // ������ұ�ǩ
            Debug.Log("��Ҷ�����ʵ������");
        }
        else
        {
            Debug.Log("��Ҷ����Ѵ��ڡ�");
        }
    }

    public void SwitchScene(string sceneName)
    {
        // ����Ҫ�л�����ʱ����
        SceneManager.LoadScene(sceneName);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // ���³������غ�����Ҷ���
        LoadPlayer();
    }

    private void OnEnable()
    {
        // ע�᳡�������¼�
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        // ע�����������¼�
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}