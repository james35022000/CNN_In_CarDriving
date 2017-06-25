using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Vehicles.Car;

public class AutoDrive : MonoBehaviour
{
    public struct Status
    {
        public CarController.CarStatus carStatus;
        public Vector3 position;
        public Vector3 rotation;
    }

    private const float SpeedSensitivity = 10f;
    private const int RoadCount = 500;
    private const float InitMaxSpeed = 100f * 2.23693629f;  // MPH
    private const float StopAccelRate = 0.8f;
    private const float PreviewRate = 0.3f;
    private const float IgnoreAngleRate = 0.5f;
    private const float CenterOffset = 1f;

    private CarController m_car;

    public int CurrentRoad = 11;
    private float[] MaxSpeedTable = new float[RoadCount];
    private int PreviewCount = 0;
    private float DirectionDegree = 0;
    private float MaxDirectionDegree = 0.2f;

    private float accel = 0;
    private float steering = 0;
    private float sensitivity = 1f;
    private float dead = 0.001f;


    private Status[] status = new Status[RoadCount];

    // Use this for initialization
    void Start()
    {
        for (int i = 0; i < RoadCount; i++)
            MaxSpeedTable[i] = InitMaxSpeed;
        m_car = GameObject.Find("Car").GetComponent<CarController>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void FixedUpdate()
    {
        if (GameObject.Find("Button").GetComponent<RoadGenScript>().isGenFinished == true)
        {
            if (CurrentRoad >= RoadCount - 5)
            {
                GameObject.Find("Button").GetComponent<RoadGenScript>().ClickBtn();
                return;
            }
            if(OutsideCheck())
            {
                GameObject.Find("Button").GetComponent<RoadGenScript>().ClickBtn();
                //LoadStatus(CurrentRoad - 20);
                return;
            }

            DirectionDegree = 0;

            SaveStatus();
            AngleCheck();
            CenterCheck();
            DisplayUI();
            //Debug.Log(DirectionDegree.ToString());

            if (DirectionDegree >= MaxDirectionDegree)
            {
                //Debug.Log("RIGHT");
                steering = SteeringSimulation("RIGHT");
                if ((int)m_car.CurrentSpeed == 0)
                    accel = AccelSimulation("FORWARD");
                else
                    accel = AccelSimulation("NULL");
            }
            else if (-DirectionDegree >= MaxDirectionDegree)
            {
                //Debug.Log("LEFT");
                steering = SteeringSimulation("LEFT");
                if ((int)m_car.CurrentSpeed == 0)
                    accel = AccelSimulation("FORWARD");
                else
                    accel = AccelSimulation("NULL");
            }
            else
            {
                if (m_car.CurrentSpeed < MaxSpeedTable[CurrentRoad] * StopAccelRate)
                    accel = AccelSimulation("FORWARD");
                else
                    accel = AccelSimulation("NULL");
                steering = SteeringSimulation("NULL");
            }

            m_car.Move(steering, accel, accel, 0f);
        }
    }

    private float AccelSimulation(string KEY)
    {
        float target = KEY.Equals("FORWARD") ? 1 : (KEY.Equals("BACKWARD") ? -1 : 0);
        accel = Mathf.MoveTowards(accel,
                      target, sensitivity * Time.deltaTime);
        return (Mathf.Abs(accel) < dead) ? 0f : target * 0.7f;//accel;
    }

    private float SteeringSimulation(string KEY)
    {
        float target = KEY.Equals("LEFT") ? -1 : KEY.Equals("RIGHT") ? 1 : 0;
        steering = Mathf.MoveTowards(steering,
                      target, sensitivity * Time.deltaTime);
        return (Mathf.Abs(steering) < dead) ? 0f : target * 0.7f;//steering;
    }

    private int GetCurrentRoad()
    {
        GameObject car = GameObject.Find("Car");
        GameObject nextRoad = GameObject.Find("road (" + (CurrentRoad + 1).ToString() + ")");
        GameObject curRoad = GameObject.Find("road (" + CurrentRoad.ToString() + ")");
        if (Vector3.Distance(car.transform.position, nextRoad.transform.position) <  
            Vector3.Distance(car.transform.position, curRoad.transform.position))
            return CurrentRoad + 1;
        return CurrentRoad;
    }

    private float NormalizeAngle(float Angle)
    {
        while (Angle < -180f)
            Angle += 360f;
        while (Angle >= 180f)
            Angle -= 360f;
        return Angle;
    }

    private int CenterCheck()
    {
        Vector3 RoadScale = GameObject.Find("Button").GetComponent<RoadGenScript>().getScale();
        GameObject road = GameObject.Find("road (" + (CurrentRoad).ToString() + ")");
        float car_x = m_car.transform.position.x, car_z = m_car.transform.position.z;
        float road_x = road.transform.position.x, road_z = road.transform.position.z;
        float x = road_x - (RoadScale.z / 2) * Mathf.Sin((road.transform.eulerAngles.y * Mathf.PI / 180));
        float z = road_z - (RoadScale.z / 2) * Mathf.Cos((road.transform.eulerAngles.y * Mathf.PI / 180));
        float x1 = road_x - (RoadScale.z / 2) * Mathf.Sin(((road.transform.eulerAngles.y + 2) * Mathf.PI / 180));
        float z1 = road_z - (RoadScale.z / 2) * Mathf.Cos(((road.transform.eulerAngles.y + 2) * Mathf.PI / 180));
        if (road_x == x)
        {
            int location = car_x < x ? 1 : -1;
            if (location == 1)
            {
                DirectionDegree += Mathf.Abs(car_x - x) / 3;
            }
            else
            {
                DirectionDegree -= Mathf.Abs(car_x - x) / 3;
            }
            if (Mathf.Abs(car_x - x) > CenterOffset)
                return location;
        }
        else
        {
            float a = (road_z - z) / (road_x - x);
            float b = z - a * x;
            int location = (a * car_x - car_z + b) * (a * x1 - z1 + b) < 0 ? -1 : 1;
            float distance = Mathf.Abs(a * car_x - car_z + b) / Mathf.Sqrt(a * a + 1);
            if (location == 1)
            {
                //Debug.Log("Distance:" + distance);
                DirectionDegree += distance / 3;
            }
            else
            {
                //Debug.Log("-Distance:" + distance);
                DirectionDegree -= distance / 3;
            }
            if (Mathf.Abs(distance) > CenterOffset)
                return location;
        }
        return 0;
    }

    private void AngleCheck()
    {
        //Debug.Log("CurrentRoad:" + CurrentRoad);
        float CarRot = NormalizeAngle(m_car.transform.eulerAngles.y);
        for (int i = 0; i < 6; i++)
        {
            GameObject road = GameObject.Find("road (" + (CurrentRoad + i).ToString() + ")");
            float RoadRot = NormalizeAngle(road.transform.eulerAngles.y);
            float Angle = (int)(RoadRot - CarRot);
            int action = (Angle > 0 ? 1 : (Angle == 0 ? 0 : -1));
            if (Mathf.Abs(Angle) > 180)
                Angle = RoadRot < 0 ? (RoadRot + 360 - CarRot) : (RoadRot - 360 - CarRot);
            if(Angle > 0)
            {
                //Debug.Log("Angle:" + Angle);
            }
            else if(Angle < 0)
            {
                //Debug.Log("-Angle:" + Angle);
            }
            DirectionDegree += Angle / 90 * ((i >= 0 && i <= 1) ? 5 : (i >= 2 || i <= 4) ? 3 : 2);
        }
    }

    private bool OutsideCheck()
    {
        if (m_car.transform.position.y < 0.2)
            return true;
        return false;
    }

    private void DisplayUI()
    {
        GameObject RightRate = GameObject.Find("RightRate");
        GameObject LeftRate = GameObject.Find("LeftRate");
        if (DirectionDegree > 0)
        {
            LeftRate.transform.localScale = new Vector3(0, 1, 1);
            RightRate.transform.localScale = new Vector3(DirectionDegree/8, 1, 1);
            RightRate.transform.position = new Vector3(400 + 75 * DirectionDegree/8, 750, 0);
        }
        else if(DirectionDegree < 0)
        {
            RightRate.transform.localScale = new Vector3(0, 1, 1);
            LeftRate.transform.localScale = new Vector3(-DirectionDegree/8, 1, 1);
            LeftRate.transform.position = new Vector3(400 + 75 * DirectionDegree/8, 750, 0);
        }
    }

    private void SaveStatus()
    {
        status[CurrentRoad].carStatus = GameObject.Find("Car").GetComponent<CarController>().SaveCarStatus();
        status[CurrentRoad].position = GameObject.Find("Car").transform.position;
        status[CurrentRoad].rotation = GameObject.Find("Car").transform.eulerAngles;
    }

    private void LoadStatus(int RoadNum)
    {
        GameObject.Find("Car").GetComponent<CarController>().LoadCarStatus(status[RoadNum].carStatus);
        GameObject.Find("Car").transform.position = new Vector3(status[RoadNum].position.x,
                                                                status[RoadNum].position.y,
                                                                status[RoadNum].position.z);
        GameObject.Find("Car").transform.eulerAngles = new Vector3(status[RoadNum].rotation.x,
                                                                   status[RoadNum].rotation.y,
                                                                   status[RoadNum].rotation.z);

    }
}

