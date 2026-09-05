using System;
using System.Collections.Generic;

public static class EventBus<T> where T : IEvent
{
    private static readonly HashSet<Action<T>> Bindings = new HashSet<Action<T>>();
    private static readonly List<Action<T>> InvocationBuffer = new List<Action<T>>();

    public static void Subscribe(Action<T> binding)
    {
        if (binding == null) return;
        Bindings.Add(binding);
    }

    public static void Unsubscribe(Action<T> binding)
    {
        if (binding == null) return;
        Bindings.Remove(binding);
    }

    public static void Raise(T @event)
    {
        if (Bindings.Count == 0) return;

        // Copy bindings to buffer in case a callback subscribes/unsubscribes during execution
        InvocationBuffer.Clear();
        InvocationBuffer.AddRange(Bindings);

        for (int i = 0; i < InvocationBuffer.Count; i++)
        {
            try
            {
                InvocationBuffer[i]?.Invoke(@event);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
            }
        }

        InvocationBuffer.Clear();
    }

    public static void Clear()
    {
        Bindings.Clear();
        InvocationBuffer.Clear();
    }
}
