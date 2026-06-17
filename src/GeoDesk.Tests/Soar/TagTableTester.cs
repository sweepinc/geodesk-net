/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * Test-only: builds tag-table ByteBuffers for named objects in tags.fab.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Clarisma.Common.Fab;
using Clarisma.Common.Parser;
using Clarisma.Common.Soar;
using GeoDesk.Gol.Compiler;
using NioBuffer = Clarisma.Common.Nio.ByteBuffer;
using NioOrder = Clarisma.Common.Nio.ByteOrder;

namespace GeoDesk.Feature.Query;

public class TagTableTester
{
    private readonly Dictionary<string, Dictionary<string, object?>> cases = new Dictionary<string, Dictionary<string, object?>>();
    private readonly Dictionary<string, int> stringTable;

    public TagTableTester()
    {
        stringTable = LoadStringTable(ResourcePath("strings.txt"));
        new CaseReader(this).ReadFile(ResourcePath("tags.fab"));
    }

    private static string ResourcePath(string name)
    {
        return Path.Combine(AppContext.BaseDirectory, "TestResources", "feature", name);
    }

    private static Dictionary<string, int> LoadStringTable(string path)
    {
        var st = new Dictionary<string, int>();
        foreach (string s in File.ReadLines(path))
        {
            st[s] = st.Count + 1; // 1-based index
        }
        return st;
    }

    private sealed class CaseReader : FabReader
    {
        private readonly TagTableTester owner;
        private readonly TagsParser parser = new TagsParser();

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

    private static string ValueToString(object? v)
    {
        return v switch
        {
            null => "",
            double d => d.ToString("R", CultureInfo.InvariantCulture),
            bool b => b ? "true" : "false",
            _ => Convert.ToString(v, CultureInfo.InvariantCulture) ?? ""
        };
    }

    public static string[] TagsAsStringArray(Dictionary<string, object?> map)
    {
        string[] tags = new string[map.Count * 2];
        int i = 0;
        foreach (KeyValuePair<string, object?> e in map)
        {
            tags[i++] = e.Key;
            tags[i++] = ValueToString(e.Value);
        }
        return tags;
    }

    public NioBuffer MakeCase(string name, int maxRandomTags, ISet<string>? excludeTags)
    {
        Dictionary<string, object?>? tags = GetTags(name);
        if (tags == null) throw new InvalidOperationException($"TagTable case \"{name}\" not found");
        var archive = new TagTestArchive(TagsAsStringArray(tags), stringTable);
        return archive.Create(name);
    }

    private sealed class TagTestArchive : Archive
    {
        public TagTestArchive(string[] tags, Dictionary<string, int> stringTable)
        {
            var localStrings = new Dictionary<string, SString>();
            STagTable tagTable = new STagTable(tags, stringTable, localStrings);
            STestFeature feature = new STestFeature(tagTable);
            SetHeader(feature);
            Place(tagTable);
            foreach (SString str in localStrings.Values)
            {
                Place(str);
            }
        }

        public NioBuffer Create(string name)
        {
            using var baos = new MemoryStream();
            var @out = new StructOutputStream(baos);
            @out.WriteChain(Header());
            NioBuffer buf = NioBuffer.Wrap(baos.ToArray());
            buf.Order(NioOrder.LittleEndian);
            return buf;
        }
    }

    private sealed class STestFeature : Struct
    {
        private readonly STagTable tagTable;

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
