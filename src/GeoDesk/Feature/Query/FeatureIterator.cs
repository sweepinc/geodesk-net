/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections;
using System.Collections.Generic;

namespace GeoDesk.Feature.Query;

// PORT: helper base (no Java counterpart) that bridges Java's Iterator<Feature> contract
// (HasNext()/Next(), where Next() yields null once exhausted) to .NET's IEnumerator<Feature>.
// The ported table iterators express their logic via HasNext()/Next() exactly as in Java; this
// base supplies the IEnumerator adapter so a view can be enumerated with foreach.
/// <summary>
/// Base class for feature table iterators that exposes Java's <c>hasNext()</c>/<c>next()</c>
/// iteration style while also implementing .NET's <see cref="IEnumerator{T}"/> so that a
/// feature view can be consumed with <c>foreach</c>.
/// </summary>
/// <remarks>Port-only adapter (no direct Java counterpart): bridges Java's
/// <c>java.util.Iterator&lt;Feature&gt;</c> contract to .NET <c>IEnumerator&lt;Feature&gt;</c>.</remarks>
internal abstract class FeatureIterator : IEnumerator<IFeature>
{

    IFeature? _current;

    /// <summary>
    /// Returns true if another feature is available from this iterator.
    /// </summary>
    /// <remarks>Mirrors Java's <c>java.util.Iterator.hasNext()</c>.</remarks>
    public abstract bool HasNext();

    /// <summary>
    /// Returns the next feature, or null once the iterator has been exhausted.
    /// </summary>
    /// <remarks>Mirrors Java's <c>java.util.Iterator.next()</c> (yields null once exhausted).</remarks>
    public abstract IFeature? Next();

    /// <summary>
    /// The feature produced by the most recent successful <see cref="MoveNext"/> call.
    /// </summary>
    /// <remarks>Port-only: <c>IEnumerator&lt;Feature&gt;.Current</c> adapter.</remarks>
    public IFeature Current => _current!;

    /// <summary>
    /// The non-generic view of <see cref="Current"/>.
    /// </summary>
    /// <remarks>Port-only: non-generic <c>IEnumerator.Current</c> adapter.</remarks>
    object IEnumerator.Current => _current!;

    /// <summary>
    /// Advances the iterator by driving <see cref="HasNext"/> and <see cref="Next"/>,
    /// caching the result in <see cref="Current"/>; returns false when exhausted.
    /// </summary>
    /// <remarks>Port-only: drives <see cref="HasNext"/>/<see cref="Next"/> to satisfy <c>IEnumerator.MoveNext()</c>.</remarks>
    public bool MoveNext()
    {
        if (!HasNext()) return false;
        _current = Next();
        return true;
    }

    /// <summary>
    /// Not supported; Java iterators cannot be rewound, so this always throws.
    /// </summary>
    /// <remarks>Port-only: unsupported, as Java iterators are not resettable.</remarks>
    public void Reset()
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// Releases any resources held by the iterator. The default implementation does
    /// nothing; subclasses override as needed.
    /// </summary>
    /// <remarks>Port-only: <c>IDisposable</c> adapter (no-op by default).</remarks>
    public virtual void Dispose()
    {
    }

}
