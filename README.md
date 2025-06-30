# UnityRuntimeDebugDraw
According to jagt's https://github.com/jagt/unity3d-runtime-debug-draw , this is a modified version of RuntimeDebugDraw .

I Add Some New Features are added. eg: DrawBox, DrawCircle, DrawSphere.

Features: Runtime DrawLine/DrawRay /DrawBox/DrawCircle/DrawSphere / DrawText for DEBUGGING , that works in both Scene/Game view, also works in built PC/mobile builds.


Very Important Notes:
1.	You are expected to make some changes in this file before intergrating this into you projects.
    a. Add 'RuntimeDebugDrawDriver.cs' to a GameObject in your scene.
    b. add `_DEBUG` symbol to your project's debugging symbol so these draw calls will be compiled away in final release builds.
        If you forget to do this, DrawXXX calls won't be shown.
    c.	`RuntimeDebugDraw.DrawLineLayer` is the layer the lines will be drawn on. If you have camera postprocessing turned on, set this to a layer that is ignored
        by the post processor.
    d.	`GetDebugDrawCamera()` will be called to get the camera for line drawings and text coordinate calcuation.
        It defaults to `Camera.main`, returning null will mute drawings.
    e.	`DrawTextDefaultSize`/`DrawDefaultColor` styling variables, defaults as Unity Debug.Draw.

2.	Performance should be relatively ok for debugging,  but it's never intended for release use. You should use conditional to
compile away these calls anyway. Additionally DrawText is implemented with OnGUI, which costs a lot on mobile devices.

3.	Don't rename this file of 'RuntimeDebugDraw' or this won't work. This file contains a MonoBehavior also named 'RuntimeDebugDraw' and    Unity needs this file
to have the same name. If you really want to rename this file, remember to rename the 'RuntimeDebugDraw' class below too.
