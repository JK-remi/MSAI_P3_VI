using System.IO;
using System.Text.RegularExpressions;
using System.Globalization;
using UnityEngine;

public class Utils
{
    public const float AUDIO_REACTOR_FACTOR = 32767f; //to convert float to Int16


    public static string GetFilePath(string filename)
    {
        string filpath = Path.Combine(Application.persistentDataPath, filename);
        return filpath;
    }

    public static string SubString(string target, string sub, int offset)
    {
        int idx = target.IndexOf(sub);
        if (idx < 0) return string.Empty;

        string r = target.Substring(idx+offset);
        return r;
    }

    public static string SubJsonString(string target, string s, string e, int sOffset = 0, int eOffset = 0)
    {
        int startIdx = target.IndexOf(s);
        if (startIdx < 0) return string.Empty;
        int endIdx = target.IndexOf(e, startIdx);
        if (endIdx < 0) return string.Empty;

        startIdx = Mathf.Clamp(startIdx + sOffset, 0, target.Length);
        endIdx = Mathf.Clamp(endIdx - eOffset, 0, target.Length);
        string r = target.Substring(startIdx, endIdx - startIdx);
        return r;
    }

    public static string RemoveSpecialChar(string str)
    {
        string result = Regex.Replace(str, @"[^0-9°¡-ÆR\s]", "");
        return result;
    }

    public static string DecodeEncodedNonAsciiCharacters(string value)
    {
        return Regex.Replace(
               value,
               @"\\u(?<Value>[a-zA-Z0-9]{4})",
               m =>
               {
                   return ((char)int.Parse(m.Groups["Value"].Value, NumberStyles.HexNumber)).ToString();
               });
    }

}
