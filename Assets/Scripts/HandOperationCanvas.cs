using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HandOperationCanvas : MonoBehaviour
{
    public GameObject shuffleButton;

    public GameObject sendHandCardButton;

    public GameObject riceNumberInput;

    private MahjongManager mahjongManager;
    private DiceController diceController;
    private InputField riceInputField;

    private GameObject rice1;
    private GameObject rice2;
    // Start is called before the first frame update
    void Start()
    {
        mahjongManager = FindObjectOfType<MahjongManager>();
        diceController = FindObjectOfType<DiceController>();
        if (shuffleButton != null && mahjongManager != null)
        {
            Button btn = shuffleButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() => StartCoroutine(ShuffleAndSetDice()));
            }
        }
        // 获取输入框组件
        if (riceNumberInput != null)
        {
            riceInputField = riceNumberInput.GetComponent<InputField>();
        }
    }

    // Update is called once per frame
    private IEnumerator ShuffleAndSetDice()
    {
        if (mahjongManager != null)
        {
            mahjongManager.PlayRackAnimation();
        }
        yield return new WaitForSeconds(0.5f);
        // 解析输入点数
        int n1 = 1, n2 = 1;
        if (riceInputField != null)
        {
            string input = riceInputField.text.Trim();
            char[] split = new char[] { ' ', ',', ';', '，' };
            string[] nums = input.Split(split, System.StringSplitOptions.RemoveEmptyEntries);
            Debug.Log("nums: " + nums.Length + " " + nums[0] + " " + nums[1]);
            if (nums.Length >= 2)
            {
                int.TryParse(nums[0], out n1);
                int.TryParse(nums[1], out n2);
            }
        }
        if (diceController != null)
        {
            diceController.SetDiceNumbers(n1, n2);
        }
    }
}
