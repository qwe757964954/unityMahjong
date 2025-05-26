using UnityEngine;
public class DiceFaceReader : MonoBehaviour
{
    public int GetUpFace()
    {
        Vector3 up = Vector3.up;
        float maxDot = -1;
        int result = -1;

        for (int i = 0; i < 6; i++)
        {
            Vector3 dir = GetFaceDirection(i);
            float dot = Vector3.Dot(transform.TransformDirection(dir), up);

            if (dot > maxDot)
            {
                maxDot = dot;
                result = i + 1; // 点数从1开始
            }
        }

        return result;
    }

    Vector3 GetFaceDirection(int index)
    {
        switch (index)
        {
            case 0: return Vector3.up;      // 1点
            case 1: return Vector3.down;    // 6点
            case 2: return Vector3.forward; // 2点
            case 3: return Vector3.back;    // 5点
            case 4: return Vector3.left;    // 3点
            case 5: return Vector3.right;   // 4点
        }
        return Vector3.up;
    }
}