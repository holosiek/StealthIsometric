using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class player : MonoBehaviour{
    // LootInfo stores information about loot
    struct LootInfo {
        string lootName;
        float lootTime;
        float lootWorth;
        int lootWeight;
    }
    
    // #############################################
    // ##### VARIABLES
    private CharacterController controller;
    private Vector3 move;
    public float moveSpeed = 5f;
    public float cameraScrollSpeed = 1f;
    public GameObject canvasObj;
    public GameObject lootObj;
    public RectTransform informationTextTransform;
    public TMPro.TextMeshProUGUI informationTextObj;
    public TMPro.TextMeshProUGUI infoToHold;
    public gameController gameController;
    private Vector3 offsetText = new Vector3(0, 150, 0);
    private short lootEntered = 0;
    private List<GameObject> lootableObjects = new List<GameObject>();
    private float timerToLoot = 0f;
    private float toLoot = 0f;
    
    // #############################################
    // ##### METHODS
    
    // #############################################
    // ##### EVENTS
    
    // On collision enter
    void OnCollisionEnter(Collision collision){
        // If player collides with enemy, restart game
        if(collision.collider.tag.Equals("Enemy")){
            SceneManager.LoadScene("SampleScene");
        }
    }
    
    // On trigger enter
    void OnTriggerEnter(Collider collider){
        // If player enters loot trigger
        if(collider.tag.Equals("Loot")){
            // Save last "lootable" object to variable
            lootObj = collider.gameObject;
            // If given object is not in our lootable object list, add it
            if(!lootableObjects.Contains(lootObj)){
                lootableObjects.Add(lootObj);
            }
            // Save information about loot in 
            toLoot = lootObj.GetComponent<lootInformation>().timeToLoot;
            lootEntered++;
            if(lootEntered == 1){
                informationTextObj.SetText("Hold [e] to loot");
            }
        // Else if player enters loot zone trigger
        } else if(collider.tag.Equals("Loot Zone")){
            
        }
    }
    
    void OnTriggerExit(Collider collider){
        if(collider.tag.Equals("Loot")){
            lootableObjects.Remove(collider.gameObject);
            lootEntered--;
            if(lootEntered == 0){
                informationTextObj.text = "";
                lootObj = null;
                toLoot = 0f;
            } else {
                lootObj = lootableObjects[lootableObjects.Count-1].gameObject;
                toLoot = lootObj.GetComponent<lootInformation>().timeToLoot;
            }
        }
    }
    
    void Start(){
        lootObj = null;
        controller = GetComponent<CharacterController>();
    }

    // Every frame
    void Update(){
        // Reset move vector
        move = Vector3.zero;
        // Check if WSAD is hold and add to move vector
        if(Input.GetKey("w")){
            move.x += 1f;
            move.z += 1f;
        }
        if(Input.GetKey("s")){
            move.x -= 1f;
            move.z -= 1f;
        }
        if(Input.GetKey("a")){
            move.x -= 1f;
            move.z += 1f;
        }
        if(Input.GetKey("d")){
            move.x += 1f;
            move.z -= 1f;
        }
        //----------------------------------
        // If there is any loot to get and player is holding "e" (loot button)
        if(lootEntered > 0 && Input.GetKey("e")){
            // Add to "holding to loot" timer
            timerToLoot += Time.deltaTime;
            // If our timer passed loot time
            if(timerToLoot >= toLoot){
                timerToLoot = 0f;
                gameController.AddToWorth(lootObj.GetComponent<lootInformation>().lootScore);
                lootObj.SetActive(false);
                lootableObjects.Remove(lootObj);
                lootEntered--;
                if(lootEntered == 0){
                    informationTextObj.text = "";
                    lootObj = null;
                    toLoot = 0f;
                } else {
                    lootObj = lootableObjects[lootableObjects.Count-1].gameObject;
                    toLoot = lootObj.GetComponent<lootInformation>().timeToLoot;
                }
            }
        } else {
            timerToLoot = 0f;
        }
        infoToHold.SetText(timerToLoot.ToString() + "/" + toLoot.ToString());
        move = move.normalized;
        move.y -= 5f;
        if(controller.isGrounded){
            move.y = 0.01f;
        }
        if(Input.mouseScrollDelta.y != 0f){
            float t_scrollZoom = Camera.main.orthographicSize-Input.mouseScrollDelta.y*cameraScrollSpeed;
            Camera.main.orthographicSize = Mathf.Clamp(t_scrollZoom, 4f, 10f);
        }
        if(!(lootObj is null)){
            informationTextTransform.position = Camera.main.WorldToScreenPoint(lootObj.transform.position)+offsetText*5/Camera.main.orthographicSize;
        }
        controller.Move(move*Time.deltaTime*moveSpeed);
    }
}
