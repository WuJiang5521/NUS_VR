﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PreviewManual : MonoBehaviour {
    public ManualController controller;
    public GameObject bg;

    // Update is called once per frame
    void Update () {
        if (Input.GetKeyDown(KeyCode.A)) OnLaserOver();
        if (Input.GetKeyUp(KeyCode.A)) OnButtonTrigger();
	}

    void OnLaserOver()
    {
        bg.SetActive(true);
        GetComponent<TextMesh>().color = new Color(16.0f / 255, 70.0f / 255, 143.0f / 255);
    }

    void OnButtonTrigger()
    {
        controller.previewPage();
        bg.SetActive(false);
        GetComponent<TextMesh>().color = new Color(1.0f, 1.0f, 1.0f);
    }
}
