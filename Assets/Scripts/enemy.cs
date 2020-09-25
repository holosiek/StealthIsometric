using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class enemy : MonoBehaviour{
    // #############################################
    // ##### VARIABLES
    
    //------------------------
    // >>> Enums
    //------------------------
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
    
    //------------------------
    // >>> Sight area
    //------------------------
    // Amount of rays to cast for "sight area"
    [Range(2, 250)]
    public int amountOfRays = 100;
    // Angle spread between middle point of "sight area" and given angle
    [Range(20f, 70f)]
    public float angleSpread = 20f;
    // Angle spread between middle point of "sight area" and given angle
    [Range(5, 30)]
    public int raycastDistance = 10;
    // Calculated degrees between rays
    private float degreeBetweenRays;
    // Mesh Filter which will hold "sight area"
    public MeshFilter enemyView;
    // Offset rays by this value
    private Vector3 raycastOffset = new Vector3(0f, -0.5f, 0f);
    
    //------------------------
    // >>> Sight area construction
    //------------------------
    // Vertixes of "sight area"
    private Vector3[] raysPoints;
    // Triangles describing mesh of "sight area"
    private int[] triangles;
    // Mesh of "sight area"
    private Mesh mesh;
    
    //------------------------
    // >>> Waypoints system
    //------------------------
    // GameObject containing waypoints
    public GameObject[] waypointList;
    // Variable holding Nav Mesh Agent
    private NavMeshAgent navAgent;
    // Point that is currently visited
    private int waypointNow = 0;
    
    //------------------------
    // >>> States
    //------------------------
    // State which enemy is right now
    private States stateRN = States.Moving;
    // Last detected player position
    private Vector3 lastPlayerPosition;
    // Step in rotation
    private float rotationStep;
    // Rotation direction
    private short rotationDir = 0;
    Quaternion rightRotation;
    Quaternion leftRotation;
    Quaternion orginalRotation;
    
    //------------------------
    // >>> Referance
    //------------------------
    public gameController gameController;
    
    // #############################################
    // ##### METHODS
    
    // Update degrees between rays
    void UpdateDegreeBetweenRays(){
        degreeBetweenRays = angleSpread/(float)(amountOfRays-1);
    }
    
    // Rotate faster when player is in sight
    void ChaseRotation(){
        Vector3 lookrotation = navAgent.steeringTarget-transform.position;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookrotation), 15.0f*Time.deltaTime);
    }
    
    // #############################################
    // ##### STATES
    
    // State: Idle - Turn around until end of timer
    void CheckIfEndOfIdle(){
        stateRN = States.Moving;
        waypointNow++;
        if(waypointNow >= waypointList.Length){
            waypointNow = 0;
        }
        navAgent.SetDestination(waypointList[waypointNow].transform.position);
    }
    
    // State: Moving - Check if it's end of path
    void CheckIfEndOfPath(){
        if(!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance){
            if(!navAgent.hasPath || navAgent.velocity.sqrMagnitude == 0f){
                stateRN = States.Idle;
                CheckIfEndOfIdle();
            }
        }
    }
    
    // State: Chasing - Move towards player
    void GoTowardsPlayer(){
        if(gameController.whichPPSettingisSet == gameController.PPSettings.Default){
            navAgent.SetDestination(lastPlayerPosition);
            if(!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance){
                if(!navAgent.hasPath || navAgent.velocity.sqrMagnitude == 0f){
                    rotationStep = 0f;
                    rotationDir = -1;
                    stateRN = States.Lost;
                    LostPlayer();
                } else {
                ChaseRotation();
                }
            } else {
                ChaseRotation();
            }
        } else {
            stateRN = States.Moving;
            navAgent.SetDestination(waypointList[waypointNow].transform.position);
        }
    }
    
    // State: Lost - Try to find player around;
    void LostPlayer(){
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
                navAgent.SetDestination(waypointList[waypointNow].transform.position);
                rotationStep = 0f;
                rotationDir = -1;
            }
        }
    }
    
    // #############################################
    // ##### EVENTS
    void Start(){   
        // Initialize all variables, settings etc.
        OnValidate();
             
        // Create mesh and append it to Mesh Filter
        mesh = new Mesh();
        enemyView.mesh = mesh;
        
        // Get Nav Mesh Agent compontent and set it's first destination
        navAgent = GetComponent<NavMeshAgent>();
        navAgent.SetDestination(waypointList[waypointNow].transform.position);
    }
    
    void OnValidate(){
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
    
    void FixedUpdate(){
        // Calculate raycast origin
        Vector3 raycastPos = transform.position + raycastOffset;
        // Initialize RaycastHit reference and if player is found;
        RaycastHit hit;
        bool isPlayerHit = false;
        
        // Loop through each ray
        for(int i=0; i<amountOfRays; i++){
            // Calculate Ray direction
            Vector3 rayDir = Quaternion.Euler(0, -angleSpread+i*2*degreeBetweenRays, 0)*transform.forward;
            // If ray hits any object (Player or wall)
            if(Physics.Raycast(raycastPos, rayDir, out hit, raycastDistance, (int)LayerIndexes.Objects)){
                // Save collider tag to string
                string tagCollider = hit.collider.tag;
                // If collider is player, raycast again, and if hit wall, then:
                if(tagCollider.Equals("Player")){
                    // Save last player known location and change state to chasing
                    lastPlayerPosition = hit.collider.transform.position;
                    isPlayerHit = true;
                    if(Physics.Raycast(raycastPos, rayDir, out hit, raycastDistance, (int)LayerIndexes.Walls)){
                        // [DEBUG] Draw "hit player" line
                        Debug.DrawRay(raycastPos, rayDir*hit.distance, Color.green);
                        // Add raycast hit point to array
                        raysPoints[i+1] = transform.InverseTransformPoint(hit.point)+raycastOffset;
                    } else {
                        // [DEBUG] Draw "hit player" line
                        Debug.DrawRay(raycastPos, rayDir*raycastDistance, Color.green);
                        // Add raycast distance to array
                        Vector3 vertexDir = Quaternion.Euler(0, -angleSpread+i*2*degreeBetweenRays, 0)*Vector3.forward;
                        raysPoints[i+1] = vertexDir*raycastDistance+raycastOffset*2;
                    }
                } else {
                    // [DEBUG] Draw ray line
                    Debug.DrawRay(raycastPos, rayDir*hit.distance, Color.red);
                    // Add raycast hit point to array
                    raysPoints[i+1] = transform.InverseTransformPoint(hit.point)+raycastOffset;        
                }
            } else {
                // [DEBUG] Draw ray line
                Debug.DrawRay(raycastPos, rayDir*raycastDistance, Color.red);
                // Add raycast distance to array
                Vector3 vertexDir = Quaternion.Euler(0, -angleSpread+i*2*degreeBetweenRays, 0)*Vector3.forward;
                raysPoints[i+1] = vertexDir*raycastDistance+raycastOffset*2;
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
    }
}
