/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using GeoDesk.Feature.Store;

namespace GeoDesk.Feature.Match;

/// <summary>
/// A matcher that accepts relation members holding a specific role, identified by its global string
/// code resolved from the feature store.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.match.RoleMatcher</c>.</remarks>
internal class RoleMatcher : Matcher
{

    readonly int _roleCode;

    /// <summary>
    /// Creates a matcher for the given role name, resolving it to its global string code via the store.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.RoleMatcher(FeatureStore, String)</c>.</remarks>
    public RoleMatcher(FeatureStore store, string role) :
        base(TypeBits.ALL)
    {
        _roleCode = store.CodeFromString(role);
        // TODO: empty role should be 0
        System.Diagnostics.Debug.Assert(_roleCode != 0);
    }

    /// <summary>
    /// Returns this matcher if the candidate role code matches the target role, otherwise null.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.RoleMatcher.acceptRole(int, String)</c>.</remarks>
    public override Matcher? AcceptRole(int roleCode, string? roleString)
    {
        return roleCode == _roleCode ? this : null;
    }

}
