using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class SaveData
{
    public float blinkThreshold;
    public float winkThreshold;
    public float openThreshold;
    public float deadzone;
    public EyeTrackingType eyeTracking;
    public float eyeSpeed;
    public float mouthOpenRatio;
    public float mouthSpeed;
    public float bodySpeed;
    public float bodyRotationRatio;
    public float squashThreshold;

    public float lightRot;
    public float zoom;
    public float camPos;
    public float baseBodyRot;

    public Quaternion HeadRotationResetOffset;
    public Quaternion LEyeRotationResetOffset;
    public Quaternion REyeRotationResetOffset;
    public SaveData(float blinkThreshold, float winkThreshold, float openThreshold, float deadzone, EyeTrackingType eyeTracking,
        float eyeSpeed, float mouthOpenRatio, float mouthSpeed, float bodySpeed, float bodyRotationRatio, float squashThreshold,
        float lightRot, float zoom, float camPos, float baseBodyRot, Quaternion HeadRotationResetOffset,
        Quaternion LEyeRotationResetOffset, Quaternion REyeRotationResetOffset)
    {
        this.blinkThreshold = blinkThreshold;
        this.winkThreshold = winkThreshold;
        this.openThreshold = openThreshold;
        this.deadzone = deadzone;
        this.eyeTracking = eyeTracking;
        this.eyeSpeed = eyeSpeed;
        this.mouthOpenRatio = mouthOpenRatio;
        this.mouthSpeed = mouthSpeed;
        this.bodySpeed = bodySpeed;
        this.bodyRotationRatio = bodyRotationRatio;
        this.squashThreshold = squashThreshold;
        this.lightRot = lightRot;
        this.zoom = zoom;
        this.camPos = camPos;
        this.baseBodyRot = baseBodyRot;
        this.HeadRotationResetOffset = HeadRotationResetOffset;
        this.LEyeRotationResetOffset = LEyeRotationResetOffset;
        this.REyeRotationResetOffset = REyeRotationResetOffset;
    }
}