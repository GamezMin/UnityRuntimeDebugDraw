/*
 * According to jagt's https://github.com/jagt/unity3d-runtime-debug-draw , this is a modified version of RuntimeDebugDraw 。
 * I Add Some New Features are added. eg: DrawBox, DrawCircle, DrawSphere.
 * Runtime DrawLine/DrawRay /DrawBox/DrawCircle/DrawSphere / DrawText for DEBUGGING , that works in both Scene/Game view, also works in built PC/mobile builds.
 *
 *	Very Important Notes:
 *	1.	You are expected to make some changes in this file before intergrating this into you projects.
 *			a. Add 'RuntimeDebugDrawDriver.cs' to a GameObject in your scene.
 *			b. add `_DEBUG` symbol to your project's debugging symbol so these draw calls will be compiled away in final release builds.
 *				If you forget to do this, DrawXXX calls won't be shown.
 *			c.	`RuntimeDebugDraw.DrawLineLayer` is the layer the lines will be drawn on. If you have camera postprocessing turned on, set this to a layer that is ignored
 *				by the post processor.
 *			d.	`GetDebugDrawCamera()` will be called to get the camera for line drawings and text coordinate calcuation.
 *				It defaults to `Camera.main`, returning null will mute drawings.
 *			e.	`DrawTextDefaultSize`/`DrawDefaultColor` styling variables, defaults as Unity Debug.Draw.
 * 
 *	2.	Performance should be relatively ok for debugging,  but it's never intended for release use. You should use conditional to
 *		compile away these calls anyway. Additionally DrawText is implemented with OnGUI, which costs a lot on mobile devices.
 * 
 *	3.	Don't rename this file of 'RuntimeDebugDraw' or this won't work. This file contains a MonoBehavior also named 'RuntimeDebugDraw' and Unity needs this file
 *		to have the same name. If you really want to rename this file, remember to rename the 'RuntimeDebugDraw' class below too.
 * 
 *   GamezMin Providing Support, Thanks!
 */

using System;
using UnityEngine;
using VoxelGame.Internal;
using Conditional = System.Diagnostics.ConditionalAttribute;

namespace VoxelGame
{
	public static class RuntimeDebugDraw
	{
		#region Main Functions
		/// <summary>
		/// Which layer the lines will be drawn on.
		/// </summary>
		public const int DrawLineLayer = 4;

		/// <summary>
		/// Default font size for DrawText.
		/// </summary>
		public const int DrawTextDefaultSize = 12;

		/// <summary>
		/// Default color for Draws.
		/// </summary>
		public static Color DrawDefaultColor = Color.white;
		
		/// <summary>
		/// Default color for Draws.
		/// </summary>
		public static Color DrawCircleDefaultColor = Color.yellow;

		/// <summary>
		///	Which camera to use for line drawing and texts coordinate calculation.
		/// </summary>
		/// <returns>Camera to debug draw on, returns null will mute debug drawing.</returns>
		public static Camera GetDebugDrawCamera()
		{
			return Camera.main;
		}

		/// <summary>
		///	Draw a line from <paramref name="start"/> to <paramref name="end"/> with <paramref name="color"/>.
		/// </summary>
		/// <param name="start">Point in world space where the line should start.</param>
		/// <param name="end">Point in world space where the line should end.</param>
		/// <param name="color">Color of the line.</param>
		/// <param name="duration">How long the line should be visible for.</param>
		/// <param name="depthTest">Should the line be obscured by objects closer to the camera?</param>
		[Conditional("_DEBUG")]
		public static void DrawLine(Vector3 start, Vector3 end, Color color, float duration, bool depthTest)
		{
			CheckAndBuildHiddenRTDrawObject();
			_rtDrawDriver.RegisterLine(start, end, color, duration, !depthTest);
		}

		/// <summary>
		/// Draws a line from start to start + dir in world coordinates.
		/// </summary>
		/// <param name="start">Point in world space where the ray should start.</param>
		/// <param name="dir">Direction and length of the ray.</param>
		/// <param name="color">Color of the drawn line.</param>
		/// <param name="duration">How long the line will be visible for (in seconds).</param>
		/// <param name="depthTest">Should the line be obscured by other objects closer to the camera?</param>
		[Conditional("_DEBUG")]
		public static void DrawRay(Vector3 start, Vector3 dir, Color color, float duration, bool depthTest)
		{
			CheckAndBuildHiddenRTDrawObject();
			_rtDrawDriver.RegisterLine(start, start + dir, color, duration, !depthTest);
		}

