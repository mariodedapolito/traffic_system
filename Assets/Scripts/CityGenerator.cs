using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class MapTile
{
    public GameObject prefabReference;
    public GameObject instantiatedStreet;
    public int prefabType;
    public int rotation;

    public MapTile(int prefabType, int rotation)
    {
        this.prefabType = prefabType;
        this.rotation = rotation;
        this.prefabReference = null;
    }
}

[System.Serializable]
public class JsonData
{
    public int numberHorizontalStreets;
    public int minNumberVerticalStreets;
    public int maxNumberVerticalStreets;
    public int minDistanceBetweenHorizontalStreets;
    public int maxDistanceBetweenHorizontalStreets;
    public int numberCarsToSpawn;
    public bool onlySimpleIntersections;
    public bool onlySemaphoreIntersections;
    public bool only1LaneStreets;
    public bool only2LaneStreets;

    public int numberCarsToSpawnOnFrame;
    public float profondity;

    public int distanceBetweenVerticalStreets = 5;   //2 prefabs for bus stops + 1 prefab (optional) for lane adapter + 2 reserved prefabs
    public int distanceBetweenHorizontalStreets = 5;
}

public class CityGenerator : MonoBehaviour
{

    public int numberHorizontalStreets;
    public int minNumberVerticalStreets;
    public int maxNumberVerticalStreets;
    public int minDistanceBetweenHorizontalStreets;
    public int maxDistanceBetweenHorizontalStreets;
    public int numberCarsToSpawn;
    public int numberBusesToSpawn;
    public bool onlySimpleIntersections;
    public bool onlySemaphoreIntersections;
    public bool only1LaneStreets;
    public bool only2LaneStreets;

    public int numberCarsToSpawnOnFrame;
    public float profondity;

    public TextAsset jsonFile;

    public GameObject cityPlane;
    public List<GameObject> carPrefab;
    public GameObject busPrefab;

    public GameObject straightStreet1Lane;
    //public GameObject straightGameObject1LaneShort;
    public GameObject straightStreet2Lane;
    //public GameObject straightGameObject2LaneShort;
    public GameObject busStop1Lane;
    public GameObject busStop2Lane;
    public GameObject laneAdaptor;
    public GameObject deadend1Lane;
    public GameObject deadend2Lane;
    public GameObject curve1Lane;
    public GameObject curve2Lane;
    public GameObject intersection4Way1Lane;
    public GameObject intersection4Way1LaneSemaphore;
    public GameObject intersection4Way2Lane;
    public GameObject intersection4Way2LaneSemaphore;
    public GameObject intersection4Way1Lane2Lane;
    public GameObject intersection4Way1Lane2LaneSemaphore;
    public GameObject intersection3Way1Lane;
    public GameObject intersection3Way1LaneSemaphore;
    public GameObject intersection3Way2Lane;
    public GameObject intersection3Way2LaneSemaphore;
    public GameObject intersection3Way1Lane2Lane;
    public GameObject intersection3Way1Lane2LaneSemaphore;
    public GameObject intersection3Way2Lane1Lane;
    public GameObject intersection3Way2Lane1LaneSemaphore;

    public bool generateBuildings;
    public List<GameObject> buildingPrefabs;


    private const int distanceBetweenVerticalStreets = 7;   //2 prefabs for bus stops + 1 prefab (optional) for lane adapter + 2 reserved prefabs
    private const int distanceBetweenHorizontalStreets = 7;

    public MapTile[,] cityMap;
    public int cityWidth;
    public int cityLength;

    private const int STRAIGHT_1LANE = 1;
    private const int STRAIGHT_2LANE = 2;
    private const int CURVE_1LANE = 3;
    private const int CURVE_2LANE = 4;
    private const int BUS_STOP_1LANE = 5;
    private const int BUS_STOP_2LANE = 6;

    private const int INTERSECTION_4WAY_1LANE = 4110;
    private const int INTERSECTION_4WAY_1LANE_SEMAPHORE = 4111;
    private const int INTERSECTION_4WAY_2LANE = 4220;
    private const int INTERSECTION_4WAY_2LANE_SEMAPHORE = 4221;
    private const int INTERSECTION_3WAY_1LANE = 3110;
    private const int INTERSECTION_3WAY_1LANE_SEMAPHORE = 3111;
    private const int INTERSECTION_3WAY_2LANE = 3220;
    private const int INTERSECTION_3WAY_2LANE_SEMAPHORE = 3221;
    private const int INTERSECTION_4WAY_1LANE_2LANE = 4120;
    private const int INTERSECTION_4WAY_1LANE_2LANE_SEMAPHORE = 4121;
    private const int INTERSECTION_3WAY_1LANE_2LANE = 3120;
    private const int INTERSECTION_3WAY_1LANE_2LANE_SEMAPHORE = 3121;
    private const int INTERSECTION_3WAY_2LANE_1LANE = 3210;
    private const int INTERSECTION_3WAY_2LANE_1LANE_SEMAPHORE = 3211;

    private const int BUILDINGS = -1;

    private const int LANE_ADAPTOR = 21;

    private const int HORIZONTAL_STREET = 0;
    private const int VERTICAL_STREET = 1;

    private const int HORIZONTAL_BUS_STOP_UP = 0;
    private const int HORIZONTAL_BUS_STOP_DOWN = 1;
    private const int VERTICAL_BUS_STOP_LEFT = 2;
    private const int VERTICAL_BUS_STOP_RIGHT = 3;

    private const int TOP_STREET = 0;
    private const int BOTTOM_STREET = 1;
    private const int MIDDLE_STREET = 2;

    private const int TOP_LEFT = 0;
    private const int TOP_RIGHT = 1;
    private const int BOTTOM_LEFT = 2;
    private const int BOTTOM_RIGHT = 3;

    private const int INTERSECTION_ROTATION_BOTTOM = 0;
    private const int INTERSECTION_ROTATION_LEFT = 90;
    private const int INTERSECTION_ROTATION_TOP = 180;
    private const int INTERSECTION_ROTATION_RIGHT = 270;
    private const int STRAIGHT_STREET_ROTATION_HORIZONTAL = 0;
    private const int STRAIGHT_STREET_ROTATION_VERTICAL = 90;
    private const int CURVE_ROTATION_TOP_LEFT = 0;
    private const int CURVE_ROTATION_TOP_RIGHT = 90;
    private const int CURVE_ROTATION_BOTTOM_RIGHT = 180;
    private const int CURVE_ROTATION_BOTTOM_LEFT = 270;
    private const int LANE_ADAPTOR_ROTATION_2LANE_UP = 90;
    private const int LANE_ADAPTOR_ROTATION_2LANE_DOWN = 270;
    private const int BUS_STOP_ROTATION_UP = 180;
    private const int BUS_STOP_ROTATION_DOWN = 0;
    private const int BUS_STOP_ROTATION_LEFT = 270;
    private const int BUS_STOP_ROTATION_RIGHT = 90;


    private int[] horizontalSpaces;
    private int[] lanesHorizontalStreets;
    private int[] numberVerticalStreets;
    private int[,] lanesVerticalStreets;

