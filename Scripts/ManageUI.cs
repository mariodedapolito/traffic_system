using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

public class ManageUI : MonoBehaviour
{
    public Text numberOfCarsText;
    public Text numberOfBusesText;
    public Text timeText;
    public Text timeScaleText;
    public Text numberOfCarsParkedText;
    public Text numberOfCarsOnStreetText;
    public Text averageFPSText;

    private int _numberOfCars;
    private int _numberOfBuses;
    private double _time;
    private float _timeScale;
    private int _numberOfCarsParked;
    private int _numberOfCarsOnStreet;
    private float _averageFPS;

    private float deltaTime;

    ManageUI manageUI;

    private EntityManager manager;

    public int numberOfCars { get { return _numberOfCars; } set { _numberOfCars = value; numberOfCarsText.text = numberOfCars.ToString(); } }
    public int numberOfBuses { get { return _numberOfBuses; } set { _numberOfBuses = value; numberOfBusesText.text = numberOfBuses.ToString(); } }
    public double time { get { return _time; } set { _time = value; var ts = TimeSpan.FromSeconds(time); timeText.text = string.Format("{0:00}:{1:00}", ts.Minutes, ts.Seconds); } }
    public float timeScale { get { return _timeScale; } set { _timeScale = value; timeScaleText.text = string.Format("x{0}", timeScale); } }
    public int numberOfCarsParked { get { return _numberOfCarsParked; } set { _numberOfCarsParked = value; numberOfCarsParkedText.text = numberOfCarsParked.ToString(); } }
    public int numberOfCarsOnStreet { get { return _numberOfCarsOnStreet; } set { _numberOfCarsOnStreet = value; numberOfCarsOnStreetText.text = numberOfCarsOnStreet.ToString(); } }
    public float averageFPS { get { return _averageFPS; } set { _averageFPS = value; averageFPSText.text = string.Format("{0}", Mathf.Ceil(averageFPS).ToString()); } }

    private void Start()
    {
        manageUI = GameObject.Find("ManageUI").GetComponent<ManageUI>();
    }

    private void Awake()
    {
        manager = World.DefaultGameObjectInjectionWorld.EntityManager;
    }

    private void Update()
    {
        time = Time.realtimeSinceStartup;
        timeScale = Time.timeScale;

        deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
        averageFPS = Time.frameCount / Time.time;//1.0f / deltaTime;
       
        numberOfCarsParked = CarsPositionSystem.numCarsArray[1];

        numberOfCarsOnStreet = CarsPositionSystem.numCarsArray[0];

    }

    public void UpdateCameraUI()
    {
        this.numberOfCars = manageUI.numberOfCars;
        this.numberOfBuses = manageUI.numberOfBuses;
        this.time = manageUI.time;
        this.timeScale = manageUI.timeScale;
        this.numberOfCarsParked = manageUI.numberOfCarsParked;
        this.numberOfCarsOnStreet = manageUI.numberOfCarsOnStreet;
        this.averageFPS = manageUI.averageFPS;

    }
}
