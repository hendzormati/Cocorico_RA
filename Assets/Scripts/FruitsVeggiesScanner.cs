using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[System.Serializable]
public class Detection
{
    public string class_name;
    public float confidence;
    public float[] bbox;
}

[System.Serializable]
public class DetectionResponse
{
    public List<Detection> detections;
}

[System.Serializable]
public class IngredientEntry
{
    public string id;
    public FoodData data;
}

[System.Serializable]
public class IngredientsDB
{
    public List<IngredientEntry> foods;
}

public class FruitsVeggiesScanner : MonoBehaviour
{
    [Header("Server Config")]
    public string serverIP = "192.168.1.42";
    public int serverPort = 5000;

    [Header("AR")]
    public GameObject labelPrefab;
    public Transform labelsParent;

    [Header("UI")]
    public Button scanButton;
    public TextMeshProUGUI statusText;
    public GameObject ingredientListBanner;
    public Button closeBannerButton;   // X button on the banner — assign in Inspector
    public Button showBannerButton;    // "📋" button to reopen banner — assign in Inspector

    // The ScrollRect that holds the horizontal chip list
    public ScrollRect ingredientScrollRect;
    public Transform ingredientListContent;
    public GameObject ingredientChipPrefab;

    [Header("Detail Card (3D World-Space)")]
    public GameObject detailCardModel3D;

    [Tooltip("Vertical offset above the label where the detail card appears (world units)")]
    public float detailCardYOffset = 0.30f;

    [Tooltip("Minimum distance from camera for the detail card (world units)")]
    public float detailCardMinDistFromCamera = 0.40f;

    public TextMeshProUGUI detailNomText;
    public TextMeshProUGUI detailNutriScoreText;
    public Image detailNutriScorePanel;
    public TextMeshProUGUI detailCaloriesText;
    public TextMeshProUGUI detailProteinesText;
    public TextMeshProUGUI detailGlucidesText;
    public TextMeshProUGUI detailFibresText;
    public TextMeshProUGUI detailConservationText;
    public Button closeDetailButton;

    [Header("AR Components")]
    public ARRaycastManager arRaycastManager;
    public ARPlaneManager arPlaneManager;

    // ── internals ──────────────────────────────────────────────────────────────
    private Dictionary<string, FoodData> _foodMap = new Dictionary<string, FoodData>();
    private List<GameObject> _spawnedLabels = new List<GameObject>();
    private bool _isScanning = false;
    private static List<ARRaycastHit> _arHits = new List<ARRaycastHit>();

    // ── lifecycle ──────────────────────────────────────────────────────────────
    void OnEnable()
    {
        Debug.Log("[MODE1] FruitsVeggiesScanner enabled.");
        ValidateReferences();
        LoadIngredients();
        SetupUI();

        if (detailCardModel3D != null)
            detailCardModel3D.SetActive(false);
        else
            Debug.LogWarning("[MODE1] detailCardModel3D is NOT assigned in Inspector!");
    }

    void OnDisable()
    {
        Debug.Log("[MODE1] FruitsVeggiesScanner disabled. Clearing labels and stopping coroutines.");
        ClearLabels();
        CloseDetail();
        StopAllCoroutines();
        _isScanning = false;
    }

