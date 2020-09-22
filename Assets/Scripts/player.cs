using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class player : MonoBehaviour{
    private CharacterController controller;
    private Vector3 move;
    public float moveSpeed = 5f;
    public float cameraScrollSpeed = 1f;
    void Start(){
        controller = GetComponent<CharacterController>();
    }

    void Update(){
        move = Vector3.zero;
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
        if(Input.GetKey("r")){
            SceneManager.LoadScene("SampleScene");
        }
        move = move.normalized;
        move.y -= 5f;
        if(controller.isGrounded){
            move.y = 0.01f;
        }
        if(Input.mouseScrollDelta.y != 0f){
            float t_scrollZoom = Camera.main.orthographicSize-Input.mouseScrollDelta.y*cameraScrollSpeed;
            Camera.main.orthographicSize = Mathf.Clamp(t_scrollZoom, 4f, 10f);
        }
        controller.Move(move*Time.deltaTime*moveSpeed);
    }
}
