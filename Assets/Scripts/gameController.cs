using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class gameController : MonoBehaviour{
    // #############################################
    // ##### STRUCTS
    
    // LootInfo stores information about loot
    public struct LootInfo {
        public string name;
        public float time;
        public float worth;
        public int weight;
        public Sprite image;
    }
    
    // #############################################
    // ##### ENUMS
    
    public enum PPSettings{
        Default,
        Success,
        Failed
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
    [Tooltip("RectTransform of header (like \"Succesful mission\" text)")]
    public RectTransform textMeshHeaderTransform;
    [Tooltip("Image component of currently equipped item")]
    public Image imageEquippedItem;
    [Tooltip("Global postprocess volume reference")]
    public Volume postProcessVolume;
    // Reference to vignette of post process volume
    private Vignette postProcessVignette;
    // Reference to color adjustments of post process volume
    private ColorAdjustments postProcessColorAdjustments;
    
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
    
    //------------------------
    // >>> Post processing
    //------------------------
    // Normal post processing settings
    [Header("PP settings - Default")]
    [Space(20)]
    [Range(0f, 1f)]
    [Tooltip("Vignette intensivity")]
    public float vignetteIntensivityDefault;
    [Tooltip("ColorAdjustments color (leave white to not change colors)")]
    public Color colorAdjustmentsColorDefault;
    
    // Success mission post processing settings
    [Header("PP settings - Successful Mission")]
    [Range(0f, 1f)]
    [Tooltip("Vignette intensivity")]
    public float vignetteIntensivitySuccess;
    [Tooltip("ColorAdjustments color (leave white to not change colors)")]
    public Color colorAdjustmentsColorSuccess;
    
    // Failed mission post processing settings
    [Header("PP settings - Failed Mission")]
    [Range(0f, 1f)]
    [Tooltip("Vignette intensivity")]
    public float vignetteIntensivityFailed;
    [Tooltip("ColorAdjustments color (leave white to not change colors)")]
    public Color colorAdjustmentsColorFailed;
    
    // Is post processing in progress of updating
    private bool isPPUpdating = false;
    // Which PP setting is set
    public PPSettings whichPPSettingisSet = PPSettings.Default;
    // Post processing timer for interp values
    private float ppInterpTimer = 0f;
    
    //------------------------
    // >>> UI
    //------------------------
    [Header("Header text")]
    [Space(20)]
    [Tooltip("Text mesh of header (like \"Succesful mission\" text)")]
    public TMPro.TextMeshProUGUI textMeshHeader;
    [Tooltip("Text mesh of subheader (like \"You have been caught\" text)")]
    public TMPro.TextMeshProUGUI textMeshSubheader;
    [Tooltip("'Successful mission' text color")]
    public Color successfulMissionColor;
    [Tooltip("'Failed mission' text color")]
    public Color failedMissionColor;
    
    [Header("UI Groups")]
    [Tooltip("Gameplay UI canvas group")]
    public CanvasGroup gameplayUIGroup;
    [Tooltip("Results UI canvas group")]
    public CanvasGroup resultsUIGroup;
    
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
    
    // Set mission state to success and display success screen
    public void SetMissionSuccess(){
        isPPUpdating = true;
        ppInterpTimer = 0f;
        textMeshHeader.SetText(Localization.MISSION_SUCCESSFUL);
        textMeshSubheader.SetText(Localization.MISSION_SUCCESSFUL_SUB);
        textMeshHeader.color = successfulMissionColor;
        whichPPSettingisSet = PPSettings.Success;
    }
    
    // Set mission state to failed and display failed screen
    public void SetMissionFailed(){
        isPPUpdating = true;
        ppInterpTimer = 0f;
        textMeshHeader.SetText(Localization.MISSION_FAILED);
        textMeshSubheader.SetText(Localization.MISSION_FAILED_SUB);
        textMeshHeader.color = failedMissionColor;
        whichPPSettingisSet = PPSettings.Failed;
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
    
    // Update header size
    private void UpdateHeader(PPSettings a_settings){
        if(a_settings != PPSettings.Default){
            textMeshHeaderTransform.localScale = new Vector3(Mathf.Min(ppInterpTimer, 1f), Mathf.Min(ppInterpTimer, 1f), 1f);
            gameplayUIGroup.alpha = Mathf.Max(1.0f-ppInterpTimer*2, 0f);
            resultsUIGroup.alpha = 0f;
        } else {
            textMeshHeaderTransform.localScale = Vector3.zero;
            gameplayUIGroup.alpha = 1f;
            resultsUIGroup.alpha = 0f;
        }
    }
    
    // Update header size
    private void UpdateResults(PPSettings a_settings){
        if(a_settings != PPSettings.Default){
            resultsUIGroup.alpha = Mathf.Min(-1.0f+ppInterpTimer, 1f);
        }
    }
    
    // Update post processing settings
    private void UpdatePP(PPSettings a_settings){
        // Switch settings
        switch(a_settings){
            // Set to default, don't interpolate
            case PPSettings.Default: {
                postProcessVignette.intensity.value = vignetteIntensivityDefault;
                postProcessColorAdjustments.colorFilter.value = colorAdjustmentsColorDefault;
                break;
            }
            // Set to successful PP, interpolate values
            case PPSettings.Success: {
                postProcessVignette.intensity.Interp(vignetteIntensivityDefault, vignetteIntensivitySuccess, ppInterpTimer);
                postProcessColorAdjustments.colorFilter.Interp(colorAdjustmentsColorDefault, colorAdjustmentsColorSuccess, ppInterpTimer);
                break;
            }
            // Set to failed PP, interpolate values
            case PPSettings.Failed: {
                postProcessVignette.intensity.Interp(vignetteIntensivityDefault, vignetteIntensivityFailed, ppInterpTimer);
                postProcessColorAdjustments.colorFilter.Interp(colorAdjustmentsColorDefault, colorAdjustmentsColorFailed, ppInterpTimer);
                break;
            }
        }
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
        UpdateWorth();
        UpdateEquipped();
        UpdateHeader(whichPPSettingisSet);
        
        // Save references to post process volume settings
        postProcessVolume.profile.TryGet<Vignette>(out postProcessVignette);
        postProcessVolume.profile.TryGet<ColorAdjustments>(out postProcessColorAdjustments);
        
        // Set post process settings to default
        UpdatePP(PPSettings.Default);
    }
    
    // Every frame
    void Update(){        
        // If PP is updating, interp values of it
        if(isPPUpdating){
            UpdatePP(whichPPSettingisSet);
            // If we are interpolating values
            if(whichPPSettingisSet != PPSettings.Default){
                // Add to interpolation timer
                ppInterpTimer += Time.deltaTime*2f;
            }
            // Update header size
            UpdateHeader(whichPPSettingisSet);
            // If interpolation timer is over 1.0, reset it and we finished updating PP
            if(ppInterpTimer >= 1.0f){
                isPPUpdating = false;
            }
        }
        if(ppInterpTimer >= 1.0f && ppInterpTimer < 2.0f){
            ppInterpTimer += Time.deltaTime;
            UpdateResults(whichPPSettingisSet);
        } else if(ppInterpTimer >= 2.0f){
            ppInterpTimer = 0f;
        }
    }
}
