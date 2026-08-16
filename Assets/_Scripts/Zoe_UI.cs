using UnityEngine;
using UnityEngine.UIElements;

public class Zoe_UI : MonoBehaviour
{

    [SerializeField] UIDocument _ui;
    private VisualElement root;
    private VisualElement ZoeUI;
    private bool uiShown = false;

    private Slider blinkThresholdSlider, winkThresholdSlider, openThresholdSlider, deadzoneSlider, eyelidSpeedSlider, mouthOpenRatioSlider,
        mouthSpeedSlider, BodySpeedSlider, BodyRotationRatioSlider, squashThresholdSlider, lightSlider, zoomSlider, posSlider, baseBodyRotSlider;
    private EnumField eyeRotType;
    void Start()
    {
        root = _ui.rootVisualElement;
        ZoeUI = root.Q<VisualElement>("ZoeUI");
        ZoeUI.style.display = DisplayStyle.None;

        var SaveButton = root.Q<VisualElement>("Save");
        var ResetButton = root.Q<VisualElement>("Reset");
        var ResetCharRot = root.Q<VisualElement>("ResetCharRot");
        var ResetEyeRot = root.Q<VisualElement>("ResetEyeRot");
        var hideOrShowButton = root.Q<VisualElement>("HideOrShow");

        blinkThresholdSlider = root.Q<Slider>("Blink");
        winkThresholdSlider = root.Q<Slider>("Wink");
        openThresholdSlider = root.Q<Slider>("EyeOpen");
        deadzoneSlider = root.Q<Slider>("Deadzone");
        eyelidSpeedSlider = root.Q<Slider>("EyeSpeed");
        mouthOpenRatioSlider = root.Q<Slider>("MouthOpen");
        mouthSpeedSlider = root.Q<Slider>("MouthSpeed");
        BodySpeedSlider = root.Q<Slider>("BodySpeed");
        BodyRotationRatioSlider = root.Q<Slider>("BodyRotRatio");
        squashThresholdSlider = root.Q<Slider>("SquashThreshold");
        lightSlider = root.Q<Slider>("Light");
        zoomSlider = root.Q<Slider>("Zoom");
        posSlider = root.Q<Slider>("Pos");
        baseBodyRotSlider = root.Q<Slider>("BaseBodyRot");

        eyeRotType = root.Q<EnumField>("EyeTrackingType");

        SaveButton.RegisterCallback<ClickEvent>(SaveEvent);
        ResetButton.RegisterCallback<ClickEvent>(ResetEvent);
        ResetCharRot.RegisterCallback<ClickEvent>(_event => OSF_Script.instance.ResetCharacterRotation());
        ResetEyeRot.RegisterCallback<ClickEvent>(_event => OSF_Script.instance.ResetEyeRotation());
        hideOrShowButton.RegisterCallback<ClickEvent>(ShowOrHideUI);

        blinkThresholdSlider.RegisterCallback<ChangeEvent<float>>(UpdateValues);
        winkThresholdSlider.RegisterCallback<ChangeEvent<float>>(UpdateValues);
        openThresholdSlider.RegisterCallback<ChangeEvent<float>>(UpdateValues);
        deadzoneSlider.RegisterCallback<ChangeEvent<float>>(UpdateValues);
        eyelidSpeedSlider.RegisterCallback<ChangeEvent<float>>(UpdateValues);
        mouthOpenRatioSlider.RegisterCallback<ChangeEvent<float>>(UpdateValues);
        mouthSpeedSlider.RegisterCallback<ChangeEvent<float>>(UpdateValues);
        BodySpeedSlider.RegisterCallback<ChangeEvent<float>>(UpdateValues);
        BodyRotationRatioSlider.RegisterCallback<ChangeEvent<float>>(UpdateValues);
        squashThresholdSlider.RegisterCallback<ChangeEvent<float>>(UpdateValues);
        lightSlider.RegisterCallback<ChangeEvent<float>>(UpdateValues);
        zoomSlider.RegisterCallback<ChangeEvent<float>>(UpdateValues);
        posSlider.RegisterCallback<ChangeEvent<float>>(UpdateValues);
        baseBodyRotSlider.RegisterCallback<ChangeEvent<float>>(UpdateValues);

        eyeRotType.RegisterValueChangedCallback(_event =>
        {
            OSF_Script.instance.eyeTrackingType = (EyeTrackingType)eyeRotType.value;
        }
        );
    }

    private void SaveEvent(ClickEvent _event)
    {
        OSF_Script.instance.SaveData();
    }
    private void ResetEvent(ClickEvent _event)
    {
        OSF_Script.instance.ResetData();
    }

    private void UpdateValues(ChangeEvent<float> _event)
    {
        OSF_Script _scr = OSF_Script.instance;

        _scr.blinkThreshold = blinkThresholdSlider.value;
        _scr.winkThreshold = winkThresholdSlider.value;
        _scr.openThreshold = openThresholdSlider.value;
        _scr.deadzone = deadzoneSlider.value;
        _scr.eyelidSpeed = eyelidSpeedSlider.value;
        _scr.mouthOpenRatio = mouthOpenRatioSlider.value;
        _scr.mouthSpeed = mouthSpeedSlider.value;
        _scr.BodySpeed = BodySpeedSlider.value;
        _scr.BodyRotationRatio = BodyRotationRatioSlider.value;
        _scr.squashThreshold = squashThresholdSlider.value;

        _scr.ChangeLightRot(lightSlider.value);
        _scr.ChangeMainBodyRot(baseBodyRotSlider.value);
        _scr.ChangeZoom(zoomSlider.value);
        _scr.ChangeCamPos(posSlider.value);
    }

    public void LoadData(SaveData data)
    {
        blinkThresholdSlider.value = data.blinkThreshold;
        winkThresholdSlider.value = data.winkThreshold;
        openThresholdSlider.value = data.openThreshold;
        deadzoneSlider.value = data.deadzone;
        eyeRotType.value = data.eyeTracking;
        eyelidSpeedSlider.value = data.eyeSpeed;
        mouthOpenRatioSlider.value = data.mouthOpenRatio;
        mouthSpeedSlider.value = data.mouthSpeed;
        BodySpeedSlider.value = data.bodySpeed;
        BodyRotationRatioSlider.value = data.bodyRotationRatio;
        squashThresholdSlider.value = data.squashThreshold;
        lightSlider.value = data.lightRot;
        zoomSlider.value = data.zoom;
        baseBodyRotSlider.value = data.baseBodyRot;
        posSlider.value = data.camPos;
    }

    public void ShowOrHideUI(ClickEvent _event)
    {
        if (uiShown) { ZoeUI.style.display = DisplayStyle.None; uiShown = false; }
        else { ZoeUI.style.display = DisplayStyle.Flex; uiShown = true; }
    }
}
