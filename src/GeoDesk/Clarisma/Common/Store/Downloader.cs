/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

namespace Clarisma.Common.Store;

// PORT-BLOCKED: The Java Downloader fetches GOL data tiles over HTTP(S) on a background
// thread pool and inserts them into the BlobStore. This is a large, network/threading-heavy
// component (~500 LOC) that is not required for offline use of an already-built GOL, and is
// not exercised by the store unit tests. Only the surface referenced by BlobStore is provided
// here; all operations throw until the full component is ported.
public class Downloader
{
    public const int METADATA_ID = 0; // TODO: confirm against full port

    public Downloader(BlobStore store, string url)
    {
        // no-op stub
    }

    public Ticket Request(int id, object? listener)
    {
        throw new NotImplementedException(
            "PORT-BLOCKED: Downloader (HTTP tile download) is not yet ported.");
    }

    public void Shutdown()
    {
        // no-op stub
    }

    public sealed class Ticket
    {
        public void AwaitCompletion()
        {
            throw new NotImplementedException("PORT-BLOCKED: Downloader is not yet ported.");
        }

        public void ThrowError()
        {
            throw new NotImplementedException("PORT-BLOCKED: Downloader is not yet ported.");
        }

        public int Page()
        {
            throw new NotImplementedException("PORT-BLOCKED: Downloader is not yet ported.");
        }
    }
}
