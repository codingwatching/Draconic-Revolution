using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    // Unity Reference
    public ChunkLoader cl;
    public CharacterController controller;
    public MainControllerManager controls;

    // Movement properties
    /*
	public float speed = 5f;
	public float gravity = -19.62f;
	public float jumpHeight = 5f;
    private int jumpticks = 6; // Amount of ticks the skinWidth will stick to new blocks
    */

    // Movement variables
    public float maxNaturalSpeed = 5f;
    public float drag = 5f;
    private float momentum = 0f;
    private Vector3 velocity = Vector3.zero;
    private Vector3 direction;
    private float movementAlignment = 0f;

    // Growth
    public float momentumGrowth = 2.4f;
    public float minimumMomentumToStop = 0.3f;


    void OnDestroy(){
        this.controller = null;
    }

    private Vector3 CalculateDirection(){return (this.transform.right * this.controls.movementX + this.transform.forward * this.controls.movementZ).normalized;}

    private float CalculateMovementAlignment(){
        if(this.velocity.magnitude == 0){
            this.movementAlignment = 1f;
        }
        else{
            this.movementAlignment = Vector3.Dot(this.velocity.normalized, this.direction);
        }

        if(this.movementAlignment >= 0.9995f){
            this.movementAlignment = 1f;
        }
        else if(this.movementAlignment <= 0.05f){
            this.movementAlignment *= this.drag * 12;
        }

        return this.movementAlignment;
    }

    private float CalculateMomentum(){
        if(this.direction != Vector3.zero){
            return Mathf.Clamp(this.momentum + (this.movementAlignment * this.momentumGrowth * Time.deltaTime), 0f, 1f);
        }
        else{
            if(this.momentum <= this.minimumMomentumToStop){
                return 0f;
            }

            return Mathf.Clamp(this.momentum - (this.drag * Time.deltaTime), 0f, 1f);
        }
    }

    private Vector3 CalculateFinalVelocity(){
        if(this.direction == Vector3.zero){
            return this.velocity.normalized * this.momentum * this.maxNaturalSpeed;
        }
        else{
            return this.direction * this.momentum * this.maxNaturalSpeed;
        }
    }

    // Update is called once per frame
    void Update(){
        this.direction = CalculateDirection();
        this.movementAlignment = CalculateMovementAlignment();
        this.momentum = CalculateMomentum();
        this.velocity = CalculateFinalVelocity();

        this.controller.Move(this.velocity * Time.deltaTime);

        //Debug.Log($"Dir: {this.direction} -- Align: {this.movementAlignment} -- Momentum: {this.momentum}");


        /*
        if(!controls.freecam){
            // If is Grounded
        	if(this.controller.isGrounded){
        		velocity.y = -0.1f;
        	}
            // If not, gravity affects
            else{
                velocity.y += gravity * Time.deltaTime;
            }

            float x = controls.movementX;
            float z = controls.movementZ;

            // Only move if not in menu
            if(!MainControllerManager.InUI){
                move = transform.right * x + transform.forward * z;
                controller.Move(move * speed * Time.deltaTime);
            }


            if(controls.jumping && this.controller.isGrounded){
                velocity.y = jumpHeight;
                jumpticks = 10;
                controller.skinWidth = 0.4f;
            }

            // Block Sticking
            if(jumpticks > 0){
                jumpticks--;
            }
            else{
                controller.skinWidth = 0.008f;
            }

            // If gravity hack is toggled
            if(controls.gravityHack){
                velocity.y = 20f;
            }

            // Gravity
            controller.Move(velocity * Time.deltaTime);
        }

        // If on Freecam
        else{
            float x = controls.movementX;
            float z = controls.movementZ;

            move = transform.right * x + transform.forward * z;
            controller.Move(move * speed * Time.deltaTime);

            if(controls.jumping){
                velocity.y = 5;
                controller.Move(velocity * Time.deltaTime);
                velocity.y = 0;
            }
            else if(MainControllerManager.shifting && !MainControllerManager.InUI){
                velocity.y = -5;
                controller.Move(velocity * Time.deltaTime);
                velocity.y = 0;
            }
        }
        */
    }

    // Headbumping Mechanics
    void OnControllerColliderHit(ControllerColliderHit hit){
        if(this.controller.isGrounded || controls.freecam)
            return;

        Vector3 impactPoint = hit.point;

        if(this.velocity.y >= 0 && impactPoint.y > this.gameObject.transform.position.y){
            this.velocity.y = -this.velocity.y;
        }
    }
}
