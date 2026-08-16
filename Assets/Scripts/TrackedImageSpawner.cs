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
    private ARAnchorManager anchorManager;

    private Dictionary<string, GameObject> spawnedObjects =
        new Dictionary<string, GameObject>();

    private Dictionary<string, ARTrackedImage> trackedImageRefs =
        new Dictionary<string, ARTrackedImage>();

    private bool isLocked = false;
    private string lockedImageName = null;

    private ARAnchor lockedAnchor = null;

    public bool IsLocked => isLocked;


    // --------------------------------------------------
    // START
    // --------------------------------------------------

    void Awake()
    {
        trackedImageManager = GetComponent<ARTrackedImageManager>();
        anchorManager = GetComponent<ARAnchorManager>();
    }


    // --------------------------------------------------
    // ENABLE / DISABLE
    // --------------------------------------------------

    void OnEnable()
    {
        if (trackedImageManager != null)
        {
            trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
        }
    }

    void OnDisable()
    {
        if (trackedImageManager != null)
        {
            trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
        }
    }


    // --------------------------------------------------
    // IMAGE TRACKING CHANGED
    // --------------------------------------------------

    void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs args)
    {
        // New images detected
        foreach (var trackedImage in args.added)
        {
            UpdateImage(trackedImage);
        }

        // Existing images updated
        foreach (var trackedImage in args.updated)
        {
            UpdateImage(trackedImage);
        }

        // Images removed
        foreach (var trackedImage in args.removed)
        {
            string imageName = trackedImage.referenceImage.name;

            // IMPORTANT:
            // If the user pressed FIX, don't destroy the Heart.
            if (isLocked && imageName == lockedImageName)
            {
                continue;
            }

            if (spawnedObjects.ContainsKey(imageName))
            {
                Destroy(spawnedObjects[imageName]);

                spawnedObjects.Remove(imageName);
                trackedImageRefs.Remove(imageName);
            }
        }
    }


    // --------------------------------------------------
    // UPDATE TRACKED IMAGE
    // --------------------------------------------------

    void UpdateImage(ARTrackedImage trackedImage)
    {
        string imageName = trackedImage.referenceImage.name;

        // Remember the tracked image
        trackedImageRefs[imageName] = trackedImage;


        // --------------------------------------------------
        // IF HEART IS LOCKED
        // --------------------------------------------------

        // Once FIX is pressed, completely ignore image
        // tracking for this Heart.
        if (isLocked && imageName == lockedImageName)
        {
            return;
        }


        // --------------------------------------------------
        // IMAGE NOT CURRENTLY TRACKING
        // --------------------------------------------------

        if (trackedImage.trackingState != TrackingState.Tracking)
        {
            if (spawnedObjects.ContainsKey(imageName))
            {
                spawnedObjects[imageName].SetActive(false);
            }

            return;
        }


        // --------------------------------------------------
        // HEART ALREADY EXISTS
        // --------------------------------------------------

        if (spawnedObjects.ContainsKey(imageName))
        {
            spawnedObjects[imageName].SetActive(true);

            return;
        }


        // --------------------------------------------------
        // CREATE HEART
        // --------------------------------------------------

        foreach (var item in imagePrefabs)
        {
            if (item.imageName == imageName)
            {
                GameObject obj = Instantiate(item.prefab);

                // Attach Heart to tracked image
                obj.transform.SetParent(
                    trackedImage.transform,
                    false
                );

                // Put Heart at center of image
                obj.transform.localPosition = Vector3.zero;

                obj.transform.localRotation = Quaternion.identity;

                obj.transform.localScale = Vector3.one;

                spawnedObjects.Add(imageName, obj);

                Debug.Log(
                    "[AR] Heart spawned on image: " + imageName
                );

                break;
            }
        }
    }


    // --------------------------------------------------
    // FIX BUTTON
    // --------------------------------------------------

    public void ToggleLock()
    {
        if (!isLocked)
        {
            LockCurrentModel();
        }
        else
        {
            UnlockCurrentModel();
        }
    }


    // --------------------------------------------------
    // LOCK HEART
    // --------------------------------------------------

    void LockCurrentModel()
    {
        string activeImageName = null;


        // Find currently visible Heart
        foreach (var kvp in spawnedObjects)
        {
            if (kvp.Value != null &&
                kvp.Value.activeInHierarchy)
            {
                activeImageName = kvp.Key;
                break;
            }
        }


        // No Heart visible
        if (activeImageName == null)
        {
            Debug.Log(
                "[FIX] No visible Heart. Scan the image first."
            );

            return;
        }


        GameObject obj = spawnedObjects[activeImageName];


        // --------------------------------------------------
        // SAVE CURRENT HEART POSITION
        // --------------------------------------------------

        Pose heartPose = new Pose(
            obj.transform.position,
            obj.transform.rotation
        );


        // --------------------------------------------------
        // CREATE AR ANCHOR
        // --------------------------------------------------

        if (anchorManager == null)
        {
            Debug.LogError(
                "[FIX] AR Anchor Manager is missing!"
            );

            return;
        }


        ARAnchor anchor = anchorManager.AddAnchor(heartPose);


        // --------------------------------------------------
        // ANCHOR CREATED
        // --------------------------------------------------

        if (anchor != null)
        {
            // Remove Heart from the image.
            // Keep its world position.
            obj.transform.SetParent(
                anchor.transform,
                true
            );


            // Make sure the Heart stays exactly
            // where the user pressed FIX.
            obj.transform.position =
                heartPose.position;

            obj.transform.rotation =
                heartPose.rotation;


            // Save anchor information
            lockedAnchor = anchor;

            lockedImageName = activeImageName;

            isLocked = true;


            Debug.Log(
                "[FIX] ❤️ Heart successfully locked!"
            );

            Debug.Log(
                "[FIX] Marker is no longer required."
            );
        }
        else
        {
            Debug.LogWarning(
                "[FIX] Could not create AR Anchor."
            );
        }
    }


    // --------------------------------------------------
    // UNLOCK HEART
    // --------------------------------------------------

    void UnlockCurrentModel()
    {
        // Remove the current anchor
        if (lockedAnchor != null)
        {
            Destroy(lockedAnchor.gameObject);

            lockedAnchor = null;
        }


        // Find the original tracked image
        if (lockedImageName != null &&
            spawnedObjects.TryGetValue(
                lockedImageName,
                out GameObject obj
            ) &&
            trackedImageRefs.TryGetValue(
                lockedImageName,
                out ARTrackedImage image
            ))
        {
            // Only reconnect if image is currently visible
            if (image.trackingState ==
                TrackingState.Tracking)
            {
                obj.transform.SetParent(
                    image.transform,
                    false
                );

                obj.transform.localPosition =
                    Vector3.zero;

                obj.transform.localRotation =
                    Quaternion.identity;

                obj.transform.localScale =
                    Vector3.one;

                obj.SetActive(true);
            }
        }


        // Reset lock information
        isLocked = false;

        lockedImageName = null;


        Debug.Log(
            "[FIX] 🔓 Heart unlocked."
        );

        Debug.Log(
            "[FIX] Image tracking resumed."
        );
    }
}