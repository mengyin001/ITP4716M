using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HealthRegenCombo : MonoBehaviour
{
    [Header("组合键设置")]
    [Tooltip("组合键输入时间窗口(秒)")]
    public float inputTimeWindow = 2f;
    
    [Tooltip("组合键冷却时间(秒)")]
    public float cooldown = 0f;
    
    [Tooltip("回血回蓝效果持续时间(秒)")]
    public float effectDuration = 10f;

    [Header("组合键序列")]
    public List<KeyCode> keySequence = new List<KeyCode>()
    {
        KeyCode.W, KeyCode.S, KeyCode.W, KeyCode.S,
        KeyCode.A, KeyCode.D, KeyCode.A, KeyCode.D
    };
    
    [Tooltip("鼠标序列 (0=左键, 1=右键)")]
    public List<int> mouseSequence = new List<int>() { 1, 0 }; // 右键, 左键
    
    // 内部状态
    private HealthSystem healthSystem;
    private int currentKeyIndex = 0;
    private int currentMouseIndex = 0;
    private float lastInputTime;
    private bool isEffectActive = false;
    private bool isCoolingDown = false;
    
    // 保存原始回复设置
    private bool originalAutoRegen;
    private bool originalAutoRegenEn;
    private float originalRegenRate;
    private float originalRegenRateEn;

    void Start()
    {
        healthSystem = GetComponent<HealthSystem>();
        if (healthSystem == null)
        {
            Debug.LogError("HealthSystem component not found on " + gameObject.name);
            enabled = false;
            return;
        }
        
        // 保存原始回复设置
        originalAutoRegen = healthSystem.autoRegen;
        originalAutoRegenEn = healthSystem.autoRegenEn;
        originalRegenRate = healthSystem.regenRate;
        originalRegenRateEn = healthSystem.regenRateEn;
    }

    void Update()
    {
        // 检查冷却或效果激活状态
        if (isCoolingDown || isEffectActive) 
        {
            return;
        }
        
        // 检查输入超时
        if (currentKeyIndex > 0 && Time.time - lastInputTime > inputTimeWindow)
        {
            ResetCombo();
        }
        
        // 检测键盘序列
        if (currentKeyIndex < keySequence.Count)
        {
            // 检查当前序列按键
            if (Input.GetKeyDown(keySequence[currentKeyIndex]))
            {
                currentKeyIndex++;
                lastInputTime = Time.time;
                return; // 成功检测后返回，避免同一帧多次检测
            }
            
            // 检查错误按键
            foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(key) && key != keySequence[currentKeyIndex])
                {
                    ResetCombo();
                    return;
                }
            }
        }
        // 检测鼠标序列
        else if (currentMouseIndex < mouseSequence.Count)
        {
            if (Input.GetMouseButtonDown(mouseSequence[currentMouseIndex]))
            {
                currentMouseIndex++;
                lastInputTime = Time.time;
                return; // 成功检测后返回
            }
            
            // 检查错误的鼠标按键
            for (int i = 0; i < 3; i++)
            {
                if (Input.GetMouseButtonDown(i) && i != mouseSequence[currentMouseIndex])
                {
                    ResetCombo();
                    return;
                }
            }
        }
        
        // 检查序列完成
        if (currentKeyIndex >= keySequence.Count && currentMouseIndex >= mouseSequence.Count)
        {
            StartCoroutine(ActivateRegenEffect());
        }
    }

    // 重置组合键状态
    private void ResetCombo()
    {
        currentKeyIndex = 0;
        currentMouseIndex = 0;
    }

    // 激活恢复效果
    private IEnumerator ActivateRegenEffect()
    {
        if (isEffectActive) yield break;
        
        isEffectActive = true;
        isCoolingDown = true;
        ResetCombo();
        
        // 保存当前设置
        bool currentAutoRegen = healthSystem.autoRegen;
        bool currentAutoRegenEn = healthSystem.autoRegenEn;
        float currentRegenRate = healthSystem.regenRate;
        float currentRegenRateEn = healthSystem.regenRateEn;
        
        // 启用并设置高速回复
        healthSystem.autoRegen = true;
        healthSystem.autoRegenEn = true;
        healthSystem.regenRate = 100f; // 每秒回100血
        healthSystem.regenRateEn = 20f; // 每秒回20蓝
        
        // 等待效果持续时间
        float timer = effectDuration;
        while (timer > 0)
        {
            timer -= Time.deltaTime;
            yield return null;
        }
        
        // 恢复之前的设置
        healthSystem.autoRegen = currentAutoRegen;
        healthSystem.autoRegenEn = currentAutoRegenEn;
        healthSystem.regenRate = currentRegenRate;
        healthSystem.regenRateEn = currentRegenRateEn;
        
        // 启动冷却
        StartCoroutine(StartCooldown());
        isEffectActive = false;
    }

    // 冷却协程
    private IEnumerator StartCooldown()
    {
        float timer = cooldown;
        while (timer > 0)
        {
            timer -= Time.deltaTime;
            yield return null;
        }
        
        isCoolingDown = false;
    }
}