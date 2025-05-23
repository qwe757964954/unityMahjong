using UnityEngine;

[ExecuteAlways]
public class MahjongTableFitter : MonoBehaviour
{
    public Transform tableTransform;
    public Camera targetCamera;
    public float viewAngle = 45f;
    public float paddingPercent = 0.05f;

    void LateUpdate()
    {
        if (tableTransform == null || targetCamera == null)
            return;

        float aspect = (float)Screen.width / Screen.height;
        float fovRad = Mathf.Deg2Rad * targetCamera.fieldOfView;
        float angleRad = Mathf.Deg2Rad * viewAngle;
        Bounds bounds = GetCombinedRendererBounds(tableTransform);

        // Bounds bounds = renderer.bounds;
        float tableWidth = bounds.size.x * (1 + paddingPercent * 2);
        float tableHeight = bounds.size.z * (1 + paddingPercent * 2);

        // —— 优先根据宽度计算相机距离 ——
        float zFromWidth = (tableWidth / 2f) / (Mathf.Tan(fovRad / 2f) * aspect);
        float yFromWidth = zFromWidth * Mathf.Tan(angleRad);
        float visibleHeight = 2f * yFromWidth; // 可视高度

        if (visibleHeight >= tableHeight)
        {
            // 宽度优先方案能容纳高度
            targetCamera.transform.position = new Vector3(0, yFromWidth, -zFromWidth);
        }
        else
        {
            // 退回用高度方案，避免上下裁切
            float yFromHeight = tableHeight / 2f;
            float zFromHeight = yFromHeight / Mathf.Tan(angleRad);
            targetCamera.transform.position = new Vector3(0, yFromHeight, -zFromHeight);
        }

        targetCamera.transform.rotation = Quaternion.Euler(viewAngle, 0, 0);
        targetCamera.transform.LookAt(tableTransform.position);
    }
    
    Bounds GetCombinedRendererBounds(Transform root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>();
        Debug.Log(renderers.Length);
        if (renderers.Length == 0) return new Bounds(root.position, Vector3.zero);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
        Debug.Log(bounds);
        return bounds;
    }
}