using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

public class ButtonUIHandler : MonoBehaviour
{

    public Text enableGraphicsErrorText;
    public Text graphicsStatusText;

    private EntityManager manager;

    private void Awake()
    {
        manager = World.DefaultGameObjectInjectionWorld.EntityManager;
    }

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
        if (!World.DefaultGameObjectInjectionWorld.GetExistingSystem<Unity.Rendering.RenderMeshSystemV2>().Enabled)
        {
            StopCoroutine(hideEnableGraphicsErrorText(5));
            enableGraphicsErrorText.gameObject.SetActive(true);
            StartCoroutine(hideEnableGraphicsErrorText(5));
            return;
        }

        GameObject cameraManager = GameObject.Find("CameraManager");
        CameraFollow followCar = cameraManager.GetComponent<CameraFollow>();

        GameObject manageUIGO = GameObject.Find("ManageUI");
        ManageUI manageUI = cameraManager.GetComponent<ManageUI>();
        

        if (followCar.currentCameraIndex != 0)
        {
            followCar.SwitchCamera(0);

        }
        else
        {            
            followCar.SwitchCamera(followCar.currentCameraIndex + 1);
        }
        
    }

    public void MaxPerformance()
    {
        World.DefaultGameObjectInjectionWorld.GetExistingSystem<Unity.Rendering.RenderMeshSystemV2>().Enabled = !World.DefaultGameObjectInjectionWorld.GetExistingSystem<Unity.Rendering.RenderMeshSystemV2>().Enabled;
        if (!World.DefaultGameObjectInjectionWorld.GetExistingSystem<Unity.Rendering.RenderMeshSystemV2>().Enabled)
        {
            graphicsStatusText.gameObject.SetActive(true);
        }
        else
        {
            enableGraphicsErrorText.gameObject.SetActive(false);
            graphicsStatusText.gameObject.SetActive(false);
        }
    }

    public void UseEithan()
    {
        GameObject cameraManager = GameObject.Find("CameraManager");
        CameraFollow followCar = cameraManager.GetComponent<CameraFollow>();

        if (followCar.eitan.gameObject.activeInHierarchy) {
            followCar.eitan.SetActive(false);
            followCar.SwitchCamera(0);
        } else
        {
            followCar.eitan.SetActive(true);
            followCar.SwitchCamera(2);
        }
    }

    IEnumerator hideEnableGraphicsErrorText(int seconds)
    {
        yield return new WaitForSeconds(seconds);
        enableGraphicsErrorText.gameObject.SetActive(false);
    }

}
