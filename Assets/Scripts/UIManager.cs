using UnityEngine;

public class UIManager : MonoBehaviour
{
    public GeneticManager geneticManager;

    public void OnSaveButtonClicked()
    {
        geneticManager.SaveBestCar();
    }

    public void OnLoadButtonClicked()
    {
        geneticManager.LoadBestCar();
    }
}