		/// <summary>
		/// Draw a text at given position.
		/// </summary>
		/// <param name="pos">Position</param>
		/// <param name="text">String of the text.</param>
		/// <param name="color">Color for the text.</param>
		/// <param name="size">Font size for the text.</param>
		/// <param name="duration">How long the text should be visible for.</param>
		/// <param name="popUp">Set to true to let the text moving up, so multiple texts at the same position can be visible.</param>
		[Conditional("_DEBUG")]
		public static void DrawText(Vector3 pos, string text, Color color, int size, float duration, bool popUp)
		{
			CheckAndBuildHiddenRTDrawObject();
			_rtDrawDriver.RegisterDrawText(pos, text, color, size, duration, popUp);
		}

		/// <summary>
		/// Attach text to a transform.
		/// </summary>
		/// <param name="transform">Target transform to attach text to.</param>
		/// <param name="strFunc">Function will be called on every frame to get a string as attached text. </param>
		/// <param name="offset">Text attach offset to transform position.</param>
		/// <param name="color">Color for the text.</param>
		/// <param name="size">Font size for the text.</param>
		[Conditional("_DEBUG")]
		public static void AttachText(Transform transform, Func<string> strFunc, Vector3 offset, Color color, int size)
		{
			CheckAndBuildHiddenRTDrawObject();
			_rtDrawDriver.RegisterAttachText(transform, strFunc, offset, color, size);
		}
		
		#endregion

		#region Overloads
		/*
		 *	These are tons of overloads following how 'Debug.DrawXXX' are overloaded.
		 */

		/// <summary>
		///	Draw a line from <paramref name="start"/> to <paramref name="end"/> with <paramref name="color"/>.
		/// </summary>
		/// <param name="start">Point in world space where the line should start.</param>
		/// <param name="end">Point in world space where the line should end.</param>
		[Conditional("_DEBUG")]
		public static void DrawLine(Vector3 start, Vector3 end)
		{
			DrawLine(start, end, DrawDefaultColor, 0f, true);
		}

		/// <summary>
		///	Draw a line from <paramref name="start"/> to <paramref name="end"/> with <paramref name="color"/>.
		/// </summary>
		/// <param name="start">Point in world space where the line should start.</param>
		/// <param name="end">Point in world space where the line should end.</param>
		/// <param name="color">Color of the line.</param>
		[Conditional("_DEBUG")]
		public static void DrawLine(Vector3 start, Vector3 end, Color color)
		{
			DrawLine(start, end, color, 0f, true);
		}

		/// <summary>
		///	Draw a line from <paramref name="start"/> to <paramref name="end"/> with <paramref name="color"/>.
		/// </summary>
		/// <param name="start">Point in world space where the line should start.</param>
		/// <param name="end">Point in world space where the line should end.</param>
		/// <param name="color">Color of the line.</param>
		/// <param name="duration">How long the line should be visible for.</param>
		[Conditional("_DEBUG")]
		public static void DrawLine(Vector3 start, Vector3 end, Color color, float duration)
		{
			DrawLine(start, end, color, duration, true);
		}

		/// <summary>
		/// Draws a line from start to start + dir in world coordinates.
		/// </summary>
		/// <param name="start">Point in world space where the ray should start.</param>
		/// <param name="dir">Direction and length of the ray.</param>
		[Conditional("_DEBUG")]
		public static void DrawRay(Vector3 start, Vector3 dir)
		{
			DrawRay(start, dir, DrawDefaultColor, 0f, true);
		}

		/// <summary>
		/// Draws a line from start to start + dir in world coordinates.
		/// </summary>
		/// <param name="start">Point in world space where the ray should start.</param>
		/// <param name="dir">Direction and length of the ray.</param>
		/// <param name="color">Color of the drawn line.</param>
		[Conditional("_DEBUG")]
		public static void DrawRay(Vector3 start, Vector3 dir, Color color)
		{
			DrawRay(start, dir, color, 0f, true);
		}

