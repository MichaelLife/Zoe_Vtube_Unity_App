# ZoeVtubeUnityApp
A vtube app for a Zoe model (LoL) made using OpenSeeFace tracking data, connects to obs with spout

It works with Open See Face -> https://github.com/emilianavt/OpenSeeFace
The model is configured to my face and set up so it may take a lot of configuration if you want to make it work correctly.
You can take the fbx transform it into a VRM and use it in VSeeFace or VRChat if you want.
You can also connect unity with obs with the spout addon for obs. (You have to add the render texture [SpoutRenderImageSender] to the camera settings output texture, you can also make the background a transparent solid color)


# HOW TO USE
**Install Open See Face** -> https://github.com/emilianavt/OpenSeeFace , if you are on Windows you'll just have to download the files and run the file **"run.bat"** in the program's Binary folder. This will open the Open See Face tracking program, you can give it the instructions on which camera and settings to use.
Download the Build in releases and, if you have Open See Face active, it should work.
You can configure the settings by clicking in the X at the top of the screen. The settings are saved in this path -> **C:\Users\"USER"\Appdata\LocalLow\LifeHMA\ZoeVtubeApp\SaveFile.json**

The expressions are controlled with the 1(surprised),2(angry),3(sad) and 7(mastery 7) keys.

You can connect the app with the obs spout2 pluggin -> https://github.com/Off-World-Live/obs-spout2-plugin

If you want to mirror the character movement, go to the run.bat file in the OpenSeeFace\Binary folder, open in with notepad (put: notepad run.bat in the windows explorer) and add -M here, save and close the notepad, it shoud be mirrored after you ran the .bat now:
<img width="1316" height="382" alt="image" src="https://github.com/user-attachments/assets/6e9d4432-941a-4a41-a138-5b76c87224b5" />

# HOW TO FURTHER CUSTOMIZE THE APP
**This was an experiment I developed in Unity, so it's a bit unstable, and it might not work for everyone.** If you want to tinker with the settings or add expressions, VFX or other stuff; download the source files on the releases tab. The project uses **Unity 6000.3.21f**, download the unity version and open the project.

### Scripts
The main script in which i handle almost all the model and shape keys is called OSF_script. I you want to change some stuff that's where everything is.
The fastest way is to change the variables in the inspector, here's a quick rundown on what everything does:

<img width="686" height="824" alt="image" src="https://github.com/user-attachments/assets/1aa2a970-bc68-453f-8735-969ee182730b" />

**OpenSeeComponents** are just references in the scene of important components, open see tracks the data, open see components tracks expressions and the UI element reference.
Then there's **Meshes with blend shapes** as the name implies it's just references to the meshes so that the script can change the blend shapes. **Meshes to rotate** are the references to the different bones that the script rotates.
**Offsets** are just a bunch of vectors to fix the base rotation of the bones, **if the rotations look weird even after resetting them in the UI maybe changing these values a bit will work for you**.

**EYES** -Blink, wink and open threshold are the values from 0(closed) to 1(open) that the script checks to consider if the eye should be closed or open.
Deadzone is the minimum angle that the eye (looking direction) has to move to show that on the pupil, this variable's function is to avoid micro movements or shaking.
The fixed eye rotation angle is similar, it detects how much the eye should move to detect a change in direction in the fixed rotation eye tracking type.
Gaze and eyelid speed are the speed that the eye rotates and the eyelids move.
Use relative speed means that micro movements will move slower than more drastic eye movements, this is to avoid micro movements moving to fast (makes the eye shake too much). Relative speed max angle is used to check what movements are considered micro movements.

There are two types of eye tracking type: Fixed and Continuous. Continuous checks constantly the eye rotation and rotates Zoe's eyes accordingly. This one works well for me, but depending on your eyes or your set up it might glitch and the eyes might rotate weirdly every once in a while. If that happens change it to Fixed. This one just moves left, right, up and down. If it still doesn't work well just turn it off and put eye tracking to None.

<img width="673" height="613" alt="image" src="https://github.com/user-attachments/assets/5130a412-283d-432a-ab71-55af456ec820" />

**MOUTH**
Mouth Open Ratio is to change how much the mouth opens when you open it. Mouth speed changes how fast the mouth moves. Lip Sync is the lip sync component. I used **Hecomi's lip sync** -> https://github.com/hecomi/uLipSync If you want to customize it look at their repository info.

**EYEBROWS**
I've tried multiple times to get the eyebrows to work but, unfortunetly, without succes. So this section doesn't do anything, it's all commented in code, if you want to try it just uncomment this line.
<img width="1334" height="81" alt="image" src="https://github.com/user-attachments/assets/45336c1a-b5b8-4ab8-9b80-88b57610a668" />

