using UnityEngine;

public class DiceRoller : MonoBehaviour
{
    public Rigidbody[] dice;
    public float throwForce = 8f;
    public float torqueForce = 20f;
    public Transform throwPoint;

    void Start()
    {
        Roll();
    }

    public void Roll()
    {
        foreach (var die in dice)
        {
            die.velocity = Vector3.zero;
            die.angularVelocity = Vector3.zero;

            // 放置初始位置
            die.transform.position = throwPoint.position + Random.insideUnitSphere * 0.1f;
            die.transform.rotation = Random.rotation;

            // 随机抛掷方向和旋转
            Vector3 dir = (Vector3.up + new Vector3(Random.Range(-0.4f, 0.4f), 0, Random.Range(-0.4f, 0.4f))).normalized;
            die.AddForce(dir * throwForce, ForceMode.Impulse);
            die.AddTorque(Random.insideUnitSphere * torqueForce, ForceMode.Impulse);
        }
    }
}