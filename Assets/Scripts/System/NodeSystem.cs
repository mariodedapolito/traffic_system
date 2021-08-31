using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;

public class NodeSystem : SystemBase
{
    protected override void OnUpdate()
    {
        Entities
            .ForEach((ref NodePosition node, in Translation translation) =>
            {
                node.position = translation.Value;
            }).Run();
    }
}
