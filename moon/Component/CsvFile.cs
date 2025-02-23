using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using Godot;
using Microsoft.VisualBasic;

namespace Component;

public class CsvFile
{
    private List<string[]> Data = [];

    public Error Load(string path, string splitter = ",")
    {
        var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null) return Error.Failed;
        
        Data.Clear();
        while (!file.EofReached())
        {
            var result = file.GetLine().Split(splitter);
            Data.Add(result);
        }
        
        file.Close();
        return Error.Ok;
    }
    
    public void Clear() => Data.Clear();

    public Dictionary<string, T> ToMap<T>(Func<string[], T> parser)
    {
        var map = new Dictionary<string, T>();
        foreach (var row in Data)
        {
            var key = Strings.Trim(row[0]);
            var value = parser(row[1..]);
            map[key] = value;
        }
        return map;
    }
    
    public FrozenDictionary<string, T> ToFrozenMap<T>(Func<string[], T> parser)
       => ToMap(parser).ToFrozenDictionary();
    
    public Dictionary<string, string> ToMap()
       => ToMap(rows => Strings.Trim(string.Join(null, rows)));
       
    public FrozenDictionary<string, string> ToFrozenMap()
       => ToMap().ToFrozenDictionary();
       
    public T[] ToArray<T>(Func<string[], T> parser)
    {
        var list = new List<T>();
        foreach (var row in Data)
        {
            var value = parser(row);
            list.Add(value);
        }
        return list.ToArray();
    }
    
    public string[] ToArray()
        => ToArray(rows => Strings.Trim(string.Join(null, rows)));
    
    public int[] ToIntArray()
        => ToArray(rows => int.Parse(Strings.Trim(string.Join(null, rows))));
    
    public float[] ToFloatArray()
        => ToArray(rows => float.Parse(Strings.Trim(string.Join(null, rows))));
    
    public double[] ToDoubleArray()
        => ToArray(rows => double.Parse(Strings.Trim(string.Join(null, rows))));
}