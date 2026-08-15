using UnityEngine;
using OpenSee;
using System.Collections;
using System;
using System.Linq;
using uLipSync;
using UnityEngine.VFX;
using UnityEngine.InputSystem;

[RequireComponent(typeof(BlendShapes))]
public class OSF_Script : MonoBehaviour
{
    public enum EyeTrackingType
    {
        Fixed,
        Continous,
        None
    };

    [Header("OPEN SEE COMPONENT")]
    [SerializeField] OpenSee.OpenSee openSee;
    [SerializeField] OpenSee.OpenSeeExpression openSeeExpression;
    private BlendShapes _bs;

    [Header("INPUT")]
    [SerializeField] INPUTS inputActions;

    [Header("MESHES WITH BLEND SHAPES")]
    [SerializeField] SkinnedMeshRenderer Face;
    [SerializeField] SkinnedMeshRenderer Eyelashes;
    [Header("MESHES TO ROTATE")]
    [SerializeField] Transform Head;
    [SerializeField] Transform Chest;
    [SerializeField] Transform Neck;
    [SerializeField] Transform LeftEye;
    [SerializeField] Transform RightEye;

    [Header("OFFSETS")]
    [SerializeField] Vector3 HeadRotationOffset;
    [SerializeField] Vector3 EyeRotationOffset;
    [SerializeField] Vector3 HeadRotationMultiplier;

    [Header("EYES")]
    [Range(0, 1)]
    [Tooltip("The percentage that the eye has to be closed to be considered closed for the blinking (0 - 1)")]
    [SerializeField] float blinkThreshold = 0.175f;
    [Range(0, 1)]
    [Tooltip("The percentage that the eye has to be closed to be considered closed for the winking (0 - 1)")]
    [SerializeField] float winkThreshold = 0.175f;
    [Range(0, 1)]
    [Tooltip("The percentage that the eye has to be open to be considered open (0 - 1)")]
    [SerializeField] float openThreshold = 0.6f;
    [Tooltip("The minimum angle of movement that the eye has to make for the model's eye to move (to avoid eye shaking)")]
    [SerializeField] float deadzone = 5f;
    [SerializeField] float maxRotationAngle = 24f;
    [SerializeField] float gazeSpeed = 5f;
    [SerializeField] float eyelidSpeed = 5f;
    [Tooltip("Small movements have lower speed")]
    [SerializeField] bool useRelativeSpeed = true;
    [Tooltip("The smalles angle where the speed is equal to the normal gaze speed")]
    [SerializeField] float relativeSpeedMaxAngle = 13f;
    [Range(0,1)]
    [SerializeField] float baseEylidPose;
    [Tooltip("Fixed -> Only goes from left to right | " +
        "Continous -> Follows eye tracking data (the result will depend on your eyes, lighting, camera, angle, if you " +
        "have glasses etc. | " +
        "None -> Eyes fixed to one position (the offset lets you change it))")]
    [SerializeField] EyeTrackingType eyeTrackingType = EyeTrackingType.Continous;

    [Header("MOUTH")]
    [Range(0, 1)]
    [SerializeField] float mouthOpenRatio;
    [SerializeField] float mouthSpeed;
    [Range(0, 2)]
    [SerializeField] float mouthInOutRatio = 1f;
    [SerializeField] uLipSyncBlendShape lipSync;
    private float minVolume;
    private float volume;

    [Header("EYEBROWS")]
    [SerializeField] float eyebrowsSpeed;
    [Range(-1, 1)]
    [SerializeField] float eyebrowsUpLeft;
    [Range(-1, 1)]
    [SerializeField] float eyebrowsDownLeft;
    [Range(0, 1)]
    [SerializeField] float eyebrowDeadzoneLeft;
    [Range(0, 1)]
    [SerializeField] float eyebrowsLoweredRatio;

    private float eyebrowNeutralLeft;

