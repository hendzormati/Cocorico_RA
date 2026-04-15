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

    void Start()
    {
        
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

        planeManager.enabled = false;
        imageManager.enabled = false;

        CurrentMode = AppMode.None;
    }

    public void SelectMode(int mode)
    {
        CurrentMode = (AppMode)mode;
        Debug.Log("Mode selected: " + CurrentMode);
        panelModeSelection.SetActive(false);
        panelSideMenu.SetActive(false);
        hamburgerButton.SetActive(true);

        planeManager.enabled = (CurrentMode == AppMode.FruitsVeggies);
        imageManager.enabled = (CurrentMode == AppMode.ImageTracking);
    Debug.Log("ImageManager enabled: " + imageManager.enabled);
    Debug.Log("ImageManager library: " + (imageManager.referenceLibrary != null ? "OK" : "NULL"));

        canvasMode1.SetActive(CurrentMode == AppMode.FruitsVeggies);
        canvasMode2.SetActive(CurrentMode == AppMode.Barcode);
        canvasMode3.SetActive(CurrentMode == AppMode.ImageTracking);
    }

    public void ToggleSideMenu()
    {
        panelSideMenu.SetActive(!panelSideMenu.activeSelf);
    }

    public void OnFruitsVeggiesSelected() => SelectMode(1);
    public void OnBarcodeSelected() => SelectMode(2);
    public void OnImageTrackingSelected() => SelectMode(3);
}