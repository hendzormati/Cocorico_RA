[System.Serializable]
public class OFFResponse
{
    public int status;
    public OFFProduct product;
}

[System.Serializable]
public class OFFProduct
{
    public string product_name;
    public string brands;
    public string nutriscore_grade;
    public string allergens_tags;
    public OFFNutriments nutriments;
}

[System.Serializable]
public class OFFNutriments
{
    public float energy_kcal_100g;
    public float sugars_100g;
    public float fat_100g;
    public float proteins_100g;
}