    [Header("BODY")]
    [SerializeField] float BodySpeed;
    [Range(0, 1)]
    [SerializeField] float BodyRotationRatio;
    [SerializeField] float squashThreshold;
    [SerializeField] Animator anim;
    [SerializeField] bool OnlyManualExpressions = true;
    private Vector3 previousHeadPos;

    [Header("VFX")]
    [SerializeField] VisualEffect sleepVFX;
    [SerializeField] float SleepTimerMax = 4f;
    [SerializeField] GameObject masteryObj;
    float SleepTimer = 0f;

    //Initial rotations for the different parts (for calibration)
    Quaternion InitialHeadRotation, InitialLEyeRotation, InitialREyeRotation, InitialBodyRotation, InitialNeckRotation;

    //Timing that the gaze have to be in a different position for the eyes to move (to avoid shaking rapidly)
    float eyeGazeTimer, eyeGazeTimerMax;

    //Timing that the gaze have to be in a different position for the eyes to move (to avoid shaking rapidly)
    float expressionTimer, expressionTimerMax;
    string expression = "neutral";

    //Target rotation for the eyes
    Quaternion _RTargetRot, _LTargetRot;

    //Coroutine for the continous eye rotation method
    Coroutine continousEyeRotation;

    #region BLEND SHAPES
    private static int BS_EyeClosed_L;
    private static int BS_EyeClosed_R;
    private static int BS_EyelashesClosed_L;
    private static int BS_EyelashesClosed_R;
    private static int BS_MouthOpen;
    private static int BS_MouthSmile;
    private static int BS_MouthKiss;
    private static int BS_MouthPuff;
    private static int BS_MouthSad;
    private static int BS_EyebrowUp_L;
    private static int BS_EyebrowUp_R;
    private static int BS_EyebrowDown_L;
    private static int BS_EyebrowDown_R;
    private static int BS_Surprised;
    private static int BS_Angry;
    #endregion

    void Start()
    {
        eyeGazeTimer = 0f;
        eyeGazeTimerMax = 0.1f;
        expressionTimerMax = 0.1f;
        InitialHeadRotation = Head.rotation;
        InitialBodyRotation = Chest.rotation;
        InitialNeckRotation = Neck.rotation;
        InitialLEyeRotation = LeftEye.localRotation;
        InitialREyeRotation = RightEye.localRotation;
        continousEyeRotation = null;

        _bs = GetComponent<BlendShapes>();
        GetBlendShapeIDs(getBlendShapeNames(Eyelashes.gameObject), getBlendShapeNames(Face.gameObject), _bs);

        openSeeExpression.filename = @"C:\Users\migue\Desktop\GitHub\VtuberApp\Assets\Zoe\Expressions\Zoe_Expressions";
        openSeeExpression.load = true;
        openSeeExpression.predict = true;

        eyebrowNeutralLeft = (eyebrowsDownLeft + eyebrowsUpLeft) / 2;

        expression = "neutral";
        minVolume = lipSync.minVolume;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        //DETECT OPEN SEE COMPONENT
        if (openSee == null) return;
        if (openSee.trackingData == null) return;

        //Get tracking data
        var _osf_data = openSee.GetOpenSeeData(0);

        //Make sure the data has been received
        if (_osf_data == null || !_osf_data.got3DPoints) return;

        if (previousHeadPos == Vector3.zero) { previousHeadPos = _osf_data.translation; StartCoroutine(HeadSquash()); }

        //Handle the face
        HandleHeadRotation(_osf_data);

        //Handle the eyes
        HandleBlink(_osf_data);
        HandleEyeRotation(_osf_data);

        //Handle the mouth
        HandleMouth(_osf_data);

        //Handle the eyebrows
        HandleEyebrows(_osf_data);

        HandleExpressions(OnlyManualExpressions ? expression : openSeeExpression.expression); //Update only manual expressions / Update tracked expresions
    }