    private int intersectionId = 0;

    private CarSpawner carSpawner;
    public List<Node> cityParkingNodes = new List<Node>();
    public NativeList<float3> cityParkingNodesPositions;
    public Dictionary<int, NativeList<float3>> cityParkings;
    public List<Node> citySpawnNodes = new List<Node>();
    public List<Node> cityBusStopsSpawn = new List<Node>();
    public List<Node> cityBusStopsDst = new List<Node>();
    public List<Node> cityNodes = new List<Node>();

    public Dictionary<Vector3, Node> nodesMap;

    private BusSpawner busSpawner;

    private bool spawn;
    private int carsNeedToSpawn;

    private NativeMultiHashMap<float3, float3> nodesCity;
    private NativeArray<float3> waypoitnsCity;

    void GatValueFromJson()
    {
        JsonData jsonData = JsonUtility.FromJson<JsonData>(jsonFile.text);

        numberHorizontalStreets = jsonData.numberHorizontalStreets;
        minNumberVerticalStreets = jsonData.minNumberVerticalStreets;
        maxNumberVerticalStreets = jsonData.maxNumberVerticalStreets;
        minDistanceBetweenHorizontalStreets = jsonData.minDistanceBetweenHorizontalStreets;
        maxDistanceBetweenHorizontalStreets = jsonData.maxDistanceBetweenHorizontalStreets;
        numberCarsToSpawn = jsonData.numberCarsToSpawn;
        onlySimpleIntersections = jsonData.onlySimpleIntersections;
        onlySemaphoreIntersections = jsonData.onlySemaphoreIntersections;
        only1LaneStreets = jsonData.only1LaneStreets;
        only2LaneStreets = jsonData.only2LaneStreets;
        numberCarsToSpawnOnFrame = jsonData.numberCarsToSpawnOnFrame;
        profondity = jsonData.profondity;
        //distanceBetweenVerticalStreets = jsonData.distanceBetweenVerticalStreets;
        //distanceBetweenHorizontalStreets = jsonData.distanceBetweenHorizontalStreets;

    }

    private void OnDestroy()
    {
        //cityParkings.Dispose();
        cityParkingNodesPositions.Dispose();
    }

