using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CityGenerator : MonoBehaviour
{

    public int numberHorizontalStreets;
    public int minNumberVerticalStreets;
    public int maxNumberVerticalStreets;
    public int minDistanceBetweenHorizontalStreets;
    public int maxDistanceBetweenHorizontalStreets;
    public bool onlySimpleIntersections;
    public bool onlySemaphoreIntersections;
    public bool only1LaneStreets;
    public bool only2LaneStreets;

    public GameObject straightStreet1Lane;
    public GameObject straightStreet1LaneShort;
    public GameObject straightStreet2Lane;
    public GameObject straightStreet2LaneShort;
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
    public GameObject intersection3Way1Lane;
    public GameObject intersection3Way1LaneSemaphore;
    public GameObject intersection3Way2Lane;
    public GameObject intersection3Way2LaneSemaphore;


    private const int distanceBetweenVerticalStreets = 5;   //2 prefabs for bus stops + 1 prefab (optional) for lane adapter + 2 reserved prefabs
    private const int distanceBetweenHorizontalStreets = 6;

    private int[,] cityMap;
    private const int STRAIGHT_1LANE = 1;
    private const int STRAIGHT_2LANE = 2;
    private const int CURVE_1LANE = 3;
    private const int CURVE_2LANE = 4;
    private const int BUS_STOP_1LANE = 5;
    private const int BUS_STOP_2LANE = 6;
    private const int INTERSECTION_4WAY_1LANE = 7;
    private const int INTERSECTION_4WAY_2LANE = 8;
    private const int INTERSECTION_3WAY_1LANE = 9;
    private const int INTERSECTION_3WAY_2LANE = 10;
    private const int INTERSECTION_4WAY_1LANE_SEMAPHORE = 11;
    private const int INTERSECTION_4WAY_2LANE_SEMAPHORE = 12;
    private const int INTERSECTION_3WAY_1LANE_SEMAPHORE = 13;
    private const int INTERSECTION_3WAY_2LANE_SEMAPHORE = 14;
    private const int LANE_ADAPTOR = 20;

    private const int HORIZONTAL_STREET = 0;
    private const int VERTICAL_STREET = 1;

    private int[] horizontalSpaces;
    private int[] lanesHorizontalStreets;
    private int[] numberVerticalStreets;
    private int[,] lanesVerticalStreets;


    // Start is called before the first frame update
    void Start()
    {
        //City map dimensions
        int cityWidth = distanceBetweenVerticalStreets * (maxNumberVerticalStreets + 2) + 1;        //X axis city dimension
        int cityLength = distanceBetweenHorizontalStreets * (numberHorizontalStreets - 1) + 1;      //Y axis city dimension

        Debug.Log(cityWidth + " " + cityLength);

        cityMap = new int[cityLength, cityWidth];

        //Generate random number of vertical streets for each section (1 section = space between 2 horizontal streets)
        numberVerticalStreets = generateVerticalStreetsNumber();

        string str = "";
        for (int i = 0; i < numberHorizontalStreets - 1; i++)
        {
            str += numberVerticalStreets[i];
        }
        Debug.Log(str);

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
            int intersectionPositionsUpperSection;
            int intersectionPositionsLowerSection;
            if (currentHorizontalStreet == 0)
            {
                intersectionPositionsLowerSection = cityWidth / (numberVerticalStreets[currentHorizontalStreet] + 1);
                intersectionPositionsUpperSection = intersectionPositionsLowerSection;
            }
            else if (currentHorizontalStreet > 0 && currentHorizontalStreet < numberHorizontalStreets - 1)
            {
                Debug.Log(currentHorizontalStreet);
                intersectionPositionsUpperSection = cityWidth / (numberVerticalStreets[currentHorizontalStreet - 1] + 1);
                intersectionPositionsLowerSection = cityWidth / (numberVerticalStreets[currentHorizontalStreet] + 1);
            }
            else
            {
                intersectionPositionsUpperSection = cityWidth / (numberVerticalStreets[currentHorizontalStreet - 1] + 1);
                intersectionPositionsLowerSection = intersectionPositionsUpperSection;
            }

            currentVerticalStreet = 0;

            for (int j = 0; j < cityWidth; j++)     //here ONLY straight street sections are created (curves and intersections are done later)
            {
                if (j % intersectionPositionsUpperSection != 0 && j % intersectionPositionsLowerSection != 0 && j != cityWidth - 1)    //Put horizontal straight streets where there are not any curves/intersections
                {
                    cityMap[i, j] = generateStraightStreetType(currentHorizontalStreet, currentVerticalStreet, HORIZONTAL_STREET);
                }
                else if ((j % intersectionPositionsLowerSection == 0 || j == cityWidth - 1) && currentHorizontalStreet >= 0 && currentHorizontalStreet < numberHorizontalStreets)      //Put vertical straight streets in the section that is under the current horizontal street (bottom horizontal street has no section under it)
                {
                    for (int k = i + 1; k < i + distanceBetweenHorizontalStreets && k < cityLength - 1; k++)
                    {
                        cityMap[k, j] = generateStraightStreetType(currentHorizontalStreet, currentVerticalStreet, VERTICAL_STREET, k);
                    }
                    currentVerticalStreet++;
                }
            }
            currentHorizontalStreet++;
        }

        for (int i = 0; i < cityLength; i++)
        {
            if (i % distanceBetweenHorizontalStreets == 0)
            {
                str = "<b><color=green>";
                for (int j = 0; j < cityWidth; j++)
                {
                    str += cityMap[i, j] + "\t";
                }
                str += "</color></b>";
            }
            else
            {
                str = "";
                for (int j = 0; j < cityWidth; j++)
                {
                    if (cityMap[i, j] != 0)
                    {
                        str += "<b><color=green>"+cityMap[i, j]+"</color></b>" + "\t";
                    }
                    else
                    {
                        str += cityMap[i, j] + "\t";
                    }
                }
            }
            Debug.Log(str);
        }

        //Vertical straight street initialization

    }

    // Update is called once per frame
    void Update()
    {

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
                    numberLanes[i, j] = UnityEngine.Random.Range(1, 3);
                }
            }
        }
        return numberLanes;
    }

    private int generateStraightStreetType(int numberHorizontalStreet, int numberVerticalStreet, int streetDirection, int yPosition = -1)
    {
        if (streetDirection == VERTICAL_STREET)
        {
            if (numberVerticalStreet == 0 || numberVerticalStreet == numberVerticalStreets[numberHorizontalStreet] + 1)
            {
                if (lanesHorizontalStreets[numberHorizontalStreet] == lanesHorizontalStreets[numberHorizontalStreet + 1])
                {
                    if (lanesHorizontalStreets[numberHorizontalStreet] == 1)
                    {
                        return STRAIGHT_1LANE;
                    }
                    return STRAIGHT_2LANE;
                }
                else
                {
                    int up = numberHorizontalStreet * distanceBetweenHorizontalStreets;
                    int down = (numberHorizontalStreet + 1) * distanceBetweenHorizontalStreets;
                    int middle = (up + down) / 2;
                    Debug.Log(up + "," + middle + "," + down+" ("+yPosition+")");
                    if (yPosition > up && yPosition < middle)
                    {
                        if (lanesHorizontalStreets[numberHorizontalStreet] == 1)
                        {
                            return STRAIGHT_1LANE;
                        }
                        return STRAIGHT_2LANE;
                    }
                    else if (yPosition > middle && yPosition < down)
                    {
                        if (lanesHorizontalStreets[numberHorizontalStreet + 1] == 1)
                        {
                            return STRAIGHT_1LANE;
                        }
                        return STRAIGHT_2LANE;
                    }
                    return LANE_ADAPTOR;
                }
            }
            else
            {
                if (lanesVerticalStreets[numberHorizontalStreet, numberVerticalStreet] == 1)
                {
                    return STRAIGHT_1LANE;
                }
                return STRAIGHT_2LANE;
            }
        }
        else
        {
            if (lanesHorizontalStreets[numberHorizontalStreet] == 1)
            {
                return STRAIGHT_1LANE;
            }
            return STRAIGHT_2LANE;
        }
    }

    private GameObject generateStraightStreetPrefab(int streetType, int streetDirection)
    {
        if (streetType == STRAIGHT_1LANE)
        {
            return straightStreet1Lane;
        }
        else if (streetType == STRAIGHT_2LANE)
        {
            return straightStreet2Lane;
        }
        throw new System.Exception("INVALID STRAIGHT STREET TYPE");
    }

    private int generateIntersectionType()
    {
        return INTERSECTION_4WAY_1LANE;
    }

    private int generateCurveType()
    {
        return CURVE_1LANE;
    }

    private int generateBusStopType()
    {
        return BUS_STOP_1LANE;
    }

}
