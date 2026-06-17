/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Threading;

namespace Java.Util.Concurrent;

// PORT: java.util.concurrent.ForkJoinTask, backed by the .NET-threaded ForkJoinPool in this
// package. Only the surface used by the query engine is implemented: fork(), join(),
// getRawResult()/setRawResult() and the abstract exec().
//
// The fork/join protocol is reproduced faithfully: fork() schedules the task on the current
// pool; join() returns its result once complete. To avoid worker starvation when a worker blocks
// in join() (which Java's ForkJoinPool solves via work-stealing and managed blocking), join()
// will execute the task inline on the calling thread if no worker has claimed it yet ("helping");
// otherwise it waits for the claiming worker.
/// <remarks>Ported from Java <c>java.util.concurrent.ForkJoinTask</c>.</remarks>
public abstract class ForkJoinTask<V>
{

    const int Fresh = 0;
    const int Running = 1;
    const int Done = 2;

    int _state;
    readonly ManualResetEventSlim _completed = new ManualResetEventSlim(false);

    /// <summary>
    /// Immediately performs the base action of this task and returns true if, upon return from
    /// this method, the task is guaranteed to have completed normally.
    /// </summary>
    /// <remarks>Ported from Java <c>java.util.concurrent.ForkJoinTask.exec()</c>.</remarks>
    protected abstract bool Exec();

    /// <remarks>Ported from Java <c>java.util.concurrent.ForkJoinTask.getRawResult()</c>.</remarks>
    public abstract V GetRawResult();

    /// <remarks>Ported from Java <c>java.util.concurrent.ForkJoinTask.setRawResult(V)</c>.</remarks>
    protected abstract void SetRawResult(V value);

    // Port-only: runs the task exactly once. The first caller (a pool worker or a helping joiner)
    // claims it via CAS; later callers fall through without re-running.
    internal void DoExec()
    {
        if (Interlocked.CompareExchange(ref _state, Running, Fresh) != Fresh) return;
        try
        {
            Exec();
        }
        finally
        {
            Volatile.Write(ref _state, Done);
            _completed.Set();
        }
    }

    /// <summary>
    /// Arranges to asynchronously execute this task in the pool the current thread is running in,
    /// or the common pool if the current thread is not a pool worker.
    /// </summary>
    /// <remarks>Ported from Java <c>java.util.concurrent.ForkJoinTask.fork()</c>.</remarks>
    public ForkJoinTask<V> Fork()
    {
        var pool = ForkJoinPool.Current ?? ForkJoinPool.CommonPool;
        pool.Execute(DoExec);
        return this;
    }

    /// <summary>
    /// Returns the result of the computation when it is done.
    /// </summary>
    /// <remarks>Ported from Java <c>java.util.concurrent.ForkJoinTask.join()</c>.</remarks>
    public V Join()
    {
        if (Volatile.Read(ref _state) != Done)
        {
            // Help out: claim and run the task ourselves if no worker has it yet; if a worker
            // already claimed it, this returns immediately and we wait.
            DoExec();
            _completed.Wait();
        }
        return GetRawResult();
    }

}
