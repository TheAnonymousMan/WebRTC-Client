using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Utility class that allows code (e.g., callbacks or coroutines) to be executed on Unity's main thread.
/// This is useful when you receive events or data from background threads (e.g., WebSocket, WebRTC)
/// and need to interact with Unity objects, which must happen on the main thread.
/// </summary>
public class MainThreadDispatcher : MonoBehaviour
{
    // A thread-safe queue that stores actions to be executed on the main thread.
    private static readonly Queue<Action> ExecutionQueue = new();

    /// <summary>
    /// Enqueues a coroutine to be started on the main thread.
    /// This wraps the IEnumerator into an Action that starts the coroutine using Unity's StartCoroutine method.
    /// </summary>
    /// <param name="action">The coroutine to run on the main thread.</param>
    public static void Enqueue(IEnumerator action)
    {
        // Lock the queue to ensure thread-safe access
        lock (ExecutionQueue)
        {
            // Enqueue an action that starts the coroutine on the Singleton MonoBehaviour
            ExecutionQueue.Enqueue(() => Singleton.StartCoroutine(action));
        }
    }

    // Static reference to the Singleton instance
    public static MainThreadDispatcher Singleton;

    /// <summary>
    /// Ensures a single instance of the MainThreadDispatcher exists and persists between scenes.
    /// </summary>
    void Awake()
    {
        if (Singleton == null)
            Singleton = this; // Set the singleton reference
        else
            Destroy(gameObject); // Destroy any duplicate instances

        // Prevent this GameObject from being destroyed when loading a new scene
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Called once per frame by Unity.
    /// Executes all actions queued for the main thread in a thread-safe manner.
    /// </summary>
    void Update()
    {
        // Lock the queue to ensure thread-safe access during dequeue
        lock (ExecutionQueue)
        {
            // Execute all queued actions
            while (ExecutionQueue.Count > 0)
            {
                var action = ExecutionQueue.Dequeue();
                action?.Invoke();
            }
        }
    }
}
