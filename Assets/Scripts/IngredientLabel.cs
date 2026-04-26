using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class IngredientLabel : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI confidenceText;
    public Image bgImage;

    private FoodData _data;
    private Action<FoodData, Vector3> _onTap;

    public void Setup(string id, float confidence, int qty, FoodData data, Action<FoodData, Vector3> onTap)
    {
        _data = data;
        _onTap = onTap;

        if (nameText == null)       Debug.LogError("[MODE1][Label] nameText is NULL on prefab!");
        if (confidenceText == null) Debug.LogError("[MODE1][Label] confidenceText is NULL on prefab!");

        string displayName = data != null ? data.nom : id;
        // Show quantity in parentheses only when more than one
        nameText.text       = qty > 1 ? $"{displayName} (x{qty})" : displayName;
        confidenceText.text = $"{(int)(confidence * 100)}%";

        if (bgImage != null && data != null)
        {
            Color c;
            string hex = data.type == "sain" ? "#65B661" : "#e74c3c";
            if (ColorUtility.TryParseHtmlString(hex, out c))
                bgImage.color = new Color(c.r, c.g, c.b, 0.88f);
        }
        else if (bgImage == null)
        {
            Debug.LogWarning("[MODE1][Label] bgImage not assigned on label prefab.");
        }

        Debug.Log($"[MODE1][Label] Setup: name='{nameText.text}' qty={qty} confidence={confidence:P0} " +
                  $"hasData={data != null} worldPos={transform.position}");

        transform.localScale = Vector3.zero;
        StartCoroutine(PopIn());
    }

    System.Collections.IEnumerator PopIn()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 4f;
            transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);
            yield return null;
        }
        transform.localScale = Vector3.one;
    }

    void Update()
    {
        // Billboard — always face camera
        if (Camera.main != null)
        {
            transform.LookAt(
                transform.position + Camera.main.transform.rotation * Vector3.forward,
                Camera.main.transform.rotation * Vector3.up
            );
        }

        // Touch tap detection
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            var touch = Input.GetTouch(0);
            Ray ray = Camera.main.ScreenPointToRay(touch.position);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider != null && hit.collider.gameObject == gameObject)
                {
                    Debug.Log($"[MODE1][Label] Tapped: {(_data != null ? _data.nom : "unknown")} at {transform.position}");
                    _onTap?.Invoke(_data, transform.position);
                }
            }
        }
    }
}