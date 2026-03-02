using UnityEngine;
using TMPro; 
using System.Collections;

public class PowerGridManager : MonoBehaviour
{
    public static PowerGridManager Instance;

    [Header("UI Hiển thị")]
    public TextMeshProUGUI progressText; 

    private int totalNodes = 0;
    private int fixedNodes = 0;

    void Awake() 
    { 
        if (Instance == null) Instance = this; 
    }

    IEnumerator Start()
    {
        yield return new WaitForSeconds(1.5f);

        CircuitNode[] allNodes = Object.FindObjectsByType<CircuitNode>(FindObjectsSortMode.None);
        foreach (var node in allNodes)
        {
            if (!node.isWire) 
            {
                totalNodes++;
            }
        }
        
        UpdateUI();
        Debug.Log($"[PowerGrid] Đã đếm được {totalNodes} Hộp nối cần sửa!");
    }

    public void AddFixedNode()
    {
        fixedNodes++;
        UpdateUI();

        if (fixedNodes >= totalNodes && totalNodes > 0)
        {
            if (Floor1Manager.Instance != null)
            {
                Floor1Manager.Instance.LevelComplete();
            }
        }
    }

    void UpdateUI()
    {
        if (progressText != null)
        {
            progressText.text = $"Circuits: {fixedNodes}/{totalNodes}";
        }
    }
}