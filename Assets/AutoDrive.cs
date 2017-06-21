using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Vehicles.Car;

public class AutoDrive : MonoBehaviour
{
    private const float SpeedSensitivity = 10f;
    private const int RoadCount = 500;
    private const float InitMaxSpeed = 15f * 2.23693629f;  // MPH
    private const float StopAccelRate = 0.8f;
    private const float PreviewRate = 0.3f;
    private const float IgnoreAngleRate = 0.5f;
    private const float CenterOffset = 1f;

    private CarController m_car;

    public int CurrentRoad = 5;
    private float[] MaxSpeedTable = new float[RoadCount];
    private int PreviewCount = 0;

    private float accel = 0;
    private float steering = 0;
    private float sensitivity = 1f;
    private float dead = 0.001f;

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
            if (CurrentRoad >= RoadCount)
                return;
            GameObject road = GameObject.Find("road (" + (CurrentRoad + PreviewCount).ToString() + ")");
            float RoadRot = NormalizeAngle(road.transform.eulerAngles.y);
            float CarRot = NormalizeAngle(m_car.transform.eulerAngles.y);
            float Angle = RoadRot - CarRot;
            if (Mathf.Abs(m_car.CurrentSpeed * IgnoreAngleRate) < Mathf.Abs(Angle))
                Angle = Angle > 0 ? (int)(Angle - m_car.CurrentSpeed * IgnoreAngleRate) :
                                    (int)(Angle + m_car.CurrentSpeed * IgnoreAngleRate);
            else
                Angle = (int)(Angle * 0.1);
            int action = (Angle > 0 ? 1 : (Angle == 0 ? 0 : -1));
            float steering, accel;
            if (action == -1) // Turn Left
            {
                steering = SteeringSimulation("LEFT");
                //if (m_car.CurrentSpeed < MaxSpeedTable[CurrentRoad] * StopAccelRate)
                //    accel = AccelSimulation("FORWARD");
                //else
                    accel = AccelSimulation("NULL");
            }
            else if (action == 1)  // Turn Right
            {
                steering = SteeringSimulation("RIGHT");
                //if (m_car.CurrentSpeed < MaxSpeedTable[CurrentRoad] * StopAccelRate)
                //    accel = AccelSimulation("FORWARD");
                //else
                    accel = AccelSimulation("NULL");
            }
            else // Forward
            {
                switch (CenterCheck())
                {
                    case -1:
                        Debug.Log("LEFT");
                        steering = SteeringSimulation("LEFT");
                        accel = AccelSimulation("NULL");
                        break;
                    case 1:
                        Debug.Log("RIGHT");
                        steering = SteeringSimulation("RIGHT");
                        accel = AccelSimulation("NULL");
                        break;
                    default:
                        Debug.Log("HERE");
                        steering = SteeringSimulation("NULL");
                        if (m_car.CurrentSpeed < MaxSpeedTable[CurrentRoad] * StopAccelRate)
                            accel = AccelSimulation("FORWARD");
                        else
                            accel = AccelSimulation("NULL");
                        break;
                }
            }
            m_car.Move(steering, accel, accel, 0f);
            //CurrentRoad = GetCurrentRoad();
            PreviewCount = (int)(m_car.CurrentSpeed * PreviewRate);
        }
        //else
        //    CurrentRoad = 5;
    }

    private float AccelSimulation(string KEY)
    {
        float target = KEY.Equals("FORWARD") ? 1 : (KEY.Equals("BACKWARD") ? -1 : 0);
        accel = Mathf.MoveTowards(accel,
                      target, sensitivity * Time.deltaTime);
        return (Mathf.Abs(accel) < dead) ? 0f : target * 0.5f;//accel;
    }

    private float SteeringSimulation(string KEY)
    {
        float target = KEY.Equals("LEFT") ? -1 : KEY.Equals("RIGHT") ? 1 : 0;
        steering = Mathf.MoveTowards(steering,
                      target, sensitivity * Time.deltaTime);
        return (Mathf.Abs(steering) < dead) ? 0f : target * 0.5f;//steering;
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
        if (Angle < 0)
            while (Angle < -180)
                Angle += 360;
        else if (Angle > 0)
            while (Angle > 180)
                Angle -= 360;
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
            if (Mathf.Abs(car_x - x) > CenterOffset)
                return location;
        }
        else
        {
            float a = (road_z - z) / (road_x - x);
            float b = z - a * x;
            int location = (a * car_x - car_z + b) * (a * x1 - z1 + b) < 0 ? -1 : 1;
            float distance = Mathf.Abs(a * car_x - car_z + b) / Mathf.Sqrt(a * a + 1) * location;
            if (Mathf.Abs(distance) > CenterOffset)
                return location;
        }
        return 0;
    }

    private bool OutsideCheck()
    {
        return false;
    }
}
