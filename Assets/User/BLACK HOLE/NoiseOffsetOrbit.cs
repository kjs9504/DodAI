using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class NoiseOffsetOrbit : MonoBehaviour
{
    [SerializeField] private float angularSpeed = 0.5f;
    [SerializeField] private float radius = 0.25f;
    [SerializeField] private float phase = 0f;

    private Renderer rend;
    private Material materialInstance; // MaterialPropertyBlock 대신 사용
    private static readonly int NoiseOffsetID = Shader.PropertyToID("_SwirlOffset"); // 블랙홀 셰이더용

    void Awake()
    {
        rend = GetComponent<Renderer>();
        // 머티리얼 인스턴스 생성 (XR에서 더 안정적)
        materialInstance = new Material(rend.material);
        rend.material = materialInstance;
    }

    void Update()
    {
        float ang = phase + Time.time * angularSpeed;
        float ox = Mathf.Cos(ang) * radius;
        float oy = Mathf.Sin(ang) * radius;

        Vector4 offset = new Vector4(ox, oy, 0, 0);
        materialInstance.SetVector(NoiseOffsetID, offset);
    }

    void OnDestroy()
    {
        if (materialInstance != null)
            DestroyImmediate(materialInstance);
    }
}