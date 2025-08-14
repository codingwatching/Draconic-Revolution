using UnityEngine;

public class CameraViewTarget : MonoBehaviour {
	private Transform cam;
	private float cameraDistance = 5f;

	void Update(){
		if(this.cam == null)
			return;

		this.gameObject.transform.position = this.cam.position + this.cam.forward * this.cameraDistance;
	}

	public void SetCamera(Transform c){this.cam = c;}
}