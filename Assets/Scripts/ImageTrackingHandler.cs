using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ImageTrackingHandler : MonoBehaviour
{
    [SerializeField] private GameObject cardPrefab;

    private ARTrackedImageManager _imageManager;
    private Dictionary<string, GameObject> _spawnedCards = new Dictionary<string, GameObject>();

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
            Debug.Log($"[Tracking] Added: {imageName}");

            if (_spawnedCards.ContainsKey(imageName))
            {
                Destroy(_spawnedCards[imageName]);
                _spawnedCards.Remove(imageName);
            }

            if (!cardPrefab) continue;

            GameObject card = Instantiate(cardPrefab, trackedImage.transform);
            card.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

            Transform marker = card.transform.Find("VisualCenter");
            if (marker != null)
            {
                Vector3 markerLocal = card.transform.InverseTransformPoint(marker.position);
                card.transform.localPosition = -markerLocal;
            }
            else
            {
                card.transform.localPosition = Vector3.zero;
            }
            Debug.Log($"[Tracking] VisualCenter  : {marker.position}");
            if (PlayerPrefs.HasKey(imageName + "_pos_x"))
            {
                float x = PlayerPrefs.GetFloat(imageName + "_pos_x");
                float y = PlayerPrefs.GetFloat(imageName + "_pos_y");
                float z = PlayerPrefs.GetFloat(imageName + "_pos_z");

                Vector3 savedLocal = new Vector3(x, y, z);

                const float MAX_VALID_OFFSET = 1.5f;

                if (savedLocal.magnitude <= MAX_VALID_OFFSET)
                {
                    card.transform.localPosition = savedLocal;
                    Debug.Log($"[Tracking] Restored valid position for {imageName}: {savedLocal}");
                }
                else
                {
                    PlayerPrefs.DeleteKey(imageName + "_pos_x");
                    PlayerPrefs.DeleteKey(imageName + "_pos_y");
                    PlayerPrefs.DeleteKey(imageName + "_pos_z");
                    PlayerPrefs.Save();
                    Debug.LogWarning($"[Tracking] Discarded corrupted saved position for {imageName}: {savedLocal}");
                }
            }

            card.SetActive(true);
            _spawnedCards[imageName] = card;
            Debug.Log($"[Tracking] Card spawned for: {imageName}");
            Debug.Log($"[Tracking] {imageName} trackedImage world pos: {trackedImage.transform.position}");
            Debug.Log($"[Tracking] {imageName} card local pos after spawn: {card.transform.localPosition}");
        }

        foreach (var trackedImage in eventArgs.updated)
        {
            string imageName = trackedImage.referenceImage.name;
            if (!_spawnedCards.ContainsKey(imageName)) continue;

            bool isTracking = trackedImage.trackingState == TrackingState.Tracking;
            _spawnedCards[imageName].SetActive(isTracking);
        }

        foreach (var trackedImage in eventArgs.removed)
        {
            string imageName = trackedImage.referenceImage.name;
            if (!_spawnedCards.ContainsKey(imageName)) continue;

            Destroy(_spawnedCards[imageName]);
            _spawnedCards.Remove(imageName);
            Debug.Log($"[Tracking] Card destroyed for: {imageName}");
        }
    }
}