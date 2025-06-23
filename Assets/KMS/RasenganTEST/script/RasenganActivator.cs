using System;
using System.Collections;
using Oculus.Interaction;
using UnityEngine;

public class RasenganActivator : MonoBehaviour
{
    [Header("Gesture Events")]
    [SerializeField] private SelectorUnityEventWrapper ninjaGestureRight;
    [SerializeField] private SelectorUnityEventWrapper rasenganGestureRight;
    [SerializeField] private SelectorUnityEventWrapper rockPoseRight;

    [Header("Rasengan Object")]
    [SerializeField] private GameObject rasenganPrefab;

    [Header("Hand Reference")]
    [SerializeField] private HandVisual handVisualRight;

    [Header("Shadow Effect")]
    [SerializeField] private Material shadowMaterial;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip ninjaClip;
    [SerializeField] private AudioClip rasenganClip;
    [SerializeField] private AudioClip rockClip;

    private enum RasenganState { Idle, Ready, Active }
    private RasenganState currentState = RasenganState.Idle;

    private Coroutine ninjaCooldownCor;
    private Coroutine followCor;
    private GameObject currentRasengan;

    void Start()
    {
        ninjaGestureRight.WhenSelected.AddListener(OnNinjaGesture);
        rasenganGestureRight.WhenSelected.AddListener(OnRasenganGesture);
        rockPoseRight.WhenSelected.AddListener(OnRockPoseGesture);
    }

    private void OnNinjaGesture()
    {
        if (currentState != RasenganState.Idle || currentRasengan != null) return;

        currentState = RasenganState.Ready;
        PlaySound(ninjaClip);

        CancelCooldown();
        ninjaCooldownCor = StartCoroutine(NinjaCooldown(3f));

        CreateShadow();
    }

    private IEnumerator NinjaCooldown(float duration)
    {
        yield return new WaitForSeconds(duration);
        ninjaCooldownCor = null;
    }

    private void CancelCooldown()
    {
        if (ninjaCooldownCor != null)
        {
            StopCoroutine(ninjaCooldownCor);
            ninjaCooldownCor = null;
        }
    }

    private void OnRasenganGesture()
    {
        if (currentState != RasenganState.Ready || currentRasengan != null) return;

        currentState = RasenganState.Active;
        PlaySound(rasenganClip);

        Transform palmTr = handVisualRight.GetTransformByHandJointId(Oculus.Interaction.Input.HandJointId.HandPalm);
        Vector3 spawnPos = palmTr.position - palmTr.up * 0.15f;

        currentRasengan = Instantiate(rasenganPrefab, spawnPos, Quaternion.identity);
        followCor = StartCoroutine(FollowRasengan(() => palmTr.position - palmTr.up * 0.15f));

        CreateShadow();
    }

    private void OnRockPoseGesture()
    {
        if (currentState != RasenganState.Active || currentRasengan == null) return;

        PlaySound(rockClip);

        if (followCor != null)
        {
            StopCoroutine(followCor);
            followCor = null;
        }

        Destroy(currentRasengan);
        currentRasengan = null;

        currentState = RasenganState.Idle;
        CreateShadow();
    }

    private IEnumerator FollowRasengan(Func<Vector3> followPos)
    {
        while (currentRasengan != null)
        {
            currentRasengan.transform.position = Vector3.Lerp(
                currentRasengan.transform.position, followPos(), 0.1f);
            yield return null;
        }
    }

    private void CreateShadow()
    {
        if (handVisualRight == null || shadowMaterial == null) return;

        SkinnedMeshRenderer skinnedRend = handVisualRight.GetComponentInChildren<SkinnedMeshRenderer>();
        if (skinnedRend == null) return;

        Mesh bakedMesh = new Mesh();
        skinnedRend.BakeMesh(bakedMesh);

        GameObject shadowObj = new GameObject("ShadowObj");
        shadowObj.transform.SetPositionAndRotation(skinnedRend.transform.position, skinnedRend.transform.rotation);

        var meshFilter = shadowObj.AddComponent<MeshFilter>();
        meshFilter.mesh = bakedMesh;

        var meshRenderer = shadowObj.AddComponent<MeshRenderer>();
        meshRenderer.material = new Material(shadowMaterial);

        StartCoroutine(ShadowEffectCor(shadowObj, 0.5f));
    }

    private IEnumerator ShadowEffectCor(GameObject shadowObj, float duration)
    {
        float time = 0f;
        MeshRenderer renderer = shadowObj.GetComponent<MeshRenderer>();
        float startAlpha = renderer.material.color.a;
        Vector3 startScale = shadowObj.transform.localScale;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            Color color = renderer.material.color;
            color.a = Mathf.Lerp(startAlpha, 0f, t);
            renderer.material.color = color;

            shadowObj.transform.localScale = Vector3.Lerp(startScale, startScale * 1.25f, t);
            yield return null;
        }

        Destroy(shadowObj);
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}
