using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Localization {
    // #############################################
    // ##### VARIABLES
    
    // Language used as default
    private static string language = "polish";
    
    // Translations
    public static Dictionary<string, Dictionary<string, string>> strings = new Dictionary<string, Dictionary<string, string>>(){
        ["english"] = new Dictionary<string, string>(){
            ["UI_EQUIPPED"] = "Equipped:",
            ["UI_PRESS_TO_RESTART"] = "Press <R> button to restart",
            ["UI_OBJECTIVE_COMPLETED"] = "Objective completed!",
            ["UI_ESCAPE_READY"] = "Return to van!",
            ["UI_EMPTY_ITEM"] = "None",
            ["UI_DEFAULT_OBJECTIVE"] = "Collect loot worth:",
            
            ["RESULTS_MISSION_SUCCESSFUL"] = "Mission successful!",
            ["RESULTS_MISSION_SUCCESSFUL_SUB"] = "You managed to escape with all gathered loot!",
            ["RESULTS_MISSION_FAILED"] = "Mission failed!",
            ["RESULTS_MISSION_FAILED_SUB"] = "You have been caught!",
            
            ["POPUP_HOLD_TO_LOOT"] = "Hold [E] to loot",
            ["POPUP_TAKE_LOOT_TO_SPAWN"] = "Firstly deposit loot at lootzone!",
            
        },
        ["polish"] = new Dictionary<string, string>(){
            ["UI_EQUIPPED"] = "Wyposażono:",
            ["UI_PRESS_TO_RESTART"] = "Wciśnij <R> by zresetować",
            ["UI_OBJECTIVE_COMPLETED"] = "Zadanie wykonane!",
            ["UI_ESCAPE_READY"] = "Wróć do wozu!",
            ["UI_EMPTY_ITEM"] = "Nic",
            ["UI_DEFAULT_OBJECTIVE"] = "Zbierz łup o wartości:",
            
            ["RESULTS_MISSION_SUCCESSFUL"] = "Misja zakończona sukcesem!",
            ["RESULTS_MISSION_SUCCESSFUL_SUB"] = "Udało Ci się uciec z zebranem łupem!",
            ["RESULTS_MISSION_FAILED"] = "Misja zakończona porażką!",
            ["RESULTS_MISSION_FAILED_SUB"] = "Zostałeś złapany!",
            
            ["POPUP_HOLD_TO_LOOT"] = "Przytrzymaj [E] by złupić",
            ["POPUP_TAKE_LOOT_TO_SPAWN"] = "Najpierw przynieś łup do depozytu!",
        }
    };
    
    // Color constants
    public static Color32 COLOR_INFORMATION = new Color32(255, 255, 255, 255);
    public static Color32 COLOR_DISABLED = new Color32(123, 123, 123, 255);
    public static Color32 COLOR_SPECIAL = new Color32(255, 194, 28, 255);
    
    // #############################################
    // ##### METHODS
    
    // Translate string based on language
    public static string Translate(string a_id){
        return strings[language][a_id];
    }
}