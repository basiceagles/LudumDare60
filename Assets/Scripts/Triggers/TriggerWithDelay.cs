using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections;

public class TriggerWithDelay : MonoBehaviour
{
    [SerializeField] bool destroyOnTriggerEvent;
    [SerializeField] UnityEvent onTriggerEnter;
    [SerializeField] UnityEvent onTriggerExit;
    [SerializeField] string tagFilter;
    [SerializeField] float onTriggerEnterDelay = 0f;
    [SerializeField] float onTriggerExitDelay = 0f;

    void OnTriggerEnter(Collider other)
    {
        if (!String.IsNullOrEmpty(tagFilter) && !other.gameObject.CompareTag(tagFilter)) return;
        
        if (onTriggerEnterDelay > 0)
        {
            StartCoroutine(InvokeWithDelay(onTriggerEnter, onTriggerEnterDelay));
        }
        else
        {
            onTriggerEnter.Invoke();
        }
        
        if (destroyOnTriggerEvent)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!String.IsNullOrEmpty(tagFilter) && !other.gameObject.CompareTag(tagFilter)) return;
        
        if (onTriggerExitDelay > 0)
        {
            StartCoroutine(InvokeWithDelay(onTriggerExit, onTriggerExitDelay));
        }
        else
        {
            onTriggerExit.Invoke();
        }
    }

    IEnumerator InvokeWithDelay(UnityEvent unityEvent, float delay)
    {
        yield return new WaitForSeconds(delay);
        unityEvent.Invoke();
    }
}