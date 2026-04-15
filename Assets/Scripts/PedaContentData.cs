using System.Collections.Generic;

[System.Serializable]
public class QuizQuestion
{
    public string question;
    public List<string> choices;
    public int correct;
}

[System.Serializable]
public class FoodData
{
    public string nom;
    public string emoji;
    public string origine;
    public string calories;
    public string proteines;
    public string glucides;
    public string conservation;
    public List<QuizQuestion> quiz;
}

[System.Serializable]
public class FoodDatabase
{
    public FoodData tomate;
    public FoodData banane;
    public FoodData brocoli;
}