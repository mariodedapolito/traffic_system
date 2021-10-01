using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;

[GenerateAuthoringComponent]
public struct EndGameComponent : IComponentData 
{
    public int numberCars;
    public int numberCarsParked;
    public bool endGame;
}

