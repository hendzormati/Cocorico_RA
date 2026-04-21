using System;
using System.Collections;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using ZXing;

public class BarcodeScanner : MonoBehaviour
{
    [Header("Camera")]
    private WebCamTexture _camTexture;
    private Color32[] _pixels;
    private bool _scanning = false;
    private bool _detected = false;

    [Header("UI References")]
    public GameObject reticulePanel;
    public TextMeshProUGUI statusText;
    public GameObject loadingPanel;
    public TextMeshProUGUI loadingText;
    public GameObject productPanel;

    public TextMeshProUGUI productNameText;
    public TextMeshProUGUI brandText;
    public TextMeshProUGUI nutriScoreText;
    public Image nutriScorePanel;
    public TextMeshProUGUI caloriesText;
    public TextMeshProUGUI sucresText;
    public TextMeshProUGUI graissesText;
    public TextMeshProUGUI proteinesText;
    public TextMeshProUGUI allergenesText;
    public Button scanAgainButton;
    Texture2D _cameraTexture;
    void OnEnable()
    {
        StartScanning();
    }

    void OnDisable()
    {
        StopScanning();
    }

    void StartScanning()
    {
        _detected = false;
        _scanning = true;
        if (reticulePanel == null)
            Debug.LogError("[BARCODE] reticulePanel is NULL!");
        else
            Debug.Log("[BARCODE] reticulePanel OK");
        productPanel.SetActive(false);
        loadingPanel.SetActive(false);
        reticulePanel.SetActive(true);
        statusText.text = "Pointez la caméra vers un code-barres";
        StartCoroutine(ScanLoop());

        scanAgainButton.onClick.RemoveAllListeners();
        scanAgainButton.onClick.AddListener(ResetScan);
        if (_cameraTexture == null)
        {
            _cameraTexture = new Texture2D(640, 480, TextureFormat.RGB24, false);
            Debug.Log("[BARCODE] Camera texture initialized once");
        }
    }

    void StopScanning()
    {
        _scanning = false;
        StopAllCoroutines();
    }

