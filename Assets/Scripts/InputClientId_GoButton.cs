using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class InputClientId_GoButton : MonoBehaviour {

    public void Clicked() {
        ClientIdSender.clientId = Int64.Parse(
                GameObject.Find("Canvas").transform.Find("InputField").gameObject.GetComponent<InputField>().text);
        SceneManager.LoadScene("Main");
    }

}
