// pistol.cs (已修改為繼承 gun 並使用 RPC 框架)
using UnityEngine;

/// <summary>
/// 一個標準的手槍武器。
/// 它繼承自 'gun' 基底類別，從而自動獲得所有通用的武器邏輯和網路功能。
/// 這個腳本的存在是為了將其掛載到手槍的遊戲物件上，
/// 並在 Unity 編輯器中設定其獨特的屬性（如射速、傷害、子彈外觀等）。
/// </summary>
public class pistol : gun
{
    // --- 腳本內容為空 ---

    // 因為標準手槍的行為（單發、直線飛行）正是 'gun' 基底類別的預設行為，
    // 所以我們不需要在這裡重寫 (override) 任何方法。

    // 'gun' 的 Start() 會自動初始化所有組件。
    // 'gun' 的 Update() 會自動處理武器旋轉和玩家輸入。
    // 'gun' 的 HandleShootingInput() 會檢查能量和開火條件。
    // 'gun' 的 Fire() 會自動觸發正確的 RPC ("RPC_FireSingle") 來在所有客戶端生成視覺子彈。

    // 你唯一需要做的事情，就是在 Unity 編輯器中選中掛載了此腳本的物件，
    // 然後在 Inspector 面板中設定從 'gun' 繼承來的公開變數，例如：
    // - Interval (射擊間隔)
    // - Bullet Prefab (子彈預製體，使用沒有 PhotonView 的那個)
    // - Damage (傷害值)
    // - Shoot Sound (開火音效)
    // - Muzzle Pos (槍口位置的 Transform)
    // 等等...
}
