using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenAI;
using UnityEngine;
using TextSpeech;
using TMPro;
using UnityEngine.Android;
using UnityEngine.Networking;
using UnityEngine.UI;
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
        "Act as an AI and you are a helpful and friendly AI with a conversational tone, your answers should be as short and helpful as possible but being short is the most important thing.";
    // "Act as an AI and you are a helpful and friendly AI with a conversational tone, your answers should be as short and helpful as possible unless I asked you to explain more but be short!!.";

    private const string BaseUrl = "http://54.166.66.9:59125/api/tts";

    private string VoiceParameter;
    private string NoiseScaleParameter;
    private string NoiseWParameter;
    private string LengthScaleParameter;
    private string SSMLParameter;

    private Dictionary<string, string> voice;

    private List<ChatMessage> messages = new List<ChatMessage>();

    public AudioSource _audioSource;

    public AudioCore audioCore;

    public float startTime;
    public float endTime;

    [SerializeField] private ScrollRect scroll;

    [SerializeField] private RectTransform sent;
    [SerializeField] private RectTransform received;

    private float height;

    public Dropdown voiceDropdown;

    private String language;

    // public TMP_Text voiceText;


    // private TMP_Dropdown.OptionData voicesEnglish = new TMP_Dropdown.OptionData();
    // private TMP_Dropdown.OptionData voicesSpanish = new TMP_Dropdown.OptionData();

    void Start()
    {
        ChooseLanguage(0);
        ChooseVoice(0);
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
        startTime = Time.time;
#if UNITY_EDITOR
        // SendReply("Hi there");
        // SendReply("What do you know about Development Alternatives Incorporated - DAI");
        SendReply("dime un hecho divertido");
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

        AppendMessage(newMessage);

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
            // Send the text generated from GPT-3.5 Turbo to Text To Speech API
            await SendPostRequest(message.Content);
            messages.Add(message);
            AppendMessage(message);
        }
        else
        {
            Debug.LogWarning("No text was generated from this prompt.");
        }
    }

    private void AppendMessage(ChatMessage message)
    {
        scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0);

        var item = Instantiate(message.Role == "user" ? sent : received, scroll.content);
        item.GetChild(0).GetChild(0).GetComponent<Text>().text = message.Content;
        item.anchoredPosition = new Vector2(0, -height);
        LayoutRebuilder.ForceRebuildLayoutImmediate(item);
        height += item.sizeDelta.y;
        scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        scroll.verticalNormalizedPosition = 0;
    }

    // Text To Speech starts here
    // Text to Speech to make the AI speak
    private async Task SendPostRequest(string rawBody)
    {
        // startTime = Time.time;
        string baseUrl = BaseUrl + "?";
        foreach (KeyValuePair<string, string> parameter in voice)
        {
            baseUrl += $"&{parameter.Key}={parameter.Value}";
        }

        UnityWebRequest webRequest = UnityWebRequest.Post(baseUrl, UnityWebRequest.kHttpVerbPOST);

        byte[] rawBodyBytes = System.Text.Encoding.UTF8.GetBytes(rawBody);
        webRequest.uploadHandler = new UploadHandlerRaw(rawBodyBytes);
        webRequest.downloadHandler = new DownloadHandlerAudioClip(baseUrl, AudioType.WAV);

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

    // public void ChooseVoice(int index)
    // {
    //     switch (index)
    //     {
    //         // case 0:
    //         //     // UK male
    //         //     voicesEnglish[0].Values.ToString();
    //         //     // VoiceParameter = "en_UK/apope_low";
    //         //     // NoiseScaleParameter = "0.667";
    //         //     // NoiseWParameter = "0.8";
    //         //     // LengthScaleParameter = "1";
    //         //     // SSMLParameter = "false";
    //         //     // // voicesEnglish.text = "Male 1";
    //         //     // Setting("en-GB");
    //         //     break;
    //         // case 1:
    //         //     // US male
    //         //     VoiceParameter = "en_US/hifi-tts_low";
    //         //     NoiseScaleParameter = "0.333";
    //         //     NoiseWParameter = "0.333";
    //         //     LengthScaleParameter = "1";
    //         //     SSMLParameter = "false";
    //         //     Setting("en-US");
    //         //     // voicesEnglish.text = "Male 2";
    //         //     break;
    //         // case 2:
    //         //     // US male
    //         //     VoiceParameter = "en_US/m-ailabs_low";
    //         //     NoiseScaleParameter = "0.2";
    //         //     NoiseWParameter = "0.2";
    //         //     LengthScaleParameter = "1";
    //         //     SSMLParameter = "false";
    //         //     Setting("en-US");
    //         //     // voicesEnglish.text = "Male 3";
    //         //     break;
    //         // case 3:
    //         //     // US male
    //         //     VoiceParameter = "en_US/cmu-arctic_low";
    //         //     NoiseScaleParameter = "0.333";
    //         //     NoiseWParameter = "0.333";
    //         //     LengthScaleParameter = "1";
    //         //     SSMLParameter = "false";
    //         //     Setting("en-US");
    //         //     // voicesEnglish.text = "Male 4";
    //         //     break;
    //         // case 4:
    //         //     // US female
    //         //     VoiceParameter = "en_US/ljspeech_low";
    //         //     NoiseScaleParameter = "0.667";
    //         //     NoiseWParameter = "0.8";
    //         //     LengthScaleParameter = "1";
    //         //     SSMLParameter = "false";
    //         //     Setting("en-US");
    //         //     // voicesEnglish.text = "Female 1";
    //         //     break;
    //         // case 5:
    //         //     // UK female
    //         //     VoiceParameter = "en_US/vctk_low";
    //         //     NoiseScaleParameter = "0.333";
    //         //     NoiseWParameter = "0.333";
    //         //     LengthScaleParameter = "1.2";
    //         //     SSMLParameter = "false";
    //         //     Setting("en-GB");
    //         //     // voicesEnglish.text = "Female 2";
    //         //     break;
    //         // case 6:
    //         //     // Español male
    //         //     VoiceParameter = "es_ES/m-ailabs_low";
    //         //     NoiseScaleParameter = "0.333";
    //         //     NoiseWParameter = "0.333";
    //         //     LengthScaleParameter = "1";
    //         //     SSMLParameter = "false";
    //         //     Setting("es-ES");
    //         //     // voicesSpanish.text = "male 1";
    //         //     break;
    //         // default:
    //         //     // UK male
    //         //     VoiceParameter = "en_UK/apope_low";
    //         //     NoiseScaleParameter = "0.667";
    //         //     NoiseWParameter = "0.8";
    //         //     LengthScaleParameter = "1";
    //         //     SSMLParameter = "false";
    //         //     Setting("en-US");
    //         //     break;
    //     }
    //
    //     voice = new Dictionary<string, string>()
    //     {
    //         { "voice", VoiceParameter },
    //         { "noiseScale", NoiseScaleParameter },
    //         { "noiseW", NoiseWParameter },
    //         { "lengthScale", LengthScaleParameter },
    //         { "ssml", SSMLParameter }
    //     };
    // }

    public void ChooseLanguage(int index)
    {
        voiceDropdown.ClearOptions();
        
        switch (index)
        {
            case 0:
                Setting("en-US");
                language = "en-US";
                voiceDropdown.AddOptions(voicesEnglish.Keys.ToList());
                break;
            case 1:
                Setting("es-ES");
                language = "es-ES";
                voiceDropdown.AddOptions(voicesSpanish.Keys.ToList());
                break;
            case 2:
                Setting("fr-FR");
                language = "fr-FR";
                voiceDropdown.AddOptions(voicesFrench.Keys.ToList());
                break;
            case 3:
                Setting("ru-RU");
                language = "ru-RU";
                voiceDropdown.AddOptions(voicesRussian.Keys.ToList());
                break;
            case 4:
                Setting("de-DE");
                language = "de-DE";
                voiceDropdown.AddOptions(voicesGerman.Keys.ToList());
                break;
            case 5:
                Setting("it-IT");
                language = "it-IT";
                voiceDropdown.AddOptions(voicesItalian.Keys.ToList());
                break;
            default:
                Setting("en-US");
                language = "en-US";
                break;
        }
        
        ChooseVoice(0);
    }

    public void ChooseVoice(int index)
    {
        // Make sure voiceDropdown.options has elements
        if (index < 0 || index >= voiceDropdown.options.Count)
        {
            Debug.LogError("Invalid index.");
            return;
        }

        if (language.Equals("en-US"))
        {
            if (voicesEnglish.TryGetValue(voiceDropdown.options[index].text, out var englishVoice))
            {
                voice = englishVoice;
            }
            else
            {
                Debug.LogError("English voice not found.");
            }
        }
        else if (language.Equals("es-ES"))
        {
            if (voicesSpanish.TryGetValue(voiceDropdown.options[index].text, out var spanishVoice))
            {
                voice = spanishVoice;
            }
            else
            {
                Debug.LogError("Spanish voice not found.");
            }
        }
        else if (language.Equals("fr-FR"))
        {
            if (voicesFrench.TryGetValue(voiceDropdown.options[index].text, out var frenchVoice))
            {
                voice = frenchVoice;
            }
            else
            {
                Debug.LogError("French voice not found.");
            }
        }
        else if (language.Equals("ru-RU"))
        {
            if (voicesRussian.TryGetValue(voiceDropdown.options[index].text, out var russianVoice))
            {
                voice = russianVoice;
            }
            else
            {
                Debug.LogError("Russian voice not found.");
            }
        }
        else if(language.Equals("de-DE"))
        {
            if (voicesGerman.TryGetValue(voiceDropdown.options[index].text, out var germanVoice))
            {
                voice = germanVoice;
            }
            else
            {
                Debug.LogError("German voice not found.");
            }
        }
        else if (language.Equals("it-IT"))
        {
            if (voicesItalian.TryGetValue(voiceDropdown.options[index].text, out var italianVoice))
            {
                voice = italianVoice;
            }
            else
            {
                Debug.LogError("Italian voice not found.");
            }
        }
        else
        {
            // Default to English if language is unknown
            if (voicesEnglish.TryGetValue(voiceDropdown.options[index].text, out var defaultVoice))
            {
                voice = defaultVoice;
            }
            else
            {
                Debug.LogError("Voice not found for default language.");
            }
        }
    }

    private Dictionary<String, Dictionary<String, String>> voicesEnglish =
        new Dictionary<String, Dictionary<String, String>>()
        {
            {
                "Male 1",
                new Dictionary<string, string>()
                {
                    { "voice", "en_UK/apope_low" },
                    { "noiseScale", "0.667" },
                    { "noiseW", "0.8" },
                    { "lengthScale", "1" },
                    { "ssml", "false" }
                }
            },
            {
                "Male 2",
                new Dictionary<string, string>()
                {
                    { "voice", "en_US/hifi-tts_low" },
                    { "noiseScale", "0.333" },
                    { "noiseW", "0.333" },
                    { "lengthScale", "1" },
                    { "ssml", "false" }
                }
            },
            {
                "Male 3",
                new Dictionary<string, string>()
                {
                    { "voice", "en_US/m-ailabs_low" },
                    { "noiseScale", "0.2" },
                    { "noiseW", "0.2" },
                    { "lengthScale", "1" },
                    { "ssml", "false" }
                }
            },
            {
                "Male 4",
                new Dictionary<string, string>()
                {
                    { "voice", "en_US/cmu-arctic_low" },
                    { "noiseScale", "0.333" },
                    { "noiseW", "0.333" },
                    { "lengthScale", "1" },
                    { "ssml", "false" }
                }
            },
            {
                "Female 1",
                new Dictionary<string, string>()
                {
                    { "voice", "en_US/ljspeech_low" },
                    { "noiseScale", "0.667" },
                    { "noiseW", "0.8" },
                    { "lengthScale", "1" },
                    { "ssml", "false" }
                }
            },
            {
                "Female 2",
                new Dictionary<string, string>()
                {
                    { "voice", "en_US/vctk_low" },
                    { "noiseScale", "0.333" },
                    { "noiseW", "0.333" },
                    { "lengthScale", "1.2" },
                    { "ssml", "false" }
                }
            }
        };

    private Dictionary<String, Dictionary<String, String>> voicesSpanish =
        new Dictionary<String, Dictionary<String, String>>()
        {
            {
                "Masculino 1",
                new Dictionary<string, string>()
                {
                    { "voice", "es_ES/m-ailabs_low" },
                    { "noiseScale", "0.333" },
                    { "noiseW", "0.333" },
                    { "lengthScale", "1" },
                    { "ssml", "false" }
                }
            }
        };
    
    private Dictionary<String, Dictionary<String, String>> voicesFrench =
        new Dictionary<String, Dictionary<String, String>>()
        {
            {
                "Femelle 1",
                new Dictionary<string, string>()
                {
                    { "voice", "fr_FR/m-ailabs_low" },
                    { "noiseScale", "0.667" },
                    { "noiseW", "0.8" },
                    { "lengthScale", "1" },
                    { "ssml", "false" }
                }
            },
            {
                "Femelle 2",
                new Dictionary<string, string>()
                {
                    { "voice", "fr_FR/siwis_low" },
                    { "noiseScale", "0.667" },
                    { "noiseW", "0.8" },
                    { "lengthScale", "1" },
                    { "ssml", "false" }
                }
            },
            {
                "Mâle 1",
                new Dictionary<string, string>()
                {
                    { "voice", "fr_FR/tom_low" },
                    { "noiseScale", "0.667" },
                    { "noiseW", "0.8" },
                    { "lengthScale", "1" },
                    { "ssml", "false" }
                }
            }
        };
    
    private Dictionary<String, Dictionary<String, String>> voicesRussian =
        new Dictionary<String, Dictionary<String, String>>()
        {
            {
                "Мужской 1",
                new Dictionary<string, string>()
                {
                    { "voice", "ru_RU/multi_low" },
                    { "noiseScale", "0.667" },
                    { "noiseW", "0.8" },
                    { "lengthScale", "1" },
                    { "ssml", "false" }
                }
            }
        };
    
    private Dictionary<String, Dictionary<String, String>> voicesGerman =
        new Dictionary<String, Dictionary<String, String>>()
        {
            {
                "Männlich 1",
                new Dictionary<string, string>()
                {
                    { "voice", "de_DE/thorsten-emotion_low" },
                    { "noiseScale", "0.667" },
                    { "noiseW", "0.8" },
                    { "lengthScale", "1" },
                    { "ssml", "false" }
                }
            },
            {
                "Männlich 2",
                new Dictionary<string, string>()
                {
                    { "voice", "de_DE/thorsten_low" },
                    { "noiseScale", "0.667" },
                    { "noiseW", "0.8" },
                    { "lengthScale", "1" },
                    { "ssml", "false" }
                }
            },
            {
                "Weiblich 1",
                new Dictionary<string, string>()
                {
                    { "voice", "de_DE/m-ailabs_low" },
                    { "noiseScale", "0.333" },
                    { "noiseW", "0.333" },
                    { "lengthScale", "1" },
                    { "ssml", "false" }
                }
            }
        };
    
    private Dictionary<String, Dictionary<String, String>> voicesItalian =
        new Dictionary<String, Dictionary<String, String>>()
        {
            {
                "Maschio 1",
                new Dictionary<string, string>()
                {
                    { "voice", "it_IT/mls_low" },
                    { "noiseScale", "0.333" },
                    { "noiseW", "0.333" },
                    { "lengthScale", "1" },
                    { "ssml", "false" }
                }
            },
            {
                "Maschio 2",
                new Dictionary<string, string>()
                {
                    { "voice", "it_IT/riccardo-fasol_low" },
                    { "noiseScale", "0.667" },
                    { "noiseW", "0.8" },
                    { "lengthScale", "1" },
                    { "ssml", "false" }
                }
            }
        };
}