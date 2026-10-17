using UnityEngine;

public class DestroyAfterTime : MonoBehaviour
{
    [Header("Destroy Settings")]
    public GameObject objectToDestroy;
    public float delay = 5f;

    public void CallDestroyAfterTime()
    {
        Destroy(objectToDestroy, delay);
    }
}