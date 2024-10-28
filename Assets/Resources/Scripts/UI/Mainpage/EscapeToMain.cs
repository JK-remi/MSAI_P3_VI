using UnityEngine;
using UnityEngine.SceneManagement;

public class EscapeToMain : MonoBehaviour
{
    public string mainSceneName = "Mainpage"; // ���� ������ �� �̸� ����

    void Update()
    {
        // ESC Ű�� ���� �� ���� ������ ��ȯ
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SceneManager.LoadScene(mainSceneName);
        }
    }
}

