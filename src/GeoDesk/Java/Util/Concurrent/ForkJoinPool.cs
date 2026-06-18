/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Java.Util.Concurrent;

// PORT: java.util.concurrent.ForkJoinPool. This is a faithful-API equivalent that dispatches to
// .NET threads (analogous to how the Java.Nio buffers wrap .NET I/O). It is NOT a
// work-stealing pool; instead it runs a fixed bank of background worker threads pulling from a
// shared queue, and ForkJoinTask.join() "helps" by running unclaimed tasks inline. This preserves
// the observable semantics the query engine relies on (asynchronous submit/fork, blocking join)
// without reproducing the work-stealing internals. Replacing this with a tuned scheduler is a
// future optimization.
/// <remarks>Ported from Java <c>java.util.concurrent.ForkJoinPool</c>.</remarks>
internal class ForkJoinPool : ExecutorService
{

    [ThreadStatic] static ForkJoinPool? _current;

    static readonly Lazy<ForkJoinPool> _commonPool = new Lazy<ForkJoinPool>(() => new ForkJoinPool());

    /// <summary>The pool the current thread is a worker of, or null.</summary>
    public static ForkJoinPool? Current => _current;

    /// <summary>The common pool, used by fork() when called outside a pool worker.</summary>
    /// <remarks>Ported from Java <c>java.util.concurrent.ForkJoinPool.commonPool()</c>.</remarks>
    public static ForkJoinPool CommonPool => _commonPool.Value;

    readonly BlockingCollection<Action> _queue = new BlockingCollection<Action>();
    readonly Thread[] _workers;
    volatile bool _shutdownRequested;

    /// <remarks>Ported from Java <c>java.util.concurrent.ForkJoinPool()</c>.</remarks>
    public ForkJoinPool()
        : this(Environment.ProcessorCount)
    {
    }

    /// <remarks>Ported from Java <c>java.util.concurrent.ForkJoinPool(int)</c>.</remarks>
    public ForkJoinPool(int parallelism)
    {
        if (parallelism < 1) parallelism = 1;
        _workers = new Thread[parallelism];
        for (var i = 0; i < parallelism; i++)
        {
            var t = new Thread(WorkerLoop)
            {
                IsBackground = true,
                Name = "ForkJoinPool-worker-" + i,
            };
            _workers[i] = t;
            t.Start();
        }
    }

    void WorkerLoop()
    {
        _current = this;
        try
        {
            foreach (var task in _queue.GetConsumingEnumerable())
            {
                task();
            }
        }
        catch (InvalidOperationException)
        {
            // queue completed while blocked in Take(); normal shutdown
        }
    }

    // Schedules a unit of work on this pool. Used by ForkJoinTask.fork()/submit().
    internal void Execute(Action task)
    {
        if (_shutdownRequested)
        {
            // After shutdown no new work is accepted; run inline so callers that still hold a task
            // reference do not stall (mirrors a rejected submit being handled by the caller).
            task();
            return;
        }
        _queue.Add(task);
    }

    /// <remarks>Ported from Java <c>java.util.concurrent.ForkJoinPool.submit(ForkJoinTask)</c>.</remarks>
    public ForkJoinTask<V> Submit<V>(ForkJoinTask<V> task)
    {
        Execute(task.DoExec);
        return task;
    }

    /// <remarks>Ported from Java <c>java.util.concurrent.ForkJoinPool.shutdown()</c>.</remarks>
    public override void Shutdown()
    {
        _shutdownRequested = true;
        _queue.CompleteAdding();
    }

    /// <remarks>Ported from Java <c>java.util.concurrent.ForkJoinPool.awaitTermination(long, TimeUnit)</c>.</remarks>
    public override bool AwaitTermination(long timeout, TimeUnit unit)
    {
        var millis = unit.ToMillis(timeout);
        var deadline = Environment.TickCount64 + millis;
        foreach (var t in _workers)
        {
            var remaining = deadline - Environment.TickCount64;
            if (remaining < 0) remaining = 0;
            var wait = remaining > int.MaxValue ? int.MaxValue : (int)remaining;
            if (!t.Join(wait)) return false;
        }
        return true;
    }

}
