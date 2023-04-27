using System.Collections.Generic;
using UnityEngine;
using System;

public class UnityMainThreadDispatcher : MonoBehaviour
{
    //
    private static readonly Queue<Action> _executionQueue = new Queue<Action>();

    public static UnityMainThreadDispatcher Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    public void Update()
    {
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0)
            {
                _executionQueue.Dequeue().Invoke();
            }
        }
    }

    public void Enqueue(Action action)
    {
        if (action == null)
        {
            throw new ArgumentNullException("action");
        }

        lock (_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }
    }

    public async System.Threading.Tasks.Task EnqueueAsync(Action action)
    {
        var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();

        Enqueue(() =>
        {
            try
            {
                action();
                tcs.SetResult(true);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        await tcs.Task;
    }
}
