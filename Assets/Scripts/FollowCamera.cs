using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    public Camera targetCamera;
    public float distanceFromCamera = 1.5f;
    public Vector3 offset = Vector3.zero;
    public float smooth = 10f;

    void LateUpdate()
    {
        if (targetCamera == null) return;

        Vector3 targetPos =
            targetCamera.transform.position +
            targetCamera.transform.forward * distanceFromCamera +
            offset;

        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * smooth);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetCamera.transform.rotation,
            Time.deltaTime * smooth
        );
    }
}