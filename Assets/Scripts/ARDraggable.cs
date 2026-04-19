using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class ARDraggable : MonoBehaviour
{
    Camera cam;
    bool dragging;

    ARTrackedImage trackedImage;
    string imageName;

    Vector2 lastTouchPos;

    [Header("Scale")]
    [SerializeField] float minScale = 1f;
    [SerializeField] float maxScale = 3.5f;

    float initialPinchDistance;
    Vector3 initialScale;
    Transform visual;

    void Start()
    {
        cam = Camera.main;

        trackedImage = GetComponentInParent<ARTrackedImage>();

        if (trackedImage != null)
        {
            imageName = trackedImage.referenceImage.name;

            transform.localPosition = Vector3.zero;

            transform.localPosition += new Vector3(0, 0, 0.02f);

            transform.localScale = Vector3.one;

            Debug.Log($"[Tracking] {imageName} forced to center");
        }
        visual = transform.Find("VisualCenter");

        if (visual == null)
        {
            Debug.LogWarning("VisualCenter not found, using root");
            visual = transform;
        }
    }

    void Update()
    {
        if (cam == null || trackedImage == null) return;

        if (Input.touchCount == 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            float dist = Vector2.Distance(t0.position, t1.position);

            if (t1.phase == TouchPhase.Began)
            {
                initialPinchDistance = dist;
                initialScale = transform.localScale;
            }
            else
            {
                float factor = Mathf.Pow(dist / initialPinchDistance, 1.2f);

                float scale = Mathf.Clamp(
                    initialScale.x * factor,
                    minScale,
                    maxScale
                );

                transform.localScale = Vector3.one * scale;
            }

            return;
        }

        if (Input.touchCount == 0)
        {
            if (Input.GetMouseButtonDown(0)) BeginDrag(Input.mousePosition);
            if (Input.GetMouseButton(0) && dragging) Drag(Input.mousePosition);
            if (Input.GetMouseButtonUp(0)) dragging = false;
            return;
        }


        Touch t = Input.GetTouch(0);

        if (t.phase == TouchPhase.Began)
            BeginDrag(t.position);
        else if (t.phase == TouchPhase.Moved && dragging)
            Drag(t.position);
        else if (t.phase == TouchPhase.Ended)
            dragging = false;
    }
    void LateUpdate()
    {
        if (cam == null || visual == null) return;

        Vector3 direction = visual.position - cam.transform.position;

        visual.rotation = Quaternion.LookRotation(direction);

        visual.Rotate(0, 180f, 0);
    }
    void BeginDrag(Vector2 screenPos)
    {
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject(
                Input.touchCount > 0 ? Input.GetTouch(0).fingerId : -1))
            return;

        Ray ray = cam.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out var hit))
        {
            if (hit.collider.GetComponentInParent<ARDraggable>() == this)
            {
                dragging = true;
                lastTouchPos = screenPos;
            }
        }
    }

    [SerializeField] float dragSensitivity = 0.0020f;

    void Drag(Vector2 screenPos)
    {
        Vector2 delta = screenPos - lastTouchPos;
        lastTouchPos = screenPos;
        Vector3 right = cam.transform.right;
        Vector3 up = cam.transform.up;
        Vector3 move =
            (right * delta.x + up * delta.y) *
            dragSensitivity;

        transform.position += move;

        ClampToCameraView();
    }

    void ClampToCameraView()
    {
        Vector3 pos = transform.position;

        Vector3 viewPos = cam.WorldToViewportPoint(pos);

        viewPos.x = Mathf.Clamp(viewPos.x, 0.05f, 0.95f);
        viewPos.y = Mathf.Clamp(viewPos.y, 0.05f, 0.95f);

        transform.position = cam.ViewportToWorldPoint(viewPos);
    }
}