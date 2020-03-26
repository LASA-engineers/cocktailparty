using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Main_VolumeSlider : MonoBehaviour {

    public long userId;

    public void VolumeChanged() {
        var slider = gameObject.GetComponent<Slider>();
        GameObject.Find("Main").GetComponent<DiscordController>().ChangeVolume(userId, slider.value);
    }

}
