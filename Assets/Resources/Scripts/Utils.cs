using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Utils
{
    public const float AUDIO_REACTOR_FACTOR = 32767f; //to convert float to Int16

    public const string BASE_LINE = "Hello Everyone. Nice to meet you. Are your READY for make some noise!!!";
    public const string PERSONALITY_TITLE = " Àº/´Â ¾î¶² ¼º°ÝÀ» °¡Áö°í ÀÖ³ª¿ä?";
    public const string FEWSHOT_TITLE = " Àº/´Â ¾î¶²½ÄÀ¸·Î ´ëÈ­ÇÏ³ª¿ä?";
    public const long FILE_SIZE_LIMIT = 350 * 1024; // 350KB

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

    public static Toggle AddCharToggle(ToggleGroup tglGroup, GameObject prefab)
    {
        if (tglGroup == null || prefab == null) return null;

        GameObject go = GameObject.Instantiate(prefab, tglGroup.transform);
        Toggle tgl = go.GetComponent<Toggle>();
        if(tgl == null)
        {
            Debug.LogError("[Toggle] " + prefab.name + " has no toggle");
            GameObject.Destroy(go);
            return null;
        }

        tglGroup.RegisterToggle(tgl);
        tgl.group = tglGroup;

        return tgl;
    }
}