		/// <summary>
		/// Draws a line from start to start + dir in world coordinates.
		/// </summary>
		/// <param name="start">Point in world space where the ray should start.</param>
		/// <param name="dir">Direction and length of the ray.</param>
		/// <param name="color">Color of the drawn line.</param>
		/// <param name="duration">How long the line will be visible for (in seconds).</param>
		[Conditional("_DEBUG")]
		public static void DrawRay(Vector3 start, Vector3 dir, Color color, float duration)
		{
			DrawRay(start, dir, color, duration, true);
		}

		/// <summary>
		/// Draw a text at given position.
		/// </summary>
		/// <param name="pos">Position</param>
		/// <param name="text">String of the text.</param>
		[Conditional("_DEBUG")]
		public static void DrawText(Vector3 pos, string text)
		{
			DrawText(pos, text, DrawDefaultColor, DrawTextDefaultSize, 0f, false);
		}

		/// <summary>
		/// Draw a text at given position.
		/// </summary>
		/// <param name="pos">Position</param>
		/// <param name="text">String of the text.</param>
		/// <param name="color">Color for the text.</param>
		[Conditional("_DEBUG")]
		public static void DrawText(Vector3 pos, string text, Color color)
		{
			DrawText(pos, text, color, DrawTextDefaultSize, 0f, false);
		}

		/// <summary>
		/// Draw a text at given position.
		/// </summary>
		/// <param name="pos">Position</param>
		/// <param name="text">String of the text.</param>
		/// <param name="color">Color for the text.</param>
		/// <param name="size">Font size for the text.</param>
		[Conditional("_DEBUG")]
		public static void DrawText(Vector3 pos, string text, Color color, int size)
		{
			DrawText(pos, text, color, size, 0f, false);
		}

		/// <summary>
		/// Draw a text at given position.
		/// </summary>
		/// <param name="pos">Position</param>
		/// <param name="text">String of the text.</param>
		/// <param name="color">Color for the text.</param>
		/// <param name="size">Font size for the text.</param>
		/// <param name="duration">How long the text should be visible for.</param>
		[Conditional("_DEBUG")]
		public static void DrawText(Vector3 pos, string text, Color color, int size, float duration)
		{
			DrawText(pos, text, color, size, duration, false);
		}

		/// <summary>
		/// Attach text to a transform.
		/// </summary>
		/// <param name="transform">Target transform to attach text to.</param>
		/// <param name="strFunc">Function will be called on every frame to get a string as attached text. </param>
		[Conditional("_DEBUG")]
		public static void AttachText(Transform transform, Func<string> strFunc)
		{
			AttachText(transform, strFunc, Vector3.zero, DrawDefaultColor, DrawTextDefaultSize);
		}

		/// <summary>
		/// Attach text to a transform.
		/// </summary>
		/// <param name="transform">Target transform to attach text to.</param>
		/// <param name="strFunc">Function will be called on every frame to get a string as attached text. </param>
		/// <param name="offset">Text attach offset to transform position.</param>
		[Conditional("_DEBUG")]
		public static void AttachText(Transform transform, Func<string> strFunc, Vector3 offset)
		{
			AttachText(transform, strFunc, offset, DrawDefaultColor, DrawTextDefaultSize);
		}

		/// <summary>
		/// Attach text to a transform.
		/// </summary>
		/// <param name="transform">Target transform to attach text to.</param>
		/// <param name="strFunc">Function will be called on every frame to get a string as attached text. </param>
		/// <param name="offset">Text attach offset to transform position.</param>
		/// <param name="color">Color for the text.</param>
		[Conditional("_DEBUG")]
		public static void AttachText(Transform transform, Func<string> strFunc, Vector3 offset, Color color)
		{
			AttachText(transform, strFunc, offset, color, DrawTextDefaultSize);
		}
		#endregion
		
