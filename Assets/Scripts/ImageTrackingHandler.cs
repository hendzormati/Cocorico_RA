using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ImageTrackingHandler : MonoBehaviour
{
    [SerializeField]
    private GameObject cardPrefab;

    private ARTrackedImageManager _imageManager;
    private Dictionary<string, GameObject> _spawnedObjects = new Dictionary<string, GameObject>();

    void Awake()
    {
        _imageManager = GetComponent<ARTrackedImageManager>();
        if (!_imageManager)
            Debug.LogError("ARTrackedImageManager not found!");
    }

    void OnEnable()
    {
        if (_imageManager)
            _imageManager.trackedImagesChanged += OnTrackedImagesChanged;
    }

    void OnDisable()
    {
        if (_imageManager)
            _imageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        foreach (var trackedImage in eventArgs.added)
        {
            string imageName = trackedImage.referenceImage.name;
            Debug.Log($"Image detected: {imageName}");
            
            // Log image position, rotation, and size
            Debug.Log($"  Image Position: {trackedImage.transform.position}");
            Debug.Log($"  Image Rotation: {trackedImage.transform.rotation.eulerAngles}");
            Debug.Log($"  Image Size: {trackedImage.size}");
            Debug.Log($"  Image Scale: {trackedImage.transform.localScale}");

            if (!_spawnedObjects.ContainsKey(imageName) && cardPrefab)
            {
                GameObject go = Instantiate(cardPrefab, trackedImage.transform);
                go.transform.localRotation = Quaternion.Euler(-90f, 180f, 0f);

                // Find VisualCenter
                Transform marker = go.transform.Find("VisualCenter");
                if (marker != null)
                {
                    Vector3 markerLocal = go.transform.InverseTransformPoint(marker.position);
                    go.transform.localPosition = -markerLocal;
                }
                else
                {
                    go.transform.localPosition = Vector3.zero;
                }
                go.SetActive(true);
                _spawnedObjects[imageName] = go;
                
                // Log card position after spawning
                Debug.Log($"Card spawned for: {imageName}");
                Debug.Log($"  Card World Position: {go.transform.position}");
                Debug.Log($"  Card Local Position: {go.transform.localPosition}");
                Debug.Log($"  Card World Scale: {go.transform.lossyScale}");
                Debug.Log($"  Card Parent: {go.transform.parent.name}");
                
                // Log Canvas info if it has one
                Canvas canvas = go.GetComponent<Canvas>();
                if (canvas)
                {
                    Debug.Log($"  Canvas Render Mode: {canvas.renderMode}");
                    Debug.Log($"  Canvas World Camera: {canvas.worldCamera}");
                }

                // after Instantiate and parent (example inside OnTrackedImagesChanged)
                string keyX = imageName + "_pos_x";
                if (PlayerPrefs.HasKey(keyX))
                {
                    float x = PlayerPrefs.GetFloat(imageName + "_pos_x");
                    float y = PlayerPrefs.GetFloat(imageName + "_pos_y");
                    float z = PlayerPrefs.GetFloat(imageName + "_pos_z");
                    go.transform.localPosition = new Vector3(x, y, z);
                }
            }
        }

        foreach (var trackedImage in eventArgs.updated)
        {
            string imageName = trackedImage.referenceImage.name;
            if (_spawnedObjects.ContainsKey(imageName))
            {
                bool isTracking = trackedImage.trackingState == TrackingState.Tracking;
                _spawnedObjects[imageName].SetActive(isTracking);
            }
        }

        foreach (var trackedImage in eventArgs.removed)
        {
            string imageName = trackedImage.referenceImage.name;
            if (_spawnedObjects.ContainsKey(imageName))
            {
                Destroy(_spawnedObjects[imageName]);
                _spawnedObjects.Remove(imageName);
                Debug.Log($"Card destroyed for: {imageName}");
            }
        }
    }
}