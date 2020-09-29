using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour{
    // #############################################
    // ##### ENUMS
    
    // Layer enum
    enum LayerIndexes {
        Walls = 1<<8,
        Player = 1<<9,
        Objects = (1<<8)+(1<<9)
    }
    
    // State enum
    enum States {
        Idle,       // patrolling or idling
        Moving,     // Moving to another point
        Chasing,    // Chasing player
        Lost        // Lost vision on player, search for him
    }
    
    // Enemy type enum
    public enum Movement {
        Guard,      // Stay and look in front
        TurnAround, // Stay, but look around
        Move        // Move towards waypoints
    }
    
    // #############################################
    // ##### VARIABLES
    
    //------------------------
    // >>> Sight area
    //------------------------
    
    [Range(2, 250)]
    [Tooltip("Amount of rays to cast for \"sight area\"")]
    public int amountOfRays = 250;
    
    [Range(10f, 70f)]
    [Tooltip("Angle spread between middle point of \"sight area\" and given angle")]
    public float angleSpread = 20f;
    
    [Range(5, 30)]
    [Tooltip("Length of \"sight area\"")]
    public int raycastDistance = 10;
    
    [Tooltip("Mesh Filter which will hold \"sight area\"")]
    public MeshFilter enemyView;
    
    [Range(10f, 90f)]
    [Tooltip("When player sight is lost, which angle should enemy rotate to search for him")]
    public float searchAngle = 40f;
    
    // Calculated degrees between rays
    private float degreeBetweenRays;
    // Offset rays by this value
    private Vector3 raycastOffset = new Vector3(0f, -0.5f, 0f);
    
    //------------------------
    // >>> Sight area construction
    //------------------------
    
    // Vertices of "sight area"
    private Vector3[] raysPoints;
    // Triangles describing mesh of "sight area"
    private int[] triangles;
    // Mesh of "sight area"
    private Mesh mesh;
    
    //------------------------
    // >>> Referances
    //------------------------
    
    [Tooltip("Reference to GameController")]
    public GameController gameControl;
    
    [Tooltip("Reference to sight area renderer")]
    public Renderer sightAreaRenderer;
    
    [Tooltip("References to materials used for sight area")]
    public Material[] sightAreaMaterials;
    
    //------------------------
    // >>> States
    //------------------------
    
    [Space(20)]
    [Tooltip("Type of enemy")]
    public Movement enemyType = Movement.Move;
    
    // State which enemy is right now
    private States stateRN = States.Moving;
    // Last detected player position
    private Vector3 lastPlayerPosition;
    // Step in rotation (used in Lost state)
    private float rotationStep;
    // Rotation direction (used in Lost state)
    private short rotationDir = 0;
    // Rotation variables (used in Lost state)
    private Quaternion rightRotation, leftRotation, orginalRotation;
    
    // Starting position and rotation used by Guard/TurnAround enemy type
    private Vector3 startPos;
    private Quaternion startRotation;
    
    [Space(20)]
    // patrolling state used by TurnAround enemy type
    private short patrollingState = 0;
    // patrolling timer used by TurnAround enemy type
    private float patrollingTimer = 0;
    
    [Space(5)]
    [Header("TurnAround enemy type:")]
    [Tooltip("Max patrolling angle of enemy on idle partoling location used by TurnAround enemy type")]
    public float patrollingAngle = 50f;
    
    [Tooltip("patrolling time between turning around in seconds used by TurnAround enemy type")]
    public float patrollingTimeBetweenNext = 2f;
    
    [Tooltip("Turning time while patrolling in seconds used by TurnAround enemy type")]
    public float patrollingTimeForRotation = 2f;
    
    //------------------------
    // >>> Waypoints system
    //------------------------
    
    [Space(5)]
    [Header("Move enemy type:")]
    [Tooltip("GameObject containing waypoints")]
    public GameObject[] waypointList;
    
    // Variable referencing to NavMeshAgent
    private NavMeshAgent navAgent;
    // Point that is currently visited
    private int waypointNow = 0;
    
    // #############################################
    // ##### METHODS
    
    // Update degrees between rays
    private void UpdateDegreeBetweenRays(){
        degreeBetweenRays = angleSpread/(float)(amountOfRays-1);
    }
    
    // Rotate faster when player is in sight
    private void ChaseRotation(){
        Vector3 lookrotation = navAgent.steeringTarget-transform.position;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookrotation), 15.0f*Time.deltaTime);
    }
    
    // Update sight area mesh
    private void UpdateSightAreaMesh(){
        // Initialize variables
        raysPoints = new Vector3[amountOfRays+1];
        triangles = new int[amountOfRays*3-3];
        
        // Set first vertex of "sight area"
        raysPoints[0] = raycastOffset*2;
        
        // Set triangles of "sight area"
        for(int i=0; i<amountOfRays-1; i++){
            triangles[i*3] = 0;
            triangles[i*3+1] = i+1;
            triangles[i*3+2] = i+2;
        }
        
        // Update degrees between rays
        UpdateDegreeBetweenRays();
    }
    
    // Check if end of road
    private bool IsEndOfRoad(){
        if(!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance
          && (!navAgent.hasPath || navAgent.velocity.sqrMagnitude == 0f)){
            return true;
        }
        return false;
    }
    
    // #############################################
    // ##### STATES
    
    // State: Idle
    private void IdleState(){
        // If enemy type is TurnAround
        if(enemyType == Movement.TurnAround){
            // Switch between patrolling states
            switch(patrollingState){
                case 0: {
                    // Slerp rotation to the right
                    transform.rotation = Quaternion.Slerp(transform.rotation, startRotation*Quaternion.Euler(0f, patrollingAngle, 0f), patrollingTimer/patrollingTimeBetweenNext);
                    // Add to patrolling timer
                    patrollingTimer += Time.deltaTime;
                    // If it's time to change state
                    if(patrollingTimer >= patrollingTimeBetweenNext){
                        patrollingTimer = 0f;
                        patrollingState++;
                    }
                    break;
                }
                case 1: {
                    // Add to patrolling timer
                    patrollingTimer += Time.deltaTime;
                    // If it's time to change state
                    if(patrollingTimer >= patrollingTimeForRotation){
                        patrollingTimer = 0f;
                        patrollingState++;
                    }
                    break;
                }
                case 2: {
                    // Slerp rotation to the left
                    transform.rotation = Quaternion.Slerp(transform.rotation, startRotation*Quaternion.Euler(0f, -patrollingAngle, 0f), patrollingTimer/patrollingTimeBetweenNext);
                    // Add to patrolling timer
                    patrollingTimer += Time.deltaTime;
                    // If it's time to change state
                    if(patrollingTimer >= patrollingTimeBetweenNext){
                        patrollingTimer = 0f;
                        patrollingState++;
                    }
                    break;
                }
                case 3: {
                    // Add to patrolling timer
                    patrollingTimer += Time.deltaTime;
                    // If it's time to change state
                    if(patrollingTimer >= patrollingTimeForRotation){
                        patrollingTimer = 0f;
                        patrollingState = 0;
                    }
                    break;
                }
            }
        // Else if enemy type is Move
        } else if(enemyType == Movement.Move){
            // Change state to moving
            stateRN = States.Moving;
            // Go to next waypoint
            waypointNow++;
            // If we get out of waypoints bound, set current waypoint to 0
            if(waypointNow >= waypointList.Length){
                waypointNow = 0;
            }
            // Set new destination for navagent
            navAgent.SetDestination(waypointList[waypointNow].transform.position);
        }
    }
    
    // State: Moving
    private void MovingState(){
        // If enemy is on destination
        if(IsEndOfRoad()){
            // If enemy type is Move
            if(enemyType == Movement.Move){
                // Set state to idle and call idle state method
                stateRN = States.Idle;
                IdleState();
            } else {
                // Add to patrolling timer
                patrollingTimer += Time.deltaTime;
                // Slerp rotation towards start position
                transform.rotation = Quaternion.Slerp(transform.rotation, startRotation, patrollingTimer);
                // If second passed
                if(patrollingTimer >= 1.0f){
                    // Reset patrolling parameters
                    patrollingTimer = 0f;
                    patrollingState = 0;
                    // Set state to idle and call idle state method
                    stateRN = States.Idle;
                    IdleState();
                }
            }
        }
    }
    
    // State: Chasing
    private void ChasingState(){
        // If there is "gameplay" time
        if(gameControl.whichPPSettingisSet == GameController.PPSettings.Default){
            // Set new destination to player position
            navAgent.SetDestination(lastPlayerPosition);
            // If it meets destination and there's no player
            if(IsEndOfRoad()){
                // Reset rotation variables
                rotationStep = 0f;
                rotationDir = -1;
                // Set material to "lost player"
                sightAreaRenderer.material = sightAreaMaterials[2];
                // Set state to lost player and call it's method
                stateRN = States.Lost;
                LostState();
            // Else if there's still player in sight and it's chased, rotate faster into it's location
            } else {
                ChaseRotation();
            }
        // else if there is any result screen
        } else {
            // Go back to moving
            stateRN = States.Moving;
            // If enemy type is Move
            if(enemyType == Movement.Move){
                // Go to current waypoint
                navAgent.SetDestination(waypointList[waypointNow].transform.position);
            // Else if enemy type is not Move
            } else {
                // Set destination to starting position and reset patrolling timer
                navAgent.SetDestination(startPos);
                patrollingTimer = 0f;
            }
        }
    }
    
    // State: Lost
    private void LostState(){
        // Check between states of rotationDir
        switch(rotationDir){
            case -1: {
                // Set rotation variables based on current rotation
                rightRotation = transform.rotation*Quaternion.Euler(0f, searchAngle, 0f);
                leftRotation = transform.rotation*Quaternion.Euler(0f, -searchAngle, 0f);
                orginalRotation = transform.rotation;
                // Go to next state
                rotationDir++;
                break;
            }
            case 0: {
                // Wait before searching
                rotationStep += Time.deltaTime*3;
                // After time passes
                if(rotationStep >= 1f){
                    // Reset rotationStep timer and go to next state
                    rotationStep = 0f;
                    rotationDir++;
                }
                break;
            }
            case 1: {
                // Slerp rotation towards right direction
                transform.rotation = Quaternion.Slerp(transform.rotation, rightRotation, rotationStep);
                // Add to rotationStep timer
                rotationStep += Time.deltaTime;
                // After time passes
                if(rotationStep >= 1f){
                    // Reset rotationStep timer and go to next state
                    rotationStep = 0f;
                    rotationDir++;
                }
                break;
            }
            case 2: {
                // Slerp rotation towards left direction
                transform.rotation = Quaternion.Slerp(transform.rotation, leftRotation, rotationStep);
                // Add to rotationStep timer
                rotationStep += Time.deltaTime;
                // After time passes
                if(rotationStep >= 1f){
                    // Reset rotationStep timer and go to next state
                    rotationStep = 0f;
                    rotationDir++;
                }
                break;
            }
            case 3: {
                // Slerp rotation towards orginal direction (before rotating)
                transform.rotation = Quaternion.Slerp(transform.rotation, orginalRotation, rotationStep);
                // Add to rotationStep timer
                rotationStep += Time.deltaTime;
                // After time passes
                if(rotationStep >= 1f){
                    // Change state to move
                    stateRN = States.Moving;
                    // Set material to "Player unseen"
                    sightAreaRenderer.material = sightAreaMaterials[0];
                    // If enemy type is Move
                    if(enemyType == Movement.Move){
                        // Set current waypoint as new destination
                        navAgent.SetDestination(waypointList[waypointNow].transform.position);
                    // Else if enemy type is not Move
                    } else {
                        // Set current destination as starting positiong and reset patrolling timer
                        navAgent.SetDestination(startPos);
                        patrollingTimer = 0f;
                    }
                    // Reset rotation state and timer
                    rotationStep = 0f;
                    rotationDir = -1;
                }
                break;
            }
        }
    }
    
    // #############################################
    // ##### EVENTS
    
    void Start(){   
        // Initialize all variables, settings etc.
        UpdateSightAreaMesh();
             
        // Create mesh and apply it to Mesh Filter
        mesh = new Mesh();
        enemyView.mesh = mesh;
        
        // Initialize starting position and rotation
        startPos = transform.position;
        startRotation = transform.rotation;
        
        // Get Nav Mesh Agent compontent and set it's first destination if it's Move type
        navAgent = GetComponent<NavMeshAgent>();
        if(enemyType == Movement.Move){
            navAgent.SetDestination(waypointList[waypointNow].transform.position);
        }
    }
    
    void OnValidate(){
        // Update sight area
        UpdateSightAreaMesh();
    }
    
    void FixedUpdate(){
        // Calculate raycast origin
        Vector3 raycastPos = transform.position + raycastOffset;
        // Initialize RaycastHit reference and if player is hit by ray bool
        RaycastHit hit;
        bool isPlayerHit = false;
        
        // Loop through each ray
        for(int i=0; i<amountOfRays; i++){
            // Calculate quaternion for ray
            Quaternion t_quaternion = Quaternion.Euler(0f, -angleSpread+i*2f*degreeBetweenRays, 0);
        
            // Calculate Ray direction
            Vector3 rayDir = t_quaternion*transform.forward;
            // If ray hits any object (Player or wall)
            if(Physics.Raycast(raycastPos, rayDir, out hit, raycastDistance, (int)LayerIndexes.Objects)){
                // Save collider tag to string
                string tagCollider = hit.collider.tag;
                // If collider is player, raycast again, and if hit wall, then:
                if(tagCollider.Equals("Player")){
                    // Save last player known location and change state to chasing
                    lastPlayerPosition = hit.collider.transform.position;
                    // Player is hit
                    isPlayerHit = true;
                    // Raycast again, this time against walls
                    if(Physics.Raycast(raycastPos, rayDir, out hit, raycastDistance, (int)LayerIndexes.Walls)){
                        // Add raycast hit point to array
                        raysPoints[i+1] = transform.InverseTransformPoint(hit.point)+raycastOffset;
                        // Debug.DrawRay(raycastPos, rayDir*hit.distance, Color.green);
                    } else {
                        // Add raycast distance to array
                        Vector3 vertexDir = t_quaternion*Vector3.forward;
                        raysPoints[i+1] = vertexDir*raycastDistance+raycastOffset*2;
                        // Debug.DrawRay(raycastPos, rayDir*raycastDistance, Color.green);
                    }
                } else {
                    // Add raycast hit point to array
                    raysPoints[i+1] = transform.InverseTransformPoint(hit.point)+raycastOffset;  
                    // Debug.DrawRay(raycastPos, rayDir*hit.distance, Color.red);      
                }
            } else {
                // Add raycast distance to array
                Vector3 vertexDir = t_quaternion*Vector3.forward;
                raysPoints[i+1] = vertexDir*raycastDistance+raycastOffset*2;
                // Debug.DrawRay(raycastPos, rayDir*raycastDistance, Color.red);
            }
        }
        // Clear mesh information
        mesh.Clear();
        // Append new vertices and triangles
        mesh.vertices = raysPoints;
        mesh.triangles = triangles;
        
        // If player was hit, change state to chasing
        if(stateRN != States.Chasing && isPlayerHit && gameControl.whichPPSettingisSet == GameController.PPSettings.Default){
            // Set material to "Chasing player"
            sightAreaRenderer.material = sightAreaMaterials[1];
            stateRN = States.Chasing;
            // Play detected sound
            FindObjectOfType<AudioManager>().Play("Hmm");
        }
        
        // Call state methods depending on enemy type
        if(enemyType == Movement.Move || enemyType == Movement.TurnAround){
            switch(stateRN){
                case States.Idle: {
                    IdleState();
                    break;
                }
                case States.Moving: {
                    MovingState();
                    break;
                }
                case States.Chasing: {
                    ChasingState();
                    break;
                }
                case States.Lost: {
                    LostState();
                    break;
                }
            }
        } else {
            switch(stateRN){
                case States.Moving: {
                    MovingState();
                    break;
                }
                case States.Chasing: {
                    ChasingState();
                    break;
                }
                case States.Lost: {
                    LostState();
                    break;
                }
            }
        }
    }
}