    // Start is called before the first frame update
    void Start()
    {

        //GatValueFromJson();

        cityParkingNodesPositions = new NativeList<float3>(100000, Allocator.Persistent);
        cityParkings = new Dictionary<int, NativeList<float3>>();

        //City map dimensions
        cityWidth = distanceBetweenVerticalStreets * (maxNumberVerticalStreets + 2) + 1;        //X axis city dimension
        cityLength = distanceBetweenHorizontalStreets * (numberHorizontalStreets - 1) + 1;      //Y axis city dimension

        //Debug.Log(cityWidth + " " + cityLength);

        cityMap = new MapTile[cityLength, cityWidth];
        for (int i = 0; i < cityLength; i++)
        {
            for (int j = 0; j < cityWidth; j++)
            {
                cityMap[i, j] = new MapTile(0, 0);
            }
        }

        //Generate random number of vertical streets for each section (1 section = space between 2 horizontal streets)
        numberVerticalStreets = generateVerticalStreetsNumber();


        //Generate the number of lanes for each horizontal street
        lanesHorizontalStreets = generateHorizontalStreetsNumberLanes();

        //Generate the number of lanes for each vertical street
        lanesVerticalStreets = generateVerticalStreetsNumberLanes();

        //Horizontal & vertical straight street initialization
        int currentVerticalStreet = 0;
        int currentHorizontalStreet = 0;
        for (int i = 0; i < cityLength; i += distanceBetweenHorizontalStreets)
        {
            //Initialize vars that will determine intersection positions on the horizontal streets
            double intersectionPositionsUpperSection;
            double intersectionPositionsLowerSection;
            double intersectionPositionsUpperSectionQuotient;
            double intersectionPositionsLowerSectionQuotient;
            if (currentHorizontalStreet == 0)       //TOP horizontal street
            {
                //Intersections of top horizontal street
                intersectionPositionsLowerSection = (double)(cityWidth) / (numberVerticalStreets[currentHorizontalStreet] + 1);
                //Intersections of top horizontal street (dont care what intersections are down)
                intersectionPositionsUpperSection = intersectionPositionsLowerSection;
            }
            else if (currentHorizontalStreet > 0 && currentHorizontalStreet < numberHorizontalStreets - 1) //MIDDLE horizontal streets
            {
                //Intersections of current horizontal street
                intersectionPositionsLowerSection = (double)(cityWidth) / (numberVerticalStreets[currentHorizontalStreet] + 1);
                //Intersections coming from upper horizontal street
                intersectionPositionsUpperSection = (double)(cityWidth) / (numberVerticalStreets[currentHorizontalStreet - 1] + 1);
            }
            else        //BOTTOM horizontal street
            {
                //Intersections coming from upper horizontal street
                intersectionPositionsUpperSection = (double)(cityWidth) / (numberVerticalStreets[currentHorizontalStreet - 1] + 1);
                //Intersections coming from upper horizontal street
                intersectionPositionsLowerSection = intersectionPositionsUpperSection;
            }

            intersectionPositionsUpperSectionQuotient = intersectionPositionsUpperSection;
            intersectionPositionsLowerSectionQuotient = intersectionPositionsLowerSection;

            currentVerticalStreet = 0;

            //TOP HORIZONTAL street
            if (currentHorizontalStreet == 0)
            {
                for (int j = 0; j < cityWidth; j++)
                {
                    if (j == 0 || j == cityWidth - 1)   //LEFT & RIGHT extreme VERTICAL roads
                    {
                        for (int k = i + 1; k < i + distanceBetweenHorizontalStreets && k < cityLength - 1; k++)
                        {
                            //match lanes of extreme vertical roads (place lane adaptor)
                            cityMap[k, j] = generateStraightStreetType(currentHorizontalStreet, currentVerticalStreet, VERTICAL_STREET, false, k);
                        }
                        currentVerticalStreet++;
                    }
                    else
                    {
                        if (j != (int)intersectionPositionsUpperSection)     //STRAIGHT HORIZONTAL street where no intersection
                        {
                            cityMap[i, j] = generateStraightStreetType(currentHorizontalStreet, currentVerticalStreet, HORIZONTAL_STREET);
                        }
                        else    //VERTICAL (down) street where there are intersections
                        {
                            intersectionPositionsUpperSection += intersectionPositionsUpperSectionQuotient;
                            for (int k = i + 1; k < i + distanceBetweenHorizontalStreets && k < cityLength - 1; k++)
                            {
                                cityMap[k, j] = generateStraightStreetType(currentHorizontalStreet, currentVerticalStreet, VERTICAL_STREET, false, -1);
                            }
                            currentVerticalStreet++;
                        }
                    }
                }
            }
            //BOTTOM HORIZONTAL street (draw ONLY horizontal streets)
            else if (currentHorizontalStreet == numberHorizontalStreets - 1)
            {
                for (int j = 1; j < cityWidth - 1; j++)  //Start after the bottom-left curve and end before the bottom-right curve
                {
                    if (j != (int)intersectionPositionsUpperSection)
                    {
                        cityMap[i, j] = generateStraightStreetType(currentHorizontalStreet, currentVerticalStreet, HORIZONTAL_STREET);
                    }
                    else
                    {
                        intersectionPositionsUpperSection += intersectionPositionsUpperSectionQuotient;
                    }
                }
            }
            //MIDDLE SECTION (draw both horizontal and vertical roads + match 4way intersection vertical number of lanes)
            else
            {
                //EXTREME LEFT VERTICAL street
                for (int k = i + 1; k < i + distanceBetweenHorizontalStreets && k < cityLength - 1; k++)
                {
                    //match lanes of extreme vertical roads (place lane adaptor)
                    cityMap[k, 0] = generateStraightStreetType(currentHorizontalStreet, currentVerticalStreet, VERTICAL_STREET, false, k);
                }
                currentVerticalStreet++;

                //MIDDLE VERTICAL streets
                for (int j = 1; j < cityWidth - 1; j++)
                {
                    if (j != (int)intersectionPositionsUpperSection && j != (int)intersectionPositionsLowerSection)    //HORIZONTAL road where there are no intersections (either coming from upper horizontal street OR starting from current horizontal street)
                    {
                        cityMap[i, j] = generateStraightStreetType(currentHorizontalStreet, currentVerticalStreet, HORIZONTAL_STREET);
                    }
                    else if (j == (int)intersectionPositionsUpperSection && j == (int)intersectionPositionsLowerSection)    //VERTICAL ROAD + match 4way intersection vertical number of lanes (place lane adapters)
                    {
                        for (int k = i + 1; k < i + distanceBetweenHorizontalStreets && k < cityLength - 1; k++)
                        {
                            cityMap[k, j] = generateStraightStreetType(currentHorizontalStreet, currentVerticalStreet, VERTICAL_STREET, true, j);   //match lanes of 4way intersections (place lane adaptor)
                        }
                        currentVerticalStreet++;
                        intersectionPositionsLowerSection += intersectionPositionsLowerSectionQuotient;
                        intersectionPositionsUpperSection += intersectionPositionsUpperSectionQuotient;
                    }
                    else if (j == (int)intersectionPositionsLowerSection)        //VERTICAL road when intersection starts from current horizontal lane (no lane matching)
                    {
                        for (int k = i + 1; k < i + distanceBetweenHorizontalStreets && k < cityLength - 1; k++)
                        {
                            cityMap[k, j] = generateStraightStreetType(currentHorizontalStreet, currentVerticalStreet, VERTICAL_STREET, false, k);
                        }
                        currentVerticalStreet++;
                        intersectionPositionsLowerSection += intersectionPositionsLowerSectionQuotient;
                    }
                    else if (j == (int)intersectionPositionsUpperSection)
                    {
                        intersectionPositionsUpperSection += intersectionPositionsUpperSectionQuotient;
                    }
                }

                //EXTREME RIGHT VERTICAL street
                for (int k = i + 1; k < i + distanceBetweenHorizontalStreets && k < cityLength - 1; k++)
                {
                    //match lanes of extreme vertical roads (place lane adaptor)
                    cityMap[k, cityWidth - 1] = generateStraightStreetType(currentHorizontalStreet, currentVerticalStreet, VERTICAL_STREET, false, k);
                }
                currentVerticalStreet++;
            }

            currentHorizontalStreet++;
        }

        //Generate bus stops
        for (int i = 0; i < cityLength; i += distanceBetweenHorizontalStreets)
        {
            for (int j = 0; j < cityWidth; j++)
            {
                if (cityMap[i, j].prefabType == 0)
                {
                    //Find next vertical intersection (if any)
                    //Dont consider the bottom horizontal street which has no vertical intersections
                    if (i / distanceBetweenHorizontalStreets < numberHorizontalStreets - 1)
                    {
                        if (cityMap[i + 1, j].prefabType != 0)
                        {
                            int nextVerticalIntersection = (i / distanceBetweenHorizontalStreets + 1) * distanceBetweenHorizontalStreets;
                            if (nextVerticalIntersection - i > 2)
                            {
                                int middlePosition = (i + nextVerticalIntersection) / 2;
                                if (cityMap[middlePosition, j].prefabType != LANE_ADAPTOR)
                                {
                                    cityMap[middlePosition, j] = generateBusStopType(middlePosition, j, VERTICAL_BUS_STOP_LEFT);
                                }
                                else
                                {
                                    cityMap[middlePosition - 1, j] = generateBusStopType(middlePosition - 1, j, VERTICAL_BUS_STOP_LEFT);
                                }
                                cityMap[middlePosition + 1, j] = generateBusStopType(middlePosition + 1, j, VERTICAL_BUS_STOP_RIGHT);
                            }
                        }
                    }


                    //Find next horizontal intersection
                    int nextHorizontalIntersection = -1;
                    for (int k = j + 1; k < cityWidth; k++)
                    {
                        if (cityMap[i, k].prefabType == 0)
                        {
                            nextHorizontalIntersection = k;
                            break;
                        }
                    }
                    //only generate bus stop if not at horizontal street extremes
                    if (nextHorizontalIntersection - j > 2 && nextHorizontalIntersection != -1)
                    {
                        int middlePosition = (j + nextHorizontalIntersection) / 2;
                        cityMap[i, middlePosition] = generateBusStopType(i, middlePosition, HORIZONTAL_BUS_STOP_UP);
                        cityMap[i, middlePosition + 1] = generateBusStopType(i, middlePosition + 1, HORIZONTAL_BUS_STOP_DOWN);
                        j = nextHorizontalIntersection - 1;
                    }
                }
            }

        }

        //Generate curves (only on 4 city extremities)
        //Top-left curve
        cityMap[0, 0] = generateCurveType(0, 0, TOP_LEFT);
        //Top-right curve
        cityMap[0, cityWidth - 1] = generateCurveType(0, cityWidth - 1, TOP_RIGHT);
        //Bottom-left curve
        cityMap[cityLength - 1, 0] = generateCurveType(cityLength - 1, 0, BOTTOM_LEFT);
        //Bottom-right curve
        cityMap[cityLength - 1, cityWidth - 1] = generateCurveType(cityLength - 1, cityWidth - 1, BOTTOM_RIGHT);

        //Generate intersections
        for (int i = 0; i < cityLength; i += distanceBetweenHorizontalStreets)   //Iterate streets horizontally
        {
            if (i == 0 || i == cityLength - 1)    //TOP & BOTTOM horizontal streets
            {
                for (int j = 1; j < cityWidth - 1; j++)
                {
                    if (cityMap[i, j].prefabType == 0)
                    {
                        if (i == 0)
                        {
                            cityMap[i, j] = generateIntersectionType(i, j, TOP_STREET);
                        }
                        else
                        {
                            cityMap[i, j] = generateIntersectionType(i, j, BOTTOM_STREET);
                        }
                    }
                }
            }
            else        //MIDDLE horizontal streets (between 2 other horizontal streets)
            {
                for (int j = 0; j < cityWidth; j++)
                {
                    if (cityMap[i, j].prefabType == 0)
                    {
                        cityMap[i, j] = generateIntersectionType(i, j, MIDDLE_STREET);
                    }
                }
            }
        }

        //Generate city buildings
        if (generateBuildings)
        {
            for (int i = 0; i < cityLength; i++)
            {
                for (int j = 0; j < cityWidth; j++)
                {
                    if (cityMap[i, j].prefabType == 0)
                    {
                        generateBuildingPrefab(cityMap[i, j], i, j);
                    }
                }
            }
        }

        //
        //DEBUGGING
        //
        /*string str = "";
        for (int i = 0; i < cityLength; i++)
        {
            if (i % distanceBetweenHorizontalStreets == 0)
            {
                str = "<b><color=green>";
                for (int j = 0; j < cityWidth; j++)
                {
                    str += cityMap[i, j].prefabType + "\t";
                }
                str += "</color></b>";
            }
            else
            {
                str = "";
                for (int j = 0; j < cityWidth; j++)
                {
                    if (cityMap[i, j].prefabType != 0)
                    {
                        str += "<b><color=green>" + cityMap[i, j].prefabType + "</color></b>" + "\t";
                    }
                    else
                    {
                        str += cityMap[i, j].prefabType + "\t";
                    }
                }
            }
            Debug.Log(str);
        }*/
        //
        //DEBUGGING END
        //

        instantiateTerrain(cityWidth, cityLength);

        nodesMap = new Dictionary<Vector3, Node>();

        for (int i = 0; i < cityLength; i++)
        {
            for (int j = 0; j < cityWidth; j++)
            {
                if (cityMap[i, j].prefabType != 0)
                {
                    generatePrefab(cityMap[i, j], i, j);
                    instantiatePrefab(cityMap[i, j], i, j);
                }
            }
        }

        //Connect all prefabs together
        cityStreetConnector();

        //Spawn cars
        carSpawner = new CarSpawner(carPrefab, this, numberCarsToSpawn);
        

        //Spawn buses
        busSpawner = new BusSpawner(busPrefab, this);

        carSpawner.generateTraffic(numberCarsToSpawn, profondity, nodesCity, waypoitnsCity);
        busSpawner.generateBuses();

        //Used for the path 
        //GenerateArrayForCars();
        carsNeedToSpawn = 0;
        spawn = true;
    }

