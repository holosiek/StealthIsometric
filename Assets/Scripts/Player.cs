using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class Player : MonoBehaviour{
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
    // >>> Interacting
    //------------------------
    
    // List containing interactable objects in range
    private List<GameObject> interactableObjects = new List<GameObject>();
    // Current interactable object or null if not in zone of any
    private GameObject currInteractableObj;
    // Current interactable object type
    private InteractableObject.InteractableType currInteractableObjType;
    // Total time passed while holding interact button if in zone of interactable object
    private float interactiveTimer = 0f;
    // Is player interactive with something
    private bool isInteractiveActive = false;
    // How many interactables are player whichin
    private short interactableEntered = 0;
    // How long need to hold "interact" till interact with another object
    private float interactableToInteract = 0;
    // Total lootable objects in range - counter
    private short lootEntered = 0;
    // Are we holding any loot?
    private bool isHoldingLoot = false;
    
    //------------------------
    // >>> References
    //------------------------
    
    [Header("References")]
    [Tooltip("Reference to main game controller")]
    public GameController gameControl;
    
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
    
    // Offset of text above interactable objects
    private Vector3 offsetText = new Vector3(0, 150, 0);
    // Timer counting next footstep sound moment
    private float footstepsTimer = 0f;
    // Time till next footstep sound
    private float footstepsTillNextSound;
    // Minimum time till next sound
    private float footstepsMinTime = 0.2f;
    // Time divided by speed of player to calculate next sound time
    private float footstepsDividerTime = 2f;
    
    // #############################################
    // ##### METHODS
    
    // Update next footstep sound time
    void UpdateTimeTillFootstep(){
        footstepsTillNextSound = Mathf.Max(footstepsMinTime, footstepsDividerTime/(moveSpeed-moveWeightPenality*gameControl.lootInfo.weight));
    }
    
    // Change popup text depending on situation
    void UpdatePopUp(){
        switch(currInteractableObjType){
            // If interactable type is loot
            case InteractableObject.InteractableType.Loot: {
                // If player is holding loot
                if(isHoldingLoot){
                    popupText.color = Localization.COLOR_DISABLED;
                    popupText.SetText(Localization.Translate("POPUP_TAKE_LOOT_TO_SPAWN"));
                // Else if player is not holding loot
                } else {
                    popupText.color = Localization.COLOR_INFORMATION;
                    popupText.SetText(Localization.Translate("POPUP_HOLD_TO_LOOT"));
                }
                break;
            }
            // If interactable type is switch
            case InteractableObject.InteractableType.Switch: {
                popupText.color = Localization.COLOR_INFORMATION;
                popupText.SetText(Localization.Translate("POPUP_HOLD_TO_SWITCH"));
                break;
            }
        }        
    }
    
    // Delete object from interactables
    void DeleteInteractableObj(GameObject a_obj){
        // Remove object from interactable object list
        interactableObjects.Remove(a_obj);
        // If interactable is loot type
        if(currInteractableObjType == InteractableObject.InteractableType.Loot){
            // Add -1 to lootable objects counter
            lootEntered--;
        }
        // Add -1 to interactable objects counter
        interactableEntered--;
        // If there's no interactables left
        if(interactableEntered == 0){
            // Reset text in pop-up text
            popupText.SetText("");
            // Set current interactable object to null
            currInteractableObj = null;
        // Otherwise
        } else {
            // Set current interactable object to last visited
            currInteractableObj = interactableObjects[interactableObjects.Count-1];
            currInteractableObjType = currInteractableObj.GetComponent<InteractableObject>().objType;
            interactableToInteract = currInteractableObj.GetComponent<InteractableObject>().objInteractTime;
            // If interactable is loot type
            if(currInteractableObjType == InteractableObject.InteractableType.Loot){             
                // Update information about loot in LootInfo struct
                gameControl.UpdateLootInfo(currInteractableObj.GetComponent<LootInformation>());
            }
            // Update popup text
            UpdatePopUp();
        }
    }
    
    // #############################################
    // ##### EVENTS
    
    // On collision enter
    void OnCollisionEnter(Collision collision){
        // If player collides with enemy and we are in gameplay state
        if(collision.collider.tag.Equals("Enemy") && gameControl.whichPPSettingisSet == GameController.PPSettings.Default){
            // Turn on failed results screen
            gameControl.SetMissionFailed();
            // Play lose sound
            FindObjectOfType<AudioManager>().Play("Lose");
            // Set our charater controller as disabled
            characterController.enabled = false;
        }
    }
    
    // On trigger enter
    void OnTriggerEnter(Collider collider){
        // If player enters loot trigger or interactable
        if(collider.tag.Equals("Interactable")){
            // Save last interactable object to variable
            currInteractableObj = collider.gameObject;
            currInteractableObjType = collider.GetComponent<InteractableObject>().objType;
            interactableToInteract = collider.GetComponent<InteractableObject>().objInteractTime;
            // If given object is not in our interactable object list, add it
            if(!interactableObjects.Contains(currInteractableObj)){
                interactableObjects.Add(currInteractableObj);
            }
            // Reset interactive timer
            interactiveTimer = 0f;
            // If collider is loot
            if(currInteractableObjType == InteractableObject.InteractableType.Loot){
                // If player is not holding any loot - update information
                if(!isHoldingLoot){
                    gameControl.UpdateLootInfo(currInteractableObj.GetComponent<LootInformation>());
                }
                // Add +1 to lootable objects counter
                lootEntered++;
            }
            // Add +1 to interactable objects counter
            interactableEntered++;
            // Update popup text
            UpdatePopUp();
        //-----------------------------------------
        // Else if player enters loot zone trigger
        } else if(collider.tag.Equals("LootZone") && isHoldingLoot){
            // Change holding loot bool to false
            isHoldingLoot = false;
            // Play "Throwing Loot" sound
            FindObjectOfType<AudioManager>().Play("Throwing Loot");
            // Add score
            gameControl.AddToWorth(gameControl.lootInfo.worth);
            // Reset information about loot
            gameControl.ResetLootInfo();
            // Update UI
            gameControl.UpdateEquipped();
            // Update next footstep sound time
            UpdateTimeTillFootstep();
        //-----------------------------------------
        // Else if player enters escape zone trigger
        } else if(collider.tag.Equals("EscapeZone")){
            // Apply mission successful state
            gameControl.SetMissionSuccess();
            // Disable CharacterController
            characterController.enabled = false;
            // Play win sound
            FindObjectOfType<AudioManager>().Play("Win");
        }
    }
    
    // On trigger exit
    void OnTriggerExit(Collider collider){
        // If player exit Interactable trigger
        if(collider.tag.Equals("Interactable")){
            // Delete that object from interactables
            DeleteInteractableObj(collider.gameObject);
        }
    }
    
    // On start
    void Start(){        
        // Set current interactive object to null (not in area of any interactive object)
        currInteractableObj = null;
        // Get components and save their references
        characterController = GetComponent<CharacterController>();
        // Set current camera zoom level
        cameraZoomLevel = Camera.main.orthographicSize;
        // Update next footstep sound time
        UpdateTimeTillFootstep();
    }
    
    // Every frame
    void Update(){
        // Init move vector
        Vector3 move = Vector3.zero;
        // Check for any WSAD button being held and add to move vector depending on button held
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
        // Normalize move vector and set speed depending on loot weight
        if(isHoldingLoot){
            move = move.normalized*Time.deltaTime*(moveSpeed-moveWeightPenality*gameControl.lootInfo.weight);
        } else {
            move = move.normalized*Time.deltaTime*moveSpeed;
        }
        // Apply small gravity
        move.y = -5f;
        // If player is on ground, apply only friction of gravity
        if(characterController.isGrounded){
            move.y = -0.01f;
        }
        // If in gameplay state, move player by move vector
        if(gameControl.whichPPSettingisSet == GameController.PPSettings.Default){
            CollisionFlags flags = characterController.Move(move);
            // If our player is moving around and not hitting walls
            if((move.x != 0f || move.z != 0f) && (flags&CollisionFlags.Sides) == 0){
                // Add to timer till next footstep sound
                footstepsTimer += Time.deltaTime;
                // If it's time to play sound
                if(footstepsTimer >= footstepsTillNextSound){
                    // Play footstep sound
                    FindObjectOfType<AudioManager>().Play("Step");
                    // Reset timer till next footstep sound 
                    footstepsTimer = 0f;
                }
            // If our player is standing
            } else {
                // Set timer till next footstep sound enough to play sound on next move
                footstepsTimer = footstepsTillNextSound;
            }
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
        // If player is holding E and can interact with something
        if(Input.GetKey("e") && interactableEntered > 0){
            // If current interactable object is loot type and player is not holding loot
            if(currInteractableObjType == InteractableObject.InteractableType.Loot && !isHoldingLoot){
                // If it's first time holding E for loot, play sound
                if(isInteractiveActive == false){
                    FindObjectOfType<AudioManager>().Play("Packing");
                }
                // Set that player is interacting with something
                isInteractiveActive = true;
                // Add to "interacting" timer
                interactiveTimer += Time.deltaTime;
                // If our timer passed loot time
                if(interactiveTimer >= gameControl.lootInfo.time){
                    // Reset that timer
                    interactiveTimer = 0f;
                    // Set current lootable object as not active
                    currInteractableObj.SetActive(false);
                    // Change bool to true, because now we hold equipped loot
                    isHoldingLoot = true;
                    // Update UI
                    gameControl.UpdateEquipped();
                    // Update next footstep sound time
                    UpdateTimeTillFootstep();
                    // Delete object from lootables
                    DeleteInteractableObj(currInteractableObj);
                }
            // Else if it's switch type
            } else if(currInteractableObjType == InteractableObject.InteractableType.Switch){
                // Set that player is interacting with something
                isInteractiveActive = true;
                // Add to "interacting" timer
                interactiveTimer += Time.deltaTime;
                // If our timer passed interact time
                if(interactiveTimer >= interactableToInteract){
                    // Reset that timer
                    interactiveTimer = 0f;
                    // Pull the lever
                    currInteractableObj.GetComponent<SwitchLever>().PullLever();
                }
            }
        } else {
            // If it's first time stopping interactive, stop all sounds created from it
            if(isInteractiveActive == true){
                FindObjectOfType<AudioManager>().Stop("Packing");
            }
            // Hide timer and reset interactive timer
            isInteractiveActive = false;
            interactiveTimer = 0f;
        }
        // Set action timer fill amount
        if(isInteractiveActive){
            // Turn on popup object
            popupObject.SetActive(true);
            // If current interactable object is loot type
            if(currInteractableObjType == InteractableObject.InteractableType.Loot){
                // Fill action timer circle depending on loot gathering time
                actionTimerImage.fillAmount = interactiveTimer/gameControl.lootInfo.time; 
            // Else if it's not loot type
            } else {
                // Fill action timer circle depending on interact time
                actionTimerImage.fillAmount = interactiveTimer/interactableToInteract; 
            }
        } else {
            // Turn off popup object
            popupObject.SetActive(false);
        }
        //----------------------------------
        // If current interactive object exists and we are in it's trigger
        if(!(currInteractableObj is null)){
            // Set position of pop-up text above it
            Vector3 t_vec = Camera.main.WorldToScreenPoint(currInteractableObj.transform.position);
            popupTransform.position = t_vec+offsetText*5/Camera.main.orthographicSize;
        }
    }
}