    #region HEAD
    private void HandleHeadRotation(OpenSee.OpenSee.OpenSeeData head_data)
    {
        var _headTargetRot = InitialHeadRotation * Quaternion.Euler(-head_data.rotation.x + HeadRotationOffset.x, -head_data.rotation.y + HeadRotationOffset.y, head_data.rotation.z + HeadRotationOffset.z);

        HandleBodyRotation(_headTargetRot.eulerAngles);

        Head.transform.rotation = Quaternion.Slerp(Head.rotation, _headTargetRot, BodySpeed * Time.deltaTime);
    }

    private void HandleBodyRotation(Vector3 headRot)
    {
        Quaternion _bodyTargetRot = Quaternion.Lerp(InitialBodyRotation, InitialBodyRotation * Quaternion.Euler(headRot.x, 0, headRot.z), BodyRotationRatio);
        Quaternion _neckTargetRot = Quaternion.Lerp(InitialNeckRotation, InitialNeckRotation * Quaternion.Euler(headRot.x, 0, headRot.z), BodyRotationRatio);

        Chest.transform.rotation = Quaternion.Slerp(Chest.rotation, _bodyTargetRot, BodySpeed * Time.deltaTime);
        Neck.transform.rotation = Quaternion.Slerp(Neck.rotation, _neckTargetRot, BodySpeed * Time.deltaTime);
    }

