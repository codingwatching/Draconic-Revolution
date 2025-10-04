using UnityEngine;

public class SwimmingMovePreset : BaseMovePreset {
	
	public SwimmingMovePreset(CharacterSheet sheet) : base(sheet){
    	this.maxNaturalSpeed = 2 + (sheet.GetSpeed().GetFinal())/10f;
		this.drag = 10f;
		this.jumpHeight = 2f;
		this.momentumGrowth = 1f;
		this.minimumMomentumToStop = 0.1f;
		this.gravityAcceleration = -8f;
		this.gravityMaxAccelerationTime = 4f;
		this.maxRunningMomentum = 0.8f;
		this.runMomentumGrowth = 0.3f;
		this.runMomentumDecrease = 20f;
		this.povAdjustment = 10f;
        this.maximumImpactAngleTolerance = -0.7f;
        this.maximumAllowedMomentumAfterImpact = 0.1f;
	}

	public override float CalculateJump(MovementFlags flags, float gravityMomentum){
        if(flags.isJumping && gravityMomentum < this.jumpHeight)
            return this.jumpHeight;

        return gravityMomentum;
	}
}