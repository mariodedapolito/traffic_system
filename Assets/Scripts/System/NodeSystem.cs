using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Transforms;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;

[UpdateBeforeAttribute(typeof(CarSystem))]
public class NodesSystem : SystemBase
{

    EndSimulationEntityCommandBufferSystem m_EndSimulationEcbSystem;
    public static NativeHashMap<int, Entity> nodesMap;

    public const int xMultiplier = 100000;

    private EntityQuery query;

    public static int GetNodeHashMapKey(float3 position)
    {
        int xPosition = (int)position.x;
        int zPosition = (int)position.z;
        return xPosition * xMultiplier + zPosition;
    }

    protected override void OnCreate()
    {
        m_EndSimulationEcbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        nodesMap = new NativeHashMap<int, Entity>(0, Allocator.Persistent);
        base.OnCreate();
    }

    protected override void OnDestroy()
    {
        nodesMap.Dispose();
        base.OnDestroy();
    }

    protected override void OnUpdate()
    {
        int numNodes = query.CalculateEntityCount() + nodesMap.Count();
        if (numNodes > nodesMap.Capacity)
        {
            nodesMap.Capacity = numNodes;
        }

        Debug.Log(nodesMap.Count());

        var ecb = m_EndSimulationEcbSystem.CreateCommandBuffer().ToConcurrent();

        NodeComponent nodeCmpToRemove = new NodeComponent() { };

        Entities
                .WithoutBurst()
                .WithStoreEntityQueryInField(ref query)
                .ForEach((Entity entity, int entityInQueryIndex, DynamicBuffer<NextNodesList> nextNodesLists, in NodeComponent nodeComp, in NodeData nodeData, in Translation translation) =>
                {
                    int keyPos1 = GetNodeHashMapKey(translation.Value);
                    nodesMap.Add(keyPos1, entity);
                    ecb.RemoveComponent<NodeComponent>(entityInQueryIndex, entity);

                }).Schedule();

    }

}