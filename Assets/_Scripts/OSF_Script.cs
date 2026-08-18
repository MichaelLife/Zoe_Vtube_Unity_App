using UnityEngine;
using OpenSee;
using System.Collections;
using System;
using System.Linq;
using uLipSync;
using UnityEngine.VFX;
using UnityEngine.InputSystem;

public enum EyeTrackingType
{
    Fixed,
    Continous,
    None
};

[RequireComponent(typeof(BlendShapes))]
public class OSF_Script : MonoBehaviour
{

    [Header("OPEN SEE COMPONENT")]
    [SerializeField] OpenSee.OpenSee openSee;
    [SerializeField] OpenSee.OpenSeeExpression openSeeExpression;
    [SerializeField] Zoe_UI ui;
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
    public float blinkThreshold = 0.175f;
    [Range(0, 1)]
    [Tooltip("The percentage that the eye has to be closed to be considered closed for the winking (0 - 1)")]
    public float winkThreshold = 0.175f;
    [Range(0, 1)]
    [Tooltip("The percentage that the eye has to be open to be considered open (0 - 1)")]
    public float openThreshold = 0.6f;
    [Tooltip("The minimum angle of movement that the eye has to make for the model's eye to move (to avoid eye shaking)")]
    public float deadzone = 5f;
    public float fixedEyeRotAngle = 6f;
    [SerializeField] float maxRotationAngle = 24f;
    public float gazeSpeed = 5f;
    public float eyelidSpeed = 5f;
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
    public EyeTrackingType eyeTrackingType = EyeTrackingType.Continous;

    [Header("MOUTH")]
    [Range(0, 1)]
    public float mouthOpenRatio;
    public float mouthSpeed;
    //[Range(0, 2)]
    //[SerializeField] float mouthInOutRatio = 1f;
    [SerializeField] uLipSyncBlendShape lipSync;
    private float minVolume;
    private float volume;

    [Header("EYEBROWS")]
    [SerializeField] float eyebrowsSpeed;
    [Range(-1, 1)]
    [SerializeField] float eyebrowsUp;
    [Range(-1, 1)]
    [SerializeField] float eyebrowsDown;
    [Range(0, 1)]
    [SerializeField] float eyebrowsRange;

    private float eyebrowNeutralLeft;

    [Header("BODY")]
    public float BodySpeed;
    [Range(0, 1)]
    public float BodyRotationRatio;
    public float squashThreshold;
    [SerializeField] Animator anim;
    [SerializeField] bool OnlyManualExpressions = true;
    private Vector3 previousHeadPos;
    [HideInInspector] public bool mirrorZAxis;

    [Header("VFX")]
    [SerializeField] VisualEffect sleepVFX;
    [SerializeField] float SleepTimerMax = 4f;
    [SerializeField] GameObject masteryObj;
    float SleepTimer = 0f;
    [SerializeField] Transform MainLigth;
    [SerializeField] Transform MainCam, AppCam;

    //Initial rotations for the different parts (for calibration)
    Quaternion InitialHeadRotation, InitialLEyeRotation, InitialREyeRotation, InitialBodyRotation, InitialNeckRotation;
    [HideInInspector]
    public Quaternion HeadRotationResetOffset, LEyeRotationResetOffset, REyeRotationResetOffset;

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

    public static OSF_Script instance;

    void Start()
    {
        instance = this;

        eyeGazeTimer = 0f;
        eyeGazeTimerMax = 0.1f;
        expressionTimerMax = 0.1f;
        HeadRotationResetOffset = Quaternion.identity;
        LEyeRotationResetOffset = Quaternion.identity;
        REyeRotationResetOffset = Quaternion.identity;
        InitialHeadRotation = Head.localRotation;
        InitialBodyRotation = Chest.localRotation;
        InitialNeckRotation = Neck.localRotation;
        InitialLEyeRotation = LeftEye.localRotation;
        InitialREyeRotation = RightEye.localRotation;
        continousEyeRotation = null;

        _bs = GetComponent<BlendShapes>();
        GetBlendShapeIDs(getBlendShapeNames(Eyelashes.gameObject), getBlendShapeNames(Face.gameObject), _bs);

        openSeeExpression.filename = @"C:\Users\migue\Desktop\GitHub\VtuberApp\Assets\Zoe\Expressions\Zoe_Expressions";
        openSeeExpression.load = true;
        openSeeExpression.predict = true;

        //eyebrowNeutralLeft = (eyebrowsDownLeft + eyebrowsUpLeft) / 2;

        expression = "neutral";
        minVolume = lipSync.minVolume;

        TryToLoadData();
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

        if (previousHeadPos == Vector3.zero) { previousHeadPos = _osf_data.translation; StartCoroutine(HeadSquash()); ResetEyeRotation(); }

        //Handle the face
        HandleHeadRotation(_osf_data);

        //Handle the eyes
        HandleBlink(_osf_data);
        HandleEyeRotation(_osf_data);

        //Handle the mouth
        HandleMouth(_osf_data);

        //Handle the eyebrows
        //HandleEyebrows(_osf_data); <---- TOO BUGGY (maybe my face is weird, it detects different depending on angle)

        HandleExpressions(OnlyManualExpressions ? expression : openSeeExpression.expression); //Update only manual expressions / Update tracked expresions
    }

