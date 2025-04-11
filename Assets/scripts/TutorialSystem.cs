using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialSystem : MonoBehaviour
{
    // 移动方向检测配置
    [Header("Movement Settings")]
    [SerializeField] private KeyCode upKey = KeyCode.W;
    [SerializeField] private KeyCode downKey = KeyCode.S;
    [SerializeField] private KeyCode leftKey = KeyCode.A;
    [SerializeField] private KeyCode rightKey = KeyCode.D;

    // 任务系统配置
    [Header("Task Settings")]
    [SerializeField] private GameObject lockedDoor;
    [SerializeField] private string nextSceneName = "Area2";
    [SerializeField] private bool[] directionFlags = new bool[4];

    // UI反馈配置
    [Header("UI Settings")]
    [SerializeField] private GameObject directionArrows;
    [SerializeField] private TMPro.TextMeshProUGUI taskPrompt;

    private void Start()
    {
        InitializeTutorial();
    }

    void InitializeTutorial()
    {
        // 初始化任务状态
        directionFlags = new bool[] { false, false, false, false };
        UpdateTaskUI("移动教学：使用 WASD 探索区域");

        // 显示方向指示箭头（参考网页2动画实现）
        if (directionArrows != null)
            directionArrows.SetActive(true);
    }

    void Update()
    {
        CheckMovementInput();
        HandleDoorInteraction();
    }

    void CheckMovementInput()
    {
        // 检测四个方向输入（参考网页6的输入检测机制）
        if (Input.GetKeyDown(upKey)) directionFlags[0] = true;
        if (Input.GetKeyDown(downKey)) directionFlags[1] = true;
        if (Input.GetKeyDown(leftKey)) directionFlags[2] = true;
        if (Input.GetKeyDown(rightKey)) directionFlags[3] = true;

        // 更新UI提示（参考网页3的动态任务系统）
        UpdateTaskUI($"已掌握方向：{GetCompletedDirections()}");

        // 检查任务完成条件
        if (CheckAllDirectionsLearned())
        {
            CompleteTutorialTask();
        }
    }

    string GetCompletedDirections()
    {
        string result = "";
        if (directionFlags[0]) result += "↑ ";
        if (directionFlags[1]) result += "↓ ";
        if (directionFlags[2]) result += "← ";
        if (directionFlags[3]) result += "→ ";
        return result.Length > 0 ? result : "无";
    }

    bool CheckAllDirectionsLearned()
    {
        foreach (bool flag in directionFlags)
        {
            if (!flag) return false;
        }
        return true;
    }

    void CompleteTutorialTask()
    {
        // 执行任务完成逻辑（参考网页3的场景切换机制）
        UpdateTaskUI("任务完成！前往下一区域");
        UnlockDoor();

        // 关闭方向提示
        if (directionArrows != null)
            directionArrows.SetActive(false);

        // 禁用继续检测输入
        this.enabled = false;
    }

    void UnlockDoor()
    {
        // 实现门解锁逻辑（参考网页5的场景对象管理）
        if (lockedDoor != null)
        {
            // 禁用碰撞器并播放动画
            lockedDoor.GetComponent<Collider2D>().enabled = false;
            lockedDoor.GetComponent<Animator>().Play("DoorOpen");
        }
    }

    void HandleDoorInteraction()
    {
        // 当玩家接触门时加载新场景（参考网页3的碰撞检测）
        if (CheckAllDirectionsLearned() && Input.GetKeyDown(KeyCode.E))
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }

    void UpdateTaskUI(string message)
    {
        // 更新任务提示UI（参考网页5的UI系统）
        if (taskPrompt != null)
            taskPrompt.text = message;
    }
}