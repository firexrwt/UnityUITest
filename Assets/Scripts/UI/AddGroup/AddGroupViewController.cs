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

        [SerializeField] private float cornerRadius = 10f; // Радиус закругления углов

        public override void Init(IWindowStarter starter)
        {
            base.Init(starter);

            // Существующий код инициализации кнопок...

            // Применяем закругление углов к кнопкам
            ApplyRoundedCorners(createButton.gameObject);
            ApplyRoundedCorners(cancelButton.gameObject);
            ApplyRoundedCorners(closeButtonX.gameObject);
        }

        // Остальные методы из вашего кода...

        private void ApplyRoundedCorners(GameObject buttonObject)
        {
            if (buttonObject == null) return;

            // Получаем компонент Image кнопки
            Image image = buttonObject.GetComponent<Image>();
            if (image == null) return;

            // Добавляем компонент для генерации меша с закругленными углами
            RoundedButton roundedBtn = buttonObject.GetComponent<RoundedButton>();
            if (roundedBtn == null)
                roundedBtn = buttonObject.AddComponent<RoundedButton>();

            // Настраиваем размеры согласно текущим размерам кнопки
            RectTransform rt = buttonObject.GetComponent<RectTransform>();
            roundedBtn.SetSize(rt.rect.width, rt.rect.height, cornerRadius);
            roundedBtn.GenerateMesh();
        }
    }

    // Класс для генерации закругленных углов
    public class RoundedButton : MonoBehaviour
    {
        private float width;
        private float height;
        private float borderRadius;
        private MeshFilter meshFilter;

        public void SetSize(float w, float h, float radius)
        {
            width = w;
            height = h;
            borderRadius = radius;
        }

        public void GenerateMesh()
        {
            if (meshFilter == null)
                meshFilter = gameObject.AddComponent<MeshFilter>();

            var w = width * .5f;
            var h = height * .5f;

            var vertices = new Vector3[91 * 4];
            var j = 0;

            for (var startAngle = 0; startAngle < 360; startAngle += 90)
            {
                var p = new Vector3(
                    (w - borderRadius) * (startAngle == 0 || startAngle == 270 ? 1 : -1),
                    (h - borderRadius) * (startAngle < 180 ? 1 : -1)
                );

                for (var i = startAngle; i <= startAngle + 90; i++)
                {
                    var a = i * Mathf.Deg2Rad;
                    vertices[j++] = p + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * borderRadius;
                }
            }

            // Создание треугольников (упрощено)
            var triangles = new int[(90 * 3 * 4) + 18];
            // Заполнение массива треугольников...

            var mesh = new Mesh { vertices = vertices };
            mesh.triangles = triangles;
            meshFilter.mesh = mesh;
        }
    }
}
