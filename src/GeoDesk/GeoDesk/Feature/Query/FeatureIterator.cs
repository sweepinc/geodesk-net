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
public abstract class FeatureIterator : IEnumerator<Feature>
{

    Feature? _current;

    public abstract bool HasNext();

    public abstract Feature? Next();

    public Feature Current => _current!;

    object IEnumerator.Current => _current!;

    public bool MoveNext()
    {
        if (!HasNext()) return false;
        _current = Next();
        return true;
    }

    public void Reset()
    {
        throw new NotSupportedException();
    }

    public virtual void Dispose()
    {
    }

}
