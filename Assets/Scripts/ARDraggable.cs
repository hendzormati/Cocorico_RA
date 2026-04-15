using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class ARDraggable : MonoBehaviour
{
    Camera cam;
    bool dragging;
    Vector2 startTouch;
    Transform trackedImageTransform;
    ARTrackedImage trackedImage;
    string imageName;

    void Start()
    {
        cam = Camera.main;
        // find parent tracked image (we expect prefab is parented to ARTrackedImage)
        trackedImage = GetComponentInParent<ARTrackedImage>();
        if (trackedImage != null)
        {
            trackedImageTransform = trackedImage.transform;
            imageName = trackedImage.referenceImage.name;
        }
    }

    void Update()
    {
        if (Input.touchCount == 0)
        {
            // support mouse for editor
            if (Input.GetMouseButtonUp(0) && dragging) EndDrag();
            if (Input.GetMouseButton(0) && dragging) DragTo(Input.mousePosition);
            if (Input.GetMouseButtonDown(0)) TryBeginDrag(Input.mousePosition);
            return;
        }

        Touch t = Input.GetTouch(0);
        if (t.phase == TouchPhase.Began) TryBeginDrag(t.position);
        else if ((t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary) && dragging) DragTo(t.position);
        else if (t.phase == TouchPhase.Ended && dragging) EndDrag();
    }

    void TryBeginDrag(Vector2 screenPos)
    {
        var ray = cam.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out var hit))
        {
            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                dragging = true;
                startTouch = screenPos;
                // ensure we are parented to the tracked image
                if (trackedImageTransform == null)
                {
                    trackedImage = GetComponentInParent<ARTrackedImage>();
                    if (trackedImage) trackedImageTransform = trackedImage.transform;
                }
            }
        }
    }

    void DragTo(Vector2 screenPos)
    {
        if (cam == null || trackedImageTransform == null) return;

        // Plane defined by the tracked image (normal = trackedImage.forward)
        Plane imagePlane = new Plane(trackedImageTransform.forward, trackedImageTransform.position);
        Ray ray = cam.ScreenPointToRay(screenPos);
        if (imagePlane.Raycast(ray, out float enter))
        {
            Vector3 worldPoint = ray.GetPoint(enter);
            // Set world pos then compute local to parent so we stay attached properly
            transform.position = worldPoint;
            transform.SetParent(trackedImageTransform, true);
            // optional: keep prefab orientation facing camera or original rotation
            // transform.localRotation = Quaternion.identity;
        }
    }

    void EndDrag()
    {
        dragging = false;
        // save final local position for this image so you can reuse it next time
        if (trackedImage != null)
        {
            Vector3 localPos = transform.localPosition;
            PlayerPrefs.SetFloat(imageName + "_pos_x", localPos.x);
            PlayerPrefs.SetFloat(imageName + "_pos_y", localPos.y);
            PlayerPrefs.SetFloat(imageName + "_pos_z", localPos.z);
            PlayerPrefs.Save();
            Debug.Log($"Saved position for {imageName}: {localPos}");
        }
    }
}