using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using Unity.Collections;

[UpdateAfter(typeof(CityGenerator))]
public class EndGameSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float time = Time.DeltaTime;
        int nCar = 0;
        int nCarParked = 0;
        bool endGameNow = false;
                
        Entities
            .WithoutBurst()
            .ForEach((ref EndGameComponent endGameComponent) =>
            {
                GameObject cityGenerator = GameObject.Find("CityGenerator");
                endGameComponent.numberCars = cityGenerator.GetComponent<CityGenerator>().numberCarsToSpawn;
                nCar = endGameComponent.numberCars;
                nCarParked = endGameComponent.numberCarsParked;
                endGameNow = endGameComponent.endGame;

                

                endGameComponent.endGame = endGameNow;
                endGameComponent.numberCarsParked = nCarParked;
            }).Run();


        if (!endGameNow) {      
            Entities
              .WithStructuralChanges()
              .ForEach((Entity e, in Car car, in EndGameNeedCount endGameNeedCount) => {
                  nCarParked++;
              
                  if (nCarParked >= nCar && !endGameNow)
                  {
                      endGameNow = true;
                      Debug.Log("END GAME!");
                  }
              
                  EntityManager.RemoveComponent<Car>(e);
                  EntityManager.RemoveComponent<EndGameNeedCount>(e);
              
              }).Run();
        }

        Entities
            .WithoutBurst()
            .ForEach((ref EndGameComponent endGameComponent) =>
            {
                endGameComponent.endGame = endGameNow;
                endGameComponent.numberCarsParked = nCarParked;
            }).Run();
    }
}
