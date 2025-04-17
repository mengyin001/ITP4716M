using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI; 

public class TutorialTaskSystem : MonoBehaviour
{
    [System.Serializable]
    public class Task
    {
        public string description;          // 任务描述
        public KeyCode triggerKey;         // 触发按键
        public bool showProgressBar;        // 是否显示进度条
        public float requiredHoldTime = 0;  // 长按需求时间
        public int requiredSteps = 1;       // 新增：完成任务需要的步骤数
        [HideInInspector] public int currentStep; // 当前完成步骤
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

    private float keyHoldTimer = 0;

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
        // 长按任务检测
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
        }

        isTransitioning = false;
    }

    void InitializeCurrentTask()
    {
        var task = tasks[currentTaskIndex];
        task.currentStep = 0;
        if (task.showProgressBar)
        {
            progressSlider.maxValue = task.requiredSteps;
            progressSlider.value = 0;
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
            }
        };
    }
}