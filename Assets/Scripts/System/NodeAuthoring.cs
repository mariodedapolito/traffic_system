using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

class NodeAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
     public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
       
        dstManager.AddComponentData(entity, new NodeECS
        {
            position = this.transform.position,
        }) ;
    
    }
}