    IEnumerator HeadSquash()
    {
        while (true)
        {
            var head_data = openSee.GetOpenSeeData(0);

            if (head_data != null)
            {
                float distance = head_data.translation.y - previousHeadPos.y;

                if (distance < -squashThreshold)
                {
                    bool down = distance < 0;
                    anim.SetTrigger("SquashDown");
                    yield return new WaitForSeconds(1f);
                }
                previousHeadPos = head_data.translation;
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    #endregion

    #region EYES

    private void HandleEyeRotation(OpenSee.OpenSee.OpenSeeData eye_data)
    {
        switch (eyeTrackingType)
        {
            case EyeTrackingType.Fixed:
                HandleFixedEyeRotation(eye_data);
                break;
            case EyeTrackingType.Continous:
                HandleContinousEyeRotation(eye_data);
                break;
            default:
                Debug.LogError("No valid eye tracking type detected");
                break;
        }

        RightEye.transform.localRotation = Quaternion.Slerp(RightEye.transform.localRotation, _RTargetRot, EyeVelocity(RightEye.transform.localRotation, _RTargetRot, gazeSpeed));
        LeftEye.transform.localRotation = Quaternion.Slerp(LeftEye.transform.localRotation, _LTargetRot, EyeVelocity(LeftEye.transform.localRotation, _RTargetRot, gazeSpeed));
    }
    private void HandleBlink(OpenSee.OpenSee.OpenSeeData eye_data)
    {
        bool L_eyeClosed = eye_data.leftEyeOpen < blinkThreshold;
        bool R_eyeClosed = eye_data.rightEyeOpen < blinkThreshold;

        bool L_eyeWink = eye_data.leftEyeOpen < winkThreshold;
        bool R_eyeWink = eye_data.rightEyeOpen < winkThreshold;

        if (L_eyeClosed && R_eyeClosed) //BOTH CLOSED (BLINK)
        {
            SleepTimer += Time.deltaTime;
            if (SleepTimer >= SleepTimerMax) sleepVFX.SetBool("PLAY", true); //ZZZ PARTICLES

            Blink(true);
        }
        else if((L_eyeWink && !R_eyeWink) || (!L_eyeWink && R_eyeWink)) //ONLY ONE EYE CLOSED (WINK)
        {
            Wink((L_eyeWink && !R_eyeWink) ? BS_EyeClosed_L : BS_EyeClosed_R,
                 (L_eyeWink && !R_eyeWink) ? BS_EyelashesClosed_L : BS_EyelashesClosed_R); //TRUE = LEFT; FALSE = RIGHT
        }
        else //NORMAL EYELID CONTROL
        {
            if (SleepTimer >= SleepTimerMax) { sleepVFX.SetBool("PLAY", false); SleepTimer = 0f; } //RESET ZZZ PARTICLES TIMER
            HandleEyelids(eye_data);
        }
    }
    private void Blink(bool startBlink)
    {
        SetBlendShapes(Face, BS_EyeClosed_R, 100, eyelidSpeed); //Close eyes
        SetBlendShapes(Face, BS_EyeClosed_L, 100, eyelidSpeed);

        SetBlendShapes(Eyelashes, BS_EyelashesClosed_L, 100, eyelidSpeed); //Close eyelashes
        SetBlendShapes(Eyelashes, BS_EyelashesClosed_R, 100, eyelidSpeed);
    }
    private void Wink(int eye, int eyelash) 
    {
        SetBlendShapes(Face, eye, 100, eyelidSpeed); //Close eye
        SetBlendShapes(Eyelashes, eyelash, 100, eyelidSpeed); //Close eyelash
    }
    private void HandleEyelids(OpenSee.OpenSee.OpenSeeData eye_data)
    {
        SetBlendShapes(Face, BS_EyeClosed_R, (1 - eye_data.leftEyeOpen) * 100, eyelidSpeed);
        SetBlendShapes(Face, BS_EyeClosed_L, (1 - eye_data.leftEyeOpen) * 100, eyelidSpeed);

        SetBlendShapes(Eyelashes, BS_EyelashesClosed_R, (1 - eye_data.leftEyeOpen) * 100, eyelidSpeed);
        SetBlendShapes(Eyelashes, BS_EyelashesClosed_L, (1 - eye_data.leftEyeOpen) * 100, eyelidSpeed);
        /*
        if (!areEyesOpen(eye_data) || eye_data.leftEyeOpen > 0.9f)
        {
            Face.SetBlendShapeWeight(BS_EyeClosed_R, Mathf.Lerp(Face.GetBlendShapeWeight(BS_EyeClosed_R), eye_data.leftEyeOpen * 100, eyelidSpeed * Time.deltaTime));
            Face.SetBlendShapeWeight(BS_EyeClosed_L, Mathf.Lerp(Face.GetBlendShapeWeight(BS_EyeClosed_L), eye_data.leftEyeOpen * 100, eyelidSpeed * Time.deltaTime));

            Eyelashes.SetBlendShapeWeight(BS_EyelashesClosed_L, Mathf.Lerp(Eyelashes.GetBlendShapeWeight(BS_EyelashesClosed_L), eye_data.leftEyeOpen * 100, eyelidSpeed * Time.deltaTime));
            Eyelashes.SetBlendShapeWeight(BS_EyelashesClosed_R, Mathf.Lerp(Eyelashes.GetBlendShapeWeight(BS_EyelashesClosed_R), eye_data.leftEyeOpen * 100, eyelidSpeed * Time.deltaTime));
        }
        else
        {
            Blink(false);
        }*/
    }

    //ROTATION TYPES-------------------------------------------------------------------
    private void HandleFixedEyeRotation(OpenSee.OpenSee.OpenSeeData eye_data)
    {
        if (!areEyesOpen(eye_data)) return;

        if (continousEyeRotation != null) StopCoroutine(continousEyeRotation);

        Quaternion _LEyeRot = InitialLEyeRotation * Quaternion.Euler(-eye_data.rightGaze.eulerAngles.x, -eye_data.rightGaze.eulerAngles.y, eye_data.leftGaze.eulerAngles.z) * Quaternion.Euler(EyeRotationOffset);
        Quaternion _REyeRot = InitialREyeRotation * Quaternion.Euler(-eye_data.rightGaze.eulerAngles.x, -eye_data.rightGaze.eulerAngles.y, eye_data.leftGaze.eulerAngles.z) * Quaternion.Euler(EyeRotationOffset);

        _LTargetRot = InitialLEyeRotation;
        _RTargetRot = InitialREyeRotation;

        if(_REyeRot.y < -0.15f && _REyeRot.w > 0.5f)
        {
            eyeGazeTimer += Time.deltaTime;
            if (eyeGazeTimer > eyeGazeTimerMax)
            {
                //LOOK RIGHT
                _RTargetRot = InitialREyeRotation * Quaternion.Euler(0, -8f, 0);
                _LTargetRot = InitialLEyeRotation * Quaternion.Euler(0, -17f, 0);
            }

        }
        else if(_REyeRot.y < -0.08f && _REyeRot.w < -0.5f)
        {
            eyeGazeTimer += Time.deltaTime;
            if (eyeGazeTimer > eyeGazeTimerMax)
            {
                //LOOK LEFT
                _RTargetRot = InitialREyeRotation * Quaternion.Euler(0, 13f, 0);
                _LTargetRot = InitialLEyeRotation * Quaternion.Euler(0, 11f, 0);
            }
        }
        else if (_REyeRot.x < -0.02f && _REyeRot.w > 0.5f)
        {
            eyeGazeTimer += Time.deltaTime;
            if (eyeGazeTimer > eyeGazeTimerMax)
            {
                //LOOK UP
                _RTargetRot = InitialREyeRotation * Quaternion.Euler(-14f, 0, 0);
                _LTargetRot = InitialLEyeRotation * Quaternion.Euler(-14f, 0, 0);
            }
        }
        else if (_REyeRot.x < -0.02f && _REyeRot.w < -0.5f)
        {
            eyeGazeTimer += Time.deltaTime;
            if (eyeGazeTimer > eyeGazeTimerMax)
            {
                //LOOK DOWN
                _RTargetRot = InitialREyeRotation * Quaternion.Euler(11, 0, 0);
                _LTargetRot = InitialLEyeRotation * Quaternion.Euler(11, 0, 0);
            }
        }
        else //RESET GAZE
        {
            _RTargetRot = InitialREyeRotation;
            _LTargetRot = InitialLEyeRotation;
            eyeGazeTimer = 0f;
        }
    }
    private void HandleContinousEyeRotation(OpenSee.OpenSee.OpenSeeData eye_data)
    {
        if(continousEyeRotation == null)
            continousEyeRotation = StartCoroutine(CoroutineHandleContinousEyeRot());

        IEnumerator CoroutineHandleContinousEyeRot()
        {
            while (true)
            {
                var eye_data = openSee.GetOpenSeeData(0);

                if (areEyesOpen(eye_data)) yield return null;

                if(isMoreThanMaxAngle(InitialREyeRotation, eye_data.rightGaze, maxRotationAngle)) yield return null;

                Quaternion rightGaze = ApplyDeadzone(InitialREyeRotation, eye_data.rightGaze, deadzone);

                Quaternion _LEyeRot = InitialLEyeRotation * Quaternion.Euler(-rightGaze.eulerAngles.x, -rightGaze.eulerAngles.y - 5, eye_data.leftGaze.eulerAngles.z) * Quaternion.Euler(EyeRotationOffset);
                Quaternion _REyeRot = InitialREyeRotation * Quaternion.Euler(-rightGaze.eulerAngles.x, -rightGaze.eulerAngles.y, eye_data.rightGaze.eulerAngles.z) * Quaternion.Euler(EyeRotationOffset);

                float LEyeRotY = _LEyeRot.eulerAngles.y;
                if (LEyeRotY > 300) LEyeRotY = Mathf.Clamp(_LEyeRot.eulerAngles.y, 336f, 360f);
                else LEyeRotY = Mathf.Clamp(_LEyeRot.eulerAngles.y, 0, 17f);

                float REyeRotY = _REyeRot.eulerAngles.y;
                if (REyeRotY > 300) REyeRotY = Mathf.Clamp(_REyeRot.eulerAngles.y, 343f, 360f);
                else REyeRotY = Mathf.Clamp(_REyeRot.eulerAngles.y, 0, 24f);

                _LTargetRot = Quaternion.Euler(_LEyeRot.eulerAngles.x, LEyeRotY, _LEyeRot.eulerAngles.z);
                _RTargetRot = Quaternion.Euler(_REyeRot.eulerAngles.x, REyeRotY, _REyeRot.eulerAngles.z);

                yield return new WaitForSeconds(0.25f);
            }
        }
    }

    //UTILS
    private float EyeVelocity(Quaternion baseRot, Quaternion rot, float _gazeSpeed)
    {
        float relativeVelMult = 1;
        if (useRelativeSpeed) relativeVelMult = Mathf.Clamp(Quaternion.Angle(baseRot, rot) / relativeSpeedMaxAngle, 0, 1);

        return (1f - Mathf.Exp(-gazeSpeed * relativeVelMult * Time.deltaTime));
    }

    private bool isMoreThanMaxAngle(Quaternion baseRot, Quaternion rot, float max)
    {
        if (Quaternion.Angle(baseRot, rot) > max)
            return true;

        return false;
    }

    private Quaternion ApplyDeadzone(Quaternion baseRot, Quaternion rot, float deadzone)
    {
        if (Quaternion.Angle(baseRot, rot) < deadzone)
            return Quaternion.identity;

        return rot;
    }
    private bool areEyesOpen(OpenSee.OpenSee.OpenSeeData eye_data) => eye_data.rightEyeOpen > openThreshold;

    #endregion

    #region MOUTH

    public void OnLipSyncUpdate(LipSyncInfo info)
    {
        volume = Mathf.Log10(info.rawVolume); 
    }

    public bool isTalking() => volume > minVolume;

    private void HandleMouth(OpenSee.OpenSee.OpenSeeData mouth_data)
    {
        if (expression != "neutral") return;

        if (!isTalking())
        {
            HandleOpenMouth(mouth_data); //OPEN MOUTH
        }
        HandleSmile(mouth_data);
    }

    private void HandleOpenMouth(OpenSee.OpenSee.OpenSeeData mouth_data)
    {
        float _mouthOpen = mouth_data.features.MouthOpen;
        if (_mouthOpen > 0.2f)
        {
            Face.SetBlendShapeWeight(BS_MouthOpen, Mathf.Lerp(Face.GetBlendShapeWeight(BS_MouthOpen), _mouthOpen * mouthOpenRatio * 100, mouthSpeed * Time.deltaTime));
        }else
        {
            Face.SetBlendShapeWeight(BS_MouthOpen, Mathf.Lerp(Face.GetBlendShapeWeight(BS_MouthOpen), 0, mouthSpeed * Time.deltaTime));
        }
    }
    private void HandleSmile(OpenSee.OpenSee.OpenSeeData mouth_data)
    {
        float _mouthWide = mouth_data.features.MouthWide;
        if (_mouthWide > 0.2f)
        {
            Debug.Log(_mouthWide * 100);
            Face.SetBlendShapeWeight(BS_MouthSmile, Mathf.Lerp(Face.GetBlendShapeWeight(BS_MouthSmile), _mouthWide * 100, mouthSpeed * Time.deltaTime));
        }
        else
        {
            Debug.Log(_mouthWide + "AAAA");
            Face.SetBlendShapeWeight(BS_MouthSmile, Mathf.Lerp(Face.GetBlendShapeWeight(BS_MouthSmile), 0, mouthSpeed * Time.deltaTime));
        }
    }

    #endregion

    #region EYEBROWS
    private void HandleEyebrows(OpenSee.OpenSee.OpenSeeData brow_data)
    {
        //HandleEyebrow(brow_data.features.EyebrowUpDownRight,BS_EyebrowUp_R, BS_EyebrowDown_R);
        //HandleEyebrow(brow_data.features.EyebrowUpDownLeft, BS_EyebrowUp_L, BS_EyebrowDown_L);
    }

    private void HandleEyebrow(float value, int eyebrowUp, int eyebrowDown)
    {
        Debug.Log(value + "  " + (eyebrowNeutralLeft + eyebrowDeadzoneLeft));

        float offset = eyebrowsDownLeft;
        if (eyebrowsDownLeft < 0) offset = -eyebrowsDownLeft;
        float mult = 1 / (eyebrowsUpLeft + offset);

        float upValue = (value + offset) * mult;
        float downValue = 1 - ((value - offset) * mult);


        if (value > eyebrowNeutralLeft + eyebrowDeadzoneLeft)
        {
            SetBlendShapes(Face, eyebrowUp, Mathf.Clamp(upValue, 0, 1) * 100, eyebrowsSpeed);
        }
        else if (value < eyebrowNeutralLeft - eyebrowDeadzoneLeft)
        {
            SetBlendShapes(Face, eyebrowDown,  Mathf.Clamp(downValue, 0, 1) * 100, eyebrowsSpeed);
        }
        else
        {
            SetBlendShapes(Face, eyebrowDown, 0, eyebrowsSpeed);
            SetBlendShapes(Face, eyebrowUp, 0, eyebrowsSpeed);
        }
    }

    #endregion

    #region BLEND SHAPES
    private void GetBlendShapeIDs(string[] blendShapeArrayEyelashes, string[] blendShapeArrayFace, BlendShapes _blendShapesNames)
    {
        BS_EyeClosed_L = Array.IndexOf(blendShapeArrayFace, _blendShapesNames.BS_EyeClosed_L);
        BS_EyeClosed_R = Array.IndexOf(blendShapeArrayFace, _blendShapesNames.BS_EyeClosed_R);
        BS_EyelashesClosed_L = Array.IndexOf(blendShapeArrayEyelashes, _blendShapesNames.BS_EyelashesClosed_L);
        BS_EyelashesClosed_R = Array.IndexOf(blendShapeArrayEyelashes, _blendShapesNames.BS_EyelashesClosed_R);
        BS_MouthOpen = Array.IndexOf(blendShapeArrayFace, _blendShapesNames.BS_MouthOpen);
        BS_MouthSmile = Array.IndexOf(blendShapeArrayFace, _blendShapesNames.BS_MouthSmile);
        BS_MouthKiss = Array.IndexOf(blendShapeArrayFace, _blendShapesNames.BS_MouthKiss);
        BS_MouthPuff = Array.IndexOf(blendShapeArrayFace, _blendShapesNames.BS_MouthPuff);
        BS_MouthSad = Array.IndexOf(blendShapeArrayFace, _blendShapesNames.BS_MouthSad);
        BS_EyebrowUp_L = Array.IndexOf(blendShapeArrayFace, _blendShapesNames.BS_EyebrowUp_L);
        BS_EyebrowUp_R = Array.IndexOf(blendShapeArrayFace, _blendShapesNames.BS_EyebrowUp_R);
        BS_EyebrowDown_L = Array.IndexOf(blendShapeArrayFace, _blendShapesNames.BS_EyebrowDown_L);
        BS_EyebrowDown_R = Array.IndexOf(blendShapeArrayFace, _blendShapesNames.BS_EyebrowDown_R);
        BS_Angry = Array.IndexOf(blendShapeArrayFace, _blendShapesNames.BS_Angry);
        BS_Surprised = Array.IndexOf(blendShapeArrayFace, _blendShapesNames.BS_Surprised);
    }

    public string[] getBlendShapeNames(GameObject obj)
    {
        SkinnedMeshRenderer head = obj.GetComponent<SkinnedMeshRenderer>();
        Mesh m = head.sharedMesh;
        string[] arr;
        arr = new string[m.blendShapeCount];
        for (int i = 0; i < m.blendShapeCount; i++)
        {
            string s = m.GetBlendShapeName(i);
            print("Blend Shape: " + i + " " + s);
            arr[i] = s;
        }
        return arr;
    }
    private void SetBlendShapes(SkinnedMeshRenderer mesh, int _blendShape, float value, float speed)
    {
        mesh.SetBlendShapeWeight(_blendShape, Mathf.Lerp(Face.GetBlendShapeWeight(_blendShape), value, speed * Time.deltaTime));
    }
    #endregion

    #region EXPRESSIONS

    private int[] expressionList =
    {
        BS_MouthKiss,
        BS_MouthPuff,
        BS_MouthSad,
        BS_Angry,
        BS_Surprised
    };

    private void HandleExpressions(string _expression)
    {
        if (expression != _expression)
        { expression = _expression; expressionTimer = 0; }

        if (isTalking()) return; //Dont do expressions if character is talking

        switch (_expression)
        {
            case "neutral":
                expressionTimer += Time.deltaTime;
                if (expressionTimer > expressionTimerMax)
                {
                    foreach (int expression in expressionList)
                    {
                        SetBlendShapes(Face, expression, 0, mouthSpeed);
                    }
                }
                break;
            case "kiss":
                expressionTimer += Time.deltaTime;
                if (expressionTimer > expressionTimerMax)
                {
                    foreach (int expression in expressionList)
                    {
                        if (expression == BS_MouthKiss) SetBlendShapes(Face, expression, 100, mouthSpeed);
                        else SetBlendShapes(Face, expression, 0, mouthSpeed);
                    }
                }
                break;
            case "sad":
                expressionTimer += Time.deltaTime;
                if (expressionTimer > expressionTimerMax)
                {
                    foreach (int expression in expressionList)
                    {
                        if (expression == BS_MouthSad) SetBlendShapes(Face, expression, 100, mouthSpeed);
                        else SetBlendShapes(Face, expression, 0, mouthSpeed);
                    }
                }
                break;
            case "angry":
                expressionTimer += Time.deltaTime;
                if (expressionTimer > expressionTimerMax)
                {
                    foreach (int expression in expressionList)
                    {
                        if (expression == BS_Angry) SetBlendShapes(Face, expression, 100, mouthSpeed);
                        else SetBlendShapes(Face, expression, 0, mouthSpeed);
                    }
                }
                break;
            case "surprised":
                expressionTimer += Time.deltaTime;
                if (expressionTimer > expressionTimerMax)
                {
                    foreach (int expression in expressionList)
                    {
                        if (expression == BS_Surprised) SetBlendShapes(Face, expression, 100, mouthSpeed);
                        else SetBlendShapes(Face, expression, 0, mouthSpeed);
                    }
                }
                break;
        }
    }

    #endregion

    #region INPUTS

    public void OnMastery(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (masteryObj.activeSelf) return;

        masteryObj.SetActive(true);
        masteryObj.GetComponent<Animator>().Rebind();

        StartCoroutine(HideMastery());

        IEnumerator HideMastery()
        {
            yield return new WaitForSeconds(5f);
            masteryObj.SetActive(false);
        }
    }

    public void OnAngry(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (context.started) //Start expression
        {
            expression = "angry";
        }
        if (context.canceled) //Stop expression
        {
            expression = "neutral";
        }
    }
    public void OnSurprised(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (context.started) //Start expression
        {
            expression = "surprised";
        }
        if (context.canceled) //Stop expression
        {
            expression = "neutral";
        }
    }
    public void OnSad(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (context.started) //Start expression
        {
            expression = "sad";
        }
        if (context.canceled) //Stop expression
        {
            expression = "neutral";
        }
    }

    private void OnEnable()
    {
        if (inputActions == null) inputActions = new INPUTS();

        inputActions.Enable();

        inputActions.ZoeActions.Mastery7.performed += OnMastery;

        inputActions.ZoeActions.Angry.started += OnAngry;
        inputActions.ZoeActions.Angry.canceled += OnAngry;

        inputActions.ZoeActions.Surprised.started += OnSurprised;
        inputActions.ZoeActions.Surprised.canceled += OnSurprised;

        inputActions.ZoeActions.Sad.started += OnSad;
        inputActions.ZoeActions.Sad.canceled += OnSad;
    }
    private void OnDisable()
    {
        inputActions.Disable();
    }

    #endregion
}
