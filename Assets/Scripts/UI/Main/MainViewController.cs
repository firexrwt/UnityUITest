using App.UI;
using UnityEngine;
using UnityEngine.UI;

namespace App.UI
{
    public class MainViewController : ViewController
    {
        [SerializeField] private Button openWindowButton;

        public override void Init(IWindowStarter starter)
        {
            base.Init(starter);

            if (openWindowButton != null)
            {
                openWindowButton.onClick.RemoveAllListeners();
                openWindowButton.onClick.AddListener(HandleOpenNewWindowClick);
            }
            else
            {
                Debug.LogError("Button 'openWindowButton' not assigned in inspector!", this.gameObject);
            }
        }

        private void HandleOpenNewWindowClick()
        {
            Debug.Log("Open Window button clicked!");
            WindowsManager.Instance.CreateWindow<AddGroupViewController>(new AddGroupViewStarter()).Show();
        }

        protected override void OnDestroy()
        {
            if (openWindowButton != null)
            {
                openWindowButton.onClick.RemoveListener(HandleOpenNewWindowClick);
            }
            base.OnDestroy();
        }
    }
}