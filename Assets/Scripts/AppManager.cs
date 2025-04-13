using App.UI;
using UnityEngine;

namespace App
{
    public class AppManager : MonoBehaviour
    {
        void Start()
        {
            Debug.Log("AppManager Start");
            LeanTween.reset();
            LeanTween.init();
            Debug.Log("AppManager: Attempting to get WindowsManager Instance and CreateWindow/Show...");
            try
            {
                // Этот вызов инициирует WindowsManager и создание окна
                WindowsManager.Instance.CreateWindow<MainViewController>(new MainViewStarter()).Show();
                Debug.Log("AppManager: CreateWindow<MainView>().Show() called successfully (no immediate exception).");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"AppManager: Error during Window creation/showing: {e.Message}\n{e.StackTrace}");
            }
        }

        private void OnDestroy()
        {
            
        }
    }
}