    #region HEAD
    private void HandleHeadRotation(OpenSee.OpenSee.OpenSeeData head_data)
    {
        var rot = Quaternion.Euler(-head_data.rotation.x + HeadRotationOffset.x, -head_data.rotation.y + HeadRotationOffset.y, head_data.rotation.z + HeadRotationOffset.z);

        var _headTargetRot = InitialHeadRotation * HeadRotationResetOffset * rot;

        HandleBodyRotation(_headTargetRot.eulerAngles);

        Head.transform.localRotation = Quaternion.Slerp(Head.localRotation, _headTargetRot, BodySpeed * Time.deltaTime);
    }

    private void HandleBodyRotation(Vector3 headRot)
    {
        Quaternion _bodyTargetRot = Quaternion.Lerp(InitialBodyRotation, InitialBodyRotation * Quaternion.Euler(headRot.x, 0, headRot.z), BodyRotationRatio);
        Quaternion _neckTargetRot = Quaternion.Lerp(InitialNeckRotation, InitialNeckRotation * Quaternion.Euler(headRot.x, 0, headRot.z), BodyRotationRatio);

        Chest.transform.localRotation = Quaternion.Slerp(Chest.localRotation, _bodyTargetRot, BodySpeed * Time.deltaTime);
        Neck.transform.localRotation = Quaternion.Slerp(Neck.localRotation, _neckTargetRot, BodySpeed * Time.deltaTime);
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

    public void ResetCharacterRotation()
    {
        var _osf_data = openSee.GetOpenSeeData(0);
        var _headTargetRot = Quaternion.Inverse(Quaternion.Euler(-_osf_data.rotation.x + HeadRotationOffset.x, -_osf_data.rotation.y + HeadRotationOffset.y, _osf_data.rotation.z + HeadRotationOffset.z));
        HeadRotationResetOffset = _headTargetRot;
    }


    #endregion

    #region EYES
    public void ResetEyeRotation()
    {
        var _osf_data = openSee.GetOpenSeeData(0);
        var _LeyeTargetRot = Quaternion.Euler(-_osf_data.rightGaze.eulerAngles.x, -_osf_data.rightGaze.eulerAngles.y - 5, _osf_data.leftGaze.eulerAngles.z);
        LEyeRotationResetOffset = Quaternion.Inverse(_LeyeTargetRot);
        Debug.Log(LEyeRotationResetOffset * _LeyeTargetRot);
        var _ReyeTargetRot = Quaternion.Inverse(Quaternion.Euler(-_osf_data.rightGaze.eulerAngles.x, -_osf_data.rightGaze.eulerAngles.y, _osf_data.leftGaze.eulerAngles.z));
        REyeRotationResetOffset = _ReyeTargetRot;
    }
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
            case EyeTrackingType.None:
                if (continousEyeRotation != null)
                    StopCoroutine(continousEyeRotation);
                _RTargetRot = InitialREyeRotation;
                _LTargetRot = InitialLEyeRotation;
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
    }

