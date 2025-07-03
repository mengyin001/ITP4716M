using Photon.Pun;
using UnityEngine;

public class PlayerSetUp : MonoBehaviourPun
{
    public PlayerMovement movement;
    private HealthSystem healthSystem;

    public void Start()
    {
        healthSystem = GetComponent<HealthSystem>();

        // ����Ǳ�����ң�ע�ᵽUIManager
        if (photonView.IsMine && healthSystem != null)
        {
            UIManager.Instance.RegisterLocalPlayer(healthSystem);
        }
    }
    void OnDestroy()
    {
        // ����Ǳ�����ң�ע��ע��
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