    private int[] generateVerticalStreetsNumber()
    {
        int[] verticalStreets = new int[numberHorizontalStreets - 1];
        for (int i = 0; i < numberHorizontalStreets - 1; i++)
        {
            verticalStreets[i] = UnityEngine.Random.Range(minNumberVerticalStreets, maxNumberVerticalStreets + 1);
        }
        return verticalStreets;
    }

    private int[] generateHorizontalStreetsNumberLanes()
    {
        int[] numberLanes = new int[numberHorizontalStreets];
        for (int i = 0; i < numberHorizontalStreets; i++)
        {
            if (only1LaneStreets)
            {
                numberLanes[i] = 1;
            }
            else if (only2LaneStreets)
            {
                numberLanes[i] = 2;
            }
            else
            {
                numberLanes[i] = UnityEngine.Random.Range(1, 3);
            }
        }
        return numberLanes;
    }

    private int[,] generateVerticalStreetsNumberLanes()
    {
        int[,] numberLanes = new int[numberHorizontalStreets - 1, maxNumberVerticalStreets + 2];
        for (int i = 0; i < numberHorizontalStreets - 1; i++)
        {
            for (int j = 0; j < numberVerticalStreets[i] + 2; j++)
            {
                if (only1LaneStreets)
                {
                    numberLanes[i, j] = 1;
                }
                else if (only2LaneStreets)
                {
                    numberLanes[i, j] = 2;
                }
                else
                {
                    if (i == 0 || i == numberHorizontalStreets - 2)
                    {
                        if (j == 0 || j == numberVerticalStreets[i] + 1)     //Match lane number for streets connected by curves
                        {
                            numberLanes[i, j] = lanesHorizontalStreets[i];
                        }
                        else
                        {
                            numberLanes[i, j] = UnityEngine.Random.Range(1, 3);
                        }
                    }
                    else
                    {
                        numberLanes[i, j] = UnityEngine.Random.Range(1, 3);
                    }
                }
            }
        }
        return numberLanes;
    }

    private MapTile generateStraightStreetType(int numberHorizontalStreet, int numberVerticalStreet, int streetDirection, bool needLaneMatching = false, int yPosition = -1)
    {
        //VERTICAL STREETS
        if (streetDirection == VERTICAL_STREET)
        {
            //4-way intersections need lane matching (vertical road coming from up #lanes = vertical road going down #lanes) otherwise no prefab to implement intersection
            if (needLaneMatching)
            {
                //#lanes of vertical street going down needs to match #lanes of vertical street coming from up
                if (cityMap[numberHorizontalStreet * distanceBetweenHorizontalStreets - 1, yPosition].prefabType == 1)
                {
                    return new MapTile(STRAIGHT_1LANE, STRAIGHT_STREET_ROTATION_VERTICAL);
                }
                return new MapTile(STRAIGHT_2LANE, STRAIGHT_STREET_ROTATION_VERTICAL);
            }
            //extreme LEFT & RIGHT vertical roads
            else if (numberVerticalStreet == 0 || numberVerticalStreet == numberVerticalStreets[numberHorizontalStreet] + 1)
            {
                //Horizontal streets (that are in parallel) having the same number of lanes
                if (lanesHorizontalStreets[numberHorizontalStreet] == lanesHorizontalStreets[numberHorizontalStreet + 1])
                {
                    if (lanesHorizontalStreets[numberHorizontalStreet] == 1)
                    {
                        return new MapTile(STRAIGHT_1LANE, STRAIGHT_STREET_ROTATION_VERTICAL);
                    }
                    return new MapTile(STRAIGHT_2LANE, STRAIGHT_STREET_ROTATION_VERTICAL);
                }
                //Insert lane adaptor when horizontal streets number of lanes doesnt match
                else
                {
                    int up = numberHorizontalStreet * distanceBetweenHorizontalStreets;
                    int down = (numberHorizontalStreet + 1) * distanceBetweenHorizontalStreets;
                    int middle = (up + down) / 2;
                    if (yPosition > up && yPosition < middle)
                    {
                        if (lanesHorizontalStreets[numberHorizontalStreet] == 1)
                        {
                            return new MapTile(STRAIGHT_1LANE, STRAIGHT_STREET_ROTATION_VERTICAL);
                        }
                        return new MapTile(STRAIGHT_2LANE, STRAIGHT_STREET_ROTATION_VERTICAL);
                    }
                    else if (yPosition > middle && yPosition < down)
                    {
                        if (lanesHorizontalStreets[numberHorizontalStreet + 1] == 1)
                        {
                            return new MapTile(STRAIGHT_1LANE, STRAIGHT_STREET_ROTATION_VERTICAL);
                        }
                        return new MapTile(STRAIGHT_2LANE, STRAIGHT_STREET_ROTATION_VERTICAL);
                    }
                    else
                    {
                        if (lanesHorizontalStreets[numberHorizontalStreet] == 2)
                        {
                            return new MapTile(LANE_ADAPTOR, LANE_ADAPTOR_ROTATION_2LANE_UP);
                        }
                        else
                        {
                            return new MapTile(LANE_ADAPTOR, LANE_ADAPTOR_ROTATION_2LANE_DOWN);
                        }
                    }
                }
            }
            else
            {
                if (lanesVerticalStreets[numberHorizontalStreet, numberVerticalStreet] == 1)
                {
                    return new MapTile(STRAIGHT_1LANE, STRAIGHT_STREET_ROTATION_VERTICAL);
                }
                return new MapTile(STRAIGHT_2LANE, STRAIGHT_STREET_ROTATION_VERTICAL);
            }
        }
        //HORIZONTAL STREETS
        else
        {
            if (lanesHorizontalStreets[numberHorizontalStreet] == 1)
            {
                return new MapTile(STRAIGHT_1LANE, STRAIGHT_STREET_ROTATION_HORIZONTAL);
            }
            return new MapTile(STRAIGHT_2LANE, STRAIGHT_STREET_ROTATION_HORIZONTAL);
        }
    }

