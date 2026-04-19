using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

public class PedaCardController : MonoBehaviour
{
    [Header("Card UI")]
    public TextMeshProUGUI nomText;
    public TextMeshProUGUI origineText;
    public TextMeshProUGUI caloriesText;
    public TextMeshProUGUI conservationText;

    public TextMeshProUGUI categorieText;
    public TextMeshProUGUI nutriScoreText;
    public TextMeshProUGUI sucresText;
    public TextMeshProUGUI fibresText;
    public TextMeshProUGUI additifsText;
    public TextMeshProUGUI messageCourtText;

    public Button openQuizButton;
    public TextMeshProUGUI openQuizButtonText;

    [Header("Quiz UI")]
    public GameObject quizPanel;
    public TextMeshProUGUI questionText;
    public Button[] choiceButtons;
    public TextMeshProUGUI scoreText;
    public Button closeQuizButton;

    private FoodData _data;
    private int _currentQuestion = 0;
    private int _score = 0;
    private bool _quizCompleted = false;
    private bool _quizStarted = false;
    private bool _answerLocked = false;
    private Dictionary<string, FoodData> foodMap;
    private ARTrackedImage _trackedImage;

    void Start()
    {
        var trackedImage = GetComponentInParent<ARTrackedImage>();
        if (trackedImage != null)
            LoadAndSetup(trackedImage.referenceImage.name);
        else
            Debug.LogError("No ARTrackedImage parent found!");
    }

    void LoadAndSetup(string imageName)
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("peda_content");
        if (jsonFile == null)
        {
            Debug.LogError("peda_content.json not found!");
            return;
        }

        FoodDatabase db = JsonUtility.FromJson<FoodDatabase>(jsonFile.text);

        BuildFoodMap(db);

        FoodData data = GetFoodData(imageName);

        if (data != null)
            Setup(data);
        else
            Debug.LogWarning($"No data for: {imageName}");
    }
    void BuildFoodMap(FoodDatabase db)
    {
        foodMap = new Dictionary<string, FoodData>();

        foreach (var entry in db.foods)
        {
            if (entry == null || entry.data == null) continue;

            foodMap[entry.id.ToLower()] = entry.data;
        }
    }
    FoodData GetFoodData(string name)
    {
        if (foodMap == null)
        {
            Debug.LogError("Food map not initialized!");
            return null;
        }

        if (foodMap.TryGetValue(name.ToLower(), out FoodData data))
            return data;

        Debug.LogWarning("Food not found: " + name);
        return null;
    }

    public void Setup(FoodData data)
    {
        _data = data;

        Canvas canvas = GetComponentInChildren<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
            canvas.worldCamera = Camera.main;

        nomText.text = data.nom;
        origineText.text = "Origine : " + data.origine;
        caloriesText.text = "Calories : " + data.calories;
        conservationText.text = "Conservation : " + data.conservation;

        if (categorieText) categorieText.text = "Catégorie : " + data.categorie;
        if (nutriScoreText) nutriScoreText.text = "Nutri-Score : " + data.nutriscore;
        if (sucresText) sucresText.text = "Sucres : " + data.sucres;
        if (fibresText) fibresText.text = "Fibres : " + data.fibres;
        if (additifsText) additifsText.text = "Additifs : " + data.additifs;
        if (messageCourtText) messageCourtText.text = data.message_court;

        ApplyNutriScoreColor(data.nutriscore);

        quizPanel.SetActive(false);
        scoreText.gameObject.SetActive(false);

        openQuizButton.onClick.RemoveAllListeners();
        openQuizButton.onClick.AddListener(OpenQuiz);

        closeQuizButton.onClick.RemoveAllListeners();
        closeQuizButton.onClick.AddListener(CloseQuiz);

        UpdateOpenButtonLabel();

        transform.localScale = Vector3.zero;
        StartCoroutine(PopIn());
    }

    IEnumerator PopIn()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 3f;
            transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);
            yield return null;
        }
        transform.localScale = Vector3.one;
    }

    void ApplyNutriScoreColor(string score)
    {
        if (!nutriScoreText) return;

        string hex = GetNutriScoreColor(score);

        if (ColorUtility.TryParseHtmlString(hex, out Color color))
            nutriScoreText.color = color;
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

    void UpdateOpenButtonLabel()
    {
        if (!openQuizButtonText) return;

        if (_quizCompleted)
            openQuizButtonText.text = "Quiz terminé";
        else if (_quizStarted)
            openQuizButtonText.text = "Continuer le quiz";
        else
            openQuizButtonText.text = "Faire le quiz";
    }

    public void OpenQuiz()
    {
        if (_quizCompleted) return;

        quizPanel.SetActive(true);
        _answerLocked = false;

        if (!_quizStarted)
        {
            _quizStarted = true;
            _currentQuestion = 0;
            _score = 0;
        }

        questionText.gameObject.SetActive(true);
        scoreText.gameObject.SetActive(false);

        foreach (var btn in choiceButtons)
            btn.gameObject.SetActive(true);

        ShowQuestion();
    }

    public void CloseQuiz()
    {
        quizPanel.SetActive(false);
        UpdateOpenButtonLabel();
    }

    void ShowQuestion()
    {
        if (_currentQuestion >= _data.quiz.Count)
        {
            ShowScore();
            return;
        }

        _answerLocked = false;

        var q = _data.quiz[_currentQuestion];
        questionText.text = q.question;

        ColorUtility.TryParseHtmlString("#006db0", out Color defaultColor);

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            bool active = i < q.choices.Count;
            choiceButtons[i].gameObject.SetActive(active);

            if (!active) continue;

            int captured = i;

            choiceButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = q.choices[i];
            choiceButtons[i].GetComponent<Image>().color = defaultColor;
            choiceButtons[i].interactable = true;

            choiceButtons[i].onClick.RemoveAllListeners();
            choiceButtons[i].onClick.AddListener(() => OnAnswer(captured));
        }
    }

    void OnAnswer(int index)
    {
        if (_answerLocked) return;
        _answerLocked = true;

        foreach (var btn in choiceButtons)
            btn.interactable = false;

        var q = _data.quiz[_currentQuestion];

        ColorUtility.TryParseHtmlString("#65B661", out Color correctColor);

        if (index == q.correct)
        {
            _score++;
            choiceButtons[index].GetComponent<Image>().color = correctColor;
        }
        else
        {
            choiceButtons[index].GetComponent<Image>().color = Color.red;
            choiceButtons[q.correct].GetComponent<Image>().color = correctColor;
        }

        StartCoroutine(NextQuestionDelay());
    }

    IEnumerator NextQuestionDelay()
    {
        yield return new WaitForSeconds(1.5f);
        _currentQuestion++;
        ShowQuestion();
    }

    void ShowScore()
    {
        _quizCompleted = true;

        questionText.gameObject.SetActive(false);

        foreach (var btn in choiceButtons)
            btn.gameObject.SetActive(false);

        scoreText.gameObject.SetActive(true);
        scoreText.text = $"Score : {_score} / {_data.quiz.Count}";

        openQuizButton.interactable = false;
        UpdateOpenButtonLabel();

        StartCoroutine(AutoCloseQuiz());
    }

    IEnumerator AutoCloseQuiz()
    {
        yield return new WaitForSeconds(3f);
        quizPanel.SetActive(false);
    }
}