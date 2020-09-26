using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ApplyLocalization : MonoBehaviour{
    // ID of localization
    public string localizationID;
    
    void Start(){
        // Set text based on Localization.cs
        GetComponent<TMPro.TextMeshProUGUI>().SetText(Localization.Translate(localizationID));
    }
}
