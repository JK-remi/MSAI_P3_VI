using UnityEngine;

public class UIScrollingBackground : MonoBehaviour
{
    public float scrollSpeed = 100f;
    private RectTransform rectTransform;
    private float resetPosition = 1920f; // ��ũ�Ѹ� �ִ� ��ġ

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        // ����� �������� �̵���Ű��
        rectTransform.anchoredPosition += new Vector2(-scrollSpeed * Time.deltaTime, 0);

        // ��ġ�� -1920���� �۾����� ���� ��ġ�� ���ƿ��� ����
        if (rectTransform.anchoredPosition.x <= -resetPosition)
        {
            rectTransform.anchoredPosition = new Vector2(0, rectTransform.anchoredPosition.y);
        }
    }
}
