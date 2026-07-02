using UnityEngine;
using Unity.Cinemachine;

[AddComponentMenu("Cinemachine/Extensions/SphereCast Collision (Pull In)")]
public class CinemachineSphereCastCollision : CinemachineExtension
{
    [Header("Collision")]
    public LayerMask collisionLayers = ~0;
    public float sphereRadius = 0.2f;
    public float minDistance = 0.6f;
    public float collisionOffset = 0.12f;

    [Header("Smoothing (anti zoom pop)")]
    [Tooltip("Velocità quando deve avvicinarsi (più alto = più rapido)")]
    public float zoomInSpeed = 14f;

    [Tooltip("Velocità quando deve tornare lontano (più basso = ritorno più morbido)")]
    public float zoomOutSpeed = 6f;

    [Tooltip("Piccolo delay prima di tornare lontano dopo aver perso l'ostacolo")]
    public float returnDelay = 0.12f;

    [Tooltip("Ignora micro-variazioni di distanza (riduce tremolio su spigoli)")]
    public float distanceEpsilon = 0.02f;

    [Header("Ignore")]
    public Transform ignoreRoot; // Player root

    private readonly RaycastHit[] _hits = new RaycastHit[16];

    private float _currentDist = -1f;
    private float _lastObstructedTime = -999f;

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage,
        ref CameraState state,
        float deltaTime)
    {
        if (stage != CinemachineCore.Stage.Finalize)
            return;

        if (deltaTime <= 0f) deltaTime = Time.deltaTime;
        if (deltaTime <= 0f) deltaTime = 0.016f;

        Vector3 focus = state.ReferenceLookAt;

        // Cinemachine 3: posizione "finale corrente" = RawPosition + PositionCorrection (di altri componenti)
        Vector3 desiredPos = state.RawPosition + state.PositionCorrection;

        Vector3 toCam = desiredPos - focus;
        float rawDist = toCam.magnitude;
        if (rawDist < 0.001f)
            return;

        Vector3 dir = toCam / rawDist;

        // --- SphereCast ---
        int count = Physics.SphereCastNonAlloc(
            focus,
            sphereRadius,
            dir,
            _hits,
            rawDist,
            collisionLayers,
            QueryTriggerInteraction.Ignore);

        bool obstructed = false;
        float bestDist = float.PositiveInfinity;

        for (int i = 0; i < count; i++)
        {
            var h = _hits[i];
            if (h.collider == null) continue;

            if (ignoreRoot != null && h.transform.IsChildOf(ignoreRoot))
                continue;

            if (h.distance > 0f && h.distance < bestDist)
            {
                bestDist = h.distance;
                obstructed = true;
            }
        }

        float targetDist = rawDist;

        if (obstructed)
        {
            _lastObstructedTime = Time.time;
            targetDist = Mathf.Max(minDistance, bestDist - collisionOffset);
        }
        else
        {
            // piccolo “hold” per evitare rimbalzi quando l’ostacolo sparisce/riappare in 1 frame
            if (Time.time - _lastObstructedTime < returnDelay)
                targetDist = (_currentDist > 0f) ? _currentDist : rawDist;
        }

        if (_currentDist < 0f) _currentDist = targetDist;

        float diff = Mathf.Abs(targetDist - _currentDist);
        if (diff > distanceEpsilon)
        {
            float speed = (targetDist < _currentDist) ? zoomInSpeed : zoomOutSpeed;
            // smoothing frame-rate indipendente
            float t = 1f - Mathf.Exp(-speed * deltaTime);
            _currentDist = Mathf.Lerp(_currentDist, targetDist, t);
        }

        Vector3 correctedPos = focus + dir * _currentDist;
        state.PositionCorrection += (correctedPos - desiredPos);
    }
}
