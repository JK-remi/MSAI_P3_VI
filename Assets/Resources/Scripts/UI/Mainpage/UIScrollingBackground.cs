using UnityEngine;

public class UIScrollingBackground : MonoBehaviour
{
    public float scrollSpeed = 100f;
    private RectTransform rectTransform;
    private float resetPosition = 1920f; // 스크롤링 최대 위치

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        // 배경을 왼쪽으로 이동시키기
        rectTransform.anchoredPosition += new Vector2(-scrollSpeed * Time.deltaTime, 0);

        // 위치가 -1920보다 작아지면 원래 위치로 돌아오게 설정
        if (rectTransform.anchoredPosition.x <= -resetPosition)
        {
            rectTransform.anchoredPosition = new Vector2(0, rectTransform.anchoredPosition.y);
        }
    }
}
