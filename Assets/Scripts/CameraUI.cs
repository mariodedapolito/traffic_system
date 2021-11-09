using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class CameraUI : MonoBehaviour
{

    // This is expressed in "units per second".
    public float speed = 1f;
    public float sensitivityFov = 0.5f;

    private float minFov = 5f;
    private float maxFov = 160f;
    CameraFollow followCar;
    private void Start()
    {
        GameObject cameraManager = GameObject.Find("CameraManager");
        followCar = cameraManager.GetComponent<CameraFollow>();
    }

    void Update()
    {
        
        if (followCar.currentCameraIndex != 0) return;

        var fov = Camera.main.fieldOfView;
        if(fov < 25f) speed = 1f;
        if(fov >= 25f) speed = 2.5f;
        if (fov >= 50f) speed = 4f;
        if (fov >= 100f) speed = 8f;
        if (fov >= 150f) speed = 16f;

        if (Input.GetKey(KeyCode.A))
        {
            transform.position += Vector3.left * speed;
        }
        if (Input.GetKey(KeyCode.D))
        {
            transform.position += Vector3.right * speed;
        }

        if (Input.GetKey(KeyCode.W))
        {
            transform.position += Vector3.forward * speed;
        }
        if (Input.GetKey(KeyCode.S))
        {
            transform.position -= Vector3.forward * speed;
        }

        if (Input.GetKey(KeyCode.Q))
        {
            fov += sensitivityFov;
            fov = Mathf.Clamp(fov, minFov, maxFov);
            Camera.main.fieldOfView = fov;
        }
        if (Input.GetKey(KeyCode.E))
        {
            fov -= sensitivityFov;
            fov = Mathf.Clamp(fov, minFov, maxFov);
            Camera.main.fieldOfView = fov;
        }

    }

}
