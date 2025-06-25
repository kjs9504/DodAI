using UnityEngine;
using System.IO;

public class MicRecorder : MonoBehaviour
{
    public AudioSender audioSender;
    private AudioClip recordedClip;
    private string micDevice;
    private AudioSource audioSource;

    private const int maxRecordTime = 300; // 최대 5분 (긴 녹음 버퍼 확보)
    private const int sampleRate = 16000;

    private int startPosition; // 녹음 시작 시점 샘플 위치
    private float startTime;   // 녹음 시작 시간

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        micDevice = Microphone.devices.Length > 0 ? Microphone.devices[0] : null;

        if (micDevice == null)
        {
            Debug.LogError("마이크를 찾을 수 없습니다.");
        }
    }

    public void StartRecording()
    {
        if (micDevice == null) return;

        recordedClip = Microphone.Start(micDevice, true, maxRecordTime, sampleRate);
        startTime = Time.time;
        startPosition = Microphone.GetPosition(micDevice);

        Debug.Log("녹음 시작...");
    }

    public void StopRecordingAndSave()
    {
        if (!Microphone.IsRecording(micDevice)) return;

        int endPosition = Microphone.GetPosition(micDevice);
        Microphone.End(micDevice);

        float duration = Time.time - startTime;
        int lengthSamples = Mathf.FloorToInt(duration * sampleRate);

        Debug.Log($"녹음 종료. 실제 녹음 시간: {duration:F2}초, 샘플 수: {lengthSamples}");

        float[] allSamples = new float[recordedClip.samples * recordedClip.channels];
        recordedClip.GetData(allSamples, 0);

        float[] trimmedSamples = new float[lengthSamples];
        System.Array.Copy(allSamples, trimmedSamples, lengthSamples);

        AudioClip trimmedClip = AudioClip.Create("TrimmedClip", lengthSamples, 1, sampleRate, false);
        trimmedClip.SetData(trimmedSamples, 0);

        string filename = "recordedAudio.wav";
        SaveWav(filename, trimmedClip);

        // ✅ 백엔드 전송 시작
        //StartCoroutine(audioSender.SendWavToServer(filename));
    }

    private void SaveWav(string filename, AudioClip clip)
    {
        string filePath = Path.Combine(Application.persistentDataPath, filename);
        byte[] wavData = WavUtility.FromAudioClip(clip);

        File.WriteAllBytes(filePath, wavData);
        Debug.Log("저장 완료: " + filePath);
    }
}
