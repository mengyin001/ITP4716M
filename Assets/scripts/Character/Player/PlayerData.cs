using UnityEngine;

public static class PlayerData
{
    // 玩家基础数据
    public static float Health { get; set; }
    public static int Money { get; set; }
    public static int CurrentGunIndex { get; set; }

    // 初始化默认值
    static PlayerData()
    {
        Health = 100f;  // 默认满血
        Money = 0;
        CurrentGunIndex = 0;
    }
}