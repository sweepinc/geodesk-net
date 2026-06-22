/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * Test-only: builds tag-table ByteBuffers for named objects in tags.fab.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.MemoryMappedFiles;

using Clarisma.Common.Soar;

using GeoDesk.Common.Fab;
using GeoDesk.Common.Parser;
using GeoDesk.Common.Store;
using GeoDesk.Gol.Compiler;

namespace GeoDesk.Feature.Query;

public class TagTableTester
{

    readonly Dictionary<string, Dictionary<string, object?>> cases = new Dictionary<string, Dictionary<string, object?>>();
    readonly Dictionary<string, int> stringTable;

    public TagTableTester()
    {
        stringTable = LoadStringTable(ResourcePath("strings.txt"));
        new CaseReader(this).ReadFile(ResourcePath("tags.fab"));
    }

    static string ResourcePath(string name)
    {
        return Path.Combine(AppContext.BaseDirectory, "TestResources", "feature", name);
    }

    static Dictionary<string, int> LoadStringTable(string path)
    {
        var st = new Dictionary<string, int>();
        foreach (string s in File.ReadLines(path))
            st[s] = st.Count + 1; // 1-based index

        return st;
    }

    sealed class CaseReader : FabReader
    {

        readonly TagTableTester owner;
        readonly TagsParser parser = new TagsParser();

        public CaseReader(TagTableTester owner)
        {
            this.owner = owner;
        }

        protected override void KeyValue(string key, string value)
        {
            parser.Parse(value);
            owner.cases[key] = parser.Tags();
        }

    }

    public Dictionary<string, object?>? GetTags(string name)
    {
        return cases.TryGetValue(name, out var t) ? t : null;
    }

    static string ValueToString(object? v) => v switch
    {
        null => "",
        double d => d.ToString("R", CultureInfo.InvariantCulture),
        bool b => b ? "true" : "false",
        _ => Convert.ToString(v, CultureInfo.InvariantCulture) ?? ""
    };

    public static string[] TagsAsStringArray(Dictionary<string, object?> map)
    {
        var tags = new string[map.Count * 2];
        int i = 0;
        foreach (KeyValuePair<string, object?> e in map)
        {
            tags[i++] = e.Key;
            tags[i++] = ValueToString(e.Value);
        }

        return tags;
    }

    internal Segment MakeCase(string name, int maxRandomTags, ISet<string>? excludeTags)
    {
        Dictionary<string, object?>? tags = GetTags(name);
        if (tags == null)
            throw new InvalidOperationException($"TagTable case \"{name}\" not found");

        var archive = new TagTestArchive(TagsAsStringArray(tags), stringTable);
        return archive.Create(name);
    }

    /// <summary>
    /// Wraps a byte array in a <see cref="Segment"/> backed by an anonymous (memory-only)
    /// memory-mapped file, so synthetic tag tables can be fed to matchers that expect a Segment.
    /// </summary>
    internal static Segment SegmentFromBytes(byte[] data)
    {
        int length = Math.Max(data.Length, 1);
        var file = MemoryMappedFile.CreateNew(null, length);
        var view = file.CreateViewAccessor(0, length);
        if (data.Length > 0)
            view.WriteArray(0, data, 0, data.Length);

        return new Segment(file, view, length);
    }

    sealed class TagTestArchive : Archive
    {

        public TagTestArchive(string[] tags, Dictionary<string, int> stringTable)
        {
            var localStrings = new Dictionary<string, SString>();
            var tagTable = new STagTable(tags, stringTable, localStrings);
            var feature = new STestFeature(tagTable);
            SetHeader(feature);
            Place(tagTable);
            foreach (SString str in localStrings.Values)
                Place(str);
        }

        public Segment Create(string name)
        {
            using var baos = new MemoryStream();
            var @out = new StructOutputStream(baos);
            @out.WriteChain(Header());
            return SegmentFromBytes(baos.ToArray());
        }

    }

    sealed class STestFeature : Struct
    {

        readonly STagTable tagTable;

        public STestFeature(STagTable tagTable)
        {
            this.tagTable = tagTable;
            SetSize(16);
        }

        public override void WriteTo(StructOutputStream @out)
        {
            @out.WriteLong(0);
            @out.WritePointer(tagTable, tagTable.UncommonKeyCount() > 0 ? 1 : 0);
            @out.WriteInt(0);
        }

    }

}
