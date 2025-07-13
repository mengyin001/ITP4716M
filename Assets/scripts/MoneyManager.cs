using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;

public class MoneyManager : MonoBehaviourPun, IPunObservable
{
    public static MoneyManager Instance;

   private MoneyData moneyData; // 仅用于货币元数据

    private int playerMoney = 0; // 本地存储的玩家金钱
    private Dictionary<int, int> otherPlayersMoney = new Dictionary<int, int>(); // 其他玩家的金钱

    private void Awake()
    {
        InitializeMoney();
    }

    public void Start()
    {
        InitializeMoney();
        if (Instance == null)
        {
            Instance = this;
        }
        LoadMoney();
    }

    private void InitializeMoney()
    {
        // 初始金钱值
        playerMoney = 100;
        otherPlayersMoney.Clear();
        UIManager.Instance.UpdateMoneyUI(playerMoney);
    }

    public int GetCurrentMoney()
    {
        return playerMoney;
    }

    public void AddMoney(int amount)
    {
        if (photonView.IsMine)
        {
            playerMoney += amount;
            UIManager.Instance.UpdateMoneyUI(playerMoney);
            SaveMoney();
            photonView.RPC("SyncMoneyRPC", RpcTarget.Others, playerMoney);
        }
    }

    public bool RemoveMoney(int amount)
    {
        if (photonView.IsMine && CanAfford(amount))
        {
            playerMoney -= amount;
            UIManager.Instance.UpdateMoneyUI(playerMoney);
            SaveMoney();
            photonView.RPC("SyncMoneyRPC", RpcTarget.Others, playerMoney);
            return true;
        }
        return false;
    }

    [PunRPC]
    private void SyncMoneyRPC(int newAmount, PhotonMessageInfo info)
    {
        int senderId = info.Sender.ActorNumber;

        // 更新其他玩家的金钱数据
        if (otherPlayersMoney.ContainsKey(senderId))
        {
            otherPlayersMoney[senderId] = newAmount;
        }
        else
        {
            otherPlayersMoney.Add(senderId, newAmount);
        }
    }

    public bool CanAfford(int amount)
    {
        return playerMoney >= amount;
    }

    public string GetCurrencyName()
    {
        return moneyData.itemName;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 发送本地玩家的金钱数据
            stream.SendNext(playerMoney);
        }
        else
        {
            // 接收其他玩家的金钱数据
            int receivedMoney = (int)stream.ReceiveNext();
            int senderId = info.Sender.ActorNumber;

            if (otherPlayersMoney.ContainsKey(senderId))
            {
                otherPlayersMoney[senderId] = receivedMoney;
            }
            else
            {
                otherPlayersMoney.Add(senderId, receivedMoney);
            }
        }
    }
    public void SaveMoney()
    {
        if (!photonView.IsMine) return;

        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
        {
            { "PlayerMoney", playerMoney }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        Debug.Log($"Money saved: {playerMoney}");
    }

    public void LoadMoney()
    {
        if (!photonView.IsMine) return;

        if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("PlayerMoney"))
        {
            playerMoney = (int)PhotonNetwork.LocalPlayer.CustomProperties["PlayerMoney"];
            UIManager.Instance.UpdateMoneyUI(playerMoney);
            Debug.Log($"Money loaded: {playerMoney}");
        }
        else
        {
            // 第一次进入游戏，初始化金币
            playerMoney = 100;
            Debug.Log("No saved money found. Initializing to 100.");
        }
    }

}