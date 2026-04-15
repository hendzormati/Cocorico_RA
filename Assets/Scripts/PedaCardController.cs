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
    public Button openQuizButton;

    [Header("Quiz UI")]
    public GameObject quizPanel;
    public TextMeshProUGUI questionText;
    public Button[] choiceButtons;
    public TextMeshProUGUI scoreText;
    public Button closeQuizButton;

    private FoodData _data;
    private int _currentQuestion = 0;
    private int _score = 0;

    void Start()
    {
        // Get image name from parent ARTrackedImage
        var trackedImage = GetComponentInParent<ARTrackedImage>();
        if (trackedImage != null)
        {
            string imageName = trackedImage.referenceImage.name;
            LoadAndSetup(imageName);
        }
        else
        {
            Debug.LogError("No ARTrackedImage parent found!");
        }
    }

    void LoadAndSetup(string imageName)
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("peda_content");
        if (jsonFile == null)
        {
            Debug.LogError("peda_content.json not found in Resources!");
            return;
        }

        FoodDatabase db = JsonUtility.FromJson<FoodDatabase>(jsonFile.text);
        FoodData data = GetFoodData(db, imageName);

        if (data != null)
            Setup(data);
        else
            Debug.LogWarning("No data for: " + imageName);
    }

    FoodData GetFoodData(FoodDatabase db, string name)
    {
        switch (name.ToLower())
        {
            case "tomate": return db.tomate;
            case "banane": return db.banane;
            case "brocoli": return db.brocoli;
            default: return null;
        }
    }

    public void Setup(FoodData data)
    {
        _data = data;
        nomText.text = data.nom;
        origineText.text = "Origine : " + data.origine;
        caloriesText.text = "Calories : " + data.calories;
        conservationText.text = "Conservation : " + data.conservation;
        quizPanel.SetActive(false);

        // Connect button listeners
        openQuizButton.onClick.AddListener(OpenQuiz);
        closeQuizButton.onClick.AddListener(CloseQuiz);

        // Pop-in animation
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

    public void OpenQuiz()
    {
        quizPanel.SetActive(true);
        _currentQuestion = 0;
        _score = 0;
        scoreText.gameObject.SetActive(false);
        ShowQuestion();
    }

    public void CloseQuiz()
    {
        quizPanel.SetActive(false);
    }

    void ShowQuestion()
    {
        if (_currentQuestion >= _data.quiz.Count)
        {
            ShowScore();
            return;
        }

        var q = _data.quiz[_currentQuestion];
        questionText.text = q.question;

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            int index = i;
            choiceButtons[i].gameObject.SetActive(i < q.choices.Count);
            choiceButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = q.choices[i];
            choiceButtons[i].GetComponent<Image>().color =
                ColorUtility.TryParseHtmlString("#006db0", out Color c) ? c : Color.blue;
            choiceButtons[i].onClick.RemoveAllListeners();
            choiceButtons[i].onClick.AddListener(() => OnAnswer(index));
        }
    }

    void OnAnswer(int index)
    {
        var q = _data.quiz[_currentQuestion];
        if (index == q.correct)
        {
            _score++;
            choiceButtons[index].GetComponent<Image>().color =
                ColorUtility.TryParseHtmlString("#65B661", out Color c) ? c : Color.green;
        }
        else
        {
            choiceButtons[index].GetComponent<Image>().color = Color.red;
            choiceButtons[q.correct].GetComponent<Image>().color =
                ColorUtility.TryParseHtmlString("#65B661", out Color c) ? c : Color.green;
        }

        StartCoroutine(NextQuestionDelay());
    }

    IEnumerator NextQuestionDelay()
    {
        yield return new WaitForSeconds(1f);
        _currentQuestion++;
        ShowQuestion();
    }

    void ShowScore()
    {
        questionText.gameObject.SetActive(false);
        foreach (var btn in choiceButtons)
            btn.gameObject.SetActive(false);

        scoreText.gameObject.SetActive(true);
        scoreText.text = $"Score : {_score} / {_data.quiz.Count}";
    }
}