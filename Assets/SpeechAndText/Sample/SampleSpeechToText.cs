using System.Collections.Generic;
using System.Threading.Tasks;
using OpenAI;
using UnityEngine;
using TextSpeech;
using UnityEngine.Android;
using UnityEngine.Networking;
using Voxell.Audio;
using Random = UnityEngine.Random;

public class SampleSpeechToText : MonoBehaviour
{
    public GameObject androidAudioVisualizer;
    public GameObject iosAudioVisualizer;

    public bool isShowPopupAndroid = true;
    public GameObject loading;

    private OpenAIApi openai = new OpenAIApi("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");

    private string prompt =
        "Act as an AI and you are a helpful and friendly AI with a conversational tone, your conversations should be as short and helpful as possible unless I asked you to explain more but be short!!.";

    private const string BaseUrl = "http://54.166.66.9:59125/api/tts";
    private const string VoiceParameter = "en_UK/apope_low";
    private const string NoiseScaleParameter = "0.667";
    private const string NoiseWParameter = "0.8";
    private const string LengthScaleParameter = "1";
    private const string SSMLParameter = "false";

    private List<ChatMessage> messages = new List<ChatMessage>();

    public AudioSource _audioSource;

    public AudioCore audioCore;

    // public float startTime;
    // public float endTime;

    void Start()
    {
#if UNITY_IOS
        androidAudioVisualizer.SetActive(false);
        iosAudioVisualizer.SetActive(true);
#else
        iosAudioVisualizer.SetActive(false);
        androidAudioVisualizer.SetActive(true);
#endif
        audioCore = GetComponent<AudioCore>();

        audioCore.idleVelocity = new Vector2(0, 0);

        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }

