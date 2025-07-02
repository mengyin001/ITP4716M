using UnityEngine;
using Photon.Pun;

public class shotgun : gun
{
    [System.Serializable]
    public class SpreadSettings
    {
        public int pelletCount = 5;
        [Range(0, 45)] public float angle = 15f;
        public float damagePerPellet = 5f;
    }

    [Header("霰弹枪设置")]
    public SpreadSettings spreadSettings;

    /// <summary>
    /// 重寫 Fire 方法以實現霰彈槍的擴散射擊。
    /// </summary>
    protected override void Fire()
    {
        if (muzzlePos == null || parentPhotonView == null) return;

        // 呼叫為霰彈槍設計的專屬 RPC
        parentPhotonView.RPC("RPC_FireShotgun", RpcTarget.All,
            muzzlePos.position,
            transform.rotation,
            spreadSettings.damagePerPellet,
            spreadSettings.pelletCount,
            spreadSettings.angle
        );

        // 仍然可以呼叫通用的特效 RPC
        parentPhotonView.RPC("PlayFireEffects", RpcTarget.All, muzzlePos.position);
    }
}
