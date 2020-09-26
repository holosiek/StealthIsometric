using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class player : MonoBehaviour{
    
    // #############################################
    // ##### VARIABLES
    
    //------------------------
    // >>> Player stats
    //------------------------
    [Header("Player stats")]
    [Range(2.5f, 10f)]
    [Tooltip("Base move speed of player")]
    public float moveSpeed = 5f;
    [Range(0f, 1f)]
    [Tooltip("How much weight affects moving (1 weight unit = -1f move speed)")]
    public float moveWeightPenality = 0.25f;
    
    //------------------------
    // >>> Camera
    //------------------------
    [Header("Camera")]
    [Tooltip("Set minimum (X) and maximum (Y) camera zoom levels")]
    public Vector2 cameraZoomBorders = new Vector2(4.0f, 10.0f);
    [Range(0.25f, 2f)]
    [Tooltip("Camera scroll step every mouse wheel rotation")]
    public float cameraScrollSpeed = 1f;
    // Camera zoom level
    private float cameraZoomLevel;
    
    //------------------------
    // >>> Loot
    //------------------------
    // List containing lootable objects in range
    private List<GameObject> lootableObjects = new List<GameObject>();
    // Current loot object or null if not in zone of any
    private GameObject currentLootObj;
    // Total time passed while holding loot button and getting loot
    private float timerToLoot = 0f;
    // Total lootable objects in range counter
    private short lootEntered = 0;
    // Are we holding any loot?
    private bool isHoldingLoot = false;
    
    //------------------------
    // >>> References
    //------------------------
    [Header("References")]
    [Tooltip("Reference to main game controller")]
    public gameController gameController;
    [Tooltip("Reference to pop-up gameObject")]
    public GameObject popupObject;
    [Tooltip("Reference to pop-up transform")]
    public RectTransform popupTransform;
    [Tooltip("Reference to pop-up text component")]
    public TMPro.TextMeshProUGUI popupText;
    [Tooltip("Reference to action timer foreground circle image")]
    public Image actionTimerImage;
    // CharacterController component of player
    private CharacterController characterController;
    
    //------------------------
    // >>> Others
    //------------------------
    // Offset of text above lootable objects
    private Vector3 offsetText = new Vector3(0, 150, 0);
    // Is player interactive with something
    private bool isInteractive = false;
    // How many interactables are player whichin
    private int interactables = 0;
    
    // #############################################
    // ##### METHODS
    
    // Change popup text depending on situation
    void UpdatePopUp(){
        if(isHoldingLoot){
            popupText.color = Localization.COLOR_DISABLED;
            popupText.SetText(Localization.TAKE_LOOT_TO_SPAWN);
        } else {
            popupText.color = Localization.COLOR_INFORMATION;
            popupText.SetText(Localization.HOLD_TO_LOOT);
        }
    }
    
    // Delete object from lootables
    void DeleteLootableObj(GameObject a_obj){
        // Remove object from lootable object list
        lootableObjects.Remove(a_obj);
        // Add -1 to lootable objects counter & interactables
        lootEntered--;
        interactables--;
        // If list is empty
        if(lootEntered == 0){
            // Reset text in pop-up text
            popupText.SetText("");
            // Set current lootable object to null
            currentLootObj = null;
        // Otherwise
        } else {
            // Set current lootable object to last visited
            currentLootObj = lootableObjects[lootableObjects.Count-1];
            // Update information about loot in LootInfo struct
            gameController.UpdateLootInfo(currentLootObj.GetComponent<lootInformation>());
            // Update popup text
            UpdatePopUp();
        }
    }
    
    // #############################################
    // ##### EVENTS
    
    // On collision enter
    void OnCollisionEnter(Collision collision){
        // If player collides with enemy, restart game
        if(collision.collider.tag.Equals("Enemy") && gameController.whichPPSettingisSet == gameController.PPSettings.Default){
            gameController.SetMissionFailed();
            characterController.enabled = false;
        }
    }
    
    // On trigger enter
    void OnTriggerEnter(Collider collider){
        // If player enters loot trigger
        if(collider.tag.Equals("Loot")){
            // Save last "lootable" object to variable
            currentLootObj = collider.gameObject;
            // If given object is not in our lootable object list, add it
            if(!lootableObjects.Contains(currentLootObj)){
                lootableObjects.Add(currentLootObj);
            }
            // If player is not holding any loot update information
            if(!isHoldingLoot){
                // Update information about loot in LootInfo struct
                gameController.UpdateLootInfo(currentLootObj.GetComponent<lootInformation>());
            }
            // Add +1 to lootable objects counter & interactables
            lootEntered++;
            interactables++;
            // If there is at least 1 lootable object, change text of pop-up text
            if(lootEntered > 0){
                // Update popup text
                UpdatePopUp();
            }
        //-----------------------------------------
        // Else if player enters loot zone trigger
        } else if(collider.tag.Equals("LootZone") && isHoldingLoot){
            // Change holding loot bool to false
            isHoldingLoot = false;
            // Add score
            gameController.AddToWorth(gameController.lootInfo.worth);
            // Reset information about loot
            gameController.ResetLootInfo();
            // Update UI
            gameController.UpdateEquipped();
        //-----------------------------------------
        // Else if player enters escape zone trigger
        } else if(collider.tag.Equals("EscapeZone")){
            // Apply mission successful state
            gameController.SetMissionSuccess();
            // Disable CharacterController
            characterController.enabled = false;
        }
    }
    
    // On trigger exit
    void OnTriggerExit(Collider collider){
        // If player exit loot trigger
        if(collider.tag.Equals("Loot")){
            // Delete that object from lootables
            DeleteLootableObj(collider.gameObject);
        }
    }
    
    // On start
    void Start(){        
        // Set current loot object to null (not in area of any loot object)
        currentLootObj = null;
        // Get components and save their references
        characterController = GetComponent<CharacterController>();
        // Set current camera zoom level
        cameraZoomLevel = Camera.main.orthographicSize;
    }

    // Every frame
    void Update(){
        // Init move vector
        Vector3 move = Vector3.zero;
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
        // Normalize move vector
        move = move.normalized*(moveSpeed-moveWeightPenality*gameController.lootInfo.weight)*Time.deltaTime;
        // Apply small gravity
        move.y = -5f;
        // If player is on ground, apply only friction of gravity
        if(characterController.isGrounded){
            move.y = -0.01f;
        }
        // Move player around according to move vector
        if(gameController.whichPPSettingisSet == gameController.PPSettings.Default){
            characterController.Move(move);
        }
        //----------------------------------
        // If there is any loot to get and player is holding "e" (loot button)
        if(lootEntered > 0 && Input.GetKey("e") && !isHoldingLoot){
            // Set that player is interative with something
            isInteractive = true;
            // Add to "holding to loot" timer
            timerToLoot += Time.deltaTime;
            // If our timer passed loot time
            if(timerToLoot >= gameController.lootInfo.time){
                // Reset that timer
                timerToLoot = 0f;
                // Set current lootable object as not active
                currentLootObj.SetActive(false);
                // Change bool to true, because now we hold equipped loot
                isHoldingLoot = true;
                // Update UI
                gameController.UpdateEquipped();
                // Delete object from lootables
                DeleteLootableObj(currentLootObj);
            }
        // Otherwise reset timer and intaractive bool
        } else {
            isInteractive = false;
            timerToLoot = 0f;
        }
        // Set action timer fill amount
        if(isInteractive){
            popupObject.SetActive(true);
            actionTimerImage.fillAmount = timerToLoot/gameController.lootInfo.time;
        } else {
            popupObject.SetActive(false);
        }
        //----------------------------------
        // Change zoom of camera with mouse scroll
        if(Input.mouseScrollDelta.y != 0f){
            // Set camera zoom
            float t_cameraZoom = cameraZoomLevel-Input.mouseScrollDelta.y*cameraScrollSpeed;
            cameraZoomLevel = Mathf.Clamp(t_cameraZoom, cameraZoomBorders.x, cameraZoomBorders.y);
            Camera.main.orthographicSize = cameraZoomLevel;
        }
        //----------------------------------
        // If current lootable object exists and we are in it's trigger
        if(!(currentLootObj is null)){
            // Set position of pop-up text above it
            Vector3 t_vec = Camera.main.WorldToScreenPoint(currentLootObj.transform.position);
            popupTransform.position = t_vec+offsetText*5/Camera.main.orthographicSize;
        }
    }
}
