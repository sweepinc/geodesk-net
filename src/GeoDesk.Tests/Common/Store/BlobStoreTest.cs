/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.IO;

using GeoDesk.Common.Store;

using Xunit;

namespace GeoDesk.Tests.Common.Store;

/// <summary>
/// A basic test that allocates and frees blobs.
/// </summary>
public class BlobStoreTest : IDisposable
{
    private readonly string testFolder;
    private readonly string storePath;
    private readonly TestBlobStore store;

    internal class TestBlobStore : BlobStore
    {
        public TestBlobStore(string filename)
        {
            SetPath(filename);
        }

        public int Alloc(int pages)
        {
            BeginTransaction(LOCK_APPEND);
            int blob = AllocateBlob((pages << pageSizeShift) - 4);
            Commit();
            EndTransaction();
            return blob;
        }

        public void Free(params int[] blobs)
        {
            BeginTransaction(LOCK_EXCLUSIVE);
            for (int i = 0; i < blobs.Length; i++)
            {
                FreeBlob(blobs[i]);
            }
            Commit();
            EndTransaction();
        }
    }

    public BlobStoreTest()
    {
        testFolder = Path.Combine(Path.GetTempPath(), "blobstore-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testFolder);
        storePath = Path.Combine(testFolder, "test.store");
        store = new TestBlobStore(storePath);
        store.Open();
    }

    public void Dispose()
    {
        store.Close();
        if (File.Exists(storePath)) File.Delete(storePath);
        string journal = storePath + "-journal";
        if (File.Exists(journal)) File.Delete(journal);
        if (Directory.Exists(testFolder)) Directory.Delete(testFolder, true);
    }

    [Fact]
    public void TestAllocFree()
    {
        int a = store.Alloc(4);     // 1:4
        Assert.Equal(1, a);
        int b = store.Alloc(20);    // 1:4, 5:20
        Assert.Equal(5, b);
        store.Free(a);                    // (1:4), 5:20
        int c = store.Alloc(2);     // 1:2, (3:2), 5:20
        Assert.Equal(1, c);
        int d = store.Alloc(15);     // 1:2, (3:2), 5:20, 25:15
        Assert.Equal(25, d);
        store.Free(b);                    // 1:2, 3:2, (3:22), 25:15
        int e = store.Alloc(21);     // 1:2, 3:21, (24:1), 25:15
        Assert.Equal(3, e);
        store.Free(d);                    // 1:2, 3:21
        int f = store.Alloc(10);
        Assert.Equal(24, f);      // c=1:2, e=3:21, f=24:10
        int g = store.Alloc(9);
        Assert.Equal(34, g);      // c=1:2, e=3:21, f=25:10, g=34:9
        int h = store.Alloc(7);
        Assert.Equal(43, h);      // c=1:2, e=3:21, f=25:10, g=35:9, h=43:7
        store.Free(e, g);                 // c=1:2, (3:21), f=25:10, (35:9), h=43:7
        store.Free(f);                 // c=1:2, (3:40), h=43:7
        int i = store.Alloc(38);
        Assert.Equal(3, i);      // c=1:2, i=3:38, (41:2), h=43:7
        store.Free(h);                 // c=1:2, i=3:38

        int max = 1 << 18;
        int j = store.Alloc(max);
        Assert.Equal(max, j);
        int k = store.Alloc(max);
        Assert.Equal(max * 2, k);

        store.Free(i, j, k);          // c=1:2
    }

    [Fact]
    public void TestBigAllocFree()
    {
        int max = 1 << 18;
        int a = store.Alloc(max);
        Check(a);
        int b = store.Alloc(max);
        Check(a, b);
        int c = store.Alloc(max);
        Check(a, b, c);
        store.Free(a);
        Check(b, c);
        store.Free(b);
        Check(c);
        store.Free(c);
        Check();
    }

    private void Check(params int[] inUse)
    {
        BlobStoreChecker checker = new BlobStoreChecker(store);
        foreach (int b in inUse) checker.UseBlob(0, b);
        checker.Check();
        checker.ReportErrors(Console.Out);
        Assert.False(checker.HasErrors());
    }
}
