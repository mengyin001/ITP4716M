using UnityEngine;

public static class PlayerData
{
    // ��һ�������
    public static float Health { get; set; }
    public static int Money { get; set; }
    public static int CurrentGunIndex { get; set; }

    // ��ʼ��Ĭ��ֵ
    static PlayerData()
    {
        Health = 100f;  // Ĭ����Ѫ
        Money = 0;
        CurrentGunIndex = 0;
    }
}