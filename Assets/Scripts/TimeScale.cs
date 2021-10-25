using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeScale : MonoBehaviour
{

    private float fixedDeltaTime;

    public float timeScale;
    public float maxTimeScale = 2.5f;


    void Awake()
    {
        this.fixedDeltaTime = Time.fixedDeltaTime;
    }

    void Update()
    {

        if (maxTimeScale < timeScale)
        {
            timeScale = maxTimeScale;
        }

        if (timeScale <= 0) timeScale = 1f;

        if (Time.timeScale <= timeScale)
        {
            Time.timeScale += 0.25f;
            Time.fixedDeltaTime = this.fixedDeltaTime * Time.timeScale;
        }
        if (Time.timeScale > timeScale)
        {
            Time.timeScale -= 0.25f;
            Time.fixedDeltaTime = this.fixedDeltaTime * Time.timeScale;
        }
        //Debug.Log(Time.timeScale + " " + Time.fixedDeltaTime);
    }
}