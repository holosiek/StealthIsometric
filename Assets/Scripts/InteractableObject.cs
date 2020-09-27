using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractableObject : MonoBehaviour {
    // #############################################
    // ##### ENUMS
    
    public enum InteractableType{
        Loot,
        Switch
    }
    
    // #############################################
    // ##### VARIABLES
    
    [Tooltip("Choose what kind of interactable this object is")]
    public InteractableType objType;
    
    [Header("Switch only:")]
    [Tooltip("Time to interact with this object [Switch only]")]
    public float objInteractTime;
}