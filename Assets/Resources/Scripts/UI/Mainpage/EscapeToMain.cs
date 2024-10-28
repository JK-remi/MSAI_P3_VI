using UnityEngine;
using UnityEngine.SceneManagement;

public class EscapeToMain : MonoBehaviour
{
    public string mainSceneName = "Mainpage"; // 메인 페이지 씬 이름 설정

    void Update()
    {
        // ESC 키를 누를 때 메인 씬으로 전환
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SceneManager.LoadScene(mainSceneName);
        }
    }
}

