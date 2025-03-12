using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class AzureUrls
{
    public const string GPT_ENDPOINT = "https://eastus-project3-team2.openai.azure.com/";
    public const string GPT_KEY = "";
    public const string GPT_DEPLOY = "project3-team2-gpt-4o";
    public const string SEARCH_URL = "https://project3team2.search.windows.net";
    public const string SEARCH_KEY = "";
    public static string GPT_URL
    {
        get
        {
            return string.Format("{0}/openai/deployments/{1}/chat/completions?api-version=2024-02-15-preview", GPT_ENDPOINT, GPT_DEPLOY);
        }
    }

    public const string SPEECH_REGION = "westus2";
    public const string SPEECH_KEY = "";   
 
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
