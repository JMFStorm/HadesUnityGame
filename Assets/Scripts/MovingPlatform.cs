using System.Collections.Generic;
using UnityEngine;

public class MovingPlatforms : MonoBehaviour
{
	public float PlatformSpeed = 3.0f;
	public bool LoopAround = true;
	public bool InitStatic = false;

	private readonly List<Vector3> _platformPoints = new();
	private Vector3 _targetPoint;

    private bool moveInitiated = false;
    private bool stepsIncrease = true;
    private int _currentPointIndex = 0;

    private void Awake()
    {
        TryAddPoint("point0");
        TryAddPoint("point1");
        TryAddPoint("point2");
        TryAddPoint("point3");

        if (_platformPoints.Count < 2)
        {
            Debug.LogError($"MovingPlatforms with only {_platformPoints.Count} travel points is a mistake.");
        }

        _targetPoint = _platformPoints[_currentPointIndex];
    }

    private void Start()
    {
    }

    void TryAddPoint(string name)
    {
        var point = transform.Find(name);

        if (point != null)
        {
            _platformPoints.Add(point.transform.position);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Vector2 collisionNormal = collision.contacts[0].normal;

            if (collisionNormal.y < 0f)
            {
                collision.transform.SetParent(transform);
                moveInitiated = true;
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.transform.SetParent(null);
        }
    }

    void FixedUpdate()
	{
        if (!InitStatic || (InitStatic && moveInitiated))
        {
            PlatformMovement();
        }
	}

    void PlatformMovement()
    {
        gameObject.transform.position = Vector3.MoveTowards(transform.position, _targetPoint, PlatformSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, _targetPoint) < 0.01f)
        {
            if (LoopAround)
            {
                _currentPointIndex = (_currentPointIndex + 1) % _platformPoints.Count;
            }
            else
            {
                if (stepsIncrease)
                {
                    if (_currentPointIndex == _platformPoints.Count - 1)
                    {
                        stepsIncrease = false;
                        _currentPointIndex -= 1;
                    }
                    else
                    {
                        _currentPointIndex += 1;
                    }
                }
                else
                {
                    if (_currentPointIndex == 0)
                    {
                        stepsIncrease = true;
                        _currentPointIndex += 1;
                    }
                    else
                    {
                        _currentPointIndex -= 1;
                    }
                }
            }

            _targetPoint = _platformPoints[_currentPointIndex];
        }
    }
}
