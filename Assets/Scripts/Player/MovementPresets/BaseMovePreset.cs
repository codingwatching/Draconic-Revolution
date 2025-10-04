using UnityEngine;

public abstract class BaseMovePreset {
	private CharacterSheet playerSheet;

    // Movement variables
    public float maxNaturalSpeed = 0f;
    protected float drag = 5f;
    protected float jumpHeight = 5f;

    // Growth
    protected float momentumGrowth = 2.4f;
    protected float minimumMomentumToStop = 0.3f;

    // Knockback
    protected float knockbackAlignment = 0f;
    protected float knockbackMomentum = 0f;

    // Gravity
    protected float gravityMomentum = 0f;
    protected float gravityAcceleration = -25f;
    protected float gravityMaxAccelerationTime = 1.6f;

    // Running
    protected float maxRunningMomentum = 1f;
    protected float runMomentumGrowth = 0.7f;
    protected float runMomentumDecrease = 3f;
    protected float runMomentumBoost = 0f;
    protected float povAdjustment = 15f;

    public BaseMovePreset(CharacterSheet sheet){
    	this.playerSheet = sheet;
    	this.maxNaturalSpeed = 2 + (sheet.GetSpeed().GetFinal())/3.3333f;
		this.drag = 5f;
		this.jumpHeight = 5f;
		this.momentumGrowth = 2.4f;
		this.minimumMomentumToStop = 0.3f;
		this.knockbackAlignment = 0f;
		this.knockbackMomentum = 0f;
		this.gravityMomentum = 0f;
		this.gravityAcceleration = -25f;
		this.gravityMaxAccelerationTime = 1.6f;
		this.maxRunningMomentum = 1f;
		this.runMomentumGrowth = 0.7f;
		this.runMomentumDecrease = 3f;
		this.runMomentumBoost = 0f;
		this.povAdjustment = 15f;
    }
	
	public virtual Vector3 CalculateDirection(Transform player, float controllerX, float controllerZ){return (player.right * controllerX + player.forward * controllerZ).normalized;}

	public virtual float CalculateMovementAlignment(Vector3 a, Vector3 b, Vector3 playerVelocity){
        float align = 0f;

        if(playerVelocity.magnitude == 0){
            align = 1f;
        }
        else{
            align = Vector3.Dot(a.normalized, b.normalized);
        }

        if(align >= 0.9995f){
            align = 1f;
        }
        else if(align <= 0.05f){
            align *= this.drag * 12;
        }

        return align;
	}

	public virtual float CalculateMomentum(Vector3 playerDirection, float currentMomentum, float currentKnockbackMomentum, float movementAlignment){
        if(playerDirection != Vector3.zero){
            return Mathf.Clamp(currentMomentum + (movementAlignment * this.momentumGrowth * Time.deltaTime), 0f, 1f);
        }
        else{
            if(currentMomentum + currentKnockbackMomentum <= this.minimumMomentumToStop){
                return 0f;
            }

            return Mathf.Clamp(currentMomentum - (this.drag * Time.deltaTime), 0f, 1f);
        }
	}

	public virtual float CalculateRunMomentumBoost(Transform transf, Vector3 playerDirection, float currentRunMomentum, float momentum, float movementAlignment){
        if(momentum == 1 && MainControllerManager.shifting && CheckValidRunDirection(transf, playerDirection)){
            return Mathf.Clamp(currentRunMomentum + (movementAlignment * this.runMomentumGrowth * Time.deltaTime), 0, this.maxRunningMomentum);
        }
        
        return Mathf.Clamp(currentRunMomentum - (movementAlignment * this.runMomentumDecrease * Time.deltaTime), 0, this.maxRunningMomentum);
	}

	public virtual Vector3 CalculateFinalVelocity(Vector3 playerDirection, Vector3 playerVelocity, float currentMomentum, float currentRunMomentum){
        if(playerDirection == Vector3.zero){
            return playerVelocity.normalized * currentMomentum * this.maxNaturalSpeed;
        }
        else{
            return playerDirection * (currentMomentum + currentRunMomentum) * this.maxNaturalSpeed;
        }
	}

	public virtual Vector3 CalculateFinalMovement(Vector3 velocity, Vector3 knockbackForce, float knockbackMomentum, float gravityMomentum){
		return velocity + (knockbackForce * knockbackMomentum) + (gravityMomentum * Vector3.up);
	}

	public virtual float CalculateGravityAcceleration(bool isGrounded, float currentGravityMomentum){
        if(isGrounded){
            return -0.01f;
        }

        return Mathf.Max(currentGravityMomentum + (this.gravityAcceleration * Time.fixedDeltaTime) / this.gravityMaxAccelerationTime, this.gravityAcceleration);
	}

	public virtual float CalculateJump(bool isGrounded, bool isJumping, float gravityMomentum){
        if(isGrounded && isJumping && this.gravityMomentum < this.jumpHeight)
            return this.jumpHeight;

        return gravityMomentum;
	}

	public virtual float CalculateKnockbackMomentumDecay(float currentKnockbackMomentum){
        float newMomentum = 0f;

        if(currentKnockbackMomentum == 0f)
            return 0f;

        newMomentum = currentKnockbackMomentum - (drag * 2 * Time.deltaTime);

        if(newMomentum <= this.minimumMomentumToStop)
            return 0f;

        return newMomentum;
	}

    public virtual void UpdateFOV(Camera cam, float currentRunMomentum){
        if(currentRunMomentum == 0 && cam.fieldOfView == Configurations.fieldOfView)
            return;

        cam.fieldOfView = Configurations.fieldOfView + Mathf.Lerp(0, this.povAdjustment, currentRunMomentum/this.maxRunningMomentum);
    }

    protected bool CheckValidRunDirection(Transform t, Vector3 playerDirection){
        float alignment = Vector3.Dot(t.forward, playerDirection);

        if(alignment >= 0.7f)
            return true;
        return false;
    }
}