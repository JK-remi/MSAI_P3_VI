using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class AzureUrls
{
    public const string GPT_ENDPOINT = "https://eastus-project3-team2.openai.azure.com/";
    public const string GPT_KEY = "3eef2399ffdb4aad8a1d577f41d0f348";
    public const string GPT_DEPLOY = "project3-team2-gpt-4o";
    public const string SEARCH_URL = "https://project3team2.search.windows.net";
    public const string SEARCH_KEY = "LOEoj43mzzihlze2MGY6UpuFjc6ViomJd6DD2f8x51AzSeBrEGd9";
    public static string GPT_URL
    {
        get
        {
            return string.Format("{0}/openai/deployments/{1}/chat/completions?api-version=2024-02-15-preview", GPT_ENDPOINT, GPT_DEPLOY);
        }
    }

    public const string SPEECH_REGION = "westus2";
    public const string SPEECH_KEY = "1ff2d7a7379b4e349aa1734718de89fc";   // 57d50f60f0aa4712a266501ca97e9ebb
 
    public const string LANGUAGE = "ko-KR";

    public static string VOICE_LIST_URL
    {
        get
        {
            return string.Format("https://{0}.tts.speech.microsoft.com/cognitiveservices/voices/list", SPEECH_REGION);
        }
    }

    public static string TTS_TOKEN_URL
    {
        get
        {
            return string.Format("https://{0}.api.cognitive.microsoft.com/sts/v1.0/issueToken", SPEECH_REGION);
        }
    }

    public static string TTS_URL
    {
        get
        {
            return string.Format("https://{0}.tts.speech.microsoft.com/cognitiveservices/v1", SPEECH_REGION);
        }
    }
}
