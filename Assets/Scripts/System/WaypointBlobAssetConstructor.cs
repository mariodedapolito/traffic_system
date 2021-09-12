using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;

[UpdateInGroup(typeof(GameObjectAfterConversionGroup))]
public class WaypointBlobAssetConstructor : GameObjectConversionSystem {
 
    protected override void OnUpdate() {
   /*       BlobAssetReference<NodeBlobAsset> waypointBlobAssetReference;

        using (BlobBuilder blobBuilder = new BlobBuilder(Allocator.Temp)) {
            ref NodeBlobAsset waypointBlobAsset = ref blobBuilder.ConstructRoot<NodeBlobAsset>();
            BlobBuilderArray<NodeECS> waypointArray = blobBuilder.Allocate(ref waypointBlobAsset.nodesArray , 3);
            EntityQuery nodeEntityQuery = DstEntityManager.CreateEntityQuery(typeof(NodeAuthoring));
           var nodeArray= nodeEntityQuery.ToEntityArray(Allocator.Persistent);

      //   var test=   GetEntityQuery(typeof(NodeECS)).ToComponentArray<NodeAuthoring>();


  
      var descriptionQry = new EntityQueryDesc { All = new ComponentType[] { typeof(NodeECS) } };

            EntityQuery tileQry = GetEntityQuery(descriptionQry);
         
        var _myTiles = tileQry.ToEntityArray(Allocator.Persistent);
  

    waypointArray[0] = new NodeECS { position = new float3(23.86f, 1f, 61.2f) };
            waypointArray[1] = new NodeECS { position = new float3(22.8f, -12.9f, 0.5f) };
            waypointArray[2] = new NodeECS { position = new float3(13.27268f, -11.57699f, 0.5000019f) };

            waypointBlobAssetReference = 
                blobBuilder.CreateBlobAssetReference<NodeBlobAsset>(Allocator.Persistent);
            
            Debug.Log(waypointBlobAssetReference.Value.nodesArray[1].position);
        }

        EntityQuery playerEntityQuery = DstEntityManager.CreateEntityQuery(typeof(VehicleSpeed));
        Entity playerEntity = playerEntityQuery.GetSingletonEntity();

        DstEntityManager.AddComponentData(playerEntity, new NodeECSArrayData
        {
            nodesRef = waypointBlobAssetReference,
            waypointIndex = 0,
        });
    */}

}
