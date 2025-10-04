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
    private PlayerSheetController playerSheetController;

    // Movement Preset
    private BaseMovePreset movementOrchestrator;

    // Movement variables
    private float momentum;
    private Vector3 velocity = Vector3.zero;
    private Vector3 direction;
    private Vector3 finalMovement;
    private float movementAlignment;
    private float runMomentumBoost;

    // Knockback
    public float knockbackAlignment;
    public Vector3 knockbackForce = Vector3.zero;
    public float knockbackMomentum;

    // Gravity
    public float gravityMomentum;


    void OnDestroy(){
        this.controller = null;
    }

    void FixedUpdate(){
        if(this.movementOrchestrator == null)
            return;

        this.gravityMomentum = this.movementOrchestrator.CalculateGravityAcceleration(this.controller.isGrounded, this.gravityMomentum);
        this.gravityMomentum = this.movementOrchestrator.CalculateJump(this.controller.isGrounded, this.controls.jumping, this.gravityMomentum);
    }

    // Update is called once per frame
    void Update(){
        if(this.movementOrchestrator == null)
            return;

        this.direction = this.movementOrchestrator.CalculateDirection(this.transform, this.controls.movementX, this.controls.movementZ);
        this.movementAlignment = this.movementOrchestrator.CalculateMovementAlignment(this.velocity, this.direction, this.velocity);
        this.momentum = this.movementOrchestrator.CalculateMomentum(this.direction, this.momentum, this.knockbackMomentum, this.movementAlignment);
        this.runMomentumBoost = this.movementOrchestrator.CalculateRunMomentumBoost(this.transform, this.direction, this.runMomentumBoost, this.momentum, this.movementAlignment);
        this.velocity = this.movementOrchestrator.CalculateFinalVelocity(this.direction, this.velocity, this.momentum, this.runMomentumBoost);
        this.finalMovement = this.movementOrchestrator.CalculateFinalMovement(this.velocity, this.knockbackForce, this.knockbackMomentum, this.gravityMomentum);

        this.controller.Move(this.finalMovement * Time.deltaTime);

        this.knockbackMomentum = this.movementOrchestrator.CalculateKnockbackMomentumDecay(this.knockbackMomentum);

        this.movementOrchestrator.UpdateFOV(this.cl.playerRaycast.playerCamera, this.runMomentumBoost);
    }

    public void Init(){
        this.playerSheetController = this.cl.playerSheetController;
        this.movementOrchestrator = new NormalMovePreset(this.playerSheetController.GetSheet());
    }

    public void AddKnockback(Vector3 dir, float momentum){
        // If has no other knockback happening
        if(this.knockbackMomentum == 0f){
            this.knockbackAlignment = this.movementOrchestrator.CalculateMovementAlignment(this.velocity, dir, this.velocity);
            this.knockbackMomentum = momentum;
            this.knockbackForce = dir.normalized;
        }
        // Handles multiple knockback at the same time
        else{
            this.knockbackForce = (this.knockbackForce * this.knockbackMomentum) + (dir * momentum);
            this.knockbackMomentum = this.knockbackForce.magnitude;
            this.knockbackForce = this.knockbackForce.normalized;
            this.knockbackAlignment = this.movementOrchestrator.CalculateMovementAlignment(this.velocity, this.knockbackForce, this.velocity);
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
