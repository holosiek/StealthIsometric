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
        Idle,
        Moving,
        Chasing,
        Lost
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
    
    [Range(20f, 70f)]
    [Tooltip("Angle spread between middle point of \"sight area\" and given angle")]
    public float angleSpread = 20f;
    
    [Range(5, 30)]
    [Tooltip("Angle spread between middle point of \"sight area\" and given angle")]
    public int raycastDistance = 10;
    
    // Calculated degrees between rays
    private float degreeBetweenRays;
    
    [Tooltip("Mesh Filter which will hold \"sight area\"")]
    public MeshFilter enemyView;
    
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
    public gameController gameController;
    
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
    // Step in rotation
    private float rotationStep;
    // Rotation direction
    private short rotationDir = 0;
    // Rotation variables used in Lost state
    private Quaternion rightRotation;
    private Quaternion leftRotation;
    private Quaternion orginalRotation;
    
    // Starting position and rotation used by Guard/TurnAround enemy type
    private Vector3 startPos;
    private Quaternion startRotation;
    
    // Patroling state used by TurnAround enemy type
    private short patrolingState = 0;
    // Patroling timer used by TurnAround enemy type
    private float patrolingTimer = 0;
    // Max patroling angle of enemy on idle partoling location used by TurnAround enemy type
    public float patrolingAngle = 50f;
    // Patroling time between turning around in seconds used by TurnAround enemy type
    public float patrolingTimeBetweenNext = 2f;
    
    //------------------------
    // >>> Waypoints system
    //------------------------
    
    [Space(20)]
    [Tooltip("GameObject containing waypoints")]
    public GameObject[] waypointList;
    
    // Variable holding Nav Mesh Agent
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
    
    // State: Idle - Turn around until end of timer
    private void CheckIfEndOfIdle(){
        if(enemyType == Movement.TurnAround){
            switch(patrolingState){
                case 0: {
                    transform.rotation = Quaternion.Slerp(transform.rotation, startRotation*Quaternion.Euler(0f, patrolingAngle, 0f), patrolingTimer/patrolingTimeBetweenNext);
                    patrolingTimer += Time.deltaTime;
                    if(patrolingTimer >= patrolingTimeBetweenNext){
                        patrolingTimer = 0f;
                        patrolingState++;
                    }
                    break;
                }
                case 1: {
                    patrolingTimer += Time.deltaTime;
                    if(patrolingTimer >= patrolingTimeBetweenNext){
                        patrolingTimer = 0f;
                        patrolingState++;
                    }
                    break;
                }
                case 2: {
                    transform.rotation = Quaternion.Slerp(transform.rotation, startRotation*Quaternion.Euler(0f, -patrolingAngle, 0f), patrolingTimer/patrolingTimeBetweenNext);
                    patrolingTimer += Time.deltaTime;
                    if(patrolingTimer >= patrolingTimeBetweenNext){
                        patrolingTimer = 0f;
                        patrolingState++;
                    }
                    break;
                }
                case 3: {
                    patrolingTimer += Time.deltaTime;
                    if(patrolingTimer >= patrolingTimeBetweenNext){
                        patrolingTimer = 0f;
                        patrolingState = 0;
                    }
                    break;
                }
            }
        } else if(enemyType == Movement.Move){
            stateRN = States.Moving;
            waypointNow++;
            if(waypointNow >= waypointList.Length){
                waypointNow = 0;
            }
            navAgent.SetDestination(waypointList[waypointNow].transform.position);
        }
    }
    
    // State: Moving - Check if it's end of path
    private void CheckIfEndOfPath(){
        if(IsEndOfRoad()){
            if(enemyType == Movement.Move){
                stateRN = States.Idle;
                CheckIfEndOfIdle();
            } else {
                patrolingTimer += Time.deltaTime;
                transform.rotation = Quaternion.Slerp(transform.rotation, startRotation, patrolingTimer);
                if(patrolingTimer >= 1.0f){
                    patrolingTimer = 0f;
                    patrolingState = 0;
                    stateRN = States.Idle;
                    CheckIfEndOfIdle();
                }
            }
        }
    }
    
    // State: Chasing - Move towards player
    private void GoTowardsPlayer(){
        if(gameController.whichPPSettingisSet == gameController.PPSettings.Default){
            navAgent.SetDestination(lastPlayerPosition);
            if(IsEndOfRoad()){
                rotationStep = 0f;
                rotationDir = -1;
                stateRN = States.Lost;
                LostPlayer();
            } else {
                ChaseRotation();
            }
        } else {
            stateRN = States.Moving;
            navAgent.SetDestination(waypointList[waypointNow].transform.position);
        }
    }
    
    // State: Lost - Try to find player around;
    private void LostPlayer(){
        if(rotationDir == -1){
            rightRotation = transform.rotation*Quaternion.Euler(0f, 20f, 0f);
            leftRotation = transform.rotation*Quaternion.Euler(0f, -20f, 0f);
            orginalRotation = transform.rotation;
            rotationDir = 0;
        }
        if(rotationDir == 0){
            rotationStep += Time.deltaTime*3;
            if(rotationStep >= 1f){
                rotationStep = 0f;
                rotationDir++;
            }
        } else if(rotationDir == 1){
            transform.rotation = Quaternion.Slerp(orginalRotation, rightRotation, rotationStep);
            rotationStep += Time.deltaTime;
            if(rotationStep >= 1f){
                rotationStep = 0f;
                rotationDir++;
            }
        } else if(rotationDir == 2){
            transform.rotation = Quaternion.Slerp(rightRotation, leftRotation, rotationStep);
            rotationStep += Time.deltaTime/2;
            if(rotationStep >= 1f){
                rotationStep = 0f;
                rotationDir++;
            }
        } else {
            transform.rotation = Quaternion.Slerp(leftRotation, orginalRotation, rotationStep);
            rotationStep += Time.deltaTime;
            if(rotationStep >= 1f){
                stateRN = States.Moving;
                if(enemyType == Movement.Move){
                    navAgent.SetDestination(waypointList[waypointNow].transform.position);
                } else {
                    navAgent.SetDestination(startPos);
                    patrolingTimer = 0f;
                }
                rotationStep = 0f;
                rotationDir = -1;
            }
        }
    }
    
    // #############################################
    // ##### EVENTS
    
    void Start(){   
        // Initialize all variables, settings etc.
        UpdateSightAreaMesh();
             
        // Create mesh and append it to Mesh Filter
        mesh = new Mesh();
        enemyView.mesh = mesh;
        
        // Initialize starting position and rotation
        startPos = transform.position;
        startRotation = transform.rotation;
        
        // Get Nav Mesh Agent compontent and set it's first destination
        navAgent = GetComponent<NavMeshAgent>();
        navAgent.SetDestination(waypointList[waypointNow].transform.position);
    }
    
    void OnValidate(){
        // Update sight area
        UpdateSightAreaMesh();
    }
    
    void FixedUpdate(){
        // Calculate raycast origin
        Vector3 raycastPos = transform.position + raycastOffset;
        // Initialize RaycastHit reference and if player is found;
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
        
        // If player was hit
        if(isPlayerHit){
            stateRN = States.Chasing;
        }
        
        // Check state
        if(enemyType == Movement.Move || enemyType == Movement.TurnAround){
            switch(stateRN){
                case States.Idle: {
                    CheckIfEndOfIdle();
                    break;
                }
                case States.Moving: {
                    CheckIfEndOfPath();
                    break;
                }
                case States.Chasing: {
                    GoTowardsPlayer();
                    break;
                }
                case States.Lost: {
                    LostPlayer();
                    break;
                }
            }
        } else {
            switch(stateRN){
                case States.Moving: {
                    CheckIfEndOfPath();
                    break;
                }
                case States.Chasing: {
                    GoTowardsPlayer();
                    break;
                }
                case States.Lost: {
                    LostPlayer();
                    break;
                }
            }
        }
    }
}