    //ROTATION TYPES-------------------------------------------------------------------
    private void HandleFixedEyeRotation(OpenSee.OpenSee.OpenSeeData eye_data)
    {
        if (continousEyeRotation != null)
            StopCoroutine(continousEyeRotation);

        if (!areEyesOpen(eye_data)) return;

        if (continousEyeRotation != null) StopCoroutine(continousEyeRotation);

        Quaternion _LEyeRot = InitialLEyeRotation * LEyeRotationResetOffset * Quaternion.Euler(-eye_data.rightGaze.eulerAngles.x, -eye_data.rightGaze.eulerAngles.y - 5, eye_data.leftGaze.eulerAngles.z);
        Quaternion _REyeRot = InitialREyeRotation * REyeRotationResetOffset * Quaternion.Euler(-eye_data.rightGaze.eulerAngles.x, -eye_data.rightGaze.eulerAngles.y, eye_data.leftGaze.eulerAngles.z);

        Quaternion LeftReyeRot = InitialREyeRotation * REyeRotationResetOffset * Quaternion.Euler(0, 17, 0);
        Quaternion RightReyeRot = InitialREyeRotation * REyeRotationResetOffset * Quaternion.Euler(0, -14, 0);
        Quaternion DownReyeRot = InitialREyeRotation * REyeRotationResetOffset * Quaternion.Euler(14, 0, 0);
        Quaternion UpReyeRot = InitialREyeRotation * REyeRotationResetOffset * Quaternion.Euler(-14, 0, 0);

        if (Quaternion.Angle(_REyeRot, LeftReyeRot) < fixedEyeRotAngle)
        {
            eyeGazeTimer += Time.deltaTime;
            if (eyeGazeTimer > eyeGazeTimerMax)
            {
                //LOOK LEFT
                _RTargetRot = InitialREyeRotation * REyeRotationResetOffset * Quaternion.Euler(0, 13f, 0);
                _LTargetRot = InitialLEyeRotation * LEyeRotationResetOffset * Quaternion.Euler(0, 11f, 0);
            }
        }
        else if (Quaternion.Angle(_REyeRot, RightReyeRot) < fixedEyeRotAngle)
        {
            eyeGazeTimer += Time.deltaTime;
            if (eyeGazeTimer > eyeGazeTimerMax)
            {
                //LOOK RIGHT
                _RTargetRot = InitialREyeRotation * REyeRotationResetOffset * Quaternion.Euler(0, -8f, 0);
                _LTargetRot = InitialLEyeRotation * LEyeRotationResetOffset * Quaternion.Euler(0, -17f, 0);
            }
        }
        else if (Quaternion.Angle(_REyeRot, DownReyeRot) < fixedEyeRotAngle)
        {
            eyeGazeTimer += Time.deltaTime;
            if (eyeGazeTimer > eyeGazeTimerMax)
            {
                //LOOK DOWN
                _RTargetRot = InitialREyeRotation * REyeRotationResetOffset * Quaternion.Euler(11, 0, 0);
                _LTargetRot = InitialLEyeRotation * LEyeRotationResetOffset * Quaternion.Euler(11, 0, 0);
            }
        }
        else if (Quaternion.Angle(_REyeRot, UpReyeRot) < fixedEyeRotAngle)
        {
            eyeGazeTimer += Time.deltaTime;
            if (eyeGazeTimer > eyeGazeTimerMax)
            {
                //LOOK UP
                _RTargetRot = InitialREyeRotation * REyeRotationResetOffset * Quaternion.Euler(-14f, 0, 0);
                _LTargetRot = InitialLEyeRotation * LEyeRotationResetOffset * Quaternion.Euler(-14f, 0, 0);
            }
        }
        else //RESET GAZE
        {
            eyeGazeTimer += Time.deltaTime;
            if (eyeGazeTimer > eyeGazeTimerMax)
            {
                _RTargetRot = InitialREyeRotation * REyeRotationResetOffset;
                _LTargetRot = InitialLEyeRotation * LEyeRotationResetOffset;
                eyeGazeTimer = 0f;
            }
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

                Quaternion _LEyeRot = InitialLEyeRotation * LEyeRotationResetOffset * Quaternion.Euler(-rightGaze.eulerAngles.x, -rightGaze.eulerAngles.y - 5, eye_data.leftGaze.eulerAngles.z);
                Quaternion _REyeRot = InitialREyeRotation * REyeRotationResetOffset * Quaternion.Euler(-rightGaze.eulerAngles.x, -rightGaze.eulerAngles.y, eye_data.rightGaze.eulerAngles.z);

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
            Face.SetBlendShapeWeight(BS_MouthSmile, Mathf.Lerp(Face.GetBlendShapeWeight(BS_MouthSmile), _mouthWide * 100, mouthSpeed * Time.deltaTime));
        }
        else
        {
            Face.SetBlendShapeWeight(BS_MouthSmile, Mathf.Lerp(Face.GetBlendShapeWeight(BS_MouthSmile), 0, mouthSpeed * Time.deltaTime));
        }
    }

    #endregion