    private MapTile generateIntersectionType(int row, int col, int streetPosition)
    {
        if (streetPosition == TOP_STREET)       //TOP horizontal streets
        {
            if (lanesHorizontalStreets[row / distanceBetweenHorizontalStreets] == 1)   //Horizontal street with 1 lane
            {
                if (cityMap[row + 1, col].prefabType == 1)
                {
                    return new MapTile(generateRandomIntersectionType(INTERSECTION_3WAY_1LANE), INTERSECTION_ROTATION_BOTTOM);
                }
                else if (cityMap[row + 1, col].prefabType == 2)
                {
                    return new MapTile(generateRandomIntersectionType(INTERSECTION_3WAY_1LANE_2LANE), INTERSECTION_ROTATION_BOTTOM);
                }
                else
                {
                    throw new Exception("Missing vertical street");
                }
            }
            else if (lanesHorizontalStreets[row / distanceBetweenHorizontalStreets] == 2)   //Horizontal street with 2 lanes
            {
                if (cityMap[row + 1, col].prefabType == 1)
                {
                    return new MapTile(generateRandomIntersectionType(INTERSECTION_3WAY_2LANE_1LANE), INTERSECTION_ROTATION_BOTTOM);
                }
                else if (cityMap[row + 1, col].prefabType == 2)
                {
                    return new MapTile(generateRandomIntersectionType(INTERSECTION_3WAY_2LANE), INTERSECTION_ROTATION_BOTTOM);
                }
                else
                {
                    throw new Exception("Missing vertical street");
                }
            }
            else
            {
                throw new Exception("Bad horizontal number of lanes");
            }
        }
        else if (streetPosition == BOTTOM_STREET)   //BOTTOM horizontal streets
        {
            if (lanesHorizontalStreets[row / distanceBetweenHorizontalStreets] == 1)    //Horizontal street with 1 lane
            {
                if (cityMap[row - 1, col].prefabType == 1)
                {
                    return new MapTile(generateRandomIntersectionType(INTERSECTION_3WAY_1LANE), INTERSECTION_ROTATION_TOP);
                }
                else if (cityMap[row - 1, col].prefabType == 2)
                {
                    return new MapTile(generateRandomIntersectionType(INTERSECTION_3WAY_1LANE_2LANE), INTERSECTION_ROTATION_TOP);
                }
                else
                {
                    throw new Exception("Missing vertical street");
                }
            }
            else if (lanesHorizontalStreets[row / distanceBetweenHorizontalStreets] == 2)   //Horizontal street with 2 lanes
            {
                if (cityMap[row - 1, col].prefabType == 1)
                {
                    return new MapTile(generateRandomIntersectionType(INTERSECTION_3WAY_2LANE_1LANE), INTERSECTION_ROTATION_TOP);
                }
                else if (cityMap[row - 1, col].prefabType == 2)
                {
                    return new MapTile(generateRandomIntersectionType(INTERSECTION_3WAY_2LANE), INTERSECTION_ROTATION_TOP);
                }
                else
                {
                    throw new Exception("Missing vertical street");
                }
            }
            else
            {
                throw new Exception("Bad horizontal number of lanes");
            }
        }
        else        //MIDDLE horizontal streets
        {
            if (col == 0)     //3-way  intersection (facing right)
            {
                if (lanesHorizontalStreets[row / distanceBetweenHorizontalStreets] == 1)    //Horizontal street with 1 lane
                {
                    if (cityMap[row - 1, col].prefabType == 1 && cityMap[row + 1, col].prefabType == 1)
                    {
                        return new MapTile(generateRandomIntersectionType(INTERSECTION_3WAY_1LANE), INTERSECTION_ROTATION_RIGHT);
                    }
                    else if (cityMap[row - 1, col].prefabType == 2 && cityMap[row + 1, col].prefabType == 2)
                    {
                        return new MapTile(generateRandomIntersectionType(INTERSECTION_3WAY_2LANE_1LANE), INTERSECTION_ROTATION_RIGHT);
                    }
                    else
                    {
                        throw new Exception("Bad vertical number of lanes");
                    }
                }
                else if (lanesHorizontalStreets[row / distanceBetweenHorizontalStreets] == 2)   //Horizontal street with 2 lanes
                {
                    if (cityMap[row - 1, col].prefabType == 1 && cityMap[row + 1, col].prefabType == 1)
                    {
                        return new MapTile(generateRandomIntersectionType(INTERSECTION_3WAY_1LANE_2LANE), INTERSECTION_ROTATION_RIGHT);
                    }
                    else if (cityMap[row - 1, col].prefabType == 2 && cityMap[row + 1, col].prefabType == 2)
                    {
                        return new MapTile(generateRandomIntersectionType(INTERSECTION_3WAY_2LANE), INTERSECTION_ROTATION_RIGHT);
                    }
                    else
                    {
                        throw new Exception("Bad vertical number of lanes on left border");
                    }
                }
                else
                {
                    throw new Exception("Bad horizontal number of lanes");
                }
            }
            else if (col == cityWidth - 1)    //3-way  intersection (facing left)
            {
                if (lanesHorizontalStreets[row / distanceBetweenHorizontalStreets] == 1)    //Horizontal street with 1 lane
                {
                    if (cityMap[row - 1, col].prefabType == 1 && cityMap[row + 1, col].prefabType == 1)
                    {
                        return new MapTile(generateRandomIntersectionType(INTERSECTION_3WAY_1LANE), INTERSECTION_ROTATION_LEFT);
                    }
                    else if (cityMap[row - 1, col].prefabType == 2 && cityMap[row + 1, col].prefabType == 2)
                    {
                        return new MapTile(generateRandomIntersectionType(INTERSECTION_3WAY_2LANE_1LANE), INTERSECTION_ROTATION_LEFT);
                    }
                    else
                    {
                        throw new Exception("Bad vertical number of lanes on right border");
                    }
                }
                else if (lanesHorizontalStreets[row / distanceBetweenHorizontalStreets] == 2)   //Horizontal street with 2 lanes
                {
                    if (cityMap[row - 1, col].prefabType == 1 && cityMap[row + 1, col].prefabType == 1)
                    {
                        return new MapTile(generateRandomIntersectionType(INTERSECTION_3WAY_1LANE_2LANE), INTERSECTION_ROTATION_LEFT);
                    }
                    else if (cityMap[row - 1, col].prefabType == 2 && cityMap[row + 1, col].prefabType == 2)
                    {
                        return new MapTile(generateRandomIntersectionType(INTERSECTION_3WAY_2LANE), INTERSECTION_ROTATION_LEFT);
                    }
                    else
                    {
                        throw new Exception("Bad vertical number of lanes on right border");
                    }
                }
                else
                {
                    throw new Exception("Bad horizontal number of lanes");
                }
            }
            else
            {
                if (cityMap[row - 1, col].prefabType != 0 && cityMap[row + 1, col].prefabType != 0)   //4-way intersection (Vertical street check)
                {
                    if (lanesHorizontalStreets[row / distanceBetweenHorizontalStreets] == 1)    //Horizontal street with 1 lane
                    {
                        if (cityMap[row - 1, col].prefabType == 1 && cityMap[row + 1, col].prefabType == 1)    //Vertical streets with 1 lane
                        {
                            return new MapTile(generateRandomIntersectionType(INTERSECTION_4WAY_1LANE), INTERSECTION_ROTATION_RIGHT);
                        }
                        else if (cityMap[row - 1, col].prefabType == 2 && cityMap[row + 1, col].prefabType == 2)   //Vertical streets with 2 lanes
                        {
                            return new MapTile(generateRandomIntersectionType(INTERSECTION_4WAY_1LANE_2LANE), INTERSECTION_ROTATION_RIGHT);
                        }
                        else
                        {
                            throw new Exception("Bad vertical number of lanes");
                        }
                    }
                    else if (lanesHorizontalStreets[row / distanceBetweenHorizontalStreets] == 2)      //Horizontal street with 2 lanes
                    {
                        if (cityMap[row - 1, col].prefabType == 1 && cityMap[row + 1, col].prefabType == 1)    //Vertical streets with 1 lane
                        {
                            return new MapTile(generateRandomIntersectionType(INTERSECTION_4WAY_1LANE_2LANE), INTERSECTION_ROTATION_BOTTOM);
                        }
                        else if (cityMap[row - 1, col].prefabType == 2 && cityMap[row + 1, col].prefabType == 2)   //Vertical streets with 2 lanes
                        {
                            return new MapTile(generateRandomIntersectionType(INTERSECTION_4WAY_2LANE), INTERSECTION_ROTATION_BOTTOM);
                        }
                        else
                        {
                            throw new Exception("Bad vertical number of lanes");
                        }
                    }
                    else
                    {
                        throw new Exception("Bad horizontal number of lanes");
                    }
                }
                else if (cityMap[row - 1, col].prefabType != 0)     //3-way intersection (facing up)
                {
                    if (lanesHorizontalStreets[row / distanceBetweenHorizontalStreets] == 1)   //Horizontal street with 1 lane
                    {
                        if (cityMap[row - 1, col].prefabType == 1)     //Vertical street with 1 lane
                        {
                            return new MapTile(generateRandomIntersectionType(INTERSECTION_3WAY_1LANE), INTERSECTION_ROTATION_TOP);
                        }
                        else        //Vertical street with 2 lanes
                        {
                            return new MapTile(generateRandomIntersectionType(INTERSECTION_3WAY_1LANE_2LANE), INTERSECTION_ROTATION_TOP);
                        }
                    }
                    else if (lanesHorizontalStreets[row / distanceBetweenHorizontalStreets] == 2)      //Horizontal street with 2 lanes
                    {
                        if (cityMap[row - 1, col].prefabType == 1)     //Vertical street with 1 lane
                        {
                            return new MapTile(generateRandomIntersectionType(INTERSECTION_3WAY_2LANE_1LANE), INTERSECTION_ROTATION_TOP);
                        }
                        else        //Vertical street with 2 lanes
                        {
                            return new MapTile(generateRandomIntersectionType(INTERSECTION_3WAY_2LANE), INTERSECTION_ROTATION_TOP);
                        }
                    }
                    else
                    {
                        throw new Exception("Bad horizontal number of lanes");
                    }
                }
                else if (cityMap[row + 1, col].prefabType != 0)     //3-way intersection (facing down)
                {
                    if (lanesHorizontalStreets[row / distanceBetweenHorizontalStreets] == 1)   //Horizontal street with 1 lane
                    {
                        if (cityMap[row + 1, col].prefabType == 1)     //Vertical street with 1 lane
                        {
                            return new MapTile(generateRandomIntersectionType(INTERSECTION_3WAY_1LANE), INTERSECTION_ROTATION_BOTTOM);
                        }
                        else        //Vertical street with 2 lanes
                        {
                            return new MapTile(generateRandomIntersectionType(INTERSECTION_3WAY_1LANE_2LANE), INTERSECTION_ROTATION_BOTTOM);
                        }
                    }
                    else if (lanesHorizontalStreets[row / distanceBetweenHorizontalStreets] == 2)      //Horizontal street with 2 lanes
                    {
                        if (cityMap[row + 1, col].prefabType == 1)     //Vertical street with 1 lane
                        {
                            return new MapTile(generateRandomIntersectionType(INTERSECTION_3WAY_2LANE_1LANE), INTERSECTION_ROTATION_BOTTOM);
                        }
                        else        //Vertical street with 2 lanes
                        {
                            return new MapTile(generateRandomIntersectionType(INTERSECTION_3WAY_2LANE), INTERSECTION_ROTATION_BOTTOM);
                        }
                    }
                    else
                    {
                        throw new Exception("Bad horizontal number of lanes");
                    }
                }
                else
                {
                    throw new Exception("Missing vertical street");
                }
            }
        }
    }

