using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class NoiseOffsetOrbit : MonoBehaviour
{
    [SerializeField] private float angularSpeed = 0.5f; // rad/sec
    [SerializeField] private float radius = 0.25f;      // ������ �˵� �ݰ�
    [SerializeField] private float phase = 0f;          // ��������(rad)

    private Renderer rend;
    private MaterialPropertyBlock mpb;
    private static readonly int NoiseOffsetID = Shader.PropertyToID("_NoiseOffset");

    void Awake()
    {
        rend = GetComponent<Renderer>();
        mpb = new MaterialPropertyBlock();
        rend.GetPropertyBlock(mpb);
    }

    void Update()
    {
        float ang = phase + Time.time * angularSpeed;
        float ox = Mathf.Cos(ang) * radius;
        float oy = Mathf.Sin(ang) * radius;

        Vector4 cur = mpb.GetVector(NoiseOffsetID);
        cur.x = ox;
        cur.y = oy;
        mpb.SetVector(NoiseOffsetID, cur);
        rend.SetPropertyBlock(mpb);
    }
}
