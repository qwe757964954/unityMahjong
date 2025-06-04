using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Camera))]
public class AutoMahjongCamera : MonoBehaviour
{
    [Header("麻将桌区域")]
    public Bounds mahjongTableBounds;

    [Header("相机偏移（向后）距离")]
    public float cameraBackDistance = 2.0f;

    [Header("最小 / 最大 FOV 限制")]
    public float minFOV = 25f;
    public float maxFOV = 60f;

    [Header("动画时长（秒）")]
    public float transitionDuration = 0.5f;

    private Camera cam;

    private int lastScreenWidth = -1;
    private int lastScreenHeight = -1;

    void Start()
    {
        cam = GetComponent<Camera>();
        ApplyCameraFit(animated: false);
    }

    void Update()
    {
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            ApplyCameraFit(animated: true);
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
        }
    }

    public void ApplyCameraFit(bool animated)
    {
        Vector3 tableCenter = mahjongTableBounds.center;
        float tableSize = Mathf.Max(mahjongTableBounds.size.x, mahjongTableBounds.size.z); // 使用更宽的边
        float aspect = (float)Screen.width / Screen.height;

        // 计算理想 FOV
        float desiredVisibleHeight = tableSize / aspect;
        float distance = cameraBackDistance;

        float fovRad = 2f * Mathf.Atan(desiredVisibleHeight / (2f * distance));
        float fovDeg = Mathf.Rad2Deg * fovRad;
        fovDeg = Mathf.Clamp(fovDeg, minFOV, maxFOV);

        // 设置相机位置（以 Z 轴为前后）
        Vector3 targetPosition = tableCenter + new Vector3(0, 0, -distance);

        // 设置视角和位置动画
        if (animated)
        {
            cam.DOFieldOfView(fovDeg, transitionDuration);
            transform.DOMove(targetPosition, transitionDuration);
        }
        else
        {
            cam.fieldOfView = fovDeg;
            transform.position = targetPosition;
        }

        // 确保相机始终看向麻将桌
        transform.LookAt(tableCenter);
    }
}