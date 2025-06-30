using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelGame.Internal
{
	internal class RuntimeDebugDrawDriver : MonoBehaviour
	{
		#region Basics
		private void CheckInitialized()
		{
			//	as RuntimeDebugDraw component has a very low execution order, other script might Awake()
			//	earlier than this and at that moment it's not initialized. check and init on every public
			//	member
			if (_drawTextEntries == null)
			{
				_ZTestBatch = new BatchedLineDraw(depthTest: true);
				_AlwaysBatch = new BatchedLineDraw(depthTest: false);
				_lineEntries = new List<DrawLineEntry>(16);
				_boxEntries = new List<DrawBoxEntry>(32);
				_circleEntries = new List<DrawCircleEntry>(32); // 初始化圆条目
				_sphereEntries = new List<DrawSphereEntry>(32); // 初始化球体条目

				_textStyle = new GUIStyle();
				_textStyle.alignment = TextAnchor.UpperLeft;
				_drawTextEntries = new List<DrawTextEntry>(16);
				_attachTextEntries = new List<AttachTextEntry>(16);
			}
		}

		private void Awake()
		{
			CheckInitialized();
		}

		private void OnGUI()
		{
			DrawTextOnGUI();
		}

		private void LateUpdate()
		{
			TickAndDrawLines();
			TickTexts();
		}

		private void OnDestroy()
		{
			_AlwaysBatch.Dispose();
			_ZTestBatch.Dispose();
		}

		private void Clear()
		{
			_drawTextEntries.Clear();
			_lineEntries.Clear();
			_linesNeedRebuild = true;
		}
		#endregion

		#region Draw Lines
		private class DrawLineEntry
		{
			public bool occupied;
			public Vector3 start;
			public Vector3 end;
			public Color color;
			public float timer;
			public bool noZTest;
		}

		private List<DrawLineEntry> _lineEntries;

		//	helper class for batching
		private class BatchedLineDraw : IDisposable
		{
			public Mesh mesh;
			public Material mat;

			private List<Vector3> _vertices;
			private List<Color> _colors;
			private List<int> _indices;

			public BatchedLineDraw(bool depthTest)
			{
				mesh = new Mesh();
				mesh.MarkDynamic();

				//	relying on a builtin shader, but it shouldn't change that much.
				mat = new Material(Shader.Find("Hidden/Internal-Colored"));
				mat.SetInt("_ZTest", depthTest 
					? 4	// LEqual
					: 0	// Always
					);

				_vertices = new List<Vector3>();
				_colors = new List<Color>();
				_indices = new List<int>();
				
			}

			public void AddLine(Vector3 from, Vector3 to, Color color)
			{
				_vertices.Add(from);
				_vertices.Add(to);
				_colors.Add(color);
				_colors.Add(color);
				int verticeCount = _vertices.Count;
				_indices.Add(verticeCount - 2);
				_indices.Add(verticeCount - 1);
				
			}

			public void Clear()
			{
				mesh.Clear();
				_vertices.Clear();
				_colors.Clear();
				_indices.Clear();
				
			}

			public void BuildBatch()
			{
				mesh.SetVertices(_vertices);
				mesh.SetColors(_colors);
				mesh.SetIndices(_indices.ToArray(), MeshTopology.Lines, 0);	// cant get rid of this alloc for now
				
			}

			public void Dispose()
			{
				GameObject.DestroyImmediate(mesh);
				GameObject.DestroyImmediate(mat);
				
			}
		}

		private BatchedLineDraw _ZTestBatch;
		private BatchedLineDraw _AlwaysBatch;
		private bool _linesNeedRebuild;
		

		public void RegisterLine(Vector3 start, Vector3 end, Color color, float timer, bool noZTest)
		{
			CheckInitialized();

			DrawLineEntry entry = null;
			for (int ix = 0; ix < _lineEntries.Count; ix++)
			{
				if (!_lineEntries[ix].occupied)
				{
					entry = _lineEntries[ix];
					break;
				}
			}
			if (entry == null)
			{
				entry = new DrawLineEntry();
				_lineEntries.Add(entry);
			}

			entry.occupied = true;
			entry.start = start;
			entry.end = end;
			entry.color = color;
			entry.timer = timer;
			entry.noZTest = noZTest;
			_linesNeedRebuild = true;
			
		}

		private void RebuildDrawLineBatchMesh()
		{
			_ZTestBatch.Clear();
			_AlwaysBatch.Clear();

			for (int ix = 0; ix < _lineEntries.Count; ix++)
			{
				var entry = _lineEntries[ix];
				if (!entry.occupied)
					continue;

				if (entry.noZTest)
					_AlwaysBatch.AddLine(entry.start, entry.end, entry.color);
				else
					_ZTestBatch.AddLine(entry.start, entry.end, entry.color);
			}
		
			
			// Process box entries
			for (int ix = 0; ix < _boxEntries.Count; ix++)
			{
				var box = _boxEntries[ix];
				if (!box.occupied)
					continue;

				if (box.noZTest)
					AddBoxToBatch(box.center, box.size, box.rotation, box.color, _AlwaysBatch);
				else
					AddBoxToBatch(box.center, box.size, box.rotation, box.color, _ZTestBatch);
			}
			
			
			// Process circle entries
			for (int ix = 0; ix < _circleEntries.Count; ix++)
			{
				var circle = _circleEntries[ix];
				if (!circle.occupied)
					continue;

				if (circle.noZTest)
					AddCircleToBatch(circle, _AlwaysBatch);
				else
					AddCircleToBatch(circle, _ZTestBatch);
			}
			
			// Process sphere entries
			for (int ix = 0; ix < _sphereEntries.Count; ix++)
			{
				var sphere = _sphereEntries[ix];
				if (!sphere.occupied)
					continue;

				if (sphere.noZTest)
					AddSphereToBatch(sphere, _AlwaysBatch);
				else
					AddSphereToBatch(sphere, _ZTestBatch);
			}
			
			
			_ZTestBatch.BuildBatch();
			_AlwaysBatch.BuildBatch();
			
		}

		private void TickAndDrawLines()
		{
			if (_linesNeedRebuild)
			{
				RebuildDrawLineBatchMesh();
				_linesNeedRebuild = false;
			}

			//	draw on UI layer which should bypass most postFX setups
			Graphics.DrawMesh(_AlwaysBatch.mesh, Vector3.zero, Quaternion.identity, _AlwaysBatch.mat, layer: RuntimeDebugDraw.DrawLineLayer ,camera : null, submeshIndex : 0,properties: null, castShadows : false, receiveShadows : false);
			Graphics.DrawMesh(_ZTestBatch.mesh, Vector3.zero, Quaternion.identity, _ZTestBatch.mat, layer: RuntimeDebugDraw.DrawLineLayer ,camera : null, submeshIndex : 0,properties: null, castShadows : false, receiveShadows : false);

			//	update timer late so every added entry can be drawed for at least one frame
			for (int ix = 0; ix < _lineEntries.Count; ix++)
			{
				var entry = _lineEntries[ix];
				if (!entry.occupied)
					continue;
				entry.timer -= Time.deltaTime;
				if (entry.timer < 0)
				{
					entry.occupied = false;
					_linesNeedRebuild = true;
				}
			}
		
			// Update box timers
			for (int ix = 0; ix < _boxEntries.Count; ix++)
			{
				var box = _boxEntries[ix];
				if (!box.occupied)
					continue;

				box.timer -= Time.deltaTime;
				if (box.timer < 0)
				{
					box.occupied = false;
					_linesNeedRebuild = true;
				}
			}
			
			// Update circle timers
			for (int ix = 0; ix < _circleEntries.Count; ix++)
			{
				var circle = _circleEntries[ix];
				if (!circle.occupied)
					continue;

				circle.timer -= Time.deltaTime;
				if (circle.timer < 0)
				{
					circle.occupied = false;
					_linesNeedRebuild = true;
				}
			}
			
			
			// Update sphere timers
			for (int ix = 0; ix < _sphereEntries.Count; ix++)
			{
				var sphere = _sphereEntries[ix];
				if (!sphere.occupied)
					continue;

				sphere.timer -= Time.deltaTime;
				if (sphere.timer < 0)
				{
					sphere.occupied = false;
					_linesNeedRebuild = true;
				}
			}
			
			return;
		}
		#endregion

		#region Draw Text
		[Flags]
		public enum DrawFlag : byte
		{
			None		= 0,
			DrawnGizmo	= 1 << 0,
			DrawnGUI	= 1 << 1,
			DrawnWireBox = 1 << 2,
			DrawnAll	= DrawnGizmo | DrawnGUI | DrawnWireBox
		}

		private class DrawTextEntry
		{
			public bool occupied;
			public GUIContent content;
			public Vector3 anchor;
			public int size;
			public Color color;
			public float timer;
			public bool popUp;
			public float duration;

			//	Text entries needs to be draw in both OnGUI/OnDrawGizmos, need flags for mark
			//	has been visited by both
			public DrawFlag flag = DrawFlag.None;

			public DrawTextEntry()
			{
				content = new GUIContent();
				return;
			}
		}

		private class AttachTextEntry
		{
			public bool occupied;
			public GUIContent content;
			public Vector3 offset;
			public int size;
			public Color color;


			public Transform transform;
			public Func<string> strFunc;

			public DrawFlag flag = DrawFlag.None;

			public AttachTextEntry()
			{
				content = new GUIContent();
				return;
			}
		}

		private List<DrawTextEntry> _drawTextEntries;
		private List<AttachTextEntry> _attachTextEntries;
		private GUIStyle _textStyle;

		public void RegisterDrawText(Vector3 anchor, string text, Color color, int size, float timer, bool popUp)
		{
			CheckInitialized();

			DrawTextEntry entry = null;
			for (int ix = 0; ix < _drawTextEntries.Count; ix++)
			{
				if (!_drawTextEntries[ix].occupied)
				{
					entry = _drawTextEntries[ix];
					break;
				}
			}
			if (entry == null)
			{
				entry = new DrawTextEntry();
				_drawTextEntries.Add(entry);
			}

			entry.occupied = true;
			entry.anchor = anchor;
			entry.content.text = text;
			entry.size = size;
			entry.color = color;
			entry.duration = entry.timer = timer;
			entry.popUp = popUp;
#if UNITY_EDITOR
			entry.flag = DrawFlag.None;
#else
			//	in builds consider gizmo is already drawn
			entry.flag = DrawFlag.DrawnGizmo;
#endif
		}

		public void RegisterAttachText(Transform target, Func<string> strFunc, Vector3 offset, Color color, int size)
		{
			CheckInitialized();
		
			AttachTextEntry entry = null;
			for (int ix = 0; ix < _attachTextEntries.Count; ix++)
			{
				if (!_attachTextEntries[ix].occupied)
				{
					entry = _attachTextEntries[ix];
					break;
				}
			}
			if (entry == null)
			{
				entry = new AttachTextEntry();
				_attachTextEntries.Add(entry);
			}

			entry.occupied = true;
			entry.offset = offset;
			entry.transform = target;
			entry.strFunc = strFunc;
			entry.color = color;
			entry.size = size;
			//	get first text
			entry.content.text = strFunc();
#if UNITY_EDITOR
			entry.flag = DrawFlag.None;
#else
			//	in builds consider gizmo is already drawn
			entry.flag = DrawFlag.DrawnGizmo;
#endif
			
		}

		private void TickTexts()
		{
			for (int ix = 0; ix < _drawTextEntries.Count; ix++)
			{
				var entry = _drawTextEntries[ix];
				if (!entry.occupied)
					continue;
				entry.timer -= Time.deltaTime;
				if (entry.flag == DrawFlag.DrawnAll)
				{
					if (entry.timer < 0)
					{
						entry.occupied = false;
					}
					//	actually no need to tick DrawFlag as it won't move
				}
			}

			for (int ix = 0; ix < _attachTextEntries.Count; ix++)
			{
				var entry = _attachTextEntries[ix];
				if (!entry.occupied)
					continue;
				if (entry.transform == null)
				{
					entry.occupied = false;
					entry.strFunc = null;	// needs to release ref to callback
				}
				else if (entry.flag == DrawFlag.DrawnAll)
				{
					// tick content
					entry.content.text = entry.strFunc();
					// tick flag
#if UNITY_EDITOR
					entry.flag = DrawFlag.None;
#else
					//	in builds consider gizmo is already drawn
					entry.flag = DrawFlag.DrawnGizmo;
#endif
				}
			}
			
		}

		private void DrawTextOnGUI()
		{
			var camera = RuntimeDebugDraw.GetDebugDrawCamera();
			if (camera == null)
				return;

			for (int ix = 0; ix < _drawTextEntries.Count; ix++)
			{
				var entry = _drawTextEntries[ix];
				if (!entry.occupied)
					continue;

				GUIDrawTextEntry(camera, entry);
				entry.flag |= DrawFlag.DrawnGUI;
			}

			for (int ix = 0; ix < _attachTextEntries.Count; ix++)
			{
				var entry = _attachTextEntries[ix];
				if (!entry.occupied)
					continue;

				GUIAttachTextEntry(camera, entry);
				entry.flag |= DrawFlag.DrawnGUI;
			}
			
		}

		private void GUIDrawTextEntry(Camera camera, DrawTextEntry entry)
		{
			Vector3 worldPos = entry.anchor;
			Vector3 screenPos = camera.WorldToScreenPoint(worldPos);
			screenPos.y = Screen.height - screenPos.y;

			if (entry.popUp)
			{
				float ratio = entry.timer / entry.duration;
				screenPos.y -=  (1 - ratio * ratio) * entry.size * 1.5f;
			}

			_textStyle.normal.textColor = entry.color;
			_textStyle.fontSize = entry.size;
			Rect rect = new Rect(screenPos, _textStyle.CalcSize(entry.content));
			GUI.Label(rect, entry.content, _textStyle);
			
		}

		private void GUIAttachTextEntry(Camera camera, AttachTextEntry entry)
		{
			if (entry.transform == null)
				return;

			Vector3 worldPos = entry.transform.position + entry.offset;
			Vector3 screenPos = camera.WorldToScreenPoint(worldPos);
			screenPos.y = Screen.height - screenPos.y;

			_textStyle.normal.textColor = entry.color;
			_textStyle.fontSize = entry.size;
			Rect rect = new Rect(screenPos, _textStyle.CalcSize(entry.content));
			GUI.Label(rect, entry.content, _textStyle);
			
		}


#if UNITY_EDITOR
		private void DrawTextOnDrawGizmos()
		{
			if (!(Camera.current == RuntimeDebugDraw.GetDebugDrawCamera()
				|| Camera.current == UnityEditor.SceneView.lastActiveSceneView.camera))
				return;

			var camera = Camera.current;
			if (camera == null)
				return;

			UnityEditor.Handles.BeginGUI();
			for (int ix = 0; ix < _drawTextEntries.Count; ix++)
			{
				var entry = _drawTextEntries[ix];
				if (!entry.occupied)
					continue;

				GUIDrawTextEntry(camera, entry);
				entry.flag |= DrawFlag.DrawnGizmo;
			}

			for (int ix = 0; ix < _attachTextEntries.Count; ix++)
			{
				var entry = _attachTextEntries[ix];
				if (!entry.occupied)
					continue;

				GUIAttachTextEntry(camera, entry);
				entry.flag |= DrawFlag.DrawnGizmo;
			}

			UnityEditor.Handles.EndGUI();
			
		}
#endif
		#endregion
		
		
		#region Box Drawing
		
		
		private class DrawBoxEntry
		{
		    public bool occupied;
		    public Vector3 center;
		    public Vector3 size;
		    public Quaternion rotation;
		    public Color color;
		    public float timer;
		    public bool noZTest;
		}

		private List<DrawBoxEntry> _boxEntries;

		public void RegisterBox(Vector3 center, Vector3 size, Quaternion rotation, Color color, float timer, bool noZTest)
		{
		    CheckInitialized();

		    DrawBoxEntry entry = null;
		    for (int ix = 0; ix < _boxEntries.Count; ix++)
		    {
		        if (!_boxEntries[ix].occupied)
		        {
		            entry = _boxEntries[ix];
		            break;
		        }
		    }
		    if (entry == null)
		    {
		        entry = new DrawBoxEntry();
		        _boxEntries.Add(entry);
		    }

		    entry.occupied = true;
		    entry.center = center;
		    entry.size = size;
		    entry.rotation = rotation;
		    entry.color = color;
		    entry.timer = timer;
		    entry.noZTest = noZTest;
		    _linesNeedRebuild = true;
		}

		private void AddBoxToBatch(Vector3 center, Vector3 size, Quaternion rotation, Color color, BatchedLineDraw batch)
		{
		    Vector3 halfSize = size * 0.5f;
		    
		    // Get local to world matrix
		    Matrix4x4 matrix = Matrix4x4.TRS(center, rotation, Vector3.one);
		    
		    // Calculate vertices relative to center
		    Vector3 frontTopLeft     = matrix.MultiplyPoint(new Vector3(-halfSize.x,  halfSize.y, -halfSize.z));
		    Vector3 frontTopRight    = matrix.MultiplyPoint(new Vector3( halfSize.x,  halfSize.y, -halfSize.z));
		    Vector3 frontBottomLeft  = matrix.MultiplyPoint(new Vector3(-halfSize.x, -halfSize.y, -halfSize.z));
		    Vector3 frontBottomRight = matrix.MultiplyPoint(new Vector3( halfSize.x, -halfSize.y, -halfSize.z));
		    
		    Vector3 backTopLeft      = matrix.MultiplyPoint(new Vector3(-halfSize.x,  halfSize.y, halfSize.z));
		    Vector3 backTopRight     = matrix.MultiplyPoint(new Vector3( halfSize.x,  halfSize.y, halfSize.z));
		    Vector3 backBottomLeft   = matrix.MultiplyPoint(new Vector3(-halfSize.x, -halfSize.y, halfSize.z));
		    Vector3 backBottomRight  = matrix.MultiplyPoint(new Vector3( halfSize.x, -halfSize.y, halfSize.z));

		    // Front face
		    batch.AddLine(frontTopLeft, frontTopRight, color);
		    batch.AddLine(frontTopRight, frontBottomRight, color);
		    batch.AddLine(frontBottomRight, frontBottomLeft, color);
		    batch.AddLine(frontBottomLeft, frontTopLeft, color);

		    // Back face
		    batch.AddLine(backTopLeft, backTopRight, color);
		    batch.AddLine(backTopRight, backBottomRight, color);
		    batch.AddLine(backBottomRight, backBottomLeft, color);
		    batch.AddLine(backBottomLeft, backTopLeft, color);

		    // Connecting edges
		    batch.AddLine(frontTopLeft, backTopLeft, color);
		    batch.AddLine(frontTopRight, backTopRight, color);
		    batch.AddLine(frontBottomRight, backBottomRight, color);
		    batch.AddLine(frontBottomLeft, backBottomLeft, color);
		}
#endregion


		#region Circle Drawing
		private class DrawCircleEntry
		{
			public bool occupied;
			public Vector3 position;
			public Vector3 axisX;
			public Vector3 axisY;
			public Color color;
			public float timer;
			public bool noZTest;
			public int increments;
		}

		private List<DrawCircleEntry> _circleEntries;

		public void RegisterCircle(Vector3 position, Vector3 axisX, Vector3 axisY, Color color, float duration, bool noZTest, int increments)
		{
			CheckInitialized();

			DrawCircleEntry entry = null;
			for (int ix = 0; ix < _circleEntries.Count; ix++)
			{
				if (!_circleEntries[ix].occupied)
				{
					entry = _circleEntries[ix];
					break;
				}
			}
			if (entry == null)
			{
				entry = new DrawCircleEntry();
				_circleEntries.Add(entry);
			}

			entry.occupied = true;
			entry.position = position;
			entry.axisX = axisX;
			entry.axisY = axisY;
			entry.color = color;
			entry.timer = duration;
			entry.noZTest = noZTest;
			entry.increments = increments;
			_linesNeedRebuild = true;
		}

		private void AddCircleToBatch(DrawCircleEntry circle, BatchedLineDraw batch)
		{
			const float TWO_PI = Mathf.PI * 2f;
			Vector3 lastPoint = circle.position + circle.axisX;
			Vector3 firstPoint = lastPoint;
		    
			for (int i = 1; i <= circle.increments; i++)
			{
				float angle = (i / (float)circle.increments) * TWO_PI;
				Vector3 point = circle.position + 
				                circle.axisX * Mathf.Cos(angle) + 
				                circle.axisY * Mathf.Sin(angle);
		        
				batch.AddLine(lastPoint, point, circle.color);
				lastPoint = point;
			}
		    
			// Connect back to the first point
			batch.AddLine(lastPoint, firstPoint, circle.color);
		}
		#endregion
		
		
		
		#region Sphere Drawing
		private class DrawSphereEntry
		{
		    public bool occupied;
		    public Vector3 center;
		    public float radius;
		    public Color color;
		    public float timer;
		    public bool noZTest;
		    public int segments;
		}

		private List<DrawSphereEntry> _sphereEntries;

		public void RegisterSphere(Vector3 center, float radius, Color color, float duration, bool noZTest, int segments)
		{
		    CheckInitialized();

		    DrawSphereEntry entry = null;
		    for (int ix = 0; ix < _sphereEntries.Count; ix++)
		    {
		        if (!_sphereEntries[ix].occupied)
		        {
		            entry = _sphereEntries[ix];
		            break;
		        }
		    }
		    if (entry == null)
		    {
		        entry = new DrawSphereEntry();
		        _sphereEntries.Add(entry);
		    }

		    entry.occupied = true;
		    entry.center = center;
		    entry.radius = radius;
		    entry.color = color;
		    entry.timer = duration;
		    entry.noZTest = noZTest;
		    entry.segments = segments;
		    _linesNeedRebuild = true;
		}

		private void AddSphereToBatch(DrawSphereEntry sphere, BatchedLineDraw batch)
		{
		    const float TWO_PI = Mathf.PI * 2f;
		    
		    // Calculate circle steps
		    float angleStep = TWO_PI / sphere.segments;
		    
		    // Draw circles in three orientations
		    DrawCircleInPlane(batch, sphere.center, Vector3.forward, Vector3.up, sphere.radius, sphere.color, sphere.segments);
		    DrawCircleInPlane(batch, sphere.center, Vector3.right, Vector3.up, sphere.radius, sphere.color, sphere.segments);
		    DrawCircleInPlane(batch, sphere.center, Vector3.right, Vector3.forward, sphere.radius, sphere.color, sphere.segments);
		    
		    // Optional: Draw meridian circles for better coverage
		    for (int i = 1; i < sphere.segments / 2; i++)
		    {
		        float angle = i * angleStep;
		        float offset = Mathf.Cos(angle) * sphere.radius;
		        float scale = Mathf.Sin(angle) * sphere.radius;
		        
		        // Draw circle in XY plane at varying heights
		        Vector3 posYShift = sphere.center + Vector3.up * offset;
		        DrawCircleInPlane(batch, posYShift, Vector3.forward, Vector3.right, scale, sphere.color, sphere.segments);
		        
		        // Draw circle in XZ plane at varying heights
		        Vector3 posZShift = sphere.center + Vector3.forward * offset;
		        DrawCircleInPlane(batch, posZShift, Vector3.right, Vector3.up, scale, sphere.color, sphere.segments);
		    }
		}

		private void DrawCircleInPlane(BatchedLineDraw batch, Vector3 center, Vector3 axis1, Vector3 axis2, float radius, Color color, int segments)
		{
		    const float TWO_PI = Mathf.PI * 2f;
		    float angleStep = TWO_PI / segments;
		    
		    Vector3 prevPoint = center + axis1 * radius;
		    Vector3 firstPoint = prevPoint;
		    
		    for (int i = 1; i <= segments; i++)
		    {
		        float angle = i * angleStep;
		        Vector3 point = center + 
		                        (axis1 * Mathf.Cos(angle) + axis2 * Mathf.Sin(angle)) * radius;
		        
		        batch.AddLine(prevPoint, point, color);
		        prevPoint = point;
		    }
		    
		    // Close the circle
		    batch.AddLine(prevPoint, firstPoint, color);
		}
		#endregion
		
		
		
#region Editor
#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoad]
		public static class DrawEditor
		{
			static DrawEditor()
			{
				//	set a low execution order
				var name = typeof(RuntimeDebugDrawDriver).Name;
				foreach (UnityEditor.MonoScript monoScript in UnityEditor.MonoImporter.GetAllRuntimeMonoScripts())
				{
					if (name != monoScript.name)
						continue;

					if (UnityEditor.MonoImporter.GetExecutionOrder(monoScript) != 9990)
					{
						UnityEditor.MonoImporter.SetExecutionOrder(monoScript, 9990);
						return;
					}
				}
			}
		}
#endif
		
		private void OnDrawGizmos()
		{
			//DrawTextOnDrawGizmos();
		}
		#endregion
	}
}

