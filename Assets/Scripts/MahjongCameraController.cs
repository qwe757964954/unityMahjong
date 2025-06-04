using UnityEngine;
using DG.Tweening;
using System.Linq;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class MahjongCameraController : MonoBehaviour
{
    [Header("麻将桌设置")] public Renderer[] tableRenderers;
    private Bounds tableBounds;

    [Header("相机模式")] public bool useOrthographic = false;

    [Header("透视相机参数")] public float perspectiveDistance = 1.75f;
    public float minFOV = 25f;
    public float maxFOV = 60f;

    [Header("正交相机参数")] public float orthoPadding = 0.1f;

    [Header("相机角度（世界空间 Euler）")] public Vector3 cameraEulerAngles = new Vector3(44f, 180f, 0f);

    [Header("偏移（可选，例如用于上下调整）")] public Vector3 additionalOffset = new Vector3(0, 1.59f, 1.75f);

    [Header("动画参数")] public bool enableTransition = true;
    public float transitionDuration = 0.5f;

    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = useOrthographic;

        if (tableRenderers == null || tableRenderers.Length == 0)
        {
            tableRenderers = FindObjectsOfType<Renderer>()
                .Where(r => r.gameObject.name.ToLower().Contains("table")).ToArray();
        }

        CalculateTableBounds();
        ApplyCameraFit(animated: false);
    }

#if UNITY_EDITOR
    void Update()
    {
        if (!Application.isPlaying)
        {
            CalculateTableBounds();
            ApplyCameraFit(animated: false);
        }
    }
#endif

    private void CalculateTableBounds()
    {
        if (tableRenderers == null || tableRenderers.Length == 0)
            return;

        tableBounds = tableRenderers[0].bounds;
        foreach (var r in tableRenderers)
        {
            tableBounds.Encapsulate(r.bounds);
        }

        Debug.Log($"[MahjongCamera] Bounds Center: {tableBounds.center}, Size: {tableBounds.size}");
    }

    public void ApplyCameraFit(bool animated)
    {
        if (cam == null) return;

        // 相机角度
        Quaternion targetRotation = Quaternion.Euler(cameraEulerAngles);

        // 麻将桌中心
        Vector3 center = tableBounds.center;

        // 相机向前方向（朝向麻将桌）
        Vector3 forward = targetRotation * Vector3.forward;

        // 最终位置：从中心往回退 distance，并添加 Y 偏移等
        Vector3 targetPosition = center - forward * perspectiveDistance + additionalOffset;

        float targetFOV = cam.fieldOfView;
        float aspect = (float)Screen.width / Screen.height;

        if (useOrthographic)
        {
            float width = tableBounds.size.x * 0.5f + orthoPadding;
            float height = tableBounds.size.z * 0.5f + orthoPadding;
            float targetOrthoSize = Mathf.Max(height, width / aspect);

            SetCamera(animated, targetPosition, targetRotation, 0f, targetOrthoSize);
        }
        else
        {
            // 自动计算适合的 FOV
            float visibleHeight = tableBounds.size.z + orthoPadding * 2f;
            float actualDistance = Vector3.Distance(targetPosition, center);
            float fovRad = 2f * Mathf.Atan(visibleHeight / (2f * actualDistance));
            targetFOV = Mathf.Clamp(Mathf.Rad2Deg * fovRad, minFOV, maxFOV);

            SetCamera(animated, targetPosition, targetRotation, targetFOV, 0f);
        }

        Debug.Log(
            $"[MahjongCamera] FOV: {targetFOV:F2}, Pos: {targetPosition}, Rot: {cameraEulerAngles}, forward: {forward}");
    }

    private void SetCamera(bool animated, Vector3 pos, Quaternion rot, float fov, float orthoSize)
    {
        if (animated && Application.isPlaying)
        {
            transform.DOMove(pos, transitionDuration);
            transform.DORotateQuaternion(rot, transitionDuration);

            if (useOrthographic)
                DOTween.To(() => cam.orthographicSize, v => cam.orthographicSize = v, orthoSize, transitionDuration);
            else
                cam.DOFieldOfView(fov, transitionDuration);
        }
        else
        {
            transform.position = pos;
            transform.rotation = rot;

            if (useOrthographic)
                cam.orthographicSize = orthoSize;
            else
                cam.fieldOfView = fov;
        }
    }
}