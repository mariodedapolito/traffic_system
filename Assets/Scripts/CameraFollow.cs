using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public class CameraFollow : MonoBehaviour
{
    public List<Entity> cars;
    public float3 offset;
    public float3 rotarion;

    public Camera[] cameras;

    private EntityManager manager;
    public int currentCameraIndex;
    private Entity car;
    public GameObject manageUIGameObject;
    private ManageUI manageUI;

    public GameObject eitan;

    private void Start()
    {
        cars = new List<Entity>();

        currentCameraIndex = 0;

        //Turn all cameras off, except the first default one
        for (int i = 1; i < cameras.Length; i++)
        {
            cameras[i].gameObject.SetActive(false);
        }

        if (cameras.Length > 0)
        {
            cameras[0].gameObject.SetActive(true);
        }

        manageUI = manageUIGameObject.GetComponent<ManageUI>();

        eitan = GameObject.Find("Eitan");
        eitan.SetActive(false);
    }

    private void Awake()
    {
        manager = World.DefaultGameObjectInjectionWorld.EntityManager;
    }


    private void LateUpdate()
    {
        if (cars.Count == 0 || currentCameraIndex == 0 || currentCameraIndex > 1) { return; }

        Translation carPos = manager.GetComponentData<Translation>(car);
        LocalToWorld carPosLocalToWorld = manager.GetComponentData<LocalToWorld>(car);
        Rotation carRot = manager.GetComponentData<Rotation>(car);
        cameras[currentCameraIndex].transform.position = carPos.Value + offset * carPosLocalToWorld.Forward + new float3(0, offset.y, 0);
        //cameras[currentCameraIndex].transform.forward = carPosLocalToWorld.Forward;

        cameras[currentCameraIndex].transform.rotation = carRot.Value;

        manageUI.UpdateCameraUI();
    }

    public void SwitchCamera(int index)
    {
        if (index >= cameras.Length) Debug.LogError("The current camera index does not exist.");

        cameras[currentCameraIndex].gameObject.SetActive(false);

        currentCameraIndex = index;

        cameras[currentCameraIndex].gameObject.SetActive(true);
        if (currentCameraIndex == 1 && cars.Count!=0)
        {
            do
            {
                car = cars[UnityEngine.Random.Range(0, cars.Count - 1)];
            } while (manager.GetComponentData<VehicleNavigation>(car).isParked);
        }
    }
}
