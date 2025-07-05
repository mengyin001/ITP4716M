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

        // 只有本地玩家才能生成子彈（網路物件）
        if (parentPhotonView.IsMine)
        {
            // 計算每個彈丸的散射角度
            float baseAngle = transform.rotation.eulerAngles.z; // 獲取槍口當前方向的Z軸角度
            float startAngle = baseAngle - (spreadSettings.angle / 2f); // 散射範圍的起始角度

            for (int i = 0; i < spreadSettings.pelletCount; i++)
            {
                // 計算當前彈丸的角度
                float currentAngle = startAngle + (spreadSettings.angle / (spreadSettings.pelletCount - 1f)) * i;
                if (spreadSettings.pelletCount == 1) // 避免除以零，如果只有一個彈丸，角度就是基礎角度
                {
                    currentAngle = baseAngle;
                }

                // 將角度轉換為四元數旋轉
                Quaternion pelletRotation = Quaternion.Euler(0, 0, currentAngle);

                // 使用 PhotonNetwork.Instantiate 创建网络同步的子弹
                GameObject bulletObj = PhotonNetwork.Instantiate(
                    bulletPrefab.name, // 確保 bulletPrefab.name 是正確的預製件名稱
                    muzzlePos.position,
                    pelletRotation // 使用計算出的彈丸旋轉
                );

                // 获取子弹组件并初始化
                Bullet bullet = bulletObj.GetComponent<Bullet>();
                if (bullet != null)
                {
                    // 傳遞每個彈丸的獨立傷害值
                    bullet.Initialize(spreadSettings.damagePerPellet, parentPhotonView);
                }
            }
        }

        // 播放开火效果（所有客户端）
        // 這裡仍然可以呼叫通用的特效 RPC，因為特效是視覺和聽覺效果，不需要每個子彈都觸發一次
        parentPhotonView.RPC("PlayFireEffects", RpcTarget.All, muzzlePos.position);
    }
}
