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

    // Start is called before the first frame update
    void Start()
    {
        mahjongManager = FindObjectOfType<MahjongManager>();
        if (shuffleButton != null && mahjongManager != null)
        {
            Button btn = shuffleButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() => mahjongManager.PlayRackAnimation());
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
