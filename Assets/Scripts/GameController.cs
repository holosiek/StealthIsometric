using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour{
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
    public float totalWorth = 50f;
    
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
    [Tooltip("Escape Zone reference, it will appear after objective will be completed")]
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
    
    //------------------------
    // >>> Others
    //------------------------
    
    // Scene which we will restart after fail/success
    private string sceneToRestart = "SampleScene";
    
    // #############################################
    // ##### METHODS
    
    // After getting enough money, return to escape zone to "win"
    private void ObjectiveComplete(){
        // Update text information on objective
        textMeshWorthTitle.SetText(Localization.Translate("UI_OBJECTIVE_COMPLETED"));
        textMeshWorthScore.SetText(Localization.Translate("UI_ESCAPE_READY"));
        // Turn escape object on
        escapeZoneObject.SetActive(true);
    }
    
    // Add score and update UI text about worth
    public void AddToWorth(float a_amount){
        // Add to current worth
        currentWorth += a_amount;
        // If current worth is bigger or equal our objective, set objective as completed
        if(currentWorth >= totalWorth){
            ObjectiveComplete();
        // Else just update objective score
        } else {
            UpdateWorth();
        }
    }
    
    // Set mission state to success and display success screen
    public void SetMissionSuccess(){
        // Set "is post processing in phase of updating" to true
        isPPUpdating = true;
        // Reset pp interpolation timer
        ppInterpTimer = 0f;
        // Update results screen text
        textMeshHeader.SetText(Localization.Translate("RESULTS_MISSION_SUCCESSFUL"));
        textMeshSubheader.SetText(Localization.Translate("RESULTS_MISSION_SUCCESSFUL_SUB"));
        textMeshHeader.color = successfulMissionColor;
        // Set which PP setting is used to success
        whichPPSettingisSet = PPSettings.Success;
    }
    
    // Set mission state to failed and display failed screen
    public void SetMissionFailed(){
        // Set "is post processing in phase of updating" to true
        isPPUpdating = true;
        // Reset pp interpolation timer
        ppInterpTimer = 0f;
        // Update results screen text
        textMeshHeader.SetText(Localization.Translate("RESULTS_MISSION_FAILED"));
        textMeshSubheader.SetText(Localization.Translate("RESULTS_MISSION_FAILED_SUB"));
        textMeshHeader.color = failedMissionColor;
        // Set which PP setting is used to failed
        whichPPSettingisSet = PPSettings.Failed;
    }
    
    // Update equipped item text mesh
    public void UpdateEquipped(){
        // If there's no loot taken
        if(String.IsNullOrEmpty(lootInfo.name)){
            // Set current item as empty
            imageEquippedItem.sprite = emptySlot;
            textMeshEquippedItem.color = Localization.COLOR_DISABLED;
            textMeshEquippedItem.SetText(Localization.Translate("UI_EMPTY_ITEM"));
        // Else player is holding loot
        } else {
            // Set current item as held loot
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
        // If argument PPsettings are different from default
        if(a_settings != PPSettings.Default){
            // Interpolate scale of results screen
            textMeshHeaderTransform.localScale = new Vector3(Mathf.Min(ppInterpTimer, 1f), Mathf.Min(ppInterpTimer, 1f), 1f);
            gameplayUIGroup.alpha = Mathf.Max(1.0f-ppInterpTimer*2, 0f);
            resultsUIGroup.alpha = 0f;
        // Else if argument PPsetting is default
        } else {
            // Set header scale to 0 and turn on gameplay UI
            textMeshHeaderTransform.localScale = Vector3.zero;
            gameplayUIGroup.alpha = 1f;
            resultsUIGroup.alpha = 0f;
        }
    }
    
    // Update header size
    private void UpdateResults(PPSettings a_settings){
        // If argument PPsettings is not default
        if(a_settings != PPSettings.Default){
            // Interpolate alpha of results screen
            resultsUIGroup.alpha = Mathf.Min(-1.0f+ppInterpTimer, 1f);
        }
    }
    
    //------------------------
    // >>> Post Process
    //------------------------
    
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
    public void UpdateLootInfo(LootInformation a_ref){
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
        
        // Hide cursor
        Cursor.visible = false;
        
        // Init loot information struct
        lootInfo = new LootInfo();
        ResetLootInfo();
        
        // Save references to post process volume settings
        postProcessVolume.profile.TryGet<Vignette>(out postProcessVignette);
        postProcessVolume.profile.TryGet<ColorAdjustments>(out postProcessColorAdjustments);
        
        // Update UI
        UpdateWorth();
        UpdateEquipped();
        UpdateHeader(whichPPSettingisSet);
        
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
        
        // If PP interp timer is after interpolating main stuff
        if(ppInterpTimer >= 1.0f && ppInterpTimer < 2.0f){
            // Add to interpolate timer and update results screen
            ppInterpTimer += Time.deltaTime;
            UpdateResults(whichPPSettingisSet);
        // Otherwise reset timer
        } else if(ppInterpTimer >= 2.0f){
            ppInterpTimer = 0f;
        }
        
        // If player failed or succeed and they press restart button, reload scene
        if(whichPPSettingisSet != PPSettings.Default && Input.GetKeyDown("r")){
            SceneManager.LoadScene(sceneToRestart);
        }
        
        // If player hits "escape", quit application
        if(Input.GetKey("escape")){
            Application.Quit();
        }
    }
}
