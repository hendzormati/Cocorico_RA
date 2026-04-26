using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class ModeManager : MonoBehaviour
{
    public enum AppMode { None, FruitsVeggies, Barcode, ImageTracking }
    public static AppMode CurrentMode { get; private set; } = AppMode.None;

    [Header("AR Components")]
    public ARPlaneManager planeManager;
    public ARTrackedImageManager imageManager;

    [Header("Canvas")]
    public GameObject canvasMenu;
    public GameObject canvasMode1;
    public GameObject canvasMode2;
    public GameObject canvasMode3;

    [Header("UI")]
    public GameObject panelModeSelection;
    public GameObject hamburgerButton;
    public GameObject panelSideMenu;

    [Header("Mode Controllers")]
    public BarcodeScanner barcodeScanner;
    public FruitsVeggiesScanner fruitsScanner;

    void Start()
    {
        Debug.Log("[ModeManager] Start — showing mode selection.");
        ShowModeSelection();
    }

    public void ShowModeSelection()
    {
        panelModeSelection.SetActive(true);
        hamburgerButton.SetActive(false);
        panelSideMenu.SetActive(false);

        canvasMode1.SetActive(false);
        canvasMode2.SetActive(false);
        canvasMode3.SetActive(false);

        planeManager.enabled  = false;
        imageManager.enabled  = false;

        if (fruitsScanner != null)  fruitsScanner.enabled  = false;
        if (barcodeScanner != null) barcodeScanner.gameObject.SetActive(false);

        CurrentMode = AppMode.None;
        Debug.Log("[ModeManager] Mode reset to None.");
    }

    public void SelectMode(int mode)
    {
        CurrentMode = (AppMode)mode;
        Debug.Log($"[ModeManager] Mode selected: {CurrentMode} (int={mode})");

        panelModeSelection.SetActive(false);
        panelSideMenu.SetActive(false);
        hamburgerButton.SetActive(true);

        // AR managers
        planeManager.enabled  = (CurrentMode == AppMode.FruitsVeggies);
        imageManager.enabled  = (CurrentMode == AppMode.ImageTracking);

        Debug.Log($"[ModeManager] planeManager.enabled={planeManager.enabled}");
        Debug.Log($"[ModeManager] imageManager.enabled={imageManager.enabled}");
        Debug.Log($"[ModeManager] imageManager.referenceLibrary={(imageManager.referenceLibrary != null ? "OK" : "NULL — assign it in Inspector!")}");

        // Mode-specific controllers
        if (fruitsScanner != null)
            fruitsScanner.enabled = (CurrentMode == AppMode.FruitsVeggies);
        else if (CurrentMode == AppMode.FruitsVeggies)
            Debug.LogError("[ModeManager] fruitsScanner is NULL — assign FruitsVeggiesManager in Inspector!");

        if (barcodeScanner != null)
            barcodeScanner.gameObject.SetActive(CurrentMode == AppMode.Barcode);
        else if (CurrentMode == AppMode.Barcode)
            Debug.LogWarning("[ModeManager] barcodeScanner is NULL.");

        // Canvases
        canvasMode1.SetActive(CurrentMode == AppMode.FruitsVeggies);
        canvasMode2.SetActive(CurrentMode == AppMode.Barcode);
        canvasMode3.SetActive(CurrentMode == AppMode.ImageTracking);

        Debug.Log($"[ModeManager] Canvases — Mode1={canvasMode1.activeSelf} Mode2={canvasMode2.activeSelf} Mode3={canvasMode3.activeSelf}");
    }

    public void ToggleSideMenu()
    {
        panelSideMenu.SetActive(!panelSideMenu.activeSelf);
    }

    public void OnFruitsVeggiesSelected() => SelectMode(1);
    public void OnBarcodeSelected()       => SelectMode(2);
    public void OnImageTrackingSelected() => SelectMode(3);
}