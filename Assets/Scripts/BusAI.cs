using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

public class BusAI : MonoBehaviour
{
    public Node startWaypoint;
    public Node endWaypoint;

    public List<Node> carPath;

    //public Transform path;
    public float maxSteerAngle = 45f;
    public float turnSpeed = 5f;
    public WheelCollider wheelFL;
    public WheelCollider wheelFR;
    public WheelCollider wheelRL;
    public WheelCollider wheelRR;
    public float maxMotorTorque = 90f;
    public float maxBrakeTorque = 10f;
    public float currentSpeed;
    public float maxSpeed = 10f;
    public Vector3 centerOfMass;
    public bool isBraking = false;
/*    public Texture2D textureNormal;
    public Texture2D textureBraking;
    public Renderer carRenderer;*/

    [Header("Sensors")]
    public float sensorLength = 1.7f;
    public float sensorFrontLength = 1.2f;
    public Vector3 frontSensorPosition = new Vector3(0f, 0.2f, 0.5f);
    public float frontSideSensorPosition = 0.15f;
    public float frontSensorAngle = 27f;

    private List<Transform> nodes;
    public int currectNode = 0;
    private bool avoiding = false;
    private bool avoidingR = false;
    private bool avoidingL = false;
    private bool avoidingI = false;
    private bool Stop = false;
    private float targetSteerAngle = 0;
    private bool busStop = false;

    public bool stopBuses;

    public IntersectionVehicle intersectionData;

    private void Start()
    {
        intersectionData = this.GetComponentInParent<IntersectionVehicle>();
        intersectionData.intersectionStop = false;
        intersectionData.isInsideIntersection = false;
        intersectionData.intersectionEnterId = -1;
        intersectionData.intersectionDirection = 1;      //init to straight (see IntersectionTrigger.cs for turning direction definitions

        GetComponent<Rigidbody>().centerOfMass = centerOfMass;

        nodes = new List<Transform>();

        Path path = new Path();

        carPath = path.findShortestPath(startWaypoint.transform, endWaypoint.transform);


        for (int i = 0; i < carPath.Count; i++)
        {
            nodes.Add(carPath[i].transform);
        }

    }

    private void FixedUpdate()
    {
        StartCoroutine(ArriveDestination());
        Sensors();
        ApplySteer();
        Drive();
        CheckWaypointDistance();
        Braking();
        LerpToSteerAngle();
    }

    private void Sensors()
    {
        RaycastHit hit;
        Vector3 sensorStartPos = transform.position;
        sensorStartPos += transform.forward * frontSensorPosition.z;
        sensorStartPos += transform.up * frontSensorPosition.y;
        float avoidMultiplier = 0;

        avoiding = false;
        avoidingR = false;
        avoidingL = false;
        avoidingI = false;

        if (currectNode < nodes.Count) // Disable sensors during the intersections
        {
            Street s = nodes[currectNode].GetComponentInParent<Street>();
            if (s.isSimpleIntersection)
            {
                sensorFrontLength = 0f;
            }
            else
            {
                sensorFrontLength = 1.2f;
            }
        }

        //front right sensor
        sensorStartPos += transform.right * frontSideSensorPosition;
        if (Physics.Raycast(sensorStartPos, transform.forward, out hit, sensorFrontLength, -1, QueryTriggerInteraction.Ignore))
        {
            if (!hit.collider.CompareTag("Terrain"))
            {
                Debug.DrawLine(sensorStartPos, hit.point);
                avoiding = true;
                avoidingR = true;
                avoidMultiplier -= 0.5f;
            }
        }

        //front right angle sensor
        if (Physics.Raycast(sensorStartPos, Quaternion.AngleAxis(frontSensorAngle, transform.up) * transform.forward, out hit, sensorLength, -1, QueryTriggerInteraction.Ignore))
        {
            if (!hit.collider.CompareTag("Terrain"))
            {
                Debug.DrawLine(sensorStartPos, hit.point);
                avoiding = true;
                avoidingR = true;
                avoidMultiplier -= 0.5f;
            }
        }

        //front left sensor
        sensorStartPos -= transform.right * frontSideSensorPosition * 2;
        if (Physics.Raycast(sensorStartPos, transform.forward, out hit, sensorFrontLength, -1, QueryTriggerInteraction.Ignore))
        {
            if (!hit.collider.CompareTag("Terrain"))
            {
                Debug.DrawLine(sensorStartPos, hit.point);
                avoiding = true;
                avoidingL = true;
                avoidMultiplier += 0.5f;
            }
        }

        //front left angle sensor
        if (Physics.Raycast(sensorStartPos, Quaternion.AngleAxis(-frontSensorAngle, transform.up) * transform.forward, out hit, sensorLength, -1, QueryTriggerInteraction.Ignore))
        {
            if (!hit.collider.CompareTag("Terrain"))
            {
                Debug.DrawLine(sensorStartPos, hit.point);
                avoiding = true;
                avoidingL = true;
                avoidMultiplier += 0.5f;
            }
        }

        //front center sensor
        if (Physics.Raycast(sensorStartPos, transform.forward, out hit, sensorFrontLength, -1, QueryTriggerInteraction.Ignore))
        {
            Debug.DrawLine(sensorStartPos, hit.point);
            avoiding = true;
            avoidingI = true;
            if (hit.normal.x < 0)
            {
                avoidMultiplier += -1f;
            }
            else
            {
                avoidMultiplier += 1f;
            }

        }
                

        if (avoidingI || intersectionData.intersectionStop)
        {
            Stop = true;
            isBraking = true;
            maxSpeed = 0f;
            currentSpeed = 0f;
            wheelFL.radius = 0.01f;
            wheelFR.radius = 0.01f;
            wheelRL.radius = 0.01f;
            wheelRR.radius = 0.01f;
        }
        else
        {
            Stop = false;
            isBraking = false;
            maxSpeed = 10f;
            wheelFL.radius = 0.12f;
            wheelFR.radius = 0.12f;
            wheelRL.radius = 0.12f;
            wheelRR.radius = 0.12f;
        }

        if (avoiding)
        {
            targetSteerAngle = maxSteerAngle * avoidMultiplier;
        }

    }

