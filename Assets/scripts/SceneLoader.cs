using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class SceneLoader : MonoBehaviourPunCallbacks
{
    public static string targetScene; // 静态变量传递目标场景名
    public Slider progressBar;
    public TextMeshProUGUI progressText;
    private bool hasNotifiedMaster = false;
    private AsyncOperation targetSceneLoadOperation; // 存储目标场景加载操作的引用

    void Start()
    {
        // 确保加载场景时已经设置了目标场景
        if (string.IsNullOrEmpty(targetScene))
        {
            Debug.LogError("Target scene is not set!");
            return;
        }

        // 如果是房主，直接开始等待所有玩家
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(WaitForAllPlayersThenLoad());
        }
        else
        {
            // 非房主通知房主自己已准备好
            StartCoroutine(LoadSceneAsync());
        }
    }

    // 非房主加载场景并通知房主
    IEnumerator LoadSceneAsync()
    {
        // 存储加载操作的引用
        targetSceneLoadOperation = SceneManager.LoadSceneAsync(targetScene, LoadSceneMode.Single);
        targetSceneLoadOperation.allowSceneActivation = false;

        while (!targetSceneLoadOperation.isDone)
        {
            float progress = Mathf.Clamp01(targetSceneLoadOperation.progress / 0.9f);
            progressBar.value = progress;
            progressText.text = (progress * 100).ToString("F0") + "%";

            // 当场景加载到90%且尚未通知房主时
            if (targetSceneLoadOperation.progress >= 0.9f && !hasNotifiedMaster)
            {
                hasNotifiedMaster = true;
                // 通知房主当前客户端已准备好
                photonView.RPC("RPC_NotifyPlayerReady", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer);
            }

            yield return null;
        }
    }

    // 房主等待所有玩家准备就绪
    IEnumerator WaitForAllPlayersThenLoad()
    {
        int readyPlayersCount = 1; // 房主自己算一个
        int totalPlayers = PhotonNetwork.CurrentRoom.PlayerCount;

        Debug.Log($"Waiting for {totalPlayers - readyPlayersCount} more players...");

        // 加载目标场景但不激活，并存储引用
        targetSceneLoadOperation = SceneManager.LoadSceneAsync(targetScene, LoadSceneMode.Single);
        targetSceneLoadOperation.allowSceneActivation = false;

        while (readyPlayersCount < totalPlayers)
        {
            // 同时更新房主自己的加载进度
            float progress = Mathf.Clamp01(targetSceneLoadOperation.progress / 0.9f);
            progressBar.value = progress;
            progressText.text = (progress * 100).ToString("F0") + "%";

            // 从房间属性获取最新的准备玩家数量
            if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("ReadyPlayersCount"))
            {
                readyPlayersCount = (int)PhotonNetwork.CurrentRoom.CustomProperties["ReadyPlayersCount"];
            }

            yield return new WaitForSeconds(0.5f);
        }

        Debug.Log("All players ready! Loading target scene...");
        // 通知所有玩家激活场景
        photonView.RPC("RPC_ActivateScene", RpcTarget.All);
        targetSceneLoadOperation.allowSceneActivation = true;
    }

    [PunRPC]
    private void RPC_NotifyPlayerReady(Player player)
    {
        // 只有房主会收到这个RPC
        if (PhotonNetwork.IsMasterClient)
        {
            int readyPlayersCount = 1; // 默认至少房主自己
            if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("ReadyPlayersCount"))
            {
                readyPlayersCount = (int)PhotonNetwork.CurrentRoom.CustomProperties["ReadyPlayersCount"];
            }
            readyPlayersCount++;

            // 更新房间属性中的准备玩家数量
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
            props["ReadyPlayersCount"] = readyPlayersCount;
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
    }

    [PunRPC]
    private void RPC_ActivateScene()
    {
        // 使用存储的加载操作引用激活场景，兼容所有Unity版本
        if (targetSceneLoadOperation != null && !targetSceneLoadOperation.isDone && targetSceneLoadOperation.progress >= 0.9f)
        {
            targetSceneLoadOperation.allowSceneActivation = true;
        }
    }

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        // 房主监听准备玩家数量变化
        if (PhotonNetwork.IsMasterClient && propertiesThatChanged.ContainsKey("ReadyPlayersCount"))
        {
            int readyCount = (int)propertiesThatChanged["ReadyPlayersCount"];
            int totalPlayers = PhotonNetwork.CurrentRoom.PlayerCount;

            Debug.Log($"Ready players: {readyCount}/{totalPlayers}");
        }
    }

    private void OnEnable()
    {
        // 初始化房间属性
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom)
        {
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
            props["ReadyPlayersCount"] = 1; // 房主自己已准备
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
    }
}
