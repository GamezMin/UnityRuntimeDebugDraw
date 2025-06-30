# UnityRuntimeDebugDraw
According to jagt's https://github.com/jagt/unity3d-runtime-debug-draw , this is a modified version of RuntimeDebugDraw .
Runtime DrawLine/DrawRay /DrawBox/DrawCircle/DrawSphere / DrawText for DEBUGGING , that works in both Scene/Game view, also works in built PC/mobile builds.

I add some new features, eg: DrawBox, DrawCircle, DrawSphere.

# How to USE
1. Add 'RuntimeDebugDrawDriver.cs' to a GameObject in your scene.
2. Add `_DEBUG` symbol to your project's debugging symbol so these draw calls will be compiled away in final release builds.
3. Have fun!

# Show Case
![image](https://github.com/user-attachments/assets/875c4d8b-1b22-43ee-9ee8-73ad196eb3b8)
