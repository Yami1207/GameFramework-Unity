using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class CameraFollow
{
    public Camera camera;
    public Transform target;

    public float x;
    public float y;
    public float distance = 10;
    public float maxDistance = 10;

    public float xSpeed = 0.2f;
    public float ySpeed = 0.2f;

    public float yMinLimit = -80;
    public float yMaxLimit = 80;

    public float cameraRadius = 0.2f;

    public LayerMask layerMask;

    private RaycastHit[] m_Hits = new RaycastHit[10];

    public void Update()
    {
        Vector3 targetPos = GetTargetPosition();
        Quaternion rotation = Quaternion.Euler(y, x, 0);
        int hitCount = Physics.SphereCastNonAlloc(targetPos, cameraRadius, rotation * Vector3.back, m_Hits, maxDistance, layerMask, QueryTriggerInteraction.Collide);
        if (hitCount > 0)
        {
            int hitIndex = 0;
            RaycastHit hit = m_Hits[hitIndex];
            distance = (hit.point + hit.normal * cameraRadius - targetPos).magnitude;
        }
        else
        {
            distance = maxDistance;
        }
        Vector3 disVector = new Vector3(0, 0, -distance);
        Vector3 position = rotation * disVector + targetPos;

        camera.transform.position = position;
        camera.transform.rotation = rotation;
    }

    public void MoveCamera(Vector2 delta)
    {
        x += delta.x * xSpeed;
        y -= delta.y * ySpeed;
        y = ClampAngle(y, yMinLimit, yMaxLimit);
    }

    private Vector3 GetTargetPosition()
    {
        return target.position + new Vector3(0, 1.5f, 0);
    }

    private static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360)
            angle += 360;
        if (angle > 360)
            angle -= 360;
        return Mathf.Clamp(angle, min, max);
    }
}
