using UnityEngine;
using Cinemachine;

[DefaultExecutionOrder(500)] // 確保在 Player 出現之後才跑
public class CameraAutoFollow : MonoBehaviour
{
    void Start()
    {
        var vcam = GetComponent<CinemachineVirtualCamera>();
        if (vcam != null && vcam.Follow == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                vcam.Follow = player.transform;
        }
    }
}
