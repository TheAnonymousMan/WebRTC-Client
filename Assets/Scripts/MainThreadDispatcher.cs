using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Action> ExecutionQueue = new();

    public static void Enqueue(IEnumerator action)
    {
        lock (ExecutionQueue)
        {
            ExecutionQueue.Enqueue(() => Singleton.StartCoroutine(action));
        }
    }

    public static MainThreadDispatcher Singleton;

    void Awake()
    {
        if (Singleton == null) Singleton = this;
        else Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        lock (ExecutionQueue)
        {
            while (ExecutionQueue.Count > 0)
            {
                var action = ExecutionQueue.Dequeue();
                action?.Invoke();
            }
        }
    }
}