    // ── reference validation ──────────────────────────────────────────────────
    void ValidateReferences()
    {
        if (labelPrefab == null)           Debug.LogError("[MODE1] labelPrefab is NULL — labels won't spawn!");
        if (arRaycastManager == null)      Debug.LogError("[MODE1] arRaycastManager is NULL!");
        if (arPlaneManager == null)        Debug.LogError("[MODE1] arPlaneManager is NULL!");
        if (scanButton == null)            Debug.LogError("[MODE1] scanButton is NULL!");
        if (statusText == null)            Debug.LogError("[MODE1] statusText is NULL!");
        if (ingredientListContent == null) Debug.LogError("[MODE1] ingredientListContent is NULL — chips won't populate!");
        if (ingredientChipPrefab == null)  Debug.LogError("[MODE1] ingredientChipPrefab is NULL!");

        // ── Auto-find ScrollRect if not assigned ─────────────────────────────
        if (ingredientScrollRect == null)
        {
            Debug.LogWarning("[MODE1] ingredientScrollRect not assigned in Inspector — attempting auto-find...");

            // Strategy 1: look on the ingredientListBanner or any child
            if (ingredientListBanner != null)
                ingredientScrollRect = ingredientListBanner.GetComponentInChildren<ScrollRect>(true);

            // Strategy 2: look on the content's parent chain
            if (ingredientScrollRect == null && ingredientListContent != null)
            {
                Transform t = ingredientListContent.parent;
                while (t != null && ingredientScrollRect == null)
                {
                    ingredientScrollRect = t.GetComponent<ScrollRect>();
                    t = t.parent;
                }
            }

            if (ingredientScrollRect == null)
                Debug.LogError("[MODE1] Auto-find FAILED — no ScrollRect found under ingredientListBanner or above ingredientListContent. " +
                               "Please assign it manually in the Inspector.");
            else
                Debug.Log($"[MODE1] Auto-found ScrollRect on '{ingredientScrollRect.gameObject.name}'.");
        }

        if (ingredientScrollRect != null)
        {
            // ── Force correct scroll settings programmatically ────────────────
            bool wasHorizontal = ingredientScrollRect.horizontal;
            bool wasVertical   = ingredientScrollRect.vertical;

            ingredientScrollRect.horizontal = true;
            ingredientScrollRect.vertical   = false;

            if (!wasHorizontal)
                Debug.LogWarning("[MODE1][SCROLL] ScrollRect.horizontal was FALSE — forced to TRUE.");
            if (wasVertical)
                Debug.LogWarning("[MODE1][SCROLL] ScrollRect.vertical was TRUE — forced to FALSE (horizontal-only mode).");

            // Ensure movement type allows scrolling
            if (ingredientScrollRect.movementType == ScrollRect.MovementType.Clamped)
                Debug.Log("[MODE1][SCROLL] MovementType = Clamped (good — content stays within bounds).");

            Debug.Log($"[MODE1] ScrollRect ready: '{ingredientScrollRect.name}' | horizontal=true | vertical=false");

            // Ensure content is wired to the ScrollRect
            if (ingredientScrollRect.content == null && ingredientListContent is RectTransform rt)
            {
                ingredientScrollRect.content = rt;
                Debug.LogWarning("[MODE1][SCROLL] ScrollRect.content was null — auto-assigned from ingredientListContent.");
            }
        }
    }

    // ── data loading ──────────────────────────────────────────────────────────
    void LoadIngredients()
    {
        TextAsset json = Resources.Load<TextAsset>("ingredients");
        if (json == null)
        {
            Debug.LogError("[MODE1] ingredients.json NOT found in Resources folder! Chips won't appear.");
            return;
        }

        var db = JsonUtility.FromJson<IngredientsDB>(json.text);
        if (db == null || db.foods == null)
        {
            Debug.LogError("[MODE1] Failed to parse ingredients.json — db or db.foods is null.");
            return;
        }

        _foodMap.Clear();
        int loaded = 0;
        foreach (var entry in db.foods)
        {
            if (entry?.data != null)
            {
                _foodMap[entry.id.ToLower()] = entry.data;
                loaded++;
            }
        }
        Debug.Log($"[MODE1] Loaded {loaded} ingredients from JSON.");

        PopulateIngredientList();
    }

