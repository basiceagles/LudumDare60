using UnityEngine;
using UnityEngine.Events;
using System.Collections;

[System.Serializable]
public class TimedEvent
{
    public UnityEvent unityEvent;
    public float delay = 0f;
}

public class DelayedEvents : MonoBehaviour
{
    [SerializeField] private TimedEvent[] events;
    [SerializeField] private bool callOnStart = true;

    void Start()
    {
        if (callOnStart)
        {
            foreach (TimedEvent timedEvent in events)
            {
                StartCoroutine(ExecuteEvent(timedEvent.unityEvent, timedEvent.delay));
            }
        }
    }

    IEnumerator ExecuteEvent(UnityEvent unityEvent, float delay)
    {
        yield return new WaitForSeconds(delay);
        unityEvent.Invoke();
    }

    public void CallDelayedEvent ()
    {
        foreach (TimedEvent timedEvent in events)
        {
            StartCoroutine(ExecuteEvent(timedEvent.unityEvent, timedEvent.delay));
        }
    }
}