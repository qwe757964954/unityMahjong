 using UnityEngine;

public class DiceController : MonoBehaviour
{
    [Header("Dice GameObjects")]
    [SerializeField] public GameObject dice1;
    [SerializeField] public GameObject dice2;
    // 色子1点数到旋转角度的映射（可根据实际模型调整）
    private static readonly Vector3[] diceRotations1 = new Vector3[]
    {
        new Vector3(0, 0, -90),      // 占位，点数从1开始
        new Vector3(0, 0, -90),     // 1
        new Vector3(0, 22, 90),    // 2
        new Vector3(0, 0, 0),    // 3
        new Vector3(0, 0, 180),      // 4
        new Vector3(0, -22, 90),     // 5
        new Vector3(0, 0, 90),     // 6
        
    };
    // 色子2点数到旋转角度的映射（可根据实际模型调整）
    private static readonly Vector3[] diceRotations2 = new Vector3[]
    {
        new Vector3(0, 0, 0),      // 占位，点数从1开始
        new Vector3(0, 0, 90),     // 1
        new Vector3(0, 0, -180),    // 2
        // new Vector3(0, 180, 0),    // 3
        new Vector3(0, 0, 0),      // 4
        new Vector3(0, 0, 0),     // 5
        new Vector3(0, 0, -90),     // 6
        
    };

    void Start()
    {
    }
    // 设置两个色子的点数
    public void SetDiceNumbers(int num1, int num2)
    {
        if (num1 < 1 || num1 > 6 || num2 < 1 || num2 > 6)
        {
            Debug.LogError("Dice numbers must be between 1 and 6.");
            return;
        }
        if (dice1 != null)
            dice1.transform.localEulerAngles = diceRotations1[num1];
        if (dice2 != null)
            dice2.transform.localEulerAngles = diceRotations2[num2];
    }
}