    void PopulateIngredientList()
    {
        // Clear old chips
        foreach (Transform child in ingredientListContent)
            Destroy(child.gameObject);

        int chipCount = 0;
        foreach (var kvp in _foodMap)
        {
            var chip = Instantiate(ingredientChipPrefab, ingredientListContent);
            var tmp = chip.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
                tmp.text = kvp.Value.emoji + " " + kvp.Value.nom;
            else
                Debug.LogWarning($"[MODE1] Chip for '{kvp.Key}' has no TextMeshProUGUI child.");
            chipCount++;
        }

        Debug.Log($"[MODE1] Populated {chipCount} ingredient chips.");

        // ── Scroll diagnosis ─────────────────────────────────────────────────
        if (ingredientScrollRect == null)
        {
            Debug.LogWarning("[MODE1] Cannot diagnose scroll — ingredientScrollRect is null. " +
                             "Drag the ScrollRect component into the ingredientScrollRect field.");
            return;
        }

        // Give the layout system one frame then log dimensions
        StartCoroutine(DiagnoseScrollAfterLayout());
    }

    IEnumerator DiagnoseScrollAfterLayout()
    {
        // Wait two frames for ContentSizeFitter / HorizontalLayoutGroup to rebuild
        yield return null;
        yield return null;

        if (ingredientScrollRect == null) yield break;

        // ── Auto-fix HorizontalLayoutGroup ───────────────────────────────────
        var hlg = ingredientListContent.GetComponent<HorizontalLayoutGroup>();
        if (hlg == null)
        {
            Debug.LogWarning("[MODE1][SCROLL] No HorizontalLayoutGroup on ingredientListContent — adding one at runtime.");
            hlg = ingredientListContent.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment        = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight= false;
            hlg.childControlWidth     = false;
            hlg.childControlHeight    = false;
            hlg.spacing               = 8f;
            hlg.padding               = new RectOffset(8, 8, 4, 4);
            Debug.Log("[MODE1][SCROLL] HorizontalLayoutGroup added with spacing=8, childForceExpandWidth=false.");
        }
        else
        {
            Debug.Log($"[MODE1][SCROLL] HorizontalLayoutGroup found: spacing={hlg.spacing}, " +
                      $"childControlWidth={hlg.childControlWidth}, childForceExpandWidth={hlg.childForceExpandWidth}");

            if (hlg.childForceExpandWidth)
            {
                hlg.childForceExpandWidth = false;
                Debug.LogWarning("[MODE1][SCROLL] childForceExpandWidth was TRUE — forced to FALSE. " +
                                 "This was preventing chips from having their natural width, making content no wider than the viewport.");
            }
        }

        // ── Auto-fix ContentSizeFitter ───────────────────────────────────────
        var csf = ingredientListContent.GetComponent<ContentSizeFitter>();
        if (csf == null)
        {
            Debug.LogWarning("[MODE1][SCROLL] No ContentSizeFitter on ingredientListContent — adding one at runtime.");
            csf = ingredientListContent.gameObject.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit   = ContentSizeFitter.FitMode.Unconstrained;
            Debug.Log("[MODE1][SCROLL] ContentSizeFitter added: horizontalFit=PreferredSize.");
        }
        else
        {
            if (csf.horizontalFit != ContentSizeFitter.FitMode.PreferredSize)
            {
                Debug.LogWarning($"[MODE1][SCROLL] ContentSizeFitter.horizontalFit was '{csf.horizontalFit}' — " +
                                 "forced to PreferredSize. Without this the content never grows wider than the viewport.");
                csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
            Debug.Log($"[MODE1][SCROLL] ContentSizeFitter: horizontal={csf.horizontalFit}, vertical={csf.verticalFit}");
        }

        // Force layout rebuild now that components are correct
        LayoutRebuilder.ForceRebuildLayoutImmediate(ingredientListContent as RectTransform);
        yield return null; // one more frame after rebuild

        // ── Dimension report ─────────────────────────────────────────────────
        RectTransform viewport = ingredientScrollRect.viewport;
        RectTransform content  = ingredientScrollRect.content;

        if (viewport == null)
        {
            Debug.LogWarning("[MODE1][SCROLL] ScrollRect.viewport is null! " +
                             "The ScrollRect needs a child named 'Viewport' with a RectMask2D or Mask component, " +
                             "and that Viewport must be assigned to the ScrollRect.viewport field.");
            yield break;
        }

        if (content == null)
        {
            Debug.LogWarning("[MODE1][SCROLL] ScrollRect.content is null! " +
                             "The content RectTransform (ingredientListContent) must be assigned to ScrollRect.content.");
            yield break;
        }

        float vpWidth  = viewport.rect.width;
        float vpHeight = viewport.rect.height;
        float ctWidth  = content.rect.width;
        float ctHeight = content.rect.height;

        Debug.Log($"[MODE1][SCROLL] Viewport size: {vpWidth:F1} x {vpHeight:F1}");
        Debug.Log($"[MODE1][SCROLL] Content size:  {ctWidth:F1} x {ctHeight:F1}");

        if (ctWidth <= vpWidth)
            Debug.LogWarning($"[MODE1][SCROLL] Content width ({ctWidth:F1}) still <= viewport ({vpWidth:F1}) after auto-fix. " +
                             "This means chips themselves have no intrinsic width. " +
                             "Check that ingredientChipPrefab has a LayoutElement or a RectTransform with a non-zero preferred width, " +
                             $"or that {_foodMap.Count} chips at their natural size are truly wider than {vpWidth:F1}px.");
        else
            Debug.Log($"[MODE1][SCROLL] ✓ Content ({ctWidth:F1}px) wider than viewport ({vpWidth:F1}px) — horizontal scroll is working.");
    }

    // ── UI setup ──────────────────────────────────────────────────────────────
    void SetupUI()
    {
        // Scan button
        scanButton.onClick.RemoveAllListeners();
        scanButton.onClick.AddListener(OnScanPressed);

        // Close detail card
        if (closeDetailButton != null)
        {
            closeDetailButton.onClick.RemoveAllListeners();
            closeDetailButton.onClick.AddListener(CloseDetail);
        }
        else Debug.LogWarning("[MODE1] closeDetailButton not assigned in Inspector!");

        // Close ingredient banner (X button)
        if (closeBannerButton != null)
        {
            closeBannerButton.onClick.RemoveAllListeners();
            closeBannerButton.onClick.AddListener(CloseBanner);
        }
        else Debug.LogWarning("[MODE1] closeBannerButton not assigned — wire the X button in Inspector.");

        // Reopen ingredient banner (📋 button, always visible after banner is closed)
        if (showBannerButton != null)
        {
            showBannerButton.onClick.RemoveAllListeners();
            showBannerButton.onClick.AddListener(OpenBanner);
            showBannerButton.gameObject.SetActive(false); // hidden while banner is open
        }
        else Debug.LogWarning("[MODE1] showBannerButton not assigned — create a small button and wire it in Inspector.");

        // Show AR planes (enabled by ARPlaneManager, just make sure visualizers are on)
        SetPlaneVisualization(true);

        statusText.text = "Pointez la caméra vers des fruits ou légumes";
        ingredientListBanner.SetActive(true);
    }

    // ── banner helpers ────────────────────────────────────────────────────────
    void CloseBanner()
    {
        ingredientListBanner.SetActive(false);
        if (showBannerButton != null) showBannerButton.gameObject.SetActive(true);
        Debug.Log("[MODE1] Banner closed.");
    }

    void OpenBanner()
    {
        ingredientListBanner.SetActive(true);
        if (showBannerButton != null) showBannerButton.gameObject.SetActive(false);
        Debug.Log("[MODE1] Banner reopened.");
    }

    // ── plane visualization ───────────────────────────────────────────────────
    void SetPlaneVisualization(bool visible)
    {
        if (arPlaneManager == null) return;
        foreach (var plane in arPlaneManager.trackables)
        {
            var r = plane.GetComponent<Renderer>();
            if (r != null) r.enabled = visible;
            var lr = plane.GetComponent<LineRenderer>();
            if (lr != null) lr.enabled = visible;
        }
    }

    // ── scan flow ─────────────────────────────────────────────────────────────
    void OnScanPressed()
    {
        if (_isScanning)
        {
            Debug.Log("[MODE1] Scan button pressed but already scanning — ignoring.");
            return;
        }

        // Clear any previous results immediately
        ClearLabels();
        CloseDetail();

        // Hide banner, show reopen button
        CloseBanner();

        // Hide plane visualizers while scanning for a cleaner view
        SetPlaneVisualization(false);

        int planeCount = arPlaneManager.trackables.count;
        Debug.Log($"[MODE1] Scan pressed. AR planes detected: {planeCount}");

        if (planeCount == 0)
        {
            // Show planes again so the user can see what's being detected
            SetPlaneVisualization(true);
            statusText.text = "Bougez lentement la caméra pour détecter la surface...";
            Debug.Log("[MODE1] No planes yet — waiting for plane detection before scanning.");
            StartCoroutine(WaitForPlaneAndScan());
        }
        else
        {
            StartCoroutine(CaptureAndDetect());
        }
    }

    IEnumerator WaitForPlaneAndScan()
    {
        scanButton.interactable = false;
        float timeout = 8f;
        float elapsed = 0f;

        while (arPlaneManager.trackables.count == 0 && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        scanButton.interactable = true;

        if (arPlaneManager.trackables.count > 0)
        {
            Debug.Log($"[MODE1] Plane detected after {elapsed:F1}s. Proceeding to scan.");
            statusText.text = "Surface détectée ! Pointez vers vos ingrédients.";
            yield return new WaitForSeconds(1f);
            StartCoroutine(CaptureAndDetect());
        }
        else
        {
            Debug.LogWarning($"[MODE1] Plane detection timed out after {timeout}s. No surface found.");
            statusText.text = "Surface non détectée. Essayez sur une table bien éclairée.";
        }
    }

    IEnumerator CaptureAndDetect()
    {
        _isScanning = true;
        statusText.text = "Analyse en cours...";
        scanButton.interactable = false;
        ClearLabels();

        yield return new WaitForEndOfFrame();
        Texture2D snap = CaptureFrame();

        if (snap == null)
        {
            Debug.LogError("[MODE1] Frame capture returned null — cannot send to server.");
            statusText.text = "Erreur de capture";
            _isScanning = false;
            scanButton.interactable = true;
            yield break;
        }

        byte[] jpg = snap.EncodeToJPG(75);
        Destroy(snap);
        string b64 = System.Convert.ToBase64String(jpg);
        Debug.Log($"[MODE1] Captured frame — JPEG size: {jpg.Length / 1024}KB, base64 length: {b64.Length} chars.");

        string url = $"http://{serverIP}:{serverPort}/detect";
        Debug.Log($"[MODE1] Sending POST to {url}");

        string body = "{\"image\":\"" + b64 + "\"}";

        using (var req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler   = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = 10;

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                // ── detailed connection error diagnosis ─────────────────────
                string errorMsg = req.error ?? "unknown error";
                string diagnosis = DiagnoseNetworkError(req.result, errorMsg, url);

                Debug.LogError($"[MODE1][NETWORK] Request FAILED.\n" +
                               $"  URL:    {url}\n" +
                               $"  Result: {req.result}\n" +
                               $"  Error:  {errorMsg}\n" +
                               $"  ResponseCode: {req.responseCode}\n" +
                               $"  Diagnosis: {diagnosis}");

                statusText.text = $"Serveur inaccessible — {diagnosis}";
            }
            else
            {
                Debug.Log($"[MODE1][NETWORK] Response received. HTTP {req.responseCode}. Body length: {req.downloadHandler.text.Length} chars.");
                Debug.Log($"[MODE1][NETWORK] Raw response: {req.downloadHandler.text}");

                DetectionResponse response = null;
                try
                {
                    response = JsonUtility.FromJson<DetectionResponse>(req.downloadHandler.text);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[MODE1] JSON parse error: {e.Message}\nRaw: {req.downloadHandler.text}");
                    statusText.text = "Erreur de parsing JSON";
                    _isScanning = false;
                    scanButton.interactable = true;
                    yield break;
                }

                if (response == null || response.detections == null)
                {
                    Debug.LogWarning("[MODE1] Response parsed but detections list is null.");
                    statusText.text = "Aucun ingrédient détecté";
                }
                else
                {
                    Debug.Log($"[MODE1] {response.detections.Count} detection(s) returned by server.");
                    for (int i = 0; i < response.detections.Count; i++)
                    {
                        var d = response.detections[i];
                        Debug.Log($"[MODE1]   [{i}] class={d.class_name}  confidence={d.confidence:P0}  " +
                                  $"bbox=[{(d.bbox != null && d.bbox.Length == 4 ? $"{d.bbox[0]:F0},{d.bbox[1]:F0},{d.bbox[2]:F0},{d.bbox[3]:F0}" : "INVALID")}]");
                    }

                    SpawnLabels(response.detections);
                    int uniqueCount = _spawnedLabels.Count;
                    statusText.text = uniqueCount > 0
                        ? $"{uniqueCount} ingrédient(s) détecté(s)"
                        : "Aucun ingrédient détecté";
                }
            }
        }

        _isScanning = false;
        scanButton.interactable = true;
        // Restore plane visualization now that scan is done
        SetPlaneVisualization(true);
    }

    // ── network error diagnosis ───────────────────────────────────────────────
    string DiagnoseNetworkError(UnityWebRequest.Result result, string error, string url)
    {
        switch (result)
        {
            case UnityWebRequest.Result.ConnectionError:
                if (error.Contains("Cannot connect") || error.Contains("refused"))
                    return $"Connection refused on {serverIP}:{serverPort}. Is the Flask server running?";
                if (error.Contains("timeout") || error.Contains("Timeout"))
                    return $"Timeout reaching {serverIP}:{serverPort}. Check that phone and server are on the same WiFi network.";
                if (error.Contains("Network is unreachable") || error.Contains("unreachable"))
                    return "Network unreachable. Phone may not be connected to WiFi.";
                return $"Connection error: {error}. Verify server IP ({serverIP}) and port ({serverPort}).";

            case UnityWebRequest.Result.ProtocolError:
                return $"HTTP error (code may be 4xx/5xx). Server is reachable but returned an error. Check Flask logs.";

            case UnityWebRequest.Result.DataProcessingError:
                return $"Data processing error: {error}. Response might be malformed.";

            default:
                return $"{result}: {error}";
        }
    }

    // ── frame capture ─────────────────────────────────────────────────────────
    Texture2D CaptureFrame()
    {
        try
        {
            var cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("[MODE1] Camera.main is null during frame capture!");
                return null;
            }

            RenderTexture rt = new RenderTexture(Screen.width, Screen.height, 0);
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = null;

            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            Destroy(rt);

            Debug.Log($"[MODE1] Frame captured successfully: {Screen.width}x{Screen.height}");
            return tex;
        }
        catch (System.Exception e)
        {
            Debug.LogError("[MODE1] Capture error: " + e.Message);
            return null;
        }
    }

