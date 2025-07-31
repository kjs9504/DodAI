using UnityEngine;

public class FloatingRotator : MonoBehaviour
{
    [Header("회전 설정")]
    [SerializeField] private float yRotationSpeed = 90f; // Y축 주 회전 속도

    [Header("무중력 효과 설정")]
    [SerializeField] private float xWobbleAmount = 10f; // X축 흔들림 각도
    [SerializeField] private float zWobbleAmount = 10f; // Z축 흔들림 각도
    [SerializeField] private float wobbleSpeed = 2f; // 흔들림 속도

    private float timeOffset;

    void Start()
    {
        // 각 오브젝트마다 다른 시작점을 가지도록 랜덤 오프셋 설정
        timeOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    void Update()
    {
        float time = Time.time + timeOffset;

        // Y축 기본 회전
        float yRotation = yRotationSpeed * Time.deltaTime;

        // X축과 Z축에 사인파를 이용한 부드러운 흔들림 추가
        float xWobble = Mathf.Sin(time * wobbleSpeed) * xWobbleAmount * Time.deltaTime;
        float zWobble = Mathf.Cos(time * wobbleSpeed * 1.3f) * zWobbleAmount * Time.deltaTime;

        // 모든 축 회전 적용
        transform.Rotate(xWobble, yRotation, zWobble);
    }
}