    private void ApplySteer()
    {
        if (avoiding || Stop || intersectionData.intersectionStop) return;
        Vector3 relativeVector = transform.InverseTransformPoint(nodes[currectNode].position);
        float newSteer = (relativeVector.x / relativeVector.magnitude) * maxSteerAngle;
        targetSteerAngle = newSteer;
    }

    private void Drive()
    {
        if (Stop || intersectionData.intersectionStop) return;
        currentSpeed = 2 * Mathf.PI * wheelFL.radius * wheelFL.rpm * 60 / 1000;
        currentSpeed = currentSpeed * 2;
        if (currentSpeed < maxSpeed && !isBraking)
        {
            wheelFL.motorTorque = maxMotorTorque;
            wheelFR.motorTorque = maxMotorTorque;
        }
        else
        {
            wheelFL.motorTorque = 0;
            wheelFR.motorTorque = 0;
        }
    }

    private void CheckWaypointDistance()
    {
        if (Vector3.Distance(transform.position, nodes[currectNode].position) < 1f)
        {
            if (currectNode == nodes.Count - 1)
            {
                currectNode = 0;
            }
            else
            {
                currectNode++;
            }
        }
    }

    private void Braking()
    {
        if (isBraking)
        {
            //carRenderer.material.mainTexture = textureBraking;
            wheelRL.brakeTorque = maxBrakeTorque;
            wheelRR.brakeTorque = maxBrakeTorque;
        }
        else
        {
            //carRenderer.material.mainTexture = textureNormal;
            wheelRL.brakeTorque = 0;
            wheelRR.brakeTorque = 0;
        }
    }
    private void LerpToSteerAngle()
    {
        if (Stop)
        {
            wheelFL.steerAngle = 0f; wheelFR.steerAngle = 0f; return;
        }
        else
        {
            wheelFL.steerAngle = Mathf.Lerp(wheelFL.steerAngle, targetSteerAngle, Time.deltaTime * turnSpeed);
            wheelFR.steerAngle = Mathf.Lerp(wheelFR.steerAngle, targetSteerAngle, Time.deltaTime * turnSpeed);
            //Debug.Log(wheelFL.steerAngle);
        }
    }

    IEnumerator ArriveDestination()
    {
        if (!busStop && currectNode == nodes.Count - 1 && Vector3.Distance(transform.position, nodes[nodes.Count - 1].position) < 2f)
        {
            StopObject();
            yield return new WaitForSeconds(10);

            busStop = true;
        } 
        else
        {
            busStop = false;
            stopBuses = false;
            StartObject();
        }

    }

    private void StopObject()
    {
        currentSpeed = 0f;
        Stop = true;
        isBraking = true;
        intersectionData.intersectionStop = true;
    }

    private void StartObject()
    {
        Stop = false;
        isBraking = false;
        intersectionData.intersectionStop = false;
        maxSpeed = 10f;
        wheelFL.radius = 0.12f;
        wheelFR.radius = 0.12f;
        wheelRL.radius = 0.12f;
        wheelRR.radius = 0.12f;
    }
}
