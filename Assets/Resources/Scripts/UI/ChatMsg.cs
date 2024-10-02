using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ChatMsg : MonoBehaviour
{
    [SerializeField]
    private TMP_Text txtMessage;

    private void Start()
    {
        txtMessage = this.GetComponent<TMP_Text>();
    }

    public void OnClicked()
    {
        string msg = Utils.SubString(txtMessage.text, ":", 1);
        GameManager.Instance.Send2GPT(msg);
    }
}
