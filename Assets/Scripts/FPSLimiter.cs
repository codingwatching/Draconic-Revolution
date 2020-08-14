﻿using UnityEngine;
 
public class FPSLimiter : MonoBehaviour 
{
    public int targetFrameRate = 60;

    private void Start()
    {
        QualitySettings.vSyncCount = 2;
        Application.targetFrameRate = targetFrameRate;
    }
    private void Update(){
		Application.targetFrameRate = targetFrameRate; 	
    }
}