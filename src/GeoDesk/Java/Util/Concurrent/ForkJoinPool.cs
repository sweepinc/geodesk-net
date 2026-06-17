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

// PORT: java.util.concurrent.ForkJoinPool. This is a faithful-API equivalent that
// dispatches to .NET threads (analogous to how the Clarisma.Common.Nio buffers wrap
// .NET I/O). It is NOT a work-stealing pool; instead it runs a fixed bank of background
// worker threads pulling from a shared queue, and ForkJoinTask.join() "helps" by running
// unclaimed tasks inline. This preserves the observable semantics the query engine relies
// on (asynchronous submit/fork, blocking join) without reproducing the work-stealing
// internals. Replacing this with a tuned scheduler is a future optimization.
public class ForkJoinPool : ExecutorService
{
    [ThreadStatic] private static ForkJoinPool? current;

    private static readonly Lazy<ForkJoinPool> commonPool =
        new Lazy<ForkJoinPool>(() => new ForkJoinPool());

    /// <summary>The pool the current thread is a worker of, or null.</summary>
    public static ForkJoinPool? Current => current;

    /// <summary>The common pool, used by fork() when called outside a pool worker.</summary>
    public static ForkJoinPool CommonPool => commonPool.Value;

    private readonly BlockingCollection<Action> queue = new BlockingCollection<Action>();
    private readonly Thread[] workers;
    private volatile bool shutdownRequested;

    public ForkJoinPool()
        : this(Environment.ProcessorCount)
    {
    }

    public ForkJoinPool(int parallelism)
    {
        if (parallelism < 1) parallelism = 1;
        workers = new Thread[parallelism];
        for (int i = 0; i < parallelism; i++)
        {
            Thread t = new Thread(WorkerLoop)
            {
                IsBackground = true,
                Name = "ForkJoinPool-worker-" + i,
            };
            workers[i] = t;
            t.Start();
        }
    }

    private void WorkerLoop()
    {
        current = this;
        try
        {
            foreach (Action task in queue.GetConsumingEnumerable())
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
        if (shutdownRequested)
        {
            // After shutdown no new work is accepted; run inline so callers that
            // still hold a task reference do not stall (mirrors a rejected submit
            // being handled by the caller).
            task();
            return;
        }
        queue.Add(task);
    }

    /// <summary>
    /// Submits a ForkJoinTask for execution and returns it (mirrors
    /// <c>ForkJoinPool.submit(ForkJoinTask)</c>).
    /// </summary>
    public ForkJoinTask<V> Submit<V>(ForkJoinTask<V> task)
    {
        Execute(task.DoExec);
        return task;
    }

    public override void Shutdown()
    {
        shutdownRequested = true;
        queue.CompleteAdding();
    }

    public override bool AwaitTermination(long timeout, TimeUnit unit)
    {
        long millis = unit.ToMillis(timeout);
        long deadline = Environment.TickCount64 + millis;
        foreach (Thread t in workers)
        {
            long remaining = deadline - Environment.TickCount64;
            if (remaining < 0) remaining = 0;
            int wait = remaining > int.MaxValue ? int.MaxValue : (int)remaining;
            if (!t.Join(wait))
            {
                return false;
            }
        }
        return true;
    }
}
