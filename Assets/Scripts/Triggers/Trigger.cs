using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class DelayedEvent
{
    public UnityEvent unityEvent;
    public float delay = 0f;
    [HideInInspector] public string name = "Event";
}

public class Trigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    [SerializeField] bool destroyOnTriggerEvent;
    [SerializeField] bool clearEventsOnTriggerEvent;
    [SerializeField] bool clearEventsOnTriggerEventOnExit;
    [SerializeField] string tagFilter;
    
    [Header("On Trigger Enter Events")]
    [SerializeField] DelayedEvent[] onTriggerEnterEvents;
    
    [Header("On Trigger Exit Events")]
    [SerializeField] DelayedEvent[] onTriggerExitEvents;

    void OnTriggerEnter(Collider other)
    {
        if (!String.IsNullOrEmpty(tagFilter) && !other.gameObject.CompareTag(tagFilter)) return;
        
        // Execute all trigger enter events with their respective delays
        ExecuteEventsWithDelay(onTriggerEnterEvents);
        
        if (destroyOnTriggerEvent)
        {
            Destroy(gameObject);
        }
        if (clearEventsOnTriggerEvent)
        {
            ClearAllEvents();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!String.IsNullOrEmpty(tagFilter) && !other.gameObject.CompareTag(tagFilter)) return;
        
        // Execute all trigger exit events with their respective delays
        ExecuteEventsWithDelay(onTriggerExitEvents);

        if (clearEventsOnTriggerEventOnExit)
        {
            ClearAllEvents();
        }
    }

    void ExecuteEventsWithDelay(DelayedEvent[] events)
    {
        foreach (DelayedEvent delayedEvent in events)
        {
            if (delayedEvent.unityEvent != null)
            {
                StartCoroutine(ExecuteSingleEvent(delayedEvent.unityEvent, delayedEvent.delay));
            }
        }
    }

    IEnumerator ExecuteSingleEvent(UnityEvent unityEvent, float delay)
    {
        if (delay > 0)
        {
            yield return new WaitForSeconds(delay);
        }

        unityEvent.Invoke();
    }
    
    public void ClearAllEvents()
    {
        onTriggerEnterEvents = new DelayedEvent[0];
        onTriggerExitEvents = new DelayedEvent[0];
    }
}