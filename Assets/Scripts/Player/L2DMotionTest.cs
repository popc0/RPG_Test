using UnityEngine;
using Live2D.Cubism.Framework.Json;    // CubismMotion3Json
using Live2D.Cubism.Framework.Motion;  // CubismMotionController

public class L2DMotionTest : MonoBehaviour
{
    public TextAsset motion3File;
    private CubismMotionController motionController;

    void Awake()
    {
        motionController = GetComponent<CubismMotionController>();
    }

    void Start()
    {
        if (motionController == null || motion3File == null)
        {
            Debug.LogError("缺少 CubismMotionController 或 motion3 檔。");
            return;
        }

        // 解析 motion3
        var clip = CubismMotion3Json.LoadFrom(motion3File.text).ToAnimationClip();
        clip.wrapMode = WrapMode.Loop;

        // 播放，使用 PriorityForce
        motionController.PlayAnimation(clip, 0, CubismMotionPriority.PriorityForce);
    }
}
