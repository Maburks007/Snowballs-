using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SnowDeposit : MonoBehaviour
{
    public Transform myCanvas;

    public TextMeshProUGUI snowText;

    int size = 0;

    // Start is called before the first frame update
    void Start()
    {
        AddSnow(0);
    }

    // Update is called once per frame
    void Update()
    {
        if(Camera.main != null)
            myCanvas.LookAt(Camera.main.transform);
    }


    public void AddSnow(int snowAmount)
    {
        size += snowAmount;

        snowText.text = size.ToString();
    }
}
