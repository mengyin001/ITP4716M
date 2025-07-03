using Photon.Pun;
using UnityEngine;

public class PlayerSetUp : MonoBehaviourPun
{
    public PlayerMovement movement;
    private HealthSystem healthSystem;

    public void Start()
    {
        healthSystem = GetComponent<HealthSystem>();

        // 如果是本地玩家，注册到UIManager
        if (photonView.IsMine && healthSystem != null)
        {
            UIManager.Instance.RegisterLocalPlayer(healthSystem);
        }
    }
    void OnDestroy()
    {
        // 如果是本地玩家，注销注册
        if (photonView.IsMine && UIManager.Instance != null)
        {
            UIManager.Instance.UnregisterPlayer();
        }
    }

    public void IsLocalPlayer()
    {
        movement.enabled = true;
    }
}
