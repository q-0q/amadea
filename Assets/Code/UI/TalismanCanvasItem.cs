using System;
using UnityEngine;
using UnityEngine.UI;

public class TalismanCanvasItem : MonoBehaviour
{

    public Image image;


    private void Awake()
    {
        image.color = new Color(1f, 1f, 1f, 0.05f);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public void MakeComplete()
    {
        image.color = Color.white;
    }
    
}
