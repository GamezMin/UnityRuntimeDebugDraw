using UnityEngine;
using VoxelGame;
public class ExampleScript : MonoBehaviour
{
	private Color[] _colors =
	{
		Color.red,
		Color.blue,
		Color.cyan,
		Color.magenta,
		Color.yellow
	};
	private int _colorIx;

	private float _timer;
	private float _intervalTimer;

	private Vector3 _bornPos;
	private static float _bornRadianOffset;

	private Color GetNextColor()
	{
		_colorIx = (_colorIx + 1) % _colors.Length;
		return _colors[_colorIx];
	}

	private void Awake()
	{
		_bornPos = transform.position;
		_timer = _bornRadianOffset;
		_bornRadianOffset += Mathf.PI * 0.3f;

		RuntimeDebugDraw.AttachText(transform, () => transform.position.y.ToString(), Vector3.up, Color.white, 12);
	}

	private void Update()
	{
		_timer += Time.deltaTime * 1.5f;

		float dx = Mathf.Cos(_timer) * 5f;
		float dy = Mathf.Sin(_timer) * 3f;
		transform.position = new Vector3(dx, dy, 0) + _bornPos;

		RuntimeDebugDraw.DrawBox(transform.position + Vector3.up * 1f, Vector3.one * 0.5f, Color.red);
		
		RuntimeDebugDraw.DrawCircleY(transform.position  -Vector3.up * 1f, 0.5f,32);
		
		RuntimeDebugDraw.DrawSphere(transform.position + Vector3.up * 2f, 0.5f, Color.blue);
		
		RuntimeDebugDraw.DrawLine(Vector3.zero, transform.position, Color.green, 0f, false);
		
		RuntimeDebugDraw.DrawRay(transform.position, Vector3.up * 10, Color.green);
		
		_intervalTimer += Time.deltaTime;
		if (_intervalTimer > 1f)
		{
			RuntimeDebugDraw.DrawText(transform.position, transform.position.ToString(), Color.green, 16, 0.5f, popUp: false);
			
			//RuntimeDebugDraw.DrawText(transform.position, Time.frameCount.ToString(), GetNextColor(), 16, 0.5f, popUp: true);
			//RuntimeDebugDraw.DrawLine(transform.position, transform.position + Vector3.up * 1.5f, GetNextColor(), 0.5f, true);
			_intervalTimer = 0f;
		}
	}
}
