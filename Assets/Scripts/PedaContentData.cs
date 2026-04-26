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
    public string categorie;

    public string type;
    public string nutriscore;

    public string calories;
    public string sucres;
    public string fibres;
    public string additifs;

    public string message_court;
    public string origine;
    public string conservation;

    public List<QuizQuestion> quiz;
    internal string proteines;
    internal string glucides;
}

[System.Serializable]
public class FoodDatabase
{
    public List<FoodEntry> foods;
}

[System.Serializable]
public class FoodEntry
{
    public string id;
    public FoodData data;
}