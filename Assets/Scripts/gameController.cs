using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class gameController : MonoBehaviour{
    // #############################################
    // ##### VARIABLES
    
    [SerializeField]
    [Tooltip("Total loot worth in k$ (current score)")]
    private float currentWorth = 0f;
    
    [Tooltip("Loot worth to get in k$ (score objective)")]
    public float totalWorth = 500f;
    
    [Header("References to objects/components")]
    [Tooltip("Text mesh holding game objective information")]
    public TMPro.TextMeshProUGUI textMeshWorthScore;
    
    // #############################################
    // ##### METHODS
    
    // Update score UI text
    private void UpdateWorth(){
        textMeshWorthScore.SetText(currentWorth.ToString("0.000") + "$ / " + totalWorth.ToString("0.000") + "$");
    }
    
    // Add score and update UI text about worth
    public void AddToWorth(float a_amount){
        currentWorth += a_amount;
        UpdateWorth();
    }
    
    // #############################################
    // ##### EVENTS
    
    // On start
    void Start(){
        // Set application framerate to current screen refresh rate
        Application.targetFrameRate = Screen.currentResolution.refreshRate;
    }
}