    // ── label spawning ────────────────────────────────────────────────────────
    void SpawnLabels(List<Detection> detections)
    {
        ClearLabels();

        if (labelPrefab == null)
        {
            Debug.LogError("[MODE1] Cannot spawn labels — labelPrefab is null!");
            return;
        }

        // ── Step 1: aggregate detections by class ────────────────────────────
        // For each unique class we keep: count, best confidence, centroid bbox
        var grouped = new Dictionary<string, (int count, float bestConf, float[] centroidBbox)>();

        int skipped = 0;
        foreach (var det in detections)
        {
            if (det.confidence < 0.5f)
            {
                Debug.Log($"[MODE1]   Skipping '{det.class_name}' — confidence {det.confidence:P0} < 50%");
                skipped++;
                continue;
            }
            if (det.bbox == null || det.bbox.Length < 4)
            {
                Debug.LogWarning($"[MODE1]   Skipping '{det.class_name}' — invalid bbox");
                skipped++;
                continue;
            }

            string key = det.class_name.ToLower();
            if (grouped.TryGetValue(key, out var existing))
            {
                // Average the bbox centres so the label lands in the middle of all instances
                float[] avg = new float[]
                {
                    (existing.centroidBbox[0] + det.bbox[0]) / 2f,
                    (existing.centroidBbox[1] + det.bbox[1]) / 2f,
                    (existing.centroidBbox[2] + det.bbox[2]) / 2f,
                    (existing.centroidBbox[3] + det.bbox[3]) / 2f,
                };
                grouped[key] = (existing.count + 1,
                                Mathf.Max(existing.bestConf, det.confidence),
                                avg);
            }
            else
            {
                grouped[key] = (1, det.confidence, det.bbox);
            }
        }

        Debug.Log($"[MODE1] Aggregated {detections.Count} raw detections → {grouped.Count} unique ingredient(s). ({skipped} skipped)");
        foreach (var kv in grouped)
            Debug.Log($"[MODE1]   {kv.Key}: qty={kv.Value.count}, bestConf={kv.Value.bestConf:P0}");

        // ── Step 2: spawn one label per unique class ─────────────────────────
        int spawned = 0;
        foreach (var kv in grouped)
        {
            string className = kv.Key;
            int    qty       = kv.Value.count;
            float  conf      = kv.Value.bestConf;
            float[] bbox     = kv.Value.centroidBbox;

            float screenX = (bbox[0] + bbox[2]) / 2f;
            float screenY = Screen.height - (bbox[1] + bbox[3]) / 2f;
            Vector2 screenPoint = new Vector2(screenX, screenY);

            Vector3 worldPos;

            if (arRaycastManager.Raycast(screenPoint, _arHits, TrackableType.PlaneWithinPolygon))
            {
                worldPos = _arHits[0].pose.position + Vector3.up * 0.05f;
                float dist = Vector3.Distance(Camera.main.transform.position, worldPos);
                Debug.Log($"[MODE1]   '{className}' (x{qty}): placed on AR plane at {worldPos} (dist: {dist:F2}m)");
            }
            else
            {
                float cx  = screenX / Screen.width;
                float cy  = screenY / Screen.height;
                Ray   ray = Camera.main.ViewportPointToRay(new Vector3(cx, cy, 0));
                worldPos  = ray.origin + ray.direction * 1.2f;
                float dist = Vector3.Distance(Camera.main.transform.position, worldPos);
                Debug.LogWarning($"[MODE1]   '{className}' (x{qty}): no AR plane — FALLBACK at {worldPos} (dist: {dist:F2}m)");
            }

            var label = Instantiate(labelPrefab, worldPos, Quaternion.identity);
            if (labelsParent != null) label.transform.SetParent(labelsParent, true);

            Vector3 ws = label.transform.lossyScale;
            Debug.Log($"[MODE1]   '{className}' label spawned. lossyScale={ws}. " +
                      (ws.magnitude < 0.01f ? "*** EXTREMELY SMALL — fix FBX import scale ***" : "Scale OK."));

            _foodMap.TryGetValue(className, out FoodData data);
            if (data == null)
                Debug.LogWarning($"[MODE1]   No FoodData for key '{className}'.");

            var lc = label.GetComponent<IngredientLabel>();
            if (lc != null)
                lc.Setup(className, conf, qty, data, ShowDetail);
            else
                Debug.LogError("[MODE1]   labelPrefab has no IngredientLabel component!");

            if (label.GetComponent<Collider>() == null)
            {
                var col = label.AddComponent<BoxCollider>();
                col.size = new Vector3(0.3f, 0.15f, 0.05f);
            }

            _spawnedLabels.Add(label);
            spawned++;
        }

        Debug.Log($"[MODE1] SpawnLabels done: {spawned} label(s) spawned.");
    }