    IEnumerator ScanLoop()
    {
        var reader = new BarcodeReader();
        reader.AutoRotate = true;
        reader.Options.TryHarder = true;

        while (_scanning && !_detected)
        {
            Debug.Log("[BARCODE] Capturing frame...");
            yield return new WaitForSeconds(0.5f);
            Texture2D snap = CaptureARFrame();
            if (snap == null)
            {
                Debug.LogWarning("[BARCODE] Frame capture FAILED");
                continue;
            }
            Debug.Log("[BARCODE] Frame captured: " + snap.width + "x" + snap.height);
            string result = null;
            bool done = false;
            var pixels = snap.GetPixels32();
            var r = reader.Decode(pixels, snap.width, snap.height);

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {


                    if (r != null)
                    {
                        result = r.Text;
                        Debug.Log("[BARCODE] Decode success: " + result);
                    }
                    else
                    {
                        Debug.Log("[BARCODE] No barcode detected in frame");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("[BARCODE] Decode error: " + e.Message);
                }
                done = true;
            });

            yield return new WaitUntil(() => done);

            if (!string.IsNullOrEmpty(result))
            {
                Debug.Log("[BARCODE] Barcode FOUND: " + result);
                _detected = true;
                _scanning = false;
                OnBarcodeDetected(result);
            }
        }
    }

    Texture2D CaptureARFrame()
    {
        try
        {
            var cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("[BARCODE] Camera.main is NULL!");
                return null;
            }
            if (_cameraTexture == null)
            {
                Debug.LogError("[BARCODE] _cameraTexture is NULL (not initialized)");
                return null;
            }
            Debug.Log("[BARCODE] Using camera: " + cam.name);

            RenderTexture rt = new RenderTexture(640, 480, 0);
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = null;

            RenderTexture.active = rt;
            _cameraTexture.ReadPixels(new Rect(0, 0, 640, 480), 0, 0);
            _cameraTexture.Apply();
            RenderTexture.active = null;
            Destroy(rt);

            return _cameraTexture;
        }
        catch (Exception e)
        {
            Debug.LogError("[BARCODE] Capture error: " + e.Message);
            return null;
        }
    }

    void OnBarcodeDetected(string barcode)
    {
        Debug.Log("Barcode detected: " + barcode);
        reticulePanel.SetActive(false);
        statusText.text = "";
        loadingPanel.SetActive(true);
        loadingText.text = "Recherche du produit...";

        StartCoroutine(FetchProduct(barcode));
    }

    IEnumerator FetchProduct(string barcode)
    {
        string url = $"https://world.openfoodfacts.org/api/v0/product/{barcode}.json";
        Debug.Log("[BARCODE] Requesting: " + url);
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.SetRequestHeader("User-Agent", "Cocorico2/1.0");
            yield return req.SendWebRequest();

            loadingPanel.SetActive(false);

            if (req.result != UnityWebRequest.Result.Success)
            {
                statusText.text = "Erreur réseau. Vérifie ta connexion.";
                reticulePanel.SetActive(true);
                _detected = false;
                _scanning = true;
                StartCoroutine(ScanLoop());
                yield break;
            }

            ParseAndDisplay(req.downloadHandler.text);
        }
    }

    void ParseAndDisplay(string json)
    {
        try
        {
            var data = JsonUtility.FromJson<OFFResponse>(json);

            if (data == null || data.status == 0)
            {
                ShowNotFound();
                return;
            }

            var p = data.product;
            Debug.Log("[BARCODE] Product found: " + data.product.product_name);

            productPanel.SetActive(true);

            productNameText.text = string.IsNullOrEmpty(p.product_name) ? "Produit inconnu" : p.product_name;
            brandText.text = string.IsNullOrEmpty(p.brands) ? "" : p.brands;

            // Nutri-Score
            string ns = string.IsNullOrEmpty(p.nutriscore_grade) || p.nutriscore_grade.ToLower() == "unknown" ? "?" : p.nutriscore_grade.ToUpper();
            nutriScoreText.text = ns;
            Color nsColor;
            ColorUtility.TryParseHtmlString(GetNutriScoreColor(ns), out nsColor);
            //nutriScorePanel.color = nsColor;
            nutriScoreText.color =nsColor;

            // Nutriments
            var n = p.nutriments;
            caloriesText.text = n.energy_kcal_100g > 0 ? $"{(int)n.energy_kcal_100g} kcal" : "—";
            sucresText.text = n.sugars_100g >= 0 ? $"{n.sugars_100g:F1}g" : "—";
            graissesText.text = n.fat_100g >= 0 ? $"{n.fat_100g:F1}g" : "—";
            proteinesText.text = n.proteins_100g >= 0 ? $"{n.proteins_100g:F1}g" : "—";

            // Allergènes
            if (!string.IsNullOrEmpty(p.allergens_tags) && p.allergens_tags != "[]")
                allergenesText.text = p.allergens_tags.Replace("en:", "").Replace("[", "").Replace("]", "").Replace("\"", "");
            else
                allergenesText.text = "Aucun allergène déclaré";
        }
        catch (Exception e)
        {
            Debug.LogError("Parse error: " + e.Message);
            ShowNotFound();
        }
    }

    void ShowNotFound()
    {
        statusText.text = "Produit non trouvé dans la base Open Food Facts";
        reticulePanel.SetActive(true);
        Invoke(nameof(ResetScan), 2f);
    }

    public void ResetScan()
    {
        productPanel.SetActive(false);
        _detected = false;
        _scanning = true;
        reticulePanel.SetActive(true);
        statusText.text = "Pointez la caméra vers un code-barres";
        StartCoroutine(ScanLoop());
    }

    string GetNutriScoreColor(string score)
    {
        switch (score)
        {
            case "A": return "#1a9e3f";
            case "B": return "#85bb2f";
            case "C": return "#f9a825";
            case "D": return "#e67e22";
            case "E": return "#e74c3c";
            default: return "#888888";
        }
    }
}