		// Add these new methods to the public static Draw class
		#region DrawBox
		/// <summary>
		/// Draw a wireframe box at given position, size and rotation.
		/// </summary>
		/// <param name="center">Center of the box in world space.</param>
		/// <param name="size">Dimensions of the box.</param>
		/// <param name="rotation">Rotation of the box.</param>
		/// <param name="color">Color of the box.</param>
		/// <param name="duration">How long the box should be visible for.</param>
		/// <param name="depthTest">Should the box be obscured by objects closer to the camera?</param>
		[Conditional("_DEBUG")]
		public static void DrawBox(Vector3 center, Vector3 size, Quaternion rotation, Color color, float duration, bool depthTest)
		{
		    CheckAndBuildHiddenRTDrawObject();
		    _rtDrawDriver.RegisterBox(center, size, rotation, color, duration, !depthTest);
		}

		[Conditional("_DEBUG")]
		public static void DrawBox(Vector3 center, Vector3 size, Quaternion rotation, Color color, float duration)
		{
		    DrawBox(center, size, rotation, color, duration, true);
		}

		[Conditional("_DEBUG")]
		public static void DrawBox(Vector3 center, Vector3 size, Quaternion rotation, Color color)
		{
		    DrawBox(center, size, rotation, color, 0f, true);
		}

		[Conditional("_DEBUG")]
		public static void DrawBox(Vector3 center, Vector3 size, Quaternion rotation)
		{
		    DrawBox(center, size, rotation, DrawDefaultColor, 0f, true);
		}

		[Conditional("_DEBUG")]
		public static void DrawBox(Vector3 center, Vector3 size, Color color, float duration, bool depthTest)
		{
		    DrawBox(center, size, Quaternion.identity, color, duration, depthTest);
		}

		[Conditional("_DEBUG")]
		public static void DrawBox(Vector3 center, Vector3 size, Color color, float duration)
		{
		    DrawBox(center, size, Quaternion.identity, color, duration, true);
		}

		[Conditional("_DEBUG")]
		public static void DrawBox(Vector3 center, Vector3 size, Color color)
		{
		    DrawBox(center, size, Quaternion.identity, color, 0f, true);
		}

		[Conditional("_DEBUG")]
		public static void DrawBox(Vector3 center, Vector3 size)
		{
		    DrawBox(center, size, Quaternion.identity, DrawDefaultColor, 0f, true);
		}
		#endregion
		
		
		#region Circle Drawing

		
		public static void DrawCircle(Vector3 position, float radius, int increments = 8)
		{
			DrawCircleZ(position, radius, increments);
		}

		public static void DrawCircleX(Vector3 position, float radius, int increments = 8)
		{
			DrawCircleAxis(position, Vector3.up * radius, Vector3.forward * radius, increments);
		}

		public static void DrawCircleZ(Vector3 position, float radius, int increments = 8)
		{
			DrawCircleAxis(position, Vector3.right * radius, Vector3.forward * radius, increments);
		}

		public static void DrawCircleY(Vector3 position, float radius, int increments = 8)
		{
			DrawCircleAxis(position, Vector3.right * radius, Vector3.up * radius, increments);
		}

		public static void DrawCircle(Vector3 position, float radius, Vector3 normal, int increments = 8)
		{
			Quaternion quaternion = Quaternion.LookRotation(normal);
			Vector3 vector = quaternion * Vector3.right;
			Vector3 vector2 = quaternion * Vector3.up;
			DrawCircleAxis(position, vector * radius, vector2 * radius, increments);
		}
		

		/// <summary>
		/// Draw a circle defined by two axes.
		/// </summary>
		/// <param name="position">Center position of the circle.</param>
		/// <param name="axisX">First axis defining the plane of the circle.</param>
		/// <param name="axisY">Second axis defining the plane of the circle.</param>
		/// <param name="increments">Number of segments to use for drawing the circle (default 8).</param>
		[Conditional("_DEBUG")]
		public static void DrawCircleAxis(Vector3 position, Vector3 axisX, Vector3 axisY, int increments = 8)
		{
			DrawCircle(position, axisX, axisY, DrawCircleDefaultColor, 0f, true, increments);
		}

		// Overload with color parameter
		[Conditional("_DEBUG")]
		public static void DrawCircleAxis(Vector3 position, Vector3 axisX, Vector3 axisY, Color color, int increments = 8)
		{
			DrawCircle(position, axisX, axisY, color, 0f, true, increments);
		}

