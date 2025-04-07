using System.Collections.Generic;
using UnityEngine;

public class MovingPlatforms : MonoBehaviour
{
	public float PlatformSpeed = 3.0f;
	public bool LoopAround = true;
	public bool InitStatic = false;

	private readonly List<Vector3> _platformPoints = new();
	private Vector3 _targetPoint;
	private AudioSource _audioSource;
	private Animator _animator;

    private bool moveInitiated = false;
    private bool stepsIncrease = true;
    private int _currentPointIndex = 0;

    private void Awake()
    {
        if (!TryGetComponent(out _animator))
        {
            Debug.LogError($"Did not find {nameof(Animator)} on {nameof(MovingPlatforms)}");
        }

        if (!TryGetComponent(out _audioSource))
        {
            Debug.LogError($"Did not find {nameof(AudioSource)} on {nameof(MovingPlatforms)}");
        }

        TryAddPoint("point0");
        TryAddPoint("point1");
        TryAddPoint("point2");
        TryAddPoint("point3");

        if (_platformPoints.Count < 2)
        {
            Debug.LogError($"MovingPlatforms with only {_platformPoints.Count} travel points is a mistake.");
        }

        _targetPoint = _platformPoints[_currentPointIndex];

        var usedPitch = NormalizePitch(PlatformSpeed, 1.5f, 4.0f, 0.75f, 1.50f);
        _audioSource.pitch = usedPitch;
    }

    private void Start()
    {
        if (!InitStatic)
        {
            _audioSource.Play();
            _animator.Play("MovingPlatformMove");
        }
        else
        {
            _animator.Play("MovingPlatformMIdle");
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Vector2 collisionNormal = collision.contacts[0].normal;

            if (collisionNormal.y < 0f && Mathf.Abs(collisionNormal.x) < Mathf.Abs(collisionNormal.y))
            {
                collision.transform.SetParent(transform);
                moveInitiated = true;

                if (InitStatic)
                {
                    _audioSource.Play();
                    _animator.Play("MovingPlatformMove");
                }
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
        gameObject.transform.position = Vector2.MoveTowards(transform.position, _targetPoint, PlatformSpeed * Time.fixedDeltaTime);

        if (Vector2.Distance(transform.position, _targetPoint) < 0.01f)
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

    void TryAddPoint(string name)
    {
        var point = transform.Find(name);

        if (point != null)
        {
            _platformPoints.Add(point.transform.position);
        }
    }

    float NormalizePitch(float value, float minSpeed, float maxSpeed, float minTarget, float maxTarget)
    {
        return minTarget + (value - minSpeed) / (maxSpeed - minSpeed) * (maxTarget - minTarget);
    }
}