    private int generateRandomIntersectionType(int intersectionType)
    {
        if (onlySemaphoreIntersections)
        {
            return intersectionType + 1;
        }
        else if (onlySimpleIntersections)
        {
            return intersectionType;
        }
        else
        {
            int randomDivider = UnityEngine.Random.Range(intersectionType, intersectionType + 2) % 2;
            if (randomDivider == 0)
            {
                return intersectionType;
            }
            return intersectionType + 1;
        }
    }

    private MapTile generateCurveType(int row, int col, int curvePosition)
    {
        if (curvePosition == TOP_LEFT)
        {
            if (cityMap[row + 1, col].prefabType == 1 && cityMap[row, col + 1].prefabType == 1)
            {
                return new MapTile(CURVE_1LANE, CURVE_ROTATION_TOP_LEFT);
            }
            else if (cityMap[row + 1, col].prefabType == 2 && cityMap[row, col + 1].prefabType == 2)
            {
                return new MapTile(CURVE_2LANE, CURVE_ROTATION_TOP_LEFT);
            }
            else
            {
                throw new Exception("Bad lane numbers of top left curve");
            }
        }
        else if (curvePosition == TOP_RIGHT)
        {
            if (cityMap[row + 1, col].prefabType == 1 && cityMap[row, col - 1].prefabType == 1)
            {
                return new MapTile(CURVE_1LANE, CURVE_ROTATION_TOP_RIGHT);
            }
            else if (cityMap[row + 1, col].prefabType == 2 && cityMap[row, col - 1].prefabType == 2)
            {
                return new MapTile(CURVE_2LANE, CURVE_ROTATION_TOP_RIGHT);
            }
            else
            {
                throw new Exception("Bad lane numbers of top right curve");
            }
        }
        else if (curvePosition == BOTTOM_LEFT)
        {
            if (cityMap[row - 1, col].prefabType == 1 && cityMap[row, col + 1].prefabType == 1)
            {
                return new MapTile(CURVE_1LANE, CURVE_ROTATION_BOTTOM_LEFT);
            }
            else if (cityMap[row - 1, col].prefabType == 2 && cityMap[row, col + 1].prefabType == 2)
            {
                return new MapTile(CURVE_2LANE, CURVE_ROTATION_BOTTOM_LEFT);
            }
            else
            {
                throw new Exception("Bad lane numbers of bottom left curve");
            }
        }
        else
        {
            if (cityMap[row - 1, col].prefabType == 1 && cityMap[row, col - 1].prefabType == 1)
            {
                return new MapTile(CURVE_1LANE, CURVE_ROTATION_BOTTOM_RIGHT);
            }
            else if (cityMap[row - 1, col].prefabType == 2 && cityMap[row, col - 1].prefabType == 2)
            {
                return new MapTile(CURVE_2LANE, CURVE_ROTATION_BOTTOM_RIGHT);
            }
            else
            {
                throw new Exception("Bad lane numbers of bottom right curve");
            }
        }
    }

