using UnityEngine;
using System.IO;
using System;

public static class WavUtility
{
    public static byte[] FromAudioClip(AudioClip clip)
    {
        float[] samples = new float[clip.samples];
        clip.GetData(samples, 0);

        byte[] pcmData = new byte[samples.Length * 2];
        int offset = 0;
        foreach (float sample in samples)
        {
            short intSample = (short)(sample * short.MaxValue);
            pcmData[offset++] = (byte)(intSample & 0xFF);
            pcmData[offset++] = (byte)((intSample >> 8) & 0xFF);
        }

        byte[] wav = new byte[44 + pcmData.Length];
        using (MemoryStream stream = new MemoryStream(wav))
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            // RIFF ���
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + pcmData.Length);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

            // fmt ����ûũ
            writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16); // ����ûũ ũ��
            writer.Write((short)1); // PCM
            writer.Write((short)clip.channels);
            writer.Write(clip.frequency);
            writer.Write(clip.frequency * clip.channels * 2); // ����Ʈ��
            writer.Write((short)(clip.channels * 2)); // �� ����
            writer.Write((short)16); // ��Ʈ ����

            // data ����ûũ
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            writer.Write(pcmData.Length);
            writer.Write(pcmData);
        }

        return wav;
    }
}

