using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class gameController : MonoBehaviour{
    // #############################################
    // ##### Structs
    
    // LootInfo stores information about loot
    public struct LootInfo {
        public string name;
        public float time;
        public float worth;
        public int weight;
        public Sprite image;
    }
    
    // #############################################
    // ##### VARIABLES
    
    [SerializeField]
    [Tooltip("Total loot worth in k$ (current score)")]
    private float currentWorth = 0f;
    
    [Tooltip("Loot worth to get in k$ (score objective)")]
    public float totalWorth = 500f;
    
    [Header("References to components")]
    [Tooltip("Text mesh holding game objective title")]
    public TMPro.TextMeshProUGUI textMeshWorthTitle;
    [Tooltip("Text mesh holding game objective score")]
    public TMPro.TextMeshProUGUI textMeshWorthScore;
    [Tooltip("Text mesh of equipped item")]
    public TMPro.TextMeshProUGUI textMeshEquippedItem;
    [Tooltip("Image component of currently equipped item")]
    public Image imageEquippedItem;
    
    [Header("References to objects")]
    [Tooltip("Escape Zone reference, it will appear after objective being completed")]
    public GameObject escapeZoneObject;
    
    [Header("UI related stuff")]
    [Tooltip("Sprite of empty item slot")]
    public Sprite emptySlot;
    
    //------------------------
    // >>> Loot
    //------------------------
    
    // Loot information struct
    public LootInfo lootInfo;
    
    // #############################################
    // ##### METHODS
    
    // After getting enough money, return to escape zone to "win"
    private void ObjectiveComplete(){
        textMeshWorthTitle.SetText(Localization.OBJECTIVE_COMPLETED);
        textMeshWorthScore.SetText(Localization.ESCAPE_READY);
        escapeZoneObject.SetActive(true);
    }
    
    // Add score and update UI text about worth
    public void AddToWorth(float a_amount){
        currentWorth += a_amount;
        if(currentWorth >= totalWorth){
            ObjectiveComplete();
        } else {
            UpdateWorth();
        }
    }
    
    // Update equipped item text mesh
    public void UpdateEquipped(){
        if(String.IsNullOrEmpty(lootInfo.name)){
            imageEquippedItem.sprite = emptySlot;
            textMeshEquippedItem.color = Localization.COLOR_DISABLED;
            textMeshEquippedItem.SetText("None");
        } else {
            imageEquippedItem.sprite = lootInfo.image;
            textMeshEquippedItem.color = Localization.COLOR_INFORMATION;
            textMeshEquippedItem.SetText(lootInfo.name);
        }
    }
    
    // Update score UI text
    private void UpdateWorth(){
        textMeshWorthScore.SetText(currentWorth.ToString("0.000") + "$ / " + totalWorth.ToString("0.000") + "$");
    }
    
    //------------------------
    // >>> Loot
    //------------------------
    
    // Update information about loot in LootInfo struct
    public void UpdateLootInfo(lootInformation a_ref){
        lootInfo.name = a_ref.lootName;
        lootInfo.time = a_ref.timeToLoot;
        lootInfo.worth = a_ref.lootScore;
        lootInfo.weight = a_ref.lootWeight;
        lootInfo.image = a_ref.lootImage;
    }
    
    // Reset information about loot in LootInfo struct
    public void ResetLootInfo(){
        lootInfo.name = "";
        lootInfo.time = 0f;
        lootInfo.worth = 0f;
        lootInfo.weight = 0;
        lootInfo.image = null;
    }
    
    // #############################################
    // ##### EVENTS
    
    // On start
    void Start(){
        // Set application framerate to current screen refresh rate
        Application.targetFrameRate = Screen.currentResolution.refreshRate;
        
        // Init loot information struct
        lootInfo = new LootInfo();
        ResetLootInfo();
        
        // Update UI
        UpdateEquipped();
    }
}
