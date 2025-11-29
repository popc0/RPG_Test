using UnityEngine;
using UnityEngine.InputSystem;

public class IMU_GravityRemover : MonoBehaviour
{
    [Header("Inputs (請拉入 Action)")]
    // 你的加速度 (包含重力)
    public InputActionReference accXAction;
    public InputActionReference accYAction;
    public InputActionReference accZAction;

    // 你的角速度 (Gyro)
    public InputActionReference gyroXAction;
    public InputActionReference gyroYAction;
    public InputActionReference gyroZAction;

    [Header("Calibration (關鍵設定)")]
    // 你的重力大小 (上次看是 0.5，請依實際平放數值設定)
    public float gravityMagnitude = 0.5f;

    // 你的 Gyro 單位係數。
    // 如果 Gyro 輸出 1.0 代表 1度/秒，這裡填 1.0。
    // 如果 Gyro 輸出 1.0 代表 1弧度/秒，這裡填 57.29 (180/PI)。
    // 許多 MPU6050 原始值較大，需要查你的 ESP32 code 除以多少。
    // 假設你傳來的是已經轉成 "度/秒 (deg/s)" 的數值：
    public float gyroSensitivity = 1.0f;

    [Header("Filter Settings")]
    // 數值越大，越相信 Gyro (反應快)；數值越小，越相信 Accel (修正漂移)
    // 0.98 是一個標準的互補濾波參數
    [Range(0.9f, 0.999f)]
    public float filterCoeff = 0.98f;

    [Header("Output")]
    public Vector3 linearAcceleration; // 去重力後的乾淨力道
    public Vector3 estimatedGravity;   // 程式計算出的重力方向

    [Header("Noise Reduction (雜訊消除)")]
    public float deadzone = 0.15f; // 死區：小於這個力道的都當作 0
    public float smoothing = 0.5f; // 平滑度：0 = 不平滑, 0.9 = 很拖, 0.2~0.5 = 剛好
    private Vector3 smoothedLinearAcc; // 用來儲存平滑後的結果

    void OnEnable()
    {
        if (accXAction) accXAction.action.Enable();
        if (gyroXAction) gyroXAction.action.Enable();
        // ... (記得確保所有 action 都 enable)
    }

    void Start()
    {
        // 初始化：假設一開始手把是平放的，重力向下 (Z軸)
        // 如果你的 Z 軸平放是正，就設 (0,0,1)；如果是負，設 (0,0,-1)
        estimatedGravity = new Vector3(0, 0, 1) * gravityMagnitude;
    }

    void Update()
    {
        // 1. 讀取數據
        Vector3 currentAcc = new Vector3(
            accXAction.action.ReadValue<float>(),
            accYAction.action.ReadValue<float>(),
            accZAction.action.ReadValue<float>()
        );

        Vector3 currentGyro = new Vector3(
            gyroXAction.action.ReadValue<float>(),
            gyroYAction.action.ReadValue<float>(),
            gyroZAction.action.ReadValue<float>()
        );

        // 2. 處理 Gyro 更新 (預測步驟)
        // 根據角速度，把上一幀的重力向量「旋轉」一下
        // Step A: 把 Gyro 轉成 "度" (角速度 * 時間)
        Vector3 gyroDelta = currentGyro * gyroSensitivity * Time.deltaTime;

        // Step B: 旋轉重力向量
        // 注意：這裡的旋轉軸可能需要依你的硬體調整 (左手/右手座標系)
        // Quaternion.Euler(x, y, z) 裡的順序很重要
        Quaternion rotation = Quaternion.Euler(gyroDelta.x, gyroDelta.y, gyroDelta.z);

        // 如果發現轉動時重力補償反了，試試看： Quaternion.Euler(-gyroDelta.x, -gyroDelta.y, -gyroDelta.z);
        Vector3 gyroEstimatedGravity = rotation * estimatedGravity;


        // 3. 處理 Accel 修正 (修正步驟)
        // 因為 Gyro 會漂移，長時間後會不準，所以我們要稍微參考一下目前的 Accel
        // 原理：長期來看，Accel 的平均方向就是重力方向
        Vector3 accEstimatedGravity = currentAcc.normalized * gravityMagnitude;


        // 4. 融合 (互補濾波)
        // 新的重力 = 98% 來自 Gyro 旋轉的結果 + 2% 來自 Accel 的指向
        estimatedGravity = Vector3.Lerp(accEstimatedGravity, gyroEstimatedGravity, filterCoeff);

        // 強制保持重力向量長度固定 (避免數值變形)
        estimatedGravity = estimatedGravity.normalized * gravityMagnitude;


        // 5. 得到結果：原始 Acc - 估算的重力 = 線性加速度
        linearAcceleration = currentAcc - estimatedGravity;

        // ---------------------------------------------------------
        // 新增：後處理 (Post-Processing)
        // ---------------------------------------------------------

        // 步驟 1: 死區 (Deadzone) - 殺掉微小的抖動
        // 如果力道小於 0.15 (視情況調整)，直接歸零
        if (linearAcceleration.magnitude < deadzone)
        {
            linearAcceleration = Vector3.zero;
        }

        // 步驟 2: 平滑化 (Smoothing) - 讓線條不要跳太快
        // 使用 Lerp 讓數值「滑」過去，而不是「跳」過去
        // 注意：這會讓反應稍微慢 0.0幾秒，但看起來會很舒服
        smoothedLinearAcc = Vector3.Lerp(smoothedLinearAcc, linearAcceleration, 1f - smoothing);

        // ---------------------------------------------------------


        // 最終顯示與使用 (改成畫 smoothedLinearAcc)
        Debug.DrawRay(transform.position, estimatedGravity * 10, Color.green);
        Debug.DrawRay(transform.position, smoothedLinearAcc * 10, Color.red); // 改畫平滑後的紅線
    }
}