**BODY**
Body speed changes the smoothness of the body tracking (if it's too low the body starts to shake). Body Rotation Ratio Continuous how much the body follows the head rotation. Squash Threshold Continuous how much the body has to move down so that the little squash animation plays.
Anim is just the animator reference. Only manual expressions: i'll explain more on the expressions section.

**VFX**
Just a bunch of vfx info, if you want to add more vfx i'll explain how in the VFX section.

# EXPRESIONS
Expressions are made in two ways:
## **MANUAL**: 
This is the default that I've gone with, mainly because the other way is a bit buggy. It just detects a key and makes the expression. To add more you have to do these steps:
  - 1: get the Zoe model, it's in the Assets\Zoe\3DModels\Zoe_Vtuber_Model.
  - 2: Import that model into blender or you software of choice, add a new shape key and edit it in any way you want.
  - 3: Reimport the model, if using blender make sure that you export into fbx with these settings
  - <img width="266" height="477" alt="image" src="https://github.com/user-attachments/assets/9c3a116d-d5d9-4c6e-9fe1-e480562624d8" />
  - 4: Replace the model on the Assets\Zoe\3DModels folder, same name, just go to the folder in your file explorer, delete the old model or rename it (I would             recommend renaming just in case you messed something up, then you have a backup).
  - 5: Go to the _Inputs folder open INPUTS and add a new action and key bind for the expression, save it.
  - <img width="982" height="425" alt="image" src="https://github.com/user-attachments/assets/4e9991b7-4c97-4a4e-bf9f-d57ea44c5266" />
  - 6: Open the OSF Script and scroll down to the INPUTS region (around ~ line 700), then copy one of the voids that are there, for example, copy this one:
  - <img width="886" height="308" alt="image" src="https://github.com/user-attachments/assets/fa5361c8-9899-47e5-a011-db9c68ad3f81" />
  - 7: Change the name of the function and expression, for example lets make it "Worry"
  - <img width="894" height="295" alt="image" src="https://github.com/user-attachments/assets/1d64743e-4739-441f-8c22-5911fba95e8e" />
  - 8: Below on the OnEnable() function add these two lines of code change the "Sad" part for whatever name you gave to the action on the INPUTS, and the OnSad           for wharever name you gave to the expression function.
  - <img width="537" height="59" alt="image" src="https://github.com/user-attachments/assets/071b910f-2be6-40b0-82cf-65dcf4c33ec9" />
  - 9: Let's go back to Unity, the Zoe_Vtuber_Model object in the hierarchy has a script called Blend Shapes, open that script and add a "public string ___" ___           being whatever name you want the variable to have.
  - 10: add the name of that the shape key has in blender or in the mesh into the script in the inspector.
  - <img width="692" height="31" alt="image" src="https://github.com/user-attachments/assets/4e92358f-0513-4c01-9cbb-8106639726f4" />
  - 11: Go back to the OSF Script and in the blend sphapes variable at the top of the script (line 131) add a new varaible of the same name but instead of a                string make it a "private static int ____"
  - 12: Go to the Blend Shape function section (line 583) and in the function GetBlendShapeIDs add a line substituting the BS_Surrised for whatever name you gave the shape key, also add that variable to the expressionList found below like this. Don't forget the comma between the last variable and the new one.
  - <img width="922" height="60" alt="image" src="https://github.com/user-attachments/assets/52ff784b-56f5-491c-bc4b-2857d0e5b730" />
  <img width="354" height="229" alt="image" src="https://github.com/user-attachments/assets/79330581-3af0-4519-a33f-1961fb16d632" />
  - 13: Go to the Expressions section (line 632) and in the function HandleExpressions(string _expression), in the switch statement, copy the last case and paste it changing the case string for whatever name you gave to the expression, in my case it was "worry" (be careful with lower and upper case), then in the if statement change the ex == ______ and put whatever name you gave to the shape key variable.
  - <img width="1316" height="268" alt="image" src="https://github.com/user-attachments/assets/58eedae4-a2f1-4a30-a410-f0bbc7a6a784" />
  -DONE
It should work after all that, I now there's a lot of steps but making a sistem for adding them with UI in App, while possible, would have taken a lot of work that I'd rather spent elsewhere. If you have any doubs you can message me in twitter/x (@hma_life) or email me (miguel.a.vida.cabezas@gmail.com).

## **AUTOMATIC DETECTION**:
automatic detection is a bit tricky and it can be buggy, but if you wan to try it just turn off **Only manual expressions** in the BODY section of the OSF_Script inspector. 
If you want to add more expressions, then follow all the steps avobe for the manual expressions except for all the ones related to the INPUT system. Then follow the Open See Face instructions on how to make expression detection work, it's all explained on their github -> https://github.com/emilianavt/OpenSeeFace

# MATERIALS
The materials are made using Shader graph and URP, you can find them in the Zoe, Materials folder and change them however you like.

# VRM / VseeFace
If you don't want to use the App I made, but you want to use the Zoe model; I've made the model compatible with VseeFace, just download the vsfavatar in the releases tab and plug it into VseeFace. There's also a VRM folder in the Zoe folder with the vrm0 I used to make the VseeFace avatar. You can also just use the Zoe_Vtuber_Model.fbx in the Zoe\3DModels folder and make the VRM yourself.


