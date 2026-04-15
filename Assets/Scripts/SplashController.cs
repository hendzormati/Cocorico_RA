using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class SplashController : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI loadingText;
    public Slider progressBar;

    [Header("Settings")]
    public float totalLoadTime = 3f;

    private string[] loadingPhrases = {
        "Chargement du modèle IA...",
        "Initialisation de la caméra AR...",
        "Préparation des ingrédients...",
        "Presque prêt..."
    };

    void Start()
    {
        StartCoroutine(LoadingSequence());
    }

    IEnumerator LoadingSequence()
    {
        float elapsed = 0f;
        int phraseIndex = 0;
        float phraseInterval = totalLoadTime / loadingPhrases.Length;
        float nextPhraseTime = phraseInterval;

        loadingText.text = loadingPhrases[0];

        while (elapsed < totalLoadTime)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / totalLoadTime;
            progressBar.value = progress;
            if (elapsed >= nextPhraseTime && phraseIndex < loadingPhrases.Length - 1)
            {
                phraseIndex++;
                loadingText.text = loadingPhrases[phraseIndex];
                nextPhraseTime += phraseInterval;
            }
            yield return null;
        }

        progressBar.value = 1f;
        loadingText.text = "Prêt !";

        yield return new WaitForSeconds(0.5f);

        SceneManager.LoadScene("MainScene");
    }
}