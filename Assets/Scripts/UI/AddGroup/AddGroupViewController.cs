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

        [SerializeField] private float cornerRadius = 10f; // ������ ����������� �����

        public override void Init(IWindowStarter starter)
        {
            base.Init(starter);

            // ������������ ��� ������������� ������...

            // ��������� ����������� ����� � �������
            ApplyRoundedCorners(createButton.gameObject);
            ApplyRoundedCorners(cancelButton.gameObject);
            ApplyRoundedCorners(closeButtonX.gameObject);
        }

        // ��������� ������ �� ������ ����...

        private void ApplyRoundedCorners(GameObject buttonObject)
        {
            if (buttonObject == null) return;

            // �������� ��������� Image ������
            Image image = buttonObject.GetComponent<Image>();
            if (image == null) return;

            // ��������� ��������� ��� ��������� ���� � ������������� ������
            RoundedButton roundedBtn = buttonObject.GetComponent<RoundedButton>();
            if (roundedBtn == null)
                roundedBtn = buttonObject.AddComponent<RoundedButton>();

            // ����������� ������� �������� ������� �������� ������
            RectTransform rt = buttonObject.GetComponent<RectTransform>();
            roundedBtn.SetSize(rt.rect.width, rt.rect.height, cornerRadius);
            roundedBtn.GenerateMesh();
        }
    }

    // ����� ��� ��������� ������������ �����
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

            // �������� ������������� (��������)
            var triangles = new int[(90 * 3 * 4) + 18];
            // ���������� ������� �������������...

            var mesh = new Mesh { vertices = vertices };
            mesh.triangles = triangles;
            meshFilter.mesh = mesh;
        }
    }
}
