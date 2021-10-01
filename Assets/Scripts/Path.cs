using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Path : MonoBehaviour
{
    public Color lineColor;

    private GameObject[] waypoints;
    protected List<Node> nodes;
 

    /*void OnDrawGizmosSelected()
    {
        Gizmos.color = lineColor;

        Transform[] pathTransforms = GetComponentsInChildren<Transform>();
        nodes = new List<Transform>();

        for (int i = 0; i < pathTransforms.Length; i++)
        {
            if (pathTransforms[i] != transform)
            {
                nodes.Add(pathTransforms[i]);
            }
        }

        for (int i = 0; i < nodes.Count; i++)
        {
            Vector3 currentNode = nodes[i].position;
            Vector3 previousNode = Vector3.zero;

            if (i > 0)
            {
                previousNode = nodes[i - 1].position;
            }
            else if (i == 0 && nodes.Count > 1)
            {
                previousNode = nodes[nodes.Count - 1].position;
            }
            Debug.Log(currentNode);
            Gizmos.DrawLine(previousNode, currentNode);
            Gizmos.DrawWireSphere(currentNode, 0.3f);
        }
    }*/

    public List<Node> findShortestPath(Transform start, Transform end)
    {

        waypoints = GameObject.FindGameObjectsWithTag("CarWaypoint");

        nodes = new List<Node>();

        foreach (GameObject w in waypoints)
        {
            if(w.GetComponent<Node>()!= null)
                nodes.Add(w.GetComponent<Node>());
        }

        List<Node> result = new List<Node>();
        List<Node> node = AStarSearch(start.GetComponent<Node>(), end.GetComponent<Node>());

        return node;
    }

    public static List<Node> AStarSearch(Node startPosition, Node endPosition)
    {
        List<Node> path = new List<Node>();

        Node start = startPosition;
        Node end = endPosition;

        List<Node> positionsTocheck = new List<Node>();
        Dictionary<Node, float> costDictionary = new Dictionary<Node, float>();
        Dictionary<Node, float> priorityDictionary = new Dictionary<Node, float>();
        Dictionary<Node, Node> parentsDictionary = new Dictionary<Node, Node>();

        positionsTocheck.Add(start);
        priorityDictionary.Add(start, 0);
        costDictionary.Add(start, 0);
        parentsDictionary.Add(start, null);

        while (positionsTocheck.Count > 0)
        {
            Node current = GetClosestNode(positionsTocheck, priorityDictionary);
            positionsTocheck.Remove(current);
            if (current.Equals(end))
            {
                path = GeneratePath(parentsDictionary, current);
                return path;
            }

            foreach (Node neighbour in current.nextNodes)
            {
                float newCost = costDictionary[current] + 1;
                if (!costDictionary.ContainsKey(neighbour) || newCost < costDictionary[neighbour])
                {
                    costDictionary[neighbour] = newCost;

                    float priority = newCost + ManhattanDiscance(end, neighbour);
                    positionsTocheck.Add(neighbour);
                    priorityDictionary[neighbour] = priority;

                    parentsDictionary[neighbour] = current;
                }
            }
        }
        return path;
    }
    public static List<Node> GeneratePath(Dictionary<Node, Node> parentMap, Node endState)
    {
        List<Node> path = new List<Node>();
        Node parent = endState;
        while (parent != null && parentMap.ContainsKey(parent))
        {
            path.Add(parent);
            parent = parentMap[parent];
        }
        path.Reverse();
        return path;
    }

    private static Node GetClosestNode(List<Node> list, Dictionary<Node, float> distanceMap)
    {
        Node candidate = list[0];
        foreach (Node vertex in list)
        {
            if (distanceMap[vertex] < distanceMap[candidate])
            {
                candidate = vertex;
            }
        }
        return candidate;
    }
    private static float ManhattanDiscance(Node endPos, Node position)
    {
        return System.Math.Abs(endPos.transform.position.x - position.transform.position.x) + System.Math.Abs(endPos.transform.position.z - position.transform.position.z);
    }

}