    void ClearLabels()
    {
        int count = _spawnedLabels.Count;
        foreach (var l in _spawnedLabels)
            if (l != null) Destroy(l);
        _spawnedLabels.Clear();
        if (count > 0) Debug.Log($"[MODE1] Cleared {count} label(s).");
    }

    // ── detail card ───────────────────────────────────────────────────────────
    void ShowDetail(FoodData data, Vector3 labelPosition)
    {
        if (data == null)
        {
            Debug.LogWarning("[MODE1] ShowDetail called with null FoodData — card not shown.");
            return;
        }

        if (detailCardModel3D == null)
        {
            Debug.LogError("[MODE1] detailCardModel3D is null — cannot show detail card!");
            return;
        }

        // Hide all labels while the detail card is open
        SetLabelsVisible(false);

        // Position: above the label, but never closer than detailCardMinDistFromCamera
        Vector3 cardPos = labelPosition + Vector3.up * detailCardYOffset;

        if (Camera.main != null)
        {
            float dist = Vector3.Distance(Camera.main.transform.position, cardPos);
            if (dist < detailCardMinDistFromCamera)
            {
                Vector3 dir = (cardPos - Camera.main.transform.position).normalized;
                cardPos = Camera.main.transform.position + dir * detailCardMinDistFromCamera;
                Debug.Log($"[MODE1] Detail card was too close ({dist:F2}m) — pushed to {detailCardMinDistFromCamera}m.");
            }

            detailCardModel3D.transform.position = cardPos;
            detailCardModel3D.transform.rotation = Quaternion.LookRotation(
                Camera.main.transform.forward * -1, Vector3.up);

            float finalDist = Vector3.Distance(Camera.main.transform.position, cardPos);
            Vector3 scale   = detailCardModel3D.transform.localScale;
            Debug.Log($"[MODE1] Detail card shown for '{data.nom}' at {cardPos} " +
                      $"(dist from camera: {finalDist:F2}m, localScale: {scale})");

            if (scale.magnitude < 0.5f)
                Debug.LogWarning("[MODE1] *** Detail card localScale is very small — fix FBX import scale. ***");
        }

        detailCardModel3D.SetActive(true);

        detailNomText.text          = data.nom;
        detailCaloriesText.text     = data.calories;
        detailProteinesText.text = data.proteines;
        detailGlucidesText.text = data.glucides;
        detailFibresText.text = data.fibres;
        detailConservationText.text = data.conservation;
        detailNutriScoreText.text   = data.nutriscore;

        Color c;
        if (ColorUtility.TryParseHtmlString(GetNutriColor(data.nutriscore), out c))
        {
            detailNutriScorePanel.color = c;
            detailNutriScoreText.color  = Color.white;
        }
    }

    void CloseDetail()
    {
        if (detailCardModel3D != null && detailCardModel3D.activeSelf)
        {
            detailCardModel3D.SetActive(false);
            Debug.Log("[MODE1] Detail card closed.");
            // Restore labels so user can tap another one
            SetLabelsVisible(true);
        }
    }

    // Show or hide all spawned labels without destroying them
    void SetLabelsVisible(bool visible)
    {
        foreach (var l in _spawnedLabels)
            if (l != null) l.SetActive(visible);
    }

    string GetNutriColor(string s)
    {
        switch (s)
        {
            case "A": return "#1a9e3f";
            case "B": return "#85bb2f";
            case "C": return "#f9a825";
            case "D": return "#e67e22";
            case "E": return "#e74c3c";
            default:  return "#888888";
        }
    }
}