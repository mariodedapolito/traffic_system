using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

[GenerateAuthoringComponent]
public struct NodePosition : IComponentData
{
    public float3 position;
}

public class NodeECS : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
    }
}
