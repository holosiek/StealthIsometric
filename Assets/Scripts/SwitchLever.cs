using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchLever : MonoBehaviour {
    // #############################################
    // ##### ENUM
    
    public enum SwitchType{
        Move
    }
    
    
    // #############################################
    // ##### VARIABLES
    
    [Tooltip("Reference to lever")]
    public GameObject leverObj;
    
    [Tooltip("Object to interact with")]
    public GameObject objToSwitch;
    
    [Tooltip("Type of switch")]
    public SwitchType type;
    
    [Tooltip("[Move type] Vector towards which should be object moved")]
    public Vector3 moveVector;
    
    // Is switch turned on
    private bool isOn = false;
    
    // #############################################
    // ##### METHODS
    
    // Pull the lever
    public void PullLever(){
        // Change state of lever
        isOn = !isOn;
        // Play switch sound
        FindObjectOfType<AudioManager>().Play("Switch");
        // Set rotation of lever element
        leverObj.transform.rotation = Quaternion.Euler(0, 0, (isOn)?(30):(-30));
        // If type is Move, change position of object by vector
        if(type == SwitchType.Move){
            objToSwitch.transform.position += (isOn)?(moveVector):(-moveVector);
        }
    }
}