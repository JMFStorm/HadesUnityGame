using System.Linq;
using UnityEngine;

public class MovingPlatforms : MonoBehaviour
{
	[Tooltip("Element 0 is starting point")]
	public Transform[] PlatformPoints;

	public float PlatformSpeed = 4.0f;
	public bool LoopAround = true;

	private Transform _targetPoint;

    private bool stepsIncrease = true;
    private int _currentPointIndex = 0;
    private int _platformPointsCount = 0;

    void Start()
    {
        _platformPointsCount = PlatformPoints.Count();

        if (_platformPointsCount < 2)
		{
			Debug.LogError($"MovingPlatforms with only {_platformPointsCount} travel points is a mistake.");
		}

        _targetPoint = PlatformPoints[_currentPointIndex];
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Vector2 collisionNormal = collision.contacts[0].normal;

            if (collisionNormal.y < 0f)
            {
                Debug.Log("Platform set parent");

                collision.transform.SetParent(transform);
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.transform.SetParent(null);

            Debug.Log("Platform remove parent");
        }
    }

    void FixedUpdate()
	{
		PlatformMovement();
	}

    void PlatformMovement()
    {
        gameObject.transform.position = Vector3.MoveTowards(transform.position, _targetPoint.position, PlatformSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, _targetPoint.position) < 0.01f)
        {
            if (LoopAround)
            {
                _currentPointIndex = (_currentPointIndex + 1) % PlatformPoints.Length;
            }
            else
            {
                if (stepsIncrease)
                {
                    if (_currentPointIndex == _platformPointsCount - 1)
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

            _targetPoint = PlatformPoints[_currentPointIndex];
        }
    }
}
