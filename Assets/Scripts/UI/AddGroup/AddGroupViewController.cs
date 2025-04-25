using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace App.UI
{
    public class AddGroupViewController : ViewController
    {
        [SerializeField] private TMP_InputField nameInputField;
        [SerializeField] private TMP_InputField shortNameInputField;
        [SerializeField] private Button createButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button closeButtonX;

        public override void Init(IWindowStarter starter)
        {
            base.Init(starter);

            if (createButton != null)
            {
                createButton.onClick.RemoveAllListeners();
                createButton.onClick.AddListener(HandleCreateClick);
            }
            else
            {
                Debug.LogError("Create Button not assigned in AddGroupView inspector!", this.gameObject);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveAllListeners();
                cancelButton.onClick.AddListener(HandleCloseClick);
            }
            else
            {
                Debug.LogError("Cancel Button not assigned in AddGroupView inspector!", this.gameObject);
            }

            if (closeButtonX != null)
            {
                closeButtonX.onClick.RemoveAllListeners();
                closeButtonX.onClick.AddListener(HandleCloseClick);
            }
            else
            {
                Debug.LogWarning("Close Button X not assigned in AddGroupView inspector.", this.gameObject);
            }
        }

        private void HandleCreateClick()
        {
            if (nameInputField != null && shortNameInputField != null)
            {
                Debug.Log($"Create clicked. Full Name: {nameInputField.text}, Short Name: {shortNameInputField.text}");
            }
            else
            {
                Debug.LogError("One or both InputFields are not assigned in the inspector!");
            }
        }

        private void HandleCloseClick()
        {
            Debug.Log($"Close/Cancel button clicked for {this.name}. Closing window.");
            Close();
        }

        protected override void OnDestroy()
        {
            if (createButton != null)
            {
                createButton.onClick.RemoveListener(HandleCreateClick);
            }
            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveListener(HandleCloseClick);
            }
            if (closeButtonX != null)
            {
                closeButtonX.onClick.RemoveListener(HandleCloseClick);
            }
            base.OnDestroy();
        }
    }
}