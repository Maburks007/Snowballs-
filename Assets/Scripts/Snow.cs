using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Snow : MonoBehaviour
{

    public int Size;

    // Start is called before the first frame update
    void Start()
    {
        transform.localScale = new Vector3(Size * 0.1f, Size * 0.1f, Size * 0.1f);
    }


    public void SetSize(int newSize)
    {
        Size = newSize;
        transform.localScale = new Vector3(Size * 0.1f, Size * 0.1f, Size * 0.1f);
    }
}