    #region EYEBROWS
    private void HandleEyebrows(OpenSee.OpenSee.OpenSeeData brow_data)
    {
        float media = (brow_data.features.EyebrowUpDownRight + brow_data.features.EyebrowUpDownLeft)/2;
        
        Debug.Log(media);

        if (media > eyebrowsUp - eyebrowsRange)
        {
            Debug.Log("EYEBROWS UP");
            float value = Math.Abs((eyebrowsUp - Mathf.Clamp(media, eyebrowsDown, eyebrowsUp) / eyebrowsRange));
            SetBlendShapes(Face, BS_EyebrowUp_R, value * 100, eyebrowsSpeed);
            SetBlendShapes(Face, BS_EyebrowUp_L, value * 100, eyebrowsSpeed);
        }
        else if (media < eyebrowsDown + eyebrowsRange)
        {
            Debug.Log("EYEBROWS DOWN");
            float value = Math.Abs((eyebrowsDown - Mathf.Clamp(media, eyebrowsDown, eyebrowsUp) / eyebrowsRange));
            SetBlendShapes(Face, BS_EyebrowDown_R, value * 100, eyebrowsSpeed);
            SetBlendShapes(Face, BS_EyebrowDown_L, value * 100, eyebrowsSpeed);
        }
        else
        {
            SetBlendShapes(Face, BS_EyebrowDown_R, 0, eyebrowsSpeed);
            SetBlendShapes(Face, BS_EyebrowDown_L, 0, eyebrowsSpeed);
            SetBlendShapes(Face, BS_EyebrowUp_R, 0, eyebrowsSpeed);
            SetBlendShapes(Face, BS_EyebrowUp_L, 0, eyebrowsSpeed);
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

        expressionList = new int[]
        {
            BS_MouthKiss,
            BS_MouthPuff,
            BS_MouthSad,
            BS_Angry,
            BS_Surprised
        };
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
        mesh.SetBlendShapeWeight(_blendShape, Mathf.Lerp(mesh.GetBlendShapeWeight(_blendShape), value, speed * Time.deltaTime));
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

    #region SAVING AND LOADING DATA
    public void TryToLoadData()
    {
        SaveData saveData = SaveManager.instance.LoadData();
        if (saveData != null)
        {
            LoadData(saveData);
        }
    }

    public void LoadData(SaveData data)
    {
        blinkThreshold = data.blinkThreshold;
        winkThreshold = data.winkThreshold;
        openThreshold = data.openThreshold;
        deadzone = data.deadzone;
        eyeTrackingType = data.eyeTracking;
        eyelidSpeed = data.eyeSpeed;
        mouthOpenRatio = data.mouthOpenRatio;
        mouthSpeed = data.mouthSpeed;
        BodySpeed = data.bodySpeed;
        BodyRotationRatio = data.bodyRotationRatio;
        squashThreshold = data.squashThreshold;

        HeadRotationResetOffset = data.HeadRotationResetOffset;
        LEyeRotationResetOffset = data.LEyeRotationResetOffset;
        REyeRotationResetOffset = data.REyeRotationResetOffset;

        ChangeLightRot(data.lightRot);
        ChangeMainBodyRot(data.baseBodyRot);
        ChangeZoom(data.zoom);
        ChangeCamPos(data.camPos);

        ui.LoadData(data);
    }

    public void SaveData()
    {
        SaveManager.instance.SaveData
            (
            new SaveData(
                blinkThreshold,
                winkThreshold,
                openThreshold,
                deadzone,
                eyeTrackingType,
                eyelidSpeed,
                mouthOpenRatio,
                mouthSpeed,
                BodySpeed,
                BodyRotationRatio,
                squashThreshold,
                MainLigth.rotation.eulerAngles.y,
                Camera.main.transform.position.z,
                Camera.main.transform.position.y,
                this.transform.rotation.eulerAngles.y,
                HeadRotationResetOffset,
                LEyeRotationResetOffset,
                REyeRotationResetOffset,
                true
                )
            );
    }
    
    public void ResetData()
    {
        //BASE DATA
        LoadData(new SaveData(0.2f, 0.45f, 0.75f, 4, EyeTrackingType.Continous, 35, 0.66f, 20, 10, 0.3f, 0.15f, 330f, 
            -4.5f, 1.17f, 160f, Quaternion.identity, Quaternion.identity, Quaternion.identity,true ));
    }

    public void ChangeLightRot(float rot)
    {
        MainLigth.rotation = Quaternion.Euler(11.4f, rot, -15.47f);
    }
    public void ChangeMainBodyRot(float rot)
    {
        this.transform.rotation = Quaternion.Euler(0f, rot, 0f);
    }
    public void ChangeZoom(float zoom)
    {
        var _camPos = Camera.main.transform.position;
        AppCam.position = new Vector3(_camPos.x, _camPos.y, zoom);
        MainCam.position = new Vector3(_camPos.x, _camPos.y, zoom);
    }
    public void ChangeCamPos(float pos)
    {
        var _camPos = Camera.main.transform.position;
        AppCam.position = new Vector3(_camPos.x, pos, _camPos.z);
        MainCam.position = new Vector3(_camPos.x, pos, _camPos.z);
    }

    #endregion
}
