using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ChatMsg : MonoBehaviour
{
    public TextMeshProUGUI txtMessage;

    private void Start()
    {
        if(txtMessage == null)
        {
            txtMessage = this.GetComponent<TextMeshProUGUI>();
        }
    }

    public void OnClicked()
    {
        string msg = Utils.SubString(txtMessage.text, ":", 1);
        GameManager.Instance.Send2GPT(msg);
    }

    public void SetMessage(string msg)
    {
        txtMessage.text = msg;
    }
}