        Setting("en-US");
        loading.SetActive(false);
        SpeechToText.Instance.onResultCallback = OnResultSpeech;
#if UNITY_ANDROID
        SpeechToText.Instance.isShowPopupAndroid = isShowPopupAndroid;
        Permission.RequestUserPermission(Permission.Microphone);
#else
        //toggleShowPopupAndroid.gameObject.SetActive(false);
#endif
    }

    private void Update()
    {
        if (!_audioSource.isPlaying)
        {
            audioCore.idleVelocity = new Vector2(0, 0);
        }
    }

    //Speech To Text starts here
    public void StartRecording()
    {
#if UNITY_EDITOR
#else
        SpeechToText.Instance.StartRecording("Speak any");
#endif
    }

    public void StopRecording()
    {
        // startTime = Time.time;
#if UNITY_EDITOR
        SendReply("What do you know about Development Alternatives Incorporated - DAI");
        // OnResultSpeech("Not support in editor.");
#else
        SpeechToText.Instance.StopRecording();
#endif
#if UNITY_IOS
        loading.SetActive(true);
#endif
    }

    void OnResultSpeech(string _data)
    {
        SendReply(_data);
#if UNITY_IOS
        loading.SetActive(false);
#endif
    }


    /// <summary>
    /// </summary>
    /// <param name="code"></param>
    public void Setting(string code)
    {
        SpeechToText.Instance.Setting(code);
    }

    /// <summary>
    /// </summary>
    /// <param name="value"></param>
    public void OnToggleShowAndroidPopupChanged(bool value)
    {
        isShowPopupAndroid = value;
        SpeechToText.Instance.isShowPopupAndroid = isShowPopupAndroid;
    }
    // Speech To Text ends here

    // Sending the text generated from Speech To Text to OpenAI API (GPT-3.5 Turbo)
    // Chat with GPT-3.5 Turbo
    private async Task SendReply(string text)
    {
        var newMessage = new ChatMessage()
        {
            Role = "user",
            Content = text
        };
        if (messages.Count == 0)
        {
            newMessage.Content = prompt + "\n" + text;
        }

        messages.Add(newMessage);

        var completionRequest = new CreateChatCompletionRequest()
        {
            Model = "gpt-3.5-turbo",
            Messages = messages
        };

        var completionResponse = await openai.CreateChatCompletion(completionRequest);

        if (completionResponse.Choices != null && completionResponse.Choices.Count > 0)
        {
            var message = completionResponse.Choices[0].Message;
            message.Content = message.Content.Trim();
            // endTime = Time.time;
            // float time = endTime - startTime;
            // Debug.Log("chatgpt Time: " + time);
            // Send the text generated from GPT-3.5 Turbo to Text To Speech API
            await SendPostRequest(message.Content);
            messages.Add(message);
        }
        else
        {
            Debug.LogWarning("No text was generated from this prompt.");
        }
    }

    // Text To Speech starts here
    // Text to Speech to make the AI speak
    private async Task SendPostRequest(string rawBody)
    {
        // startTime = Time.time;
        string baseUrl = BaseUrl;
        var parameters = GetRequestParameters();

        using (UnityWebRequest webRequest = CreateWebRequest(baseUrl, parameters, rawBody))
        {
            var tcs = new TaskCompletionSource<bool>();

            webRequest.SendWebRequest().completed += operation =>
            {
                if (webRequest.result == UnityWebRequest.Result.ConnectionError ||
                    webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError("POST request error: " + webRequest.error);
                }
                else
                {
                    ProcessResponse(webRequest.downloadHandler.data);
                }

                tcs.SetResult(true);
            };

            await tcs.Task;
        }
    }

    // The parameters (Query Params) for the Text To Speech POST API
    private Dictionary<string, string> GetRequestParameters()
    {
        return new Dictionary<string, string>()
        {
            { "voice", VoiceParameter },
            { "noiseScale", NoiseScaleParameter },
            { "noiseW", NoiseWParameter },
            { "lengthScale", LengthScaleParameter },
            { "ssml", SSMLParameter }
        };
    }

    // Sending the POST request to the Text To Speech API
    // The POST request is sent to the API and the response is processed
    // The response is then converted to an AudioClip and played
    private UnityWebRequest CreateWebRequest(string url, Dictionary<string, string> parameters, string rawBody)
    {
        UnityWebRequest webRequest = UnityWebRequest.Post(url, parameters);

        byte[] rawBodyBytes = System.Text.Encoding.UTF8.GetBytes(rawBody);
        webRequest.uploadHandler = new UploadHandlerRaw(rawBodyBytes);
        webRequest.downloadHandler = new DownloadHandlerAudioClip(url, AudioType.WAV);

        return webRequest;
    }

    // The response from the Text To Speech API is processed here
    // The response is converted to an AudioClip and played
    private void ProcessResponse(byte[] responseData)
    {
        AudioClip audioClip = CreateAudioClipFromData(responseData);

        if (audioClip != null)
        {
            // GameObject audioObject = new GameObject("AudioObject");
            // audioCore.idleNoiseIntensity = 0.05f;
            _audioSource.clip = audioClip;
            int randomValue1 = Random.Range(0, 2) == 0 ? -1 : 1;
            int randomValue2 = Random.Range(0, 2) == 0 ? -1 : 1;
            audioCore.idleVelocity = new Vector2(randomValue1 * 1, randomValue2 * 1);
            // endTime = Time.time;
            // float timeTaken = endTime - startTime;
            // Debug.Log("tts Time taken: " + timeTaken);
            _audioSource.Play();
        }
        else
        {
            Debug.LogError("Failed to create AudioClip from the response data.");
        }
    }

    // Processing the response from the Text To Speech API to an AudioClip to be played
    private AudioClip CreateAudioClipFromData(byte[] audioData)
    {
        const int bitDepth = 16; // Bit depth of the audio samples
        const float normalizationFactor = 1.0f / 32768.0f;

        int numSamples = audioData.Length / (bitDepth / 8);
        float[] audioFloatData = new float[numSamples];

        for (int i = 0; i < numSamples; i++)
        {
            short sample = (short)((audioData[i * 2 + 1] << 8) | audioData[i * 2]);
            audioFloatData[i] = sample * normalizationFactor;
        }

        AudioClip audioClip = AudioClip.Create("AudioClip", numSamples, 1, 22050, false);
        audioClip.SetData(audioFloatData, 0);

        return audioClip;
    }
    // Text To Speech ends here
}