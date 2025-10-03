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
    private Vector3 finalMovement;
    private float movementAlignment = 0f;
    public float jumpHeight = 5f;

    // Growth
    public float momentumGrowth = 2.4f;
    public float minimumMomentumToStop = 0.3f;

    // Knockback
    public float knockbackAlignment = 0f;
    public Vector3 knockbackForce = Vector3.zero;
    public float knockbackMomentum = 0f;

    // Gravity
    public float gravityMomentum = 0f;
    private float gravityAcceleration = -25f;
    private float gravityMaxAccelerationTime = 1.6f;

    // Running
    private float maxRunningMomentum = 1f;
    private float runMomentumGrowth = 0.7f;
    private float runMomentumDecrease = 3f;
    private float runMomentumBoost = 0f;


    void OnDestroy(){
        this.controller = null;
    }

    void FixedUpdate(){
        this.gravityMomentum = CalculateGravityAcceleration();
        JumpCheck();
    }

    // Update is called once per frame
    void Update(){
        this.direction = CalculateDirection();
        this.movementAlignment = CalculateMovementAlignment(this.velocity, this.direction);
        this.momentum = CalculateMomentum();
        this.runMomentumBoost = CalculateRunMomentumBoost();
        this.velocity = CalculateFinalVelocity();
        this.finalMovement = CalculateFinalMovement();

        this.controller.Move(this.finalMovement * Time.deltaTime);

        this.knockbackMomentum = CalculateKnockbackMomentumDecay();
    }

    public void AddKnockback(Vector3 dir, float momentum){
        // If has no other knockback happening
        if(this.knockbackMomentum == 0f){
            this.knockbackAlignment = CalculateMovementAlignment(this.velocity, dir);
            this.knockbackMomentum = momentum;
            this.knockbackForce = dir.normalized;
        }
        // Handles multiple knockback at the same time
        else{
            this.knockbackForce = (this.knockbackForce * this.knockbackMomentum) + (dir * momentum);
            this.knockbackMomentum = this.knockbackForce.magnitude;
            this.knockbackForce = this.knockbackForce.normalized;
            this.knockbackAlignment = CalculateMovementAlignment(this.velocity, this.knockbackForce);
        }

        // Adjust gravity momentum
        if(this.gravityMomentum < 0f){
            this.gravityMomentum += Mathf.Max(this.knockbackForce.y * this.knockbackMomentum, 0f);
        }

        // Fast Stop
        if(this.knockbackAlignment <= 0){
            this.momentum = Mathf.Max(this.momentum * (this.knockbackAlignment * momentum), 0f);
        }
    }

    private float CalculateRunMomentumBoost(){
        if(this.momentum == 1 && MainControllerManager.shifting && CheckValidRunDirection()){
            return Mathf.Clamp(this.runMomentumBoost + (this.movementAlignment * this.runMomentumGrowth * Time.deltaTime), 0, this.maxRunningMomentum);
        }
        
        return Mathf.Clamp(this.runMomentumBoost - (this.movementAlignment * this.runMomentumDecrease * Time.deltaTime), 0, this.maxRunningMomentum);
    }

    private void JumpCheck(){
        if(this.controller.isGrounded && this.controls.jumping)
            this.gravityMomentum = this.jumpHeight;
    }

    private float CalculateGravityAcceleration(){
        if(this.controller.isGrounded){
            return -0.01f;
        }

        return Mathf.Max(this.gravityMomentum + (this.gravityAcceleration * Time.fixedDeltaTime) / this.gravityMaxAccelerationTime, this.gravityAcceleration);
    }

    private Vector3 CalculateDirection(){return (this.transform.right * this.controls.movementX + this.transform.forward * this.controls.movementZ).normalized;}

    private float CalculateMovementAlignment(Vector3 dir1, Vector3 dir2){
        float align = 0f;

        if(this.velocity.magnitude == 0){
            align = 1f;
        }
        else{
            align = Vector3.Dot(dir1.normalized, dir2.normalized);
        }

        if(align >= 0.9995f){
            align = 1f;
        }
        else if(align <= 0.05f){
            align *= this.drag * 12;
        }

        return align;
    }

    private float CalculateMomentum(){
        if(this.direction != Vector3.zero){
            return Mathf.Clamp(this.momentum + (this.movementAlignment * this.momentumGrowth * Time.deltaTime), 0f, 1f);
        }
        else{
            if(this.momentum + this.knockbackMomentum <= this.minimumMomentumToStop){
                return 0f;
            }

            return Mathf.Clamp(this.momentum - (this.drag * Time.deltaTime), 0f, 1f);
        }
    }

    private float CalculateKnockbackMomentumDecay(){
        float newMomentum = 0f;

        if(this.knockbackMomentum == 0f)
            return 0f;

        newMomentum = this.knockbackMomentum - (drag * 2 * Time.deltaTime);

        if(newMomentum <= this.minimumMomentumToStop)
            return 0f;

        return newMomentum;
    }

    private Vector3 CalculateFinalVelocity(){
        if(this.direction == Vector3.zero){
            return this.velocity.normalized * this.momentum * this.maxNaturalSpeed;
        }
        else{
            return this.direction * (this.momentum + this.runMomentumBoost) * this.maxNaturalSpeed;
        }
    }

    private Vector3 CalculateFinalMovement(){
        return this.velocity + (this.knockbackForce * this.knockbackMomentum) + (this.gravityMomentum * Vector3.up);
    }

    private bool CheckValidRunDirection(){
        float alignment = Vector3.Dot(this.transform.forward, this.direction);

        if(alignment >= 0.7f)
            return true;
        return false;
    }

    // Headbumping Mechanics
    private void OnControllerColliderHit(ControllerColliderHit hit){
        if(this.controller.isGrounded || controls.freecam)
            return;

        Vector3 impactPoint = hit.point;

        if(this.velocity.y >= 0 && impactPoint.y > this.gameObject.transform.position.y){
            this.velocity.y = -this.velocity.y;
        }
    }
}
