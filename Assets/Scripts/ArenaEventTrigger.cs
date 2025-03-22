using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class ArenaEventTrigger : MonoBehaviour
{
    public ArenaEvent ArenaEvent;

    private BoxCollider2D _triggerCollider;

    private void Awake()
    {
        _triggerCollider = GetComponent<BoxCollider2D>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("Arena event area triggered!");

            _triggerCollider.enabled = false;

            ArenaEvent.TriggerArenaEvent();
        }
    }
}
