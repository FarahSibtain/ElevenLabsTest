/**
 * An example script on how to use ElevenLabs APIs in a Unity script.
 *
 * More info at https://www.davideaversa.it/blog/elevenlabs-text-to-speech-unity-script/
 */

using System;
using System.Collections;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class ElevenlabsAPI2 : MonoBehaviour
{
    [SerializeField]
    private string _voiceId;
    [SerializeField]
    private string _key;
    [SerializeField]
    private string _apiUrl = "https://api.elevenlabs.io";
    [SerializeField]
    private TMPro.TMP_InputField _inputField;
    [SerializeField]
    private AudioSource audioSource;
    //private const string _apiUrl = "https://api.elevenlabs.io";
    //private const string _key = "deb1ab0544c1e8aa70035c91d8780c0e";
    VoiceSettings voiceSettings = new VoiceSettings();
    // If true, the audio will be streamed instead of downloaded
    // Unfortunately, Unity has some problems with streaming audio
    // but I left this option here in case you want to try it.
    private bool Streaming = false;

    [Range(0, 4)]
    public int LatencyOptimization;
    [Min(1024)] public ulong bytesToDownloadBeforePlaying = 4096;

    // This event is used to broadcast the received AudioClip
    public UnityEvent<AudioClip> AudioReceived;

    public void GetAudio()
    {        
        StartCoroutine(DoRequest(_voiceId, _inputField.text));
    }

    IEnumerator DoRequest(string VoiceID, string message)
    {
        var postData = new TextToSpeechRequest
        {
            text = message,
            model_id = "eleven_monolingual_v1",
            voice_settings = voiceSettings
        };

        // TODO: This could be easily exposed in the Unity inspector,
        // but I had no use for it in my work demo.
        //var voiceSetting = new VoiceSettings
        //{
        //    stability = elevenLabsVoiceSettings.Stability,
        //    similarity_boost = elevenLabsVoiceSettings.Similarity,
        //    style = elevenLabsVoiceSettings.Style,
        //    use_speaker_boost = elevenLabsVoiceSettings.Use_speaker_boost
        //};
        //postData.voice_settings = voiceSettings;
        var json = JsonConvert.SerializeObject(postData);
        var stream = (Streaming) ? "/stream" : "";
        var url = $"{_apiUrl}/v1/text-to-speech/{VoiceID}{stream}?optimize_streaming_latency={LatencyOptimization}";
        //var url = $"{_apiUrl}/v1/text-to-speech/{VoiceID}?optimize_streaming_latency={LatencyOptimization}";
        using (var request = UnityWebRequest.Post(url, string.Empty))
        using (var uH = new UploadHandlerRaw(Encoding.ASCII.GetBytes(json)))
        {
            request.uploadHandler = uH;
            DownloadHandlerAudioClip downloadHandler = new DownloadHandlerAudioClip(url, AudioType.MPEG);
            downloadHandler.streamAudio = true;
            request.downloadHandler = downloadHandler;
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("xi-api-key", _key);
            request.SetRequestHeader("Accept", "audio/mpeg");
            Debug.Log("Sending text message");
            var download = request.SendWebRequest();
            yield return null;

            while (request.downloadedBytes > 0 && request.downloadedBytes < bytesToDownloadBeforePlaying)
            {
                Debug.Log("response received");
                if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogWarning($"Error downloading audio stream from:{url} : {request.error}");
                    yield break;
                }
                yield return null;
            }
            // Debug.Log("Request sent for audio clip");            
            AudioClip audioClip = downloadHandler.audioClip; // DownloadHandlerAudioClip.GetContent(request);
            if (audioClip == null)
            {
                Debug.Log("Couldn't process audio stream.");
                yield break;
            }
            AudioSource.PlayClipAtPoint(audioClip, Vector3.zero);
            Debug.Log("streaming audio - downloadHandler.audioClip.loadType " + downloadHandler.audioClip.loadType);
            //audioSrc.Play();
            //AudioReceived.Invoke(audioClip);

            //yield return download;
            while (!request.isDone)
                yield return null;
            Debug.Log("Finished downloading audio stream!");

            request.disposeUploadHandlerOnDispose = true;
            request.disposeDownloadHandlerOnDispose = true;
            uH.Dispose();
            request.uploadHandler.Dispose();
            request.downloadHandler.Dispose();
            request.Dispose();
        }
    }

    public void AudioClipReceived(AudioClip clip)
    {
        Debug.Log("AudioClip received");
        audioSource.PlayOneShot(clip);
    }

    [Serializable]
    public class TextToSpeechRequest
    {
        public string text;
        public string model_id; // eleven_monolingual_v1
        public VoiceSettings voice_settings;
    }

    [Serializable]
    public class VoiceSettings
    {
        public float stability = 0.71f; // 0
        public float similarity_boost = 0.5f; // 0
        public float style = 0; // 0.5
        public bool use_speaker_boost = true; // true        
    }
}
