using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class player : MonoBehaviour{
    // #############################################
    // ##### Structs
    
    // LootInfo stores information about loot
    private struct LootInfo {
        public string name;
        public float time;
        public float worth;
        public int weight;
    }
    
    // #############################################
    // ##### VARIABLES
    
    //------------------------
    // >>> Player stats
    //------------------------
    [Header("Player stats")]
    [Tooltip("Base move speed of player")]
    public float moveSpeed = 5f;
    
    //------------------------
    // >>> Camera
    //------------------------
    [Header("Camera")]
    [Tooltip("Set minimum (X) and maximum (Y) camera zoom levels")]
    public Vector2 cameraZoomBorders = new Vector2(4.0f, 10.0f);
    [Tooltip("Camera scroll step every mouse wheel rotation")]
    public float cameraScrollSpeed = 1f;
    // Camera zoom level
    private float cameraZoomLevel;
    
    //------------------------
    // >>> Loot
    //------------------------
    // Loot information struct
    private LootInfo lootInfo;
    // Total time passed while holding loot button and getting loot
    private float timerToLoot = 0f;
    
    private CharacterController chararacterController;
    public GameObject canvasObj;
    
    // Current loot object or null if not in zone of any
    private GameObject currentLootObj;
    public RectTransform informationTextTransform;
    public TMPro.TextMeshProUGUI informationTextObj;
    public TMPro.TextMeshProUGUI infoToHold;
    public gameController gameController;
    
    private Vector3 offsetText = new Vector3(0, 150, 0);
    private short lootEntered = 0;
    private List<GameObject> lootableObjects = new List<GameObject>();
    
    // #############################################
    // ##### METHODS
    
    // Update information about loot in LootInfo struct
    void UpdateLootInfo(lootInformation a_ref){
        lootInfo.name = a_ref.lootName;
        lootInfo.time = a_ref.timeToLoot;
        lootInfo.worth = a_ref.lootScore;
        lootInfo.weight = a_ref.lootWeight;
    }
    
    // Reset information about loot in LootInfo struct
    void ResetLootInfo(){
        lootInfo.name = "";
        lootInfo.time = 0f;
        lootInfo.worth = 0f;
        lootInfo.weight = 0;
    }
    
    // Delete object from lootables
    void DeleteLootableObj(GameObject a_obj){
        // Remove object from lootable object list
        lootableObjects.Remove(a_obj);
        // Add -1 to lootable objects counter
        lootEntered--;
        // If list is empty
        if(lootEntered == 0){
            // Reset text in pop-up text
            informationTextObj.SetText("");
            // Set current lootable object to null
            currentLootObj = null;
            // Reset information about loot
            ResetLootInfo();
        // Otherwise
        } else {
            // Set current lootable object to last visited
            currentLootObj = lootableObjects[lootableObjects.Count-1];
            // Update information about loot in LootInfo struct
            UpdateLootInfo(currentLootObj.GetComponent<lootInformation>());
        }
    }
    
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
            currentLootObj = collider.gameObject;
            // If given object is not in our lootable object list, add it
            if(!lootableObjects.Contains(currentLootObj)){
                lootableObjects.Add(currentLootObj);
            }
            // Update information about loot in LootInfo struct
            UpdateLootInfo(currentLootObj.GetComponent<lootInformation>());
            // Add +1 to lootable objects counter
            lootEntered++;
            // If there is at least 1 lootable object, change text of pop-up text
            if(lootEntered > 0){
                informationTextObj.SetText(Localization.HOLD_TO_LOOT);
            }
        //-----------------------------------------
        // Else if player enters loot zone trigger
        } else if(collider.tag.Equals("Loot Zone")){
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
        // Init loot information struct
        lootInfo = new LootInfo();
        ResetLootInfo();
        
        // Set current loot object to null (not in area of any loot object)
        currentLootObj = null;
        // Get components and save their references
        chararacterController = GetComponent<CharacterController>();
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
        move = move.normalized;
        // Apply small gravity
        move.y = -5f;
        // If player is on ground, apply only friction of gravity
        if(chararacterController.isGrounded){
            move.y = -0.01f;
        }
        // Move player around according to move vector
        chararacterController.Move(move*Time.deltaTime*moveSpeed);
        //----------------------------------
        // If there is any loot to get and player is holding "e" (loot button)
        if(lootEntered > 0 && Input.GetKey("e")){
            // Add to "holding to loot" timer
            timerToLoot += Time.deltaTime;
            // If our timer passed loot time
            if(timerToLoot >= lootInfo.time){
                // Reset that timer
                timerToLoot = 0f;
                // Add score
                gameController.AddToWorth(lootInfo.worth);
                // Set current lootable object as not active
                currentLootObj.SetActive(false);
                // Delete object from lootables
                DeleteLootableObj(currentLootObj);
            }
        // Otherwise reset timer
        } else {
            timerToLoot = 0f;
        }
        // [DEBUG] Update timer information
        infoToHold.SetText(timerToLoot.ToString() + "/" + lootInfo.time.ToString());
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
            informationTextTransform.position = t_vec+offsetText*5/Camera.main.orthographicSize;
        }
    }
}
