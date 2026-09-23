using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.IO;
using System;

public class VoiceInputController : MonoBehaviour
{
    [Header("References")]
    public ChatbotController chatbot;       // drag ChatPanel here
    public TMP_Text micButtonLabel;         // drag MicButton's own Text (TMP) child here

    [Header("API Settings")]
    [Tooltip("Same key style as ChatbotController/ExplanationPanelController. NEVER ship in a public build.")]
    public string apiKey = "YOUR_API_KEY_HERE";
    private const string whisperUrl = "https://api.openai.com/v1/audio/transcriptions";

    private const int maxRecordSeconds = 15;
    private const int sampleRate = 16000;

    private AudioClip recordedClip;
    private string micDevice;
    private bool isRecording = false;

    // Wire this to MicButton's OnClick.
    public void OnMicButtonPressed()
    {
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
            MenuToggle.CloseOpenMenu();
            return; // user needs to tap again after granting
        }

        if (!isRecording) StartRecording();
        else StopRecordingAndSend();

        MenuToggle.CloseOpenMenu();
    }

    void StartRecording()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("[Voice] No microphone found on this device.");
            return;
        }

        micDevice = Microphone.devices[0];
        recordedClip = Microphone.Start(micDevice, false, maxRecordSeconds, sampleRate);
        isRecording = true;
        if (micButtonLabel != null) micButtonLabel.text = "Stop";
    }

    void StopRecordingAndSend()
    {
        int lastPos = Microphone.GetPosition(micDevice);
        Microphone.End(micDevice);
        isRecording = false;
        if (micButtonLabel != null) micButtonLabel.text = "🎤 Record";

        if (lastPos <= 0)
        {
            Debug.LogWarning("[Voice] No audio captured.");
            return;
        }

        // Trim the clip down to what was actually recorded.
        float[] samples = new float[lastPos * recordedClip.channels];
        recordedClip.GetData(samples, 0);
        byte[] wavData = ConvertToWav(samples, recordedClip.channels, sampleRate);

        StartCoroutine(SendToWhisper(wavData));
    }

    IEnumerator SendToWhisper(byte[] wavData)
    {
        if (micButtonLabel != null) micButtonLabel.text = "Transcribing...";

        WWWForm form = new WWWForm();
        form.AddBinaryData("file", wavData, "recording.wav", "audio/wav");
        form.AddField("model", "whisper-1");

        using (UnityWebRequest request = UnityWebRequest.Post(whisperUrl, form))
        {
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);

            yield return request.SendWebRequest();

            if (micButtonLabel != null) micButtonLabel.text = "🎤 Record";

            if (request.result == UnityWebRequest.Result.Success)
            {
                string transcript = ParseTranscript(request.downloadHandler.text);

                if (!string.IsNullOrWhiteSpace(transcript) && chatbot != null)
                {
                    chatbot.OpenChat();
                    if (chatbot.inputField != null)
                        chatbot.inputField.text = transcript;
                    chatbot.OnSendPressed();
                }
            }
            else
            {
                Debug.LogError("[Voice] Transcription failed: " + request.error + "\n" + request.downloadHandler.text);
            }
        }
    }

    string ParseTranscript(string json)
    {
        try
        {
            int contentIndex = json.IndexOf("\"text\":\"");
            if (contentIndex == -1) return null;

            int start = contentIndex + "\"text\":\"".Length;
            int end = json.IndexOf("\"", start);
            while (end > 0 && json[end - 1] == '\\')
                end = json.IndexOf("\"", end + 1);

            return json.Substring(start, end - start).Replace("\\n", " ").Replace("\\\"", "\"");
        }
        catch
        {
            return null;
        }
    }

    // Minimal 16-bit PCM WAV encoder — no external plugin needed.
    byte[] ConvertToWav(float[] samples, int channels, int frequency)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            int byteRate = frequency * channels * 2;
            int dataSize = samples.Length * 2;

            void WriteString(string s) => stream.Write(System.Text.Encoding.ASCII.GetBytes(s), 0, s.Length);
            void WriteInt(int v) => stream.Write(BitConverter.GetBytes(v), 0, 4);
            void WriteShort(short v) => stream.Write(BitConverter.GetBytes(v), 0, 2);

            WriteString("RIFF");
            WriteInt(36 + dataSize);
            WriteString("WAVE");
            WriteString("fmt ");
            WriteInt(16);
            WriteShort(1); // PCM
            WriteShort((short)channels);
            WriteInt(frequency);
            WriteInt(byteRate);
            WriteShort((short)(channels * 2));
            WriteShort(16);
            WriteString("data");
            WriteInt(dataSize);

            foreach (float sample in samples)
            {
                short s = (short)Mathf.Clamp(sample * short.MaxValue, short.MinValue, short.MaxValue);
                WriteShort(s);
            }

            return stream.ToArray();
        }
    }
}
