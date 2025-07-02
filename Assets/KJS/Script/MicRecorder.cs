using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

[RequireComponent(typeof(AudioSender))]
public class MicRecorder : MonoBehaviour
{
    [Header("녹음/전송 설정")]
    public AudioSender audioSender;    // Inspector에 할당하거나 Awake에서 자동 할당

    private AudioClip recordedClip;
    private string micDevice;
    private const int maxRecordTime = 300;
    private const int sampleRate = 16000;
    private float startTime;

    void Awake()
    {
        // Inspector에 할당 안 했다면 같은 GameObject의 AudioSender 가져오기
        if (audioSender == null)
            audioSender = GetComponent<AudioSender>();
    }

    void Start()
    {
        micDevice = Microphone.devices.Length > 0
                    ? Microphone.devices[0]
                    : null;
        if (micDevice == null)
            Debug.LogError("마이크를 찾을 수 없습니다.");
    }

    public void StartRecording()
    {
        if (micDevice == null) return;
        recordedClip = Microphone.Start(micDevice, true, maxRecordTime, sampleRate);
        startTime = Time.time;
        Debug.Log("🔴 녹음 시작...");
    }

    public void StopRecordingAndSend()
    {
        if (recordedClip == null)
        {
            Debug.LogError("⚠️ 녹음된 데이터가 없습니다. StartRecording() 호출을 확인하세요.");
            return;
        }
        if (!Microphone.IsRecording(micDevice))
        {
            Debug.LogWarning("⚠️ 현재 녹음 중이 아닙니다.");
            return;
        }

        Microphone.End(micDevice);
        float duration = Time.time - startTime;
        int lengthSamples = Mathf.FloorToInt(duration * sampleRate);
        Debug.Log($"⏹ 녹음 종료: {duration:F2}초, 샘플 수={lengthSamples}");

        // 전체 샘플 가져오기
        var allSamples = new float[recordedClip.samples * recordedClip.channels];
        recordedClip.GetData(allSamples, 0);

        // 실제 길이만큼 잘라내기
        var trimmed = new float[lengthSamples];
        System.Array.Copy(allSamples, trimmed, lengthSamples);

        // 1채널짜리 AudioClip 생성
        var clip = AudioClip.Create("TrimmedClip", lengthSamples, 1, sampleRate, false);
        clip.SetData(trimmed, 0);

        // 파일로 저장
        string filename = "recordedAudio.wav";
        string path = SaveWav(filename, clip);

        // 저장된 파일 전송
        StartCoroutine(audioSender.SendWavToServer(filename));
    }

    private string SaveWav(string filename, AudioClip clip)
    {
        string path = Path.Combine(Application.persistentDataPath, filename);
        byte[] wavData = WavUtility.FromAudioClip(clip);
        File.WriteAllBytes(path, wavData);
        Debug.Log("✅ 저장 완료: " + path);
        return path;
    }
}

