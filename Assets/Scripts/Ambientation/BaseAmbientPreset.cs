using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public abstract class BaseAmbientPreset{
	private static Dictionary<AmbientGroup, BaseAmbientPreset> presets = new Dictionary<AmbientGroup, BaseAmbientPreset>();

	// Constants
	protected static readonly float SURFACE_LIGHT_LUMINOSITY_DAY = 4f;
	protected static readonly float SURFACE_LIGHT_LUMINOSITY_NIGHT = 2.4f;
	protected static readonly Color SURFACE_LIGHT_COLOR_DAY = Color.white;
	protected static readonly Color SURFACE_LIGHT_COLOR_NIGHT = new Color(0.27f, 0.57f, 1f, 1f);

	// Physical Based Sky
	protected Color horizonTintDay;
	protected Color zenithTintDay;

	protected Color horizonTintSunset;
	protected Color zenithTintSunset;

	protected Color horizonTintSunrise;
	protected Color zenithTintSunrise;

	protected Color horizonTintNight;
	protected Color zenithTintNight;

	// Fog
	protected float fogAttenuation1;
	protected float fogAttenuation2;
	protected Color fogAlbedo;
	protected float fogAmbientLight;

	// Cloud Layer
	protected Color cloudTintDay;
	protected Color cloudTintSunset;
	protected Color cloudTintSunrise;
	protected Color cloudTintNight;

	// White Balance
	protected float wbTemperature;
	protected float wbTint;

	// Lift, Gamma, Gain
	protected float4 gainDay;
	protected float4 gainSunset;
	protected float4 gainSunrise;
	protected float4 gainNight;

	// Directional Light
	protected float lightIntensity;
	protected float2 sunRotation;
	protected Color sunColor;

	public static BaseAmbientPreset GetPreset(AmbientGroup g){
		if(presets.Count == 0)
			LoadPresets();

		return presets[g];
	}

	private static BaseAmbientPreset GeneratePreset(AmbientGroup g){
		switch(g){
			case AmbientGroup.PLAINS:
				return new PlainsAmbientPreset();
			case AmbientGroup.SNOW:
				return new SnowAmbientPreset();	
			case AmbientGroup.DESERT:
				return new DesertAmbientPreset();
			case AmbientGroup.FOREST:
				return new ForestAmbientPreset();
			case AmbientGroup.OCEAN:
				return new OceanAmbientPreset();			
			default:
				return new PlainsAmbientPreset();
		}
	}

	private static void LoadPresets(){
		int numberOfPresets = AmbientGroup.GetNames(typeof(AmbientGroup)).Length;
		AmbientGroup gp;

		for(int i=0; i < numberOfPresets; i++){
			gp = (AmbientGroup)i;
			presets.Add(gp, GeneratePreset(gp));
		}
	}

	public virtual float4 GetGain(float t){return this.gainDay;}
	public virtual Color GetHorizonTint(float t){return this.horizonTintDay;}
	public virtual Color GetZenithTint(float t){return this.zenithTintDay;}
	public virtual float GetFogAttenuation(float t){return this.fogAttenuation1;}
	public virtual Color GetFogAlbedo(float t){return this.fogAlbedo;}
	public virtual float GetFogAmbientLight(float t){return this.fogAmbientLight;}
	public virtual Color GetCloudTint(float t){return this.cloudTintDay;}
	public virtual float GetWhiteBalanceTemperature(){return this.wbTemperature;}
	public virtual float GetWhiteBalanceTint(){return this.wbTint;}
	public virtual float GetSunIntensity(float t){return this.lightIntensity;}
	public virtual float2 GetSunRotation(float t){return this.sunRotation;}
	public virtual Color GetSunColor(float t){return this.sunColor;}


	protected float BehaviourLerp4(float sunrise, float day, float sunset, float night, float x){
        if(x >= 180 && x < 240)
            return Mathf.Lerp(night, sunrise, (x-180)/60f);
        else if(x >= 240 && x < 360)
            return sunrise;
        else if(x >= 360  && x < 540)
            return Mathf.Lerp(sunrise, day, (x-360)/180f);
        else if(x >= 540 && x < 1080)
        	return day;
        else if(x >= 1080 && x < 1140)
            return Mathf.Lerp(day, sunset, (x-1080)/60f);
        else if(x >= 1140 && x < 1200)
        	return Mathf.Lerp(sunset, night, (x-1140)/60f);
        else
            return night;
	}

	protected Color BehaviourColor4(Color sunrise, Color day, Color sunset, Color night, float x){
        if(x >= 180 && x < 240)
            return Color.Lerp(night, sunrise, (x-180)/60f);
        else if(x >= 240 && x < 360)
            return sunrise;
        else if(x >= 360  && x < 540)
            return Color.Lerp(sunrise, day, (x-360)/180f);
        else if(x >= 540 && x < 1080)
        	return day;
        else if(x >= 1080 && x < 1140)
            return Color.Lerp(day, sunset, (x-1080)/60f);
        else if(x >= 1140 && x < 1200)
        	return Color.Lerp(sunset, night, (x-1140)/60f);
        else
            return night;
	}

	protected float4 BehaviourFloat4(float4 sunrise, float4 day, float4 sunset, float4 night, float x){
		Color sr = Float4ToColor(sunrise);
		Color d = Float4ToColor(day);
		Color ss = Float4ToColor(sunset);
		Color n = Float4ToColor(night);

        if(x >= 180 && x < 240)
            return ColorToFloat4(Color.Lerp(n, sr, (x-180)/60f));
        else if(x >= 240 && x < 360)
            return ColorToFloat4(sr);
        else if(x >= 360  && x < 540)
            return ColorToFloat4(Color.Lerp(sr, d, (x-360)/180f));
        else if(x >= 540 && x < 1080)
        	return ColorToFloat4(d);
        else if(x >= 1080 && x < 1140)
            return ColorToFloat4(Color.Lerp(d, ss, (x-1080)/60f));
        else if(x >= 1140 && x < 1200)
        	return ColorToFloat4(Color.Lerp(ss, n, (x-1140)/60f));
        else
            return ColorToFloat4(n);
	}

	protected T BehaviourFlipDayNight<T>(T day, T night, float x){
		if(x >= 240 && x < 1200)
			return day;
		return night;
	}

	// Rotation for main Skybox light
    protected float SunRotationX(float x){
        if(x > 240 && x <= 720){
            return Mathf.Lerp(0f, 90f, ClampTime(x));
        }
        else if(x > 720 && x < 1200){
            return Mathf.Lerp(90f, 180f, ClampTime(x));
        }
        else if(x <= 240){
            return Mathf.Lerp(90f, 180f, ClampTime(x));
        }
        else{
            return Mathf.Lerp(0f, 90f, ClampTime(x));
        }
    }

    // Rotation for Z component of Skybox Light
    protected float SunRotationZ(float x){
        if(x > 240 && x <= 720)
            return Mathf.Lerp(0f, 30f, (x-240)/480);
        else if(x > 720 && x <= 1200)
            return Mathf.Lerp(30f, 0f, (x-720)/480);
        else if(x > 1200)
            return Mathf.Lerp(0f, 30f, (x-1200)/1440);
        else
            return Mathf.Lerp(30f, 0f, x/240);
    }

    // Clamps the current time to a float[0,1]
    protected float ClampTime(float x){
        // Zero Lerp if below 4h
        if(x <= 240){
            return x/240f;
        }
        // Zero Lerp after 20h
        else if(x >= 1200){
            return (x-1200)/240f;
        }
        // Inclination until 12h
        else if(x <= 720){
            return (x-240)/480f;
        }
        // Declination until 20h
        else{
            return ((x-720)/480f);
        }
    }

    public void SetValues(AmbientationTestingTool att){
    	horizonTintDay = att.horizonTint_day;
    	horizonTintSunrise = att.horizonTint_sunrise;
    	horizonTintSunset = att.horizonTint_sunset;
    	horizonTintNight = att.horizonTint_night;

    	zenithTintDay = att.zenithTint_day;
    	zenithTintSunrise = att.zenithTint_sunrise;
    	zenithTintSunset = att.zenithTint_sunset;
    	zenithTintNight = att.zenithTint_night;
    	
    	fogAttenuation1 = att.fogAttenuation1;
    	fogAttenuation2 = att.fogAttenuation2;
    	fogAlbedo = att.fogAlbedo;
    	fogAmbientLight = att.fogAmbientLight;

    	cloudTintDay = att.cloudTint_day;
    	cloudTintSunrise = att.cloudTint_sunrise;
    	cloudTintSunset = att.cloudTint_sunset;
    	cloudTintNight = att.cloudTint_night;

    	wbTemperature = att.wbTemperature;
    	wbTint = att.wbTint;

    	gainDay = ColorToFloat4(att.gain_day);
    	gainSunrise = ColorToFloat4(att.gain_sunrise);
    	gainSunset = ColorToFloat4(att.gain_sunset);
    	gainNight = ColorToFloat4(att.gain_night);

    	lightIntensity = att.lightIntensity;
    	sunRotation = att.sunRotation;
    	sunColor = att.sunColor;
    }

	private float4 ColorToFloat4(Color c){
		return new float4(c.r, c.g, c.b, c.a);
	}

	private Color Float4ToColor(float4 f){
		return new Color(f.x, f.y, f.z, f.w);
	}

	public Color _ht_d(){return horizonTintDay;}
	public Color _ht_sr(){return horizonTintSunrise;}
	public Color _ht_ss(){return horizonTintSunset;}
	public Color _ht_n(){return horizonTintNight;}
	public Color _zt_d(){return zenithTintDay;}
	public Color _zt_sr(){return zenithTintSunrise;}
	public Color _zt_ss(){return zenithTintSunset;}
	public Color _zt_n(){return zenithTintNight;}
	public float _fa1(){return fogAttenuation1;}
	public float _fa2(){return fogAttenuation2;}
	public Color _falb(){return fogAlbedo;}
	public float _fal(){return fogAmbientLight;}
	public Color _ct_d(){return cloudTintDay;}
	public Color _ct_sr(){return cloudTintSunrise;}
	public Color _ct_ss(){return cloudTintSunset;}
	public Color _ct_n(){return cloudTintNight;}
	public float _wbte(){return wbTemperature;}
	public float _wbti(){return wbTint;}
	public float _li(){return lightIntensity;}
	public float2 _sr(){return sunRotation;}
	public Color _sc(){return sunColor;}
	public Color _g_d(){return Float4ToColor(gainDay);}
	public Color _g_sr(){return Float4ToColor(gainSunrise);}
	public Color _g_ss(){return Float4ToColor(gainSunset);}
	public Color _g_n(){return Float4ToColor(gainNight);}

}