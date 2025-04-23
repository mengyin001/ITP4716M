using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class TutorialTaskSystem : MonoBehaviour
{
    [System.Serializable]
    public class Task
    {
        public enum TaskType
        {
            KeyPress,
            HoldKey,
            Dialogue,
            KillEnemies,
            OpenChest,
            CompleteGame
        }
        public TaskType taskType = TaskType.KeyPress; // 任务类型
        public string description;          // 任务描述
        public KeyCode triggerKey;         // 触发按键
        public bool showProgressBar;        // 是否显示进度条
        public float requiredHoldTime = 0;  // 长按需求时间
        public int requiredSteps = 1;       // 新增：完成任务需要的步骤数
        [HideInInspector] public int currentStep; // 当前完成步骤
        [Header("NPCID设置")]
        public string targetNPCID;  // 需要对话的NPC标识
        [Header("怪物设置")]
        public GameObject[] activationObjects;
        [Header("宝箱任务设置")]
        public ChestInteraction2D targetChest;
    }

    [Header("任务设置")]
    [SerializeField] private List<Task> tasks = new List<Task>();
    private int currentTaskIndex = 0;

    [Header("任务过渡设置")]
    [SerializeField] private float taskTransitionDelay = 2f;  // 任务切换延迟时间
    private bool isTransitioning = false;  // 是否正在切换任务

    [Header("UI组件")]
    [SerializeField] private TextMeshProUGUI taskDescriptionText;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private GameObject completedEffect;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI underTip;

    [Header("任务完成设置")]
    [SerializeField] private GameObject objectToSpawn; // 要生成的物体预制体
    [SerializeField] private Transform playerTransform; // 玩家角色Transform
    [SerializeField] private float spawnOffset = 1f;    // 生成位置偏移量

    private float keyHoldTimer = 0;
    private ChestInteraction2D currentSubscribedChest;

    void Start()
    {
        InitializeTask();
        UpdateTaskDisplay();
    }

    void InitializeTask()
    {
        // 初始化每个任务的进度
        foreach (var task in tasks)
        {
            task.currentStep = 0;

            if (task.taskType == Task.TaskType.CompleteGame)
            {
                task.requiredSteps = EnemyManager.Instance.MaxWaves; // 设置为最大波数
            }

            if (task.showProgressBar)
            {
                progressSlider.maxValue = task.requiredSteps;
            }
        }
    }

    void Update()
    {
        if (isTransitioning || currentTaskIndex >= tasks.Count) return;

        Task currentTask = tasks[currentTaskIndex];
        HandleTaskProgress(currentTask);
    }

    void HandleTaskProgress(Task currentTask)
    {
        if (DialogueSystem.Instance != null && DialogueSystem.Instance.isDialogueActive)
            return;
         if (ShopManager.Instance != null && ShopManager.Instance.isOPen)
            return;
        // 长按任务检测

        if (currentTask.taskType == Task.TaskType.CompleteGame)
    {
        int completedWaves = EnemyManager.Instance.CurrentWaveIndex; // 获取当前波数
        if (completedWaves > currentTask.currentStep) // 每完成一个波数
        {
            currentTask.currentStep++; // 增加当前步骤
            UpdateProgressDisplay(currentTask); // 更新进度显示

            // 检查是否完成所有步骤
            if (currentTask.currentStep >= currentTask.requiredSteps)
            {
                CompleteStep(currentTask);
            }
        }
        return;
        }

        if (!string.IsNullOrEmpty(currentTask.targetNPCID)) return;
        if (currentTask.requiredHoldTime > 0)
        {
            if (Input.GetKey(currentTask.triggerKey))
            {
                keyHoldTimer += Time.deltaTime;
                UpdateProgressBar(keyHoldTimer / currentTask.requiredHoldTime);

                if (keyHoldTimer >= currentTask.requiredHoldTime)
                {
                    CompleteStep(currentTask);
                    keyHoldTimer = 0;
                }
            }
            else
            {
                keyHoldTimer = 0;
                UpdateProgressBar(0);
            }
        }
        // 单击任务检测
        else if (Input.GetKeyDown(currentTask.triggerKey))
        {
            CompleteStep(currentTask);
        }
    }

    void CompleteStep(Task task)
    {
        task.currentStep++;
        UpdateProgressDisplay(task);
        UpdateProgressText(task);

        // 检查是否完成全部步骤
        if (task.currentStep >= task.requiredSteps)
        {
            CompleteCurrentTask();
        }
    }

    void UpdateProgressDisplay(Task task)
    {
        // 更新进度条和文本
        if (task.showProgressBar)
        {
            progressSlider.value = task.currentStep;
            underTip.text = $"{task.description} ({task.currentStep}/{task.requiredSteps})";
        }
    }

    void CompleteCurrentTask()
    {
        StartCoroutine(TransitionToNextTask());
    }

    IEnumerator TransitionToNextTask()
    {
        isTransitioning = true;

        if (currentSubscribedChest != null)
        {
            currentSubscribedChest.OnChestOpened -= HandleChestOpened;
            currentSubscribedChest = null;
        }
        // 关闭当前任务的GameObject
        var currentTask = tasks[currentTaskIndex];
        if (currentTask.activationObjects != null && currentTask.activationObjects.Length > 0)
        {
            foreach (var obj in currentTask.activationObjects)
            {
                if (obj != null) obj.SetActive(false);
            }
        }

        // 显示完成效果
        StartCoroutine(ShowCompletionEffect());

        // 隐藏当前任务UI
        taskDescriptionText.text = "Mission accomplished!";
        underTip.gameObject.SetActive(false);

        // 等待2秒
        yield return new WaitForSeconds(taskTransitionDelay);

        currentTaskIndex++;

        if (currentTaskIndex < tasks.Count)
        {
            InitializeCurrentTask();
            UpdateTaskDisplay();
            underTip.gameObject.SetActive(true);
        }
        else
        {
            Debug.Log("Tutorial completed!");
            taskDescriptionText.text = "Tutorial completed!";
            progressSlider.gameObject.SetActive(false);

            SpawnObjectBelowPlayer();
        }

        isTransitioning = false;
    }

    private void SpawnObjectBelowPlayer()
    {
        if (objectToSpawn != null && playerTransform != null)
        {
            Vector3 spawnPosition = playerTransform.position + Vector3.down * spawnOffset;
            Instantiate(objectToSpawn, spawnPosition, Quaternion.identity);
        }
        else
        {
            Debug.LogError("生成物体失败：请检查objectToSpawn和playerTransform是否已设置");
        }
    }

    void InitializeCurrentTask()
    {
        var task = tasks[currentTaskIndex];
        task.currentStep = 0;

        // 激活关联的GameObject
        if (task.activationObjects != null && task.activationObjects.Length > 0)
        {
            foreach (var obj in task.activationObjects)
            {
                if (obj != null) obj.SetActive(true);
            }
        }
        // 取消之前的宝箱订阅
        if (currentSubscribedChest != null)
        {
            currentSubscribedChest.OnChestOpened -= HandleChestOpened;
            currentSubscribedChest = null;
        }

        // 激活关联对象（原有代码）
        if (task.activationObjects != null && task.activationObjects.Length > 0)
        {
            foreach (var obj in task.activationObjects)
                if (obj != null) obj.SetActive(true);
        }

        // 初始化宝箱任务
        if (task.taskType == Task.TaskType.OpenChest)
        {
            if (task.targetChest == null)
            {
                Debug.LogError("OpenChest任务未指定目标宝箱！");
                return;
            }

            // 如果宝箱已开启则直接完成
            if (task.targetChest.IsOpened)
            {
                task.currentStep = task.requiredSteps;
                StartCoroutine(CompleteIfAlreadyOpened());
            }
            else // 否则订阅开启事件
            {
                currentSubscribedChest = task.targetChest;
                currentSubscribedChest.OnChestOpened += HandleChestOpened;
            }
        }

        if (task.showProgressBar)
        {
            progressSlider.maxValue = task.requiredSteps;
            progressSlider.value = 0;
        }

    }

    private IEnumerator CompleteIfAlreadyOpened()
    {
        yield return null; // 等待一帧确保初始化完成
        CompleteCurrentTask();
    }

    private void HandleChestOpened(ChestInteraction2D chest)
    {
        if (isTransitioning || currentTaskIndex >= tasks.Count) return;

        Task currentTask = tasks[currentTaskIndex];
        if (currentTask.taskType == Task.TaskType.OpenChest &&
            currentTask.targetChest == chest)
        {
            CompleteStep(currentTask);
        }
    }

    void UpdateTaskDisplay()
    {
        Task task = tasks[currentTaskIndex];
        taskDescriptionText.text = task.description;
        underTip.text = task.description;

        // 新增进度文本更新
        progressText.text = $"{task.currentStep}/{task.requiredSteps}";

        progressSlider.gameObject.SetActive(task.showProgressBar);
        if (task.showProgressBar)
        {
            progressSlider.maxValue = task.requiredSteps;
            progressSlider.value = task.currentStep;
        }
    }

    void UpdateProgressBar(float progress)
    {
        progressSlider.value = progress;
    }

    System.Collections.IEnumerator ShowCompletionEffect()
    {
        completedEffect.SetActive(true);
        yield return new WaitForSeconds(0.5f);
        completedEffect.SetActive(false);
    }

    void UpdateProgressText(Task task)
    {
        progressText.text = $"{task.currentStep}/{task.requiredSteps}";
    }

    void OnEnable()
    {
        DialogueSystem.Instance.OnDialogueCompleted += HandleDialogueComplete;
        Character.OnEnemyDeath += HandleEnemyKilled;
    }

    void OnDisable()
    {
        if (DialogueSystem.Instance != null)
            DialogueSystem.Instance.OnDialogueCompleted -= HandleDialogueComplete;
        Character.OnEnemyDeath -= HandleEnemyKilled;
        if (currentSubscribedChest != null)
        {
            currentSubscribedChest.OnChestOpened -= HandleChestOpened;
        }
    }

    void HandleDialogueComplete(string npcID)
    {
        if (isTransitioning || currentTaskIndex >= tasks.Count) return;

        Task currentTask = tasks[currentTaskIndex];
        if (currentTask.targetNPCID == npcID)
        {
            Debug.Log($"检测到敌人死亡，当前任务：{currentTask.description}，进度：{currentTask.currentStep + 1}/{currentTask.requiredSteps}"); 
            CompleteStep(currentTask);
        }
    }
      private void HandleEnemyKilled()
    {
        if (isTransitioning || currentTaskIndex >= tasks.Count) return;

        Task currentTask = tasks[currentTaskIndex];
        if (currentTask.taskType == Task.TaskType.KillEnemies)
        {
            CompleteStep(currentTask);
        }
    }
    // 示例任务配置
    private void Reset()
    {
        tasks = new List<Task>{
            new Task{
                description = "收集4个能量核心",
                triggerKey = KeyCode.E,
                showProgressBar = true,
                requiredSteps = 4
            },
            new Task{
                description = "长按W冲刺4次",
                triggerKey = KeyCode.W,
                showProgressBar = true,
                requiredHoldTime = 1f,
                requiredSteps = 4
            },
            new Task{
            description = "完成所有敌人波次",
            taskType = Task.TaskType.CompleteGame,
            requiredSteps = EnemyManager.Instance.MaxWaves // 使用最大波数
        }
        };
    }
}


