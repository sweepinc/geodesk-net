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
/// <remarks>Port-only adapter (no direct Java counterpart): bridges Java's
/// <c>java.util.Iterator&lt;Feature&gt;</c> contract to .NET <c>IEnumerator&lt;Feature&gt;</c>.</remarks>
internal abstract class FeatureIterator : IEnumerator<Feature>
{

    Feature? _current;

    /// <remarks>Mirrors Java's <c>java.util.Iterator.hasNext()</c>.</remarks>
    public abstract bool HasNext();

    /// <remarks>Mirrors Java's <c>java.util.Iterator.next()</c> (yields null once exhausted).</remarks>
    public abstract Feature? Next();

    /// <remarks>Port-only: <c>IEnumerator&lt;Feature&gt;.Current</c> adapter.</remarks>
    public Feature Current => _current!;

    /// <remarks>Port-only: non-generic <c>IEnumerator.Current</c> adapter.</remarks>
    object IEnumerator.Current => _current!;

    /// <remarks>Port-only: drives <see cref="HasNext"/>/<see cref="Next"/> to satisfy <c>IEnumerator.MoveNext()</c>.</remarks>
    public bool MoveNext()
    {
        if (!HasNext()) return false;
        _current = Next();
        return true;
    }

    /// <remarks>Port-only: unsupported, as Java iterators are not resettable.</remarks>
    public void Reset()
    {
        throw new NotSupportedException();
    }

    /// <remarks>Port-only: <c>IDisposable</c> adapter (no-op by default).</remarks>
    public virtual void Dispose()
    {
    }

}