    private MapTile generateBusStopType(int row, int col, int busStopType)
    {
        if (busStopType == HORIZONTAL_BUS_STOP_DOWN)
        {
            if (lanesHorizontalStreets[row / distanceBetweenHorizontalStreets] == 1)
            {
                return new MapTile(BUS_STOP_1LANE, BUS_STOP_ROTATION_DOWN);
            }
            else
            {
                return new MapTile(BUS_STOP_2LANE, BUS_STOP_ROTATION_DOWN);
            }
        }
        else if (busStopType == HORIZONTAL_BUS_STOP_UP)
        {
            if (lanesHorizontalStreets[row / distanceBetweenHorizontalStreets] == 1)
            {
                return new MapTile(BUS_STOP_1LANE, BUS_STOP_ROTATION_UP);
            }
            else
            {
                return new MapTile(BUS_STOP_2LANE, BUS_STOP_ROTATION_UP);
            }
        }
        else if (busStopType == VERTICAL_BUS_STOP_LEFT)
        {
            if (cityMap[row - 1, col].prefabType == STRAIGHT_1LANE || cityMap[row + 1, col].prefabType == STRAIGHT_1LANE)
            {
                return new MapTile(BUS_STOP_1LANE, BUS_STOP_ROTATION_LEFT);
            }
            else
            {
                return new MapTile(BUS_STOP_2LANE, BUS_STOP_ROTATION_LEFT);
            }
        }
        else
        {
            if (cityMap[row - 1, col].prefabType == STRAIGHT_1LANE || cityMap[row + 1, col].prefabType == STRAIGHT_1LANE)
            {
                return new MapTile(BUS_STOP_1LANE, BUS_STOP_ROTATION_RIGHT);
            }
            else
            {
                return new MapTile(BUS_STOP_2LANE, BUS_STOP_ROTATION_RIGHT);
            }
        }
    }

    private void cityStreetConnector()
    {
        for (int i = 0; i < cityLength; i++)
        {
            for (int j = 0; j < cityWidth; j++)
            {
                if (cityMap[i, j].prefabType > 0)
                {
                    //CONNECT NEIGHBORING STREET PREFABS (IN ORDER TO GENERATE A GRAPH FOR THE WHOLE CITY)
                    List<Node> carWaypoints = cityMap[i, j].instantiatedStreet.GetComponent<Street>().carWaypoints;
                    foreach (var node in carWaypoints)
                    {
                        if (node.needOutgoingConnection)
                        {
                            Collider[] nearbyWaypoints = Physics.OverlapSphere(node.transform.position, 8f, 1 << 8);
                            //Debug.Log("# of nearby waypoints:" + nearbyWaypoints.Length);
                            Node targetWaypoint = null;
                            float shortestDistance = 999999999;
                            foreach (var nearbyWaypoint in nearbyWaypoints)
                            {
                                if (nearbyWaypoint.transform.parent.position != this.transform.position &&
                                    node.laneNumber == nearbyWaypoint.GetComponent<Node>().laneNumber &&
                                    nearbyWaypoint.GetComponent<Node>().needIncomingConnection &&
                                    !carWaypoints.Contains(nearbyWaypoint.GetComponent<Node>()))
                                {
                                    float distance = Vector3.Distance(node.transform.position, nearbyWaypoint.transform.position);
                                    if (distance < shortestDistance)
                                    {
                                        targetWaypoint = nearbyWaypoint.GetComponent<Node>();
                                    }
                                }
                            }
                            if (targetWaypoint != null)
                            {
                                node.nextNodes.Add(targetWaypoint);
                                //Debug.Log("Added connection");
                            }
                        }
                    }
                }
            }
        }
        for (int i = 0; i < cityLength; i++)
        {
            for (int j = 0; j < cityWidth; j++)
            {
                if (cityMap[i, j].prefabType > 0)
                {
                    foreach (var node in cityMap[i, j].instantiatedStreet.GetComponent<Street>().carWaypoints)
                    {
                        Destroy(node.GetComponent<SphereCollider>());
                    }
                }
            }
        }
    }

