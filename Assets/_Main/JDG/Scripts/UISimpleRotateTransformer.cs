/*
 * UI용 간단한 회전 Transformer
 * OneGrabRotateTransformer의 복잡한 계산 대신 UI에 특화된 단순한 회전 제공
 */

using UnityEngine;
using Oculus.Interaction;

public class UISimpleRotateTransformer : MonoBehaviour, ITransformer
{
    [SerializeField]
    [Tooltip("회전 감도 (1.0이 기본값)")]
    private float _rotationSensitivity = 1.0f;

    [SerializeField]
    [Tooltip("Z축 회전만 허용 (UI용 권장)")]
    private bool _constrainToZAxis = true;

    [SerializeField]
    [Tooltip("최소 회전 각도 제한")]
    private float _minRotationAngle = -360f;

    [SerializeField]
    [Tooltip("최대 회전 각도 제한")]
    private float _maxRotationAngle = 360f;

    private IGrabbable _grabbable;
    private RectTransform _rectTransform;

    // 초기 상태 저장
    private Vector3 _initialGrabDirection;
    private Quaternion _initialRotation;
    private Vector3 _pivotPosition;
    private float _currentTotalRotation = 0f;

    // VR UI 크기 보존용
    private Vector2 _originalSizeDelta;
    private Vector3 _originalLocalScale;

    public void Initialize(IGrabbable grabbable)
    {
        _grabbable = grabbable;
        _rectTransform = _grabbable.Transform.GetComponent<RectTransform>();

        // RectTransform이 없으면 경고
        if (_rectTransform == null)
        {
            Debug.LogWarning($"UISimpleRotateTransformer: {gameObject.name}에 RectTransform이 없습니다. 일반 Transform을 사용합니다.");
        }
    }

    public void BeginTransform()
    {
        if (_grabbable.GrabPoints.Count == 0) return;

        var grabPoint = _grabbable.GrabPoints[0];
        var targetTransform = _grabbable.Transform;

        // Pivot 위치 결정 (RectTransform의 실제 pivot 사용)
        if (_rectTransform != null)
        {
            _pivotPosition = _rectTransform.position;
            // 원본 크기 저장 (VR에서 중요!)
            _originalSizeDelta = _rectTransform.sizeDelta;
            _originalLocalScale = _rectTransform.localScale;
        }
        else
        {
            _pivotPosition = targetTransform.position;
            _originalLocalScale = targetTransform.localScale;
        }

        // 초기 잡기 방향과 회전 저장
        _initialGrabDirection = (grabPoint.position - _pivotPosition).normalized;
        _initialRotation = targetTransform.rotation;

        // 현재 총 회전량 초기화
        _currentTotalRotation = 0f;
    }

    public void UpdateTransform()
    {
        if (_grabbable.GrabPoints.Count == 0) return;

        var grabPoint = _grabbable.GrabPoints[0];
        var targetTransform = _grabbable.Transform;

        // 현재 잡기 방향 계산
        Vector3 currentGrabDirection = (grabPoint.position - _pivotPosition).normalized;

        float rotationAngle;

        if (_constrainToZAxis)
        {
            // VR에서 더 안정적인 회전 계산
            Vector3 toGrab = grabPoint.position - _pivotPosition;
            Vector3 initialToGrab = _initialGrabDirection * toGrab.magnitude;

            // 카메라의 forward 벡터를 기준으로 회전 계산
            Vector3 cameraForward = Camera.main != null ? Camera.main.transform.forward : Vector3.forward;
            rotationAngle = Vector3.SignedAngle(initialToGrab, toGrab, cameraForward);
        }
        else
        {
            // 3D 회전 (전체 축)
            rotationAngle = Vector3.SignedAngle(_initialGrabDirection, currentGrabDirection,
                                              (_pivotPosition - Camera.main.transform.position).normalized);
        }

        // 감도 적용
        rotationAngle *= _rotationSensitivity;

        // 회전 제한 적용
        float clampedAngle = Mathf.Clamp(rotationAngle, _minRotationAngle, _maxRotationAngle);

        // RectTransform 크기 보존을 위한 특별 처리
        Vector2 originalSizeDelta = Vector2.zero;
        if (_rectTransform != null)
        {
            originalSizeDelta = _rectTransform.sizeDelta;
        }

        // 회전 적용
        if (_constrainToZAxis)
        {
            targetTransform.rotation = _initialRotation * Quaternion.Euler(0, 0, clampedAngle);
        }
        else
        {
            Vector3 rotationAxis = (_pivotPosition - Camera.main.transform.position).normalized;
            targetTransform.rotation = _initialRotation * Quaternion.AngleAxis(clampedAngle, rotationAxis);
        }

        // RectTransform 크기 복원 (VR에서 중요!)
        if (_rectTransform != null && originalSizeDelta != Vector2.zero)
        {
            _rectTransform.sizeDelta = originalSizeDelta;
            _rectTransform.ForceUpdateRectTransforms();
        }

        _currentTotalRotation = clampedAngle;
    }

    public void EndTransform()
    {
        // 회전 끝날 때 정리 작업 (필요시)
        if (_rectTransform != null)
        {
            // RectTransform의 경우 추가 정리 작업
            _rectTransform.ForceUpdateRectTransforms();
        }
    }

    // Inspector에서 실시간으로 값 변경 확인용
    private void OnValidate()
    {
        _rotationSensitivity = Mathf.Max(0.1f, _rotationSensitivity);

        if (_minRotationAngle > _maxRotationAngle)
        {
            _maxRotationAngle = _minRotationAngle;
        }
    }

    // 현재 회전량 확인용 (디버깅)
    public float GetCurrentRotation()
    {
        return _currentTotalRotation;
    }

    // 회전 리셋 (필요시)
    public void ResetRotation()
    {
        if (_grabbable != null && _grabbable.Transform != null)
        {
            _grabbable.Transform.rotation = Quaternion.identity;
            _currentTotalRotation = 0f;
        }
    }
}