		// Overload with duration parameter
		[Conditional("_DEBUG")]
		public static void DrawCircleAxis(Vector3 position, Vector3 axisX, Vector3 axisY, float duration, int increments = 8)
		{
			DrawCircle(position, axisX, axisY, DrawCircleDefaultColor, duration, true, increments);
		}

		// Overload with all parameters
		[Conditional("_DEBUG")]
		public static void DrawCircleAxis(Vector3 position, Vector3 axisX, Vector3 axisY, Color color, float duration, bool depthTest, int increments = 8)
		{
			DrawCircle(position, axisX, axisY, color, duration, depthTest, increments);
		}

		// Core circle drawing method
		[Conditional("_DEBUG")]
		private static void DrawCircle(Vector3 position, Vector3 axisX, Vector3 axisY, Color color, float duration, bool depthTest, int increments)
		{
			CheckAndBuildHiddenRTDrawObject();
			_rtDrawDriver.RegisterCircle(position, axisX, axisY, color, duration, !depthTest, increments);
		}

		#endregion
		
		
		#region Sphere Drawing

		/// <summary>
		/// Draw a wireframe sphere.
		/// </summary>
		/// <param name="center">Center of the sphere in world space.</param>
		/// <param name="radius">Radius of the sphere.</param>
		/// <param name="color">Color of the sphere.</param>
		/// <param name="duration">How long the sphere should be visible for.</param>
		/// <param name="depthTest">Should the sphere be obscured by objects closer to the camera?</param>
		/// <param name="segments">Number of segments per circle (default 16).</param>
		[Conditional("_DEBUG")]
		public static void DrawSphere(Vector3 center, float radius, Color color, float duration, bool depthTest, int segments = 16)
		{
			CheckAndBuildHiddenRTDrawObject();
			_rtDrawDriver.RegisterSphere(center, radius, color, duration, !depthTest, segments);
		}

		// Overload with default segments
		[Conditional("_DEBUG")]
		public static void DrawSphere(Vector3 center, float radius, Color color, float duration, bool depthTest)
		{
			DrawSphere(center, radius, color, duration, depthTest, 16);
		}

		// Overload with duration only
		[Conditional("_DEBUG")]
		public static void DrawSphere(Vector3 center, float radius, float duration, bool depthTest, int segments = 16)
		{
			DrawSphere(center, radius, DrawDefaultColor, duration, depthTest, segments);
		}

		// Overload with color only
		[Conditional("_DEBUG")]
		public static void DrawSphere(Vector3 center, float radius, Color color, int segments = 16)
		{
			DrawSphere(center, radius, color, 0f, true, segments);
		}

		// Overload with default parameters
		[Conditional("_DEBUG")]
		public static void DrawSphere(Vector3 center, float radius)
		{
			DrawSphere(center, radius, DrawDefaultColor, 0f, true, 16);
		}

		#endregion

		#region Internal
		/// <summary>
		/// Singleton RuntimeDebugDraw component that is needed to call Unity APIs.
		/// </summary>
		private static Internal.RuntimeDebugDrawDriver _rtDrawDriver;

		/// <summary>
		/// Check and build 
		/// </summary>
		private static string HIDDEN_GO_NAME = "________HIDDEN_C4F6A87F298241078E21C0D7C1D87A76_";
		private static void CheckAndBuildHiddenRTDrawObject()
		{
			if (_rtDrawDriver != null)
				return;

			//	try reuse existing one first
			_rtDrawDriver =  GameObject.FindObjectOfType<RuntimeDebugDrawDriver>();
			if (_rtDrawDriver != null)
				return;

			//	instantiate an hidden gameobject w/ RuntimeDebugDraw attached.
			//	hardcode an GUID in the name so one won't accidentally get this by name.
			var go = new GameObject(HIDDEN_GO_NAME);
			var childGo = new GameObject(HIDDEN_GO_NAME);
			childGo.transform.parent = go.transform;
			_rtDrawDriver = childGo.AddComponent<RuntimeDebugDrawDriver>();
			//	hack to only hide outer go, so that RuntimeDebugDraw's OnGizmos will work properly.
			go.hideFlags = HideFlags.HideAndDontSave;
			if (Application.isPlaying)
				GameObject.DontDestroyOnLoad(go);
			
		}
		#endregion
	}
	
}