    private void generatePrefab(MapTile tile, int row, int col)
    {
        if (tile.prefabType == STRAIGHT_1LANE || tile.prefabType == STRAIGHT_2LANE)
        {
            if (tile.prefabType == STRAIGHT_1LANE)
            {
                cityMap[row, col].prefabReference = straightStreet1Lane;
            }
            else
            {
                cityMap[row, col].prefabReference = straightStreet2Lane;
            }
        }
        else if (tile.prefabType == CURVE_1LANE || tile.prefabType == CURVE_2LANE)
        {
            if (tile.prefabType == CURVE_1LANE)
            {
                cityMap[row, col].prefabReference = curve1Lane;
            }
            else
            {
                cityMap[row, col].prefabReference = curve2Lane;
            }
        }
        else if (tile.prefabType == LANE_ADAPTOR)
        {
            cityMap[row, col].prefabReference = laneAdaptor;
        }
        else if (tile.prefabType == BUS_STOP_1LANE || tile.prefabType == BUS_STOP_2LANE)
        {
            if (tile.prefabType == BUS_STOP_1LANE)
            {
                cityMap[row, col].prefabReference = busStop1Lane;
            }
            else if (tile.prefabType == BUS_STOP_2LANE)
            {
                cityMap[row, col].prefabReference = busStop2Lane;
            }
        }
        //INTERSECTIONS
        else
        {
            if (tile.prefabType == INTERSECTION_4WAY_1LANE)
            {
                cityMap[row, col].prefabReference = intersection4Way1Lane;
                cityMap[row, col].rotation = 90 * UnityEngine.Random.Range(0, 4);
            }
            else if (tile.prefabType == INTERSECTION_4WAY_1LANE_SEMAPHORE)
            {
                cityMap[row, col].prefabReference = intersection4Way1LaneSemaphore;
                cityMap[row, col].rotation = 90 * UnityEngine.Random.Range(0, 4);
            }
            else if (tile.prefabType == INTERSECTION_4WAY_2LANE)
            {
                cityMap[row, col].prefabReference = intersection4Way2Lane;
                cityMap[row, col].rotation = 90 * UnityEngine.Random.Range(0, 4);
            }
            else if (tile.prefabType == INTERSECTION_4WAY_2LANE_SEMAPHORE)
            {
                cityMap[row, col].prefabReference = intersection4Way2LaneSemaphore;
                cityMap[row, col].rotation = 90 * UnityEngine.Random.Range(0, 4);
            }
            else if (tile.prefabType == INTERSECTION_3WAY_1LANE)
            {
                cityMap[row, col].prefabReference = intersection3Way1Lane;
            }
            else if (tile.prefabType == INTERSECTION_3WAY_1LANE_SEMAPHORE)
            {
                cityMap[row, col].prefabReference = intersection3Way1LaneSemaphore;
            }
            else if (tile.prefabType == INTERSECTION_3WAY_2LANE)
            {
                cityMap[row, col].prefabReference = intersection3Way2Lane;
            }
            else if (tile.prefabType == INTERSECTION_3WAY_2LANE_SEMAPHORE)
            {
                cityMap[row, col].prefabReference = intersection3Way2LaneSemaphore;
            }
            else if (tile.prefabType == INTERSECTION_4WAY_1LANE_2LANE)
            {
                cityMap[row, col].prefabReference = intersection4Way1Lane2Lane;
            }
            else if (tile.prefabType == INTERSECTION_4WAY_1LANE_2LANE_SEMAPHORE)
            {
                cityMap[row, col].prefabReference = intersection4Way1Lane2LaneSemaphore;
            }
            else if (tile.prefabType == INTERSECTION_3WAY_1LANE_2LANE)
            {
                cityMap[row, col].prefabReference = intersection3Way1Lane2Lane;
            }
            else if (tile.prefabType == INTERSECTION_3WAY_1LANE_2LANE_SEMAPHORE)
            {
                cityMap[row, col].prefabReference = intersection3Way1Lane2LaneSemaphore;
            }
            else if (tile.prefabType == INTERSECTION_3WAY_2LANE_1LANE)
            {
                cityMap[row, col].prefabReference = intersection3Way2Lane1Lane;
            }
            else if (tile.prefabType == INTERSECTION_3WAY_2LANE_1LANE_SEMAPHORE)
            {
                cityMap[row, col].prefabReference = intersection3Way2Lane1LaneSemaphore;
            }
            Component[] intersectionTriggers = cityMap[row, col].prefabReference.GetComponentsInChildren<IntersectionTriggerComponent>();
            foreach (IntersectionTriggerComponent trig in intersectionTriggers)
            {
                trig.dynamicIntersectionId = intersectionId;
            }
            this.intersectionId++;
        }
    }

    private void generateBuildingPrefab(MapTile tile, int row, int col)
    {
        GameObject randomBuldingBlock = buildingPrefabs[UnityEngine.Random.Range(0, buildingPrefabs.Count)];
        int randomBlockRotation = 90 * UnityEngine.Random.Range(0, 4);
        cityMap[row, col].prefabType = BUILDINGS;
        cityMap[row, col].prefabReference = randomBuldingBlock;
        cityMap[row, col].rotation = randomBlockRotation;
    }

    private void instantiateTerrain(int cityWidth, int cityLength)
    {
        cityPlane.transform.localScale = new Vector3(cityWidth * 15, 1, cityLength * 15);
        GameObject terrain = Instantiate(cityPlane, new Vector3(cityWidth * 10, 0.05f, cityLength * 10), Quaternion.identity);
    }

    private void instantiatePrefab(MapTile tile, int row, int col)
    {
        //int SceneRow = (row - (cityLength / 2));
        //int SceneCol = -(col - (cityLength / 2));

        int zPosition = Mathf.Abs(row * 60 - cityLength * 60) + 10;
        int xPosition = col * 60 + 10;

        cityMap[row, col].instantiatedStreet = Instantiate(tile.prefabReference, new Vector3(xPosition, 0, zPosition), Quaternion.Euler(0, tile.rotation, 0));

        if (cityMap[row, col].prefabType > 0)
        {
            Street currentStreet = cityMap[row, col].instantiatedStreet.GetComponent<Street>();
            //set bus stop lane directions correctly
            if ((tile.prefabType == BUS_STOP_1LANE || tile.prefabType == BUS_STOP_2LANE) && (tile.rotation == BUS_STOP_ROTATION_RIGHT || tile.rotation == BUS_STOP_ROTATION_UP))
            {
                foreach (var node in currentStreet.carWaypoints)
                {
                    node.trafficDirection = (node.trafficDirection + 1) % 2;
                }
            }

            //fill nodes (waypoint) list
            foreach (var node in currentStreet.carWaypoints)
            {
                    cityNodes.Add(node);
                if(!nodesMap.TryGetValue(node.transform.position, out _))
                    nodesMap.Add(node.transform.position, node);
                
                if (node.isParkingGateway)
                {
                    cityParkingNodes.Add(node);
                    //cityParkingNodesPositions.Add((float3)node.transform.position);
                    

                }
                else if (node.isBusStop)
                {
                    cityBusStopsSpawn.Add(node);
                    cityBusStopsDst.Add(node);
                }
                else if (((cityMap[row, col].prefabType == STRAIGHT_1LANE || cityMap[row, col].prefabType == STRAIGHT_2LANE || cityMap[row, col].prefabType == BUS_STOP_1LANE || cityMap[row, col].prefabType == BUS_STOP_2LANE)
                        && !node.isLaneChange) && node.isCarSpawn)   //spawn nodes dont include lane-change nodes and bus lanes
                {
                    citySpawnNodes.Add(node);
                }
            }

           


        }
    }

    public int GetParkingPositionHashMapKey(float3 position)
    {
        int xMultiplier = 100000;
        int xPosition = (int)position.x;
        int zPosition = (int)position.z;
        return xPosition * xMultiplier + zPosition;
    }

}