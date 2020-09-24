using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class lootInformation : MonoBehaviour{
    // #############################################
    // ##### VARIABLES
    [Range(0.2f, 10f)]
    [Tooltip("Time to loot this in seconds")]
    public float timeToLoot = 1f;
    
    [Tooltip("Worth of loot in k$")]
    public float lootScore = 10f;
    
    [Tooltip("Name of Loot")]
    public string lootName;
    
    [Range(0, 5)]
    [Tooltip("Weight of loot")]
    public int lootWeight;
}
