using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarAI : MonoBehaviour
{
    public Node startWaypoint;
    public Node endWaypoint;

    public List<Node> carPath;

    //public Transform path;
    public float maxSteerAngle = 50f;
    public float turnSpeed = 6f;
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
    public float sensorLength = 1f;
    public float sensorFrontLength = 1.2f;
    public Vector3 frontSensorPosition = new Vector3(0f, 0.2f, 0.5f);
    public float frontSideSensorPosition = 0.15f;
    public float frontSensorAngle = 27f;

    public List<Transform> nodes;
    public int currectNode = 0;
    private bool avoiding = false;
    private bool avoidingR = false;
    private bool avoidingL = false;
    private bool avoidingI = false;
    private bool avoidingIR = false;
    private bool avoidingIF = false;
    private bool Stop = false;
    private float targetSteerAngle = 0;
    private bool collisionHappen;
    private bool inPath = false;

    private bool isIntersactionF = false;
    private bool isLaneOne = false;
    private bool isCurveOne = false;
    private bool precedence = false;
    private bool precedenceLeft = false;
    private bool isIntersactionStop = false;
    private bool isCarIR = false;
    private bool isCarIL = false;
    private bool isCarR = false;
    private bool isCarL = false;


    private bool isCar = false;

    public IntersectionVehicle intersectionData;
	public bool needParkingSpot;

    private void Start()
    {
        intersectionData = this.GetComponentInParent<IntersectionVehicle>();
        intersectionData.intersectionStop = false;
        intersectionData.isInsideIntersection = false;
        intersectionData.intersectionEnterId = -1;
        intersectionData.intersectionDirection = 1;      //init to straight (see IntersectionTrigger.cs for turning direction definitions)

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
        avoidingIR = false;
        avoidingIF = false;
        precedence = false;
        precedenceLeft = false;
        isCarIR = false;
        isCarIL = false;
        isCarR = false;
        isCarL = false;


        if (currectNode < nodes.Count) // Disable sensors during the intersections
        {   
            Street s = nodes[currectNode].GetComponentInParent<Street>();
            if(s.isSemaphoreIntersection)
            {
            	sensorFrontLength = 0.8f;
                sensorLength = 0.8f;
            }
            else
            {
                sensorLength = 0.8f;
                sensorFrontLength = 1.2f;
            }

            if (s.isSimpleIntersection)
            {
                sensorFrontLength = 0.8f;
                sensorLength = 0.8f;
                isIntersactionF = true;
            }
            else
            {
                sensorLength = 0.8f;
                sensorFrontLength = 1.2f;
                isIntersactionF = false;
            }

            if(s.isCurve)
            {
                sensorFrontLength = 0.8f;
                sensorLength = 0.8f;
            }
        }

        //front right sensor
        sensorStartPos += transform.right * frontSideSensorPosition;
        if (Physics.Raycast(sensorStartPos, transform.forward, out hit, sensorFrontLength,-1, QueryTriggerInteraction.Ignore))
        {
            if (hit.rigidbody != null && (hit.rigidbody.CompareTag("Car") || hit.rigidbody.CompareTag("Bus")))
            {
                isCarIR = true;
            }

            if (hit.rigidbody != null && (hit.rigidbody.CompareTag("Car") || hit.rigidbody.CompareTag("Bus")) && isIntersactionF )
            {
                precedence = true;
            }
            else
            {
                if (!hit.collider.CompareTag("Terrain"))
                {
                    precedence = false;
                    Debug.DrawLine(sensorStartPos, hit.point);
                    avoiding = true;
                    avoidingI = true;
                    avoidingIR = true;
                    avoidMultiplier -= 0.5f;
                }
            }
        }

        //front right angle sensor
        if (Physics.Raycast(sensorStartPos, Quaternion.AngleAxis(frontSensorAngle, transform.up) * transform.forward, out hit, sensorLength, -1, QueryTriggerInteraction.Ignore))
        {
            if (hit.rigidbody != null && (hit.rigidbody.CompareTag("Car") || hit.rigidbody.CompareTag("Bus")))
            {
                isCarR = true;
            }
         
           
            if (hit.rigidbody != null && (hit.rigidbody.CompareTag("Car") || hit.rigidbody.CompareTag("Bus")) && isIntersactionF)
            {
                precedence = true;
            }
            else
            { 
                if (!hit.collider.CompareTag("Terrain"))
                {
                    precedence = false;
                    Debug.DrawLine(sensorStartPos, hit.point);
                    avoiding = true;
                    avoidingR = true;
                    avoidMultiplier -= 1f;
                }                
            }
        }

        //front left sensor
        sensorStartPos -= transform.right * frontSideSensorPosition * 2;
        if (Physics.Raycast(sensorStartPos, transform.forward, out hit, sensorFrontLength, -1, QueryTriggerInteraction.Ignore))
        {

            if (hit.rigidbody != null && (hit.rigidbody.CompareTag("Car") || hit.rigidbody.CompareTag("Bus")))
            {
                isCarIL = true;
            }


            if (!hit.collider.CompareTag("Terrain"))
            {
                Debug.DrawLine(sensorStartPos, hit.point);
                avoiding = true;
                avoidingI = true;
                avoidingIF = true;
                avoidMultiplier += 0.5f;
            }
            
        }

        //front left angle sensor
        if (Physics.Raycast(sensorStartPos, Quaternion.AngleAxis(-frontSensorAngle, transform.up) * transform.forward, out hit, sensorLength, -1, QueryTriggerInteraction.Ignore))
        {

            if (hit.rigidbody != null && (hit.rigidbody.CompareTag("Car") || hit.rigidbody.CompareTag("Bus")))
            {
                isCarL = true;
            }

            if (hit.rigidbody != null && (hit.rigidbody.CompareTag("Car") || hit.rigidbody.CompareTag("Bus")) && isIntersactionF)
            {
                precedenceLeft = true;
            }
            else
            {
                precedenceLeft = false;
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

        if(isCarIL || isCarIR || isCarL || isCarR)
        {
            isCar = true;
        }
        else
        {
            isCar = false;
        }


        if (currectNode < nodes.Count) // Disable sensors during the intersections
        {
            Street s = nodes[currectNode].GetComponentInParent<Street>();
            if (/*s.numberLanes == 1 && */isCar /*&& !s.isSemaphoreIntersection*/) //dont surpass in one lane
            {
                isLaneOne = true;                
            }
            else
            {
                isLaneOne = false;
            }

            if(s.isSemaphoreIntersection){
            	precedence = false;
            } 
			else{
				
			}
			
            if(s.isSimpleIntersection && (avoidingI || precedence || precedenceLeft)) //dont surpass in intersection
            {
                isIntersactionStop = true;
            }
            else
            {
                isIntersactionStop = false;
            }

            if(s.isCurve && isCar ) //dont surpass in Curve
            {
                
                isCurveOne = true;

            }
            else
            {
                isCurveOne = false;
            }
        }

        if (intersectionData.intersectionStop || isIntersactionStop || isLaneOne || isCurveOne || precedence)
        {
            Stop = true;
            isBraking = true;
            maxSpeed = 0f;
            currentSpeed = 0f;

            wheelFL.mass = 0f;
            wheelFR.mass = 0f;
            wheelRL.mass = 0f;
            wheelRR.mass = 0f;

            wheelFL.radius = 0.089f;
            wheelFR.radius = 0.089f;
            wheelRL.radius = 0.089f;
            wheelRR.radius = 0.089f;

        }
        else
        {

            wheelFL.mass = 20f;
            wheelFR.mass = 20f;
            wheelRL.mass = 20f;
            wheelRR.mass = 20f;

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
        if (currectNode >= nodes.Count ) newPath();
		try{
        Vector3 relativeVector = transform.InverseTransformPoint(nodes[currectNode].position);
		float newSteer = (relativeVector.x / relativeVector.magnitude) * maxSteerAngle;
        targetSteerAngle = newSteer;
		}
		catch(System.Exception e){
			Debug.Log(transform.position);
			throw new System.Exception("hey");
		}
    }

    private void Drive()
    {
        //if (Stop || intersectionData.intersectionStop) return;
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
        if (this.inPath) return;
        
        if (nodes.Count > 0)
        {   
            this.inPath = true;
            if (Vector3.Distance(transform.position, nodes[currectNode].position) < 1f)
            {
				if(currectNode == nodes.Count - 2 && nodes[currectNode + 1].GetComponent<Node>().isParkingGateway)
                {
                    needParkingSpot = true;
                }
                if (currectNode == nodes.Count - 1)
                {
                    
                    // end == parking Waypoint
                    /*
                    {
                        - trasform to position waypoint into parking
                        - List of free parking waypoint
                        - pathfinding with min
                        - function sleep mode (t = random)
                        - check exti (queue)
                        - return to enter
                        - new path
                    }
                    */

                    GameObject[] waypointsNew = GameObject.FindGameObjectsWithTag("CarWaypoint");
                    List<Node> nodesNew = new List<Node>();
                    Node lastWaypoint = this.endWaypoint;
                    foreach (GameObject w in waypointsNew)
                    {
                        if (w.GetComponent<Node>() != null && !w.GetComponent<Node>().isParkingSpot)
                            nodesNew.Add(w.GetComponent<Node>());
                    }

                    this.startWaypoint = nodes[currectNode].GetComponent<Node>();

                    int randomSrcNode = (int)UnityEngine.Random.Range(0, nodesNew.Count - 1);

                    Street s = nodesNew[randomSrcNode].GetComponentInParent<Street>();

                    while (!s.hasBusStop && s.isSemaphoreIntersection && s.isSimpleIntersection)
                    {
                        randomSrcNode = (int)UnityEngine.Random.Range(0, nodesNew.Count - 1);
                        s = nodesNew[randomSrcNode].GetComponentInParent<Street>();
                    }
                    this.endWaypoint = nodesNew[randomSrcNode];
                    currectNode = 1;

                    Path path = new Path(); // problem with MonoBehaviour
                    this.nodes.Clear();
                    //this.carPath.Clear();

                    this.carPath = path.findShortestPath(startWaypoint.transform, endWaypoint.transform);


                    for (int i = 0; i < this.carPath.Count; i++)
                    {
                        this.nodes.Add(carPath[i].transform);
                    }
                    
                }
                else
                {
                    currectNode++;
                } 
                
            }
            this.inPath = false;
        }
        else
        {
            newPath();
        }
       
    }

    private void newPath()
    {
        if (this.inPath) return;
        this.inPath = true;
        GameObject[] waypointsNew = GameObject.FindGameObjectsWithTag("CarWaypoint");
        List<Node> nodesNew = new List<Node>();
        float min = 1f;
        Node lastWaypoint = this.endWaypoint;

        //Destroy(this);

        
        foreach (GameObject w in waypointsNew)
        {
            if (w.GetComponent<Node>() != null && !w.GetComponent<Node>().isParkingSpot)
            {
                Street sStart = w.GetComponent<Node>().GetComponentInParent<Street>();
                if (!sStart.hasBusStop && !sStart.isSemaphoreIntersection && !sStart.isSimpleIntersection)
                {
                    nodesNew.Add(w.GetComponent<Node>());
                }
            }
        }

        this.startWaypoint = lastWaypoint;

        int randomSrcNode = (int)UnityEngine.Random.Range(0, nodesNew.Count - 1);

        Street s = nodesNew[randomSrcNode].GetComponentInParent<Street>();
        

        this.endWaypoint = nodesNew[randomSrcNode];
        currectNode = 1;

        Path path = new Path(); // problem with MonoBehaviour
        this.nodes.Clear();
        //this.carPath.Clear();

        this.carPath = path.findShortestPath(startWaypoint.transform, endWaypoint.transform);


        for (int i = 0; i < this.carPath.Count; i++)
        {
            this.nodes.Add(carPath[i].transform);
        }
        this.inPath = false;
        
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

    public void OnTriggerExit(Collider other)
    {
        if (other.gameObject.GetComponent<Node>() != null && collisionHappen)
        {
            Node n = other.gameObject.GetComponent<Node>();
            if (n.isCarSpawn)
            {
                n.numberCars--;
                collisionHappen = false;
                n.isOccupied = false;
            }
        }
    }

    public void OnTriggerEnter(Collider other)
    {
        //this.numberCars++;
        if (other.gameObject.GetComponent<Node>() != null && !collisionHappen)
        {
            Node n = other.gameObject.GetComponent<Node>();
            if (n.isCarSpawn)
            {
                n.numberCars++;
                collisionHappen = true;
                n.isOccupied = true;
            }
        }
    }
}
