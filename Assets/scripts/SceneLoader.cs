using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class SceneLoader : MonoBehaviourPunCallbacks
{
    public static string targetScene;
    public Slider progressBar;
    public TextMeshProUGUI progressText;
    private bool hasNotifiedMaster = false;
    private AsyncOperation targetSceneLoadOperation;
    private float loadStartTime;
    private const float MAX_LOAD_WAIT_TIME = 30f; // 最大等待时间，防止无限等待

    void Start()
    {
        loadStartTime = Time.time;

        // 确保Photon连接正常
        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogError("Photon is not connected!");
            return;
        }

        // 确保目标场景已设置
        if (string.IsNullOrEmpty(targetScene))
        {
            Debug.LogError("Target scene is not set!");
            return;
        }

        // 无论是否为主机，都先加载目标场景资源
        StartCoroutine(LoadSceneResources());
    }

    // 加载目标场景资源（所有玩家都执行）
    IEnumerator LoadSceneResources()
    {
        // 加载目标场景但不激活
        targetSceneLoadOperation = SceneManager.LoadSceneAsync(targetScene, LoadSceneMode.Single);
        targetSceneLoadOperation.allowSceneActivation = false;

        while (targetSceneLoadOperation.progress < 0.9f)
        {
            // 检查是否超时
            if (Time.time - loadStartTime > MAX_LOAD_WAIT_TIME)
            {
                Debug.LogError("Loading timed out!");
                break;
            }

            float progress = Mathf.Clamp01(targetSceneLoadOperation.progress / 0.9f);
            UpdateProgressUI(progress);
            yield return null;
        }

        // 加载完成，更新UI为100%
        UpdateProgressUI(1f);

        // 通知主机已准备好
        if (!PhotonNetwork.IsMasterClient && !hasNotifiedMaster)
        {
            hasNotifiedMaster = true;
            photonView.RPC("RPC_NotifyPlayerReady", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
        }
        // 如果是主机，开始等待所有玩家
        else if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(WaitForAllPlayers());
        }
    }

    // 主机等待所有玩家准备就绪
    IEnumerator WaitForAllPlayers()
    {
        int totalPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
        int readyPlayers = 1; // 主机自己

        // 初始化房间属性
        UpdateReadyPlayersCount(readyPlayers);

        Debug.Log($"Waiting for {totalPlayers - readyPlayers} players...");

        while (readyPlayers < totalPlayers)
        {
            // 检查是否超时
            if (Time.time - loadStartTime > MAX_LOAD_WAIT_TIME)
            {
                Debug.LogWarning("Timeout waiting for players, proceeding anyway");
                break;
            }

            // 从房间属性获取最新的准备玩家数量
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("ReadyPlayers", out var count))
            {
                readyPlayers = (int)count;
            }

            progressText.text = $"Waiting for {totalPlayers - readyPlayers} players...";
            yield return new WaitForSeconds(0.5f);
        }

        // 通知所有玩家激活场景
        Debug.Log("All players ready! Loading target scene...");
        photonView.RPC("RPC_ActivateScene", RpcTarget.All);
    }

    [PunRPC]
    private void RPC_NotifyPlayerReady(int playerActorNumber)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            int readyPlayers = 1;
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("ReadyPlayers", out var count))
            {
                readyPlayers = (int)count;
            }

            readyPlayers++;
            UpdateReadyPlayersCount(readyPlayers);
            Debug.Log($"Player {playerActorNumber} is ready. Total: {readyPlayers}");
        }
    }

    [PunRPC]
    private void RPC_ActivateScene()
    {
        if (targetSceneLoadOperation != null && !targetSceneLoadOperation.isDone)
        {
            Debug.Log("Activating target scene");
            targetSceneLoadOperation.allowSceneActivation = true;
        }
        else
        {
            // 容错处理：如果加载操作已完成，直接跳转
            Debug.Log("Directly loading target scene");
            SceneManager.LoadScene(targetScene);
        }
    }

    // 更新准备玩家数量到房间属性
    private void UpdateReadyPlayersCount(int count)
    {
        var props = new ExitGames.Client.Photon.Hashtable();
        props["ReadyPlayers"] = count;
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    // 更新进度UI
    private void UpdateProgressUI(float progress)
    {
        if (progressBar != null)
            progressBar.value = progress;

        if (progressText != null)
            progressText.text = $"{(int)(progress * 100)}%";
    }

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        // 调试用：显示房间属性变化
        if (propertiesThatChanged.ContainsKey("ReadyPlayers"))
        {
            Debug.Log($"Ready players updated: {propertiesThatChanged["ReadyPlayers"]}");
        }
    }
}
