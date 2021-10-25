using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ButtonUIHandler : MonoBehaviour
{

    public void IncreaseTimeScale()
    {
        TimeScale timeScale = FindObjectOfType<TimeScale>();
        timeScale.timeScale += 0.25f;
    }

    public void DecreaseTimeScale()
    {
        TimeScale timeScale = FindObjectOfType<TimeScale>();
        timeScale.timeScale -= 0.25f;
    }

    public void ChangeCamera()
    {
        GameObject cameraManager = GameObject.Find("CameraManager");
        CameraFollow followCar = cameraManager.GetComponent<CameraFollow>();

        GameObject manageUIGO = GameObject.Find("ManageUI");
        ManageUI manageUI = cameraManager.GetComponent<ManageUI>();

        if (followCar.currentCameraIndex + 1 >= followCar.cameras.Length)
        {
            followCar.SwitchCamera(0);

        }
        else
        {
            followCar.SwitchCamera(followCar.currentCameraIndex + 1);
        }
        
    }

}
