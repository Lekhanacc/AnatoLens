using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[System.Serializable]
public class ImagePrefab
{
    public string imageName;
    public GameObject prefab;
}

public class TrackedImageSpawner : MonoBehaviour
{
    public List<ImagePrefab> imagePrefabs;

    private ARTrackedImageManager trackedImageManager;
    private Dictionary<string, GameObject> spawnedObjects = new Dictionary<string, GameObject>();
    private Dictionary<string, ARTrackedImage> trackedImageRefs = new Dictionary<string, ARTrackedImage>();

    private bool isLocked = false;
    private string lockedImageName = null;

    public bool IsLocked => isLocked;

    void Awake()
    {
        trackedImageManager = GetComponent<ARTrackedImageManager>();
    }

    void OnEnable()
    {
        trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
    }

    void OnDisable()
    {
        trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs args)
    {
        foreach (var trackedImage in args.added) UpdateImage(trackedImage);
        foreach (var trackedImage in args.updated) UpdateImage(trackedImage);
        foreach (var trackedImage in args.removed)
        {
            string imageName = trackedImage.referenceImage.name;
            // Don't destroy a model the user has locked in place, even if ARCore drops the trackable.
            if (isLocked && imageName == lockedImageName) continue;

            if (spawnedObjects.ContainsKey(imageName))
            {
                Destroy(spawnedObjects[imageName]);
                spawnedObjects.Remove(imageName);
                trackedImageRefs.Remove(imageName);
            }
        }
    }

    void UpdateImage(ARTrackedImage trackedImage)
    {
        string imageName = trackedImage.referenceImage.name;
        trackedImageRefs[imageName] = trackedImage;

        // While locked, this image's object is frozen in world space — leave it alone.
        if (isLocked && imageName == lockedImageName) return;

        if (trackedImage.trackingState != TrackingState.Tracking)
        {
            if (spawnedObjects.ContainsKey(imageName))
                spawnedObjects[imageName].SetActive(false);
            return;
        }

        if (spawnedObjects.ContainsKey(imageName))
        {
            spawnedObjects[imageName].SetActive(true);
            return;
        }

        foreach (var item in imagePrefabs)
        {
            if (item.imageName == imageName)
            {
                GameObject obj = Instantiate(item.prefab);
                obj.transform.SetParent(trackedImage.transform, false);
                obj.transform.localPosition = Vector3.zero;
                obj.transform.localRotation = Quaternion.identity;
                obj.transform.localScale = Vector3.one;
                spawnedObjects.Add(imageName, obj);
                break;
            }
        }
    }

    // Hook this up to the Fix button's OnClick.
    public void ToggleLock()
    {
        if (!isLocked) LockCurrentModel();
        else UnlockCurrentModel();
    }

    void LockCurrentModel()
    {
        string activeImageName = null;
        foreach (var kvp in spawnedObjects)
        {
            if (kvp.Value.activeInHierarchy)
            {
                activeImageName = kvp.Key;
                break;
            }
        }

        if (activeImageName == null)
        {
            Debug.Log("[Fix] No visible model right now — scan the image first.");
            return;
        }

        GameObject obj = spawnedObjects[activeImageName];
        obj.transform.SetParent(null, true); // detach, keep current world position/rotation
        isLocked = true;
        lockedImageName = activeImageName;
        Debug.Log("[Fix] Model locked in place.");
    }

    void UnlockCurrentModel()
    {
        if (lockedImageName != null &&
            spawnedObjects.TryGetValue(lockedImageName, out GameObject obj) &&
            trackedImageRefs.TryGetValue(lockedImageName, out ARTrackedImage image))
        {
            // Re-snap onto the image's current position and resume normal tracking.
            obj.transform.SetParent(image.transform, false);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one;
            obj.SetActive(image.trackingState == TrackingState.Tracking);
        }

        isLocked = false;
        lockedImageName = null;
        Debug.Log("[Fix] Model unlocked, resuming image tracking.");
    }
} 