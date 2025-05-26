using UnityEngine;

public class DiceShaker : MonoBehaviour
{
    public Rigidbody[] dice;
    public float shakeDuration = 1.5f;
    public float shakeForce = 20f;         // 更大推力
    public float torqueForce = 10f;
    public float radius = 0.3f;            // 旋转半径
    public float angularSpeed = 720f;      // 每秒角度

    private float timer = 0f;
    private bool shaking = false;
    private float angle = 0f;

    public Transform centerPoint; // 圆筒中心点位置

    void Update()
    {
        if (shaking)
        {
            timer += Time.deltaTime;
            angle += angularSpeed * Time.deltaTime;

            foreach (var die in dice)
            {
                // 计算目标圆周上的位置
                Vector3 dir = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0, Mathf.Sin(angle * Mathf.Deg2Rad));
                Vector3 targetPos = centerPoint.position + dir * radius;

                // 计算朝目标位置的方向并施加力
                Vector3 forceDir = (targetPos - die.position).normalized;
                die.AddForce(forceDir * shakeForce, ForceMode.Acceleration);
                die.AddTorque(Random.onUnitSphere * torqueForce);
            }

            if (timer > shakeDuration)
            {
                shaking = false;
                timer = 0f;
            }
        }
    }

    public void StartShaking()
    {
        angle = Random.Range(0f, 360f);
        timer = 0f;
        shaking = true;

        foreach (var die in dice)
        {
            die.velocity = Vector3.zero;
            die.angularVelocity = Vector3.zero;
            die.transform.position = centerPoint.position + Random.insideUnitSphere * 0.1f;
            die.transform.rotation = Random.rotation;
        }
    }
    
    void Start()
    {
        StartShaking();
    }
}