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
    private Image micButtonImage;
    private Color micIdleColor = new Color(0.13f, 0.19f, 0.28f, 1f);

    // Wire this to MicButton's OnClick.
    public void OnMicButtonPressed()
    {
        if (!isRecording && !HasOpenAIKey())
        {
            if (micButtonLabel != null)
                micButtonLabel.text = "Voice";
            SetButtonState(recording: false, transcribing: false);
            const string message = "Voice needs a valid OpenAI API key in the MicButton settings. Paste the key into the VoiceInputController's API Key field in Unity.";
            if (chatbot != null)
                chatbot.ShowNotice(message);
            Debug.LogError("[Voice] " + message);
            return;
        }

        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
            MenuToggle.CloseOpenMenu();
            return; // user needs to tap again after granting
        }

        if (!isRecording)
        {
            StartRecording();
        }
        else StopRecordingAndSend();

        MenuToggle.CloseOpenMenu();
    }

    void StartRecording()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("[Voice] No microphone found on this device.");
            if (chatbot != null)
                chatbot.ShowNotice("No microphone was found. Connect or enable a microphone, then try Voice again.");
            return;
        }

        micDevice = Microphone.devices[0];
        recordedClip = Microphone.Start(micDevice, false, maxRecordSeconds, sampleRate);
        if (recordedClip == null)
        {
            Debug.LogError("[Voice] The microphone could not start recording.");
            if (chatbot != null)
                chatbot.ShowNotice("The microphone could not start. Check AnatoLens microphone permission in Android settings.");
            return;
        }

        isRecording = true;
        if (micButtonLabel != null) micButtonLabel.text = "Stop";
        SetButtonState(recording: true, transcribing: false);
    }

    void StopRecordingAndSend()
    {
        int lastPos = Microphone.GetPosition(micDevice);
        Microphone.End(micDevice);
        isRecording = false;
        if (micButtonLabel != null) micButtonLabel.text = "Voice";
        SetButtonState(recording: false, transcribing: true);

        if (lastPos <= 0)
        {
            Debug.LogWarning("[Voice] No audio captured.");
            SetButtonState(recording: false, transcribing: false);
            if (chatbot != null)
                chatbot.ShowNotice("I didn't capture any speech. Tap the microphone, speak, then tap it again to finish.");
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
        SetButtonState(recording: false, transcribing: true);

        WWWForm form = new WWWForm();
        form.AddBinaryData("file", wavData, "recording.wav", "audio/wav");
        form.AddField("model", "whisper-1");

        using (UnityWebRequest request = UnityWebRequest.Post(whisperUrl, form))
        {
            request.SetRequestHeader("Authorization", "Bearer " + apiKey.Trim());

            yield return request.SendWebRequest();

            if (micButtonLabel != null) micButtonLabel.text = "Voice";
            SetButtonState(recording: false, transcribing: false);

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
                string notice = request.responseCode == 401
                    ? "OpenAI rejected the Voice API key. Check that the MicButton has an active OpenAI key."
                    : request.responseCode == 429
                        ? "Voice API usage or billing limit reached. Check your OpenAI API account."
                        : "Voice transcription failed. Check the internet connection and try again.";
                if (chatbot != null)
                    chatbot.ShowNotice(notice);
            }
        }
    }

    private void SetButtonState(bool recording, bool transcribing)
    {
        if (micButtonImage == null && micButtonLabel != null)
        {
            Transform buttonTransform = micButtonLabel.transform.parent;
            if (buttonTransform != null)
                micButtonImage = buttonTransform.GetComponent<Image>();
        }

        if (micButtonImage == null)
            return;

        if (recording)
            micButtonImage.color = new Color(0.78f, 0.16f, 0.22f, 1f);
        else if (transcribing)
            micButtonImage.color = new Color(0.08f, 0.52f, 0.58f, 1f);
        else
            micButtonImage.color = micIdleColor;
    }

    private bool HasOpenAIKey()
    {
        string key = apiKey != null ? apiKey.Trim() : "";
        return key.StartsWith("sk-") && key.Length > 20;
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
