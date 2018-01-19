using System;
using System.Collections;
using System.Collections.Generic;
using UnityStandardAssets.Vehicles.Car;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class DriveByDQN : MonoBehaviour
{

    static string LocalHost = "192.168.1.49";
    static int port = 8787;
    static string data = "-1";
    static int width = Screen.width;
    static int height = Screen.height;
    static int CLIENT_GET = 2;
    static int CLIENT_WAIT = 1;
    static int NOT_YET = 0;
    static Texture2D texture2d;
    CarData carData;
    private float Reward;
    private int Done;


    private CarController m_car;
    private AutoDrive autoDrive;
    private Text m_Reward;
    private int CurrentRoad = 11;

    private float accel = 0;
    private float steering = 0;
    private float sensitivity = 1f;
    private float dead = 0.001f;
    public bool outside = false;

    private Thread thread1, thread2;
    private int current_thread = 1;

    int screanShotCnt = 0;

    private class CarData
    {
        public CarData(byte[] screenShot, float speed, bool backward)
        {
            this.screenShot = screenShot;
            this.speed = speed;
            if (backward)
                this.speed = -this.speed;
        }

        public byte[] screenShot;
        public float speed;
    }

    // Use this for initialization
    void Start()
    {
        thread1 = new Thread(ServerListening);
        thread1.IsBackground = true;
        thread1.Start();
        m_car = GameObject.Find("Car").GetComponent<CarController>();
        autoDrive = GameObject.Find("Car").GetComponent<AutoDrive>();
        m_Reward = GameObject.Find("m_Reward").GetComponent<Text>();
        texture2d = new Texture2D(width, height, TextureFormat.RGB24, false);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        StartCoroutine(ScreenShot());
        
        ControlCar(Convert.ToInt32(data));
        
        if (CurrentRoad >= 990)
        {
            data = "-1";
            CurrentRoad = 5;
            //ResetCar(CurrentRoad);
            GameObject.Find("Road").GetComponent<RoadGenScript>().ClickBtn();
        }
            
        CurrentRoad = GetCurrentRoad();
        Reward = GetReward() * 30;
        m_Reward.text = GetReward().ToString();
        if (outside == true && Done != CLIENT_GET)
        {
            data = "-1";
            Done = CLIENT_WAIT;
        }
        else if (Done == CLIENT_GET)
        {
            data = "-1";
            ResetCar(CurrentRoad);
            Done = NOT_YET;
        }
        else
            Done = NOT_YET;
    }

    private void ServerListening()
    {
        Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        try
        {
            listener.Bind(new IPEndPoint(IPAddress.Parse(LocalHost), port + current_thread - 1));
            listener.Listen(1);
            while (true)
            {
                Debug.Log("Waiting for client connect.(" + (port + current_thread - 1) + ")");
                Socket handler = listener.Accept();
                if (current_thread == 1)
                {
                    if (thread2 != null && thread2.IsAlive)
                    {
                        thread2.Abort();
                        thread2 = null;
                    }
                    current_thread = 2;
                    thread2 = new Thread(ServerListening);
                    thread2.IsBackground = true;
                    thread2.Start();
                }
                else
                {
                    if (thread1 != null && thread1.IsAlive)
                    {
                        thread1.Abort();
                        thread1 = null;
                    }
                    current_thread = 1;
                    thread1 = new Thread(ServerListening);
                    thread1.IsBackground = true;
                    thread1.Start();
                }
                handler.ReceiveTimeout = 300;
                handler.SendTimeout = 300;
                byte[] bytes = new byte[1024];
                int count;
                Debug.Log("Connect!");
                while (true)
                {
                    Thread.Sleep(100);
                    handler.Send(carData.screenShot);
                    carData.screenShot = null;
                    count = handler.Receive(bytes);
                    if (bytes != null)
                        if (!Encoding.ASCII.GetString(bytes, 0, count).Equals("GetInfo"))
                        {
                            handler.Send(Encoding.ASCII.GetBytes("Error"));
                            break;
                        }
                    handler.Send(Encoding.ASCII.GetBytes(carData.speed + "," + Reward + "," + Done));
                    //handler.Send(Encoding.ASCII.GetBytes(carData.speed + "," + Reward + "," + Done + "," + autoDrive.direction));
                    count = handler.Receive(bytes);
                    if (count > 10)
                    {
                        handler.Send(Encoding.ASCII.GetBytes("Error"));
                        break;
                    }
                    if (Done == CLIENT_WAIT)
                        Done = CLIENT_GET;
                    if (bytes != null)
                        data = Encoding.ASCII.GetString(bytes, 0, count);
                }
                handler.Close();
                handler = null;
            }
        }
        catch (Exception e)
        {
            Debug.Log(e.Message.ToString());
            listener.Shutdown(SocketShutdown.Both);
            listener.Close();
        }
    }

    /**
     * No action : 0
     * Forward : 1
     * Left : 2
     * Right : 3
     * Forward + Left : 4
     * Forward + Right : 5
     * Backward : 6
     * Backward + Left : 7
     * Backward + Right : 8
     **/
    private void ControlCar(int direction)
    {
        steering = SteeringSimulation("NULL");
        accel = AccelSimulation("NULL");

        switch(direction)
        {
            case 0:
                Debug.Log("N");
                break;
            case 1:
                Debug.Log("F");
                accel = AccelSimulation("FORWARD");
                break;
            case 2:
                Debug.Log("L");
                steering = SteeringSimulation("LEFT");
                break;
            case 3:
                Debug.Log("R");
                steering = SteeringSimulation("RIGHT");
                break;
            case 4:
                Debug.Log("FL");
                accel = AccelSimulation("FORWARD");
                steering = SteeringSimulation("LEFT");
                break;
            case 5:
                Debug.Log("FR");
                accel = AccelSimulation("FORWARD");
                steering = SteeringSimulation("RIGHT");
                break;
            case 6:
                Debug.Log("B");
                accel = AccelSimulation("BACKWARD");
                break;
            case 7:
                Debug.Log("BL");
                accel = AccelSimulation("BACKWARD");
                steering = SteeringSimulation("LEFT");
                break;
            case 8:
                Debug.Log("BR");
                accel = AccelSimulation("BACKWARD");
                steering = SteeringSimulation("RIGHT");
                break;
        }
        m_car.Move(steering, accel, accel, 0f);
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

    private float GetReward()
    {
        outside = false;
        if (m_car.transform.position.y < 0.2)
        {
            outside = true;
            return -2;
        }
        float Distance_Reward, Speed_Reward;
        Vector3 RoadScale = GameObject.Find("Road").GetComponent<TestDataGen>().getScale();
        GameObject road = GameObject.Find("road (" + (CurrentRoad).ToString() + ")");
        float car_x = m_car.transform.position.x, car_z = m_car.transform.position.z;
        float road_x = road.transform.position.x, road_z = road.transform.position.z;
        float x = road_x - (RoadScale.z / 2) * Mathf.Sin((road.transform.eulerAngles.y * Mathf.PI / 180));
        float z = road_z - (RoadScale.z / 2) * Mathf.Cos((road.transform.eulerAngles.y * Mathf.PI / 180));
        float x1 = road_x - (RoadScale.z / 2) * Mathf.Sin(((road.transform.eulerAngles.y + 2) * Mathf.PI / 180));
        float z1 = road_z - (RoadScale.z / 2) * Mathf.Cos(((road.transform.eulerAngles.y + 2) * Mathf.PI / 180));

        if (road_x == x)
        {
            float distance = Mathf.Abs(car_x - x);
            Distance_Reward = (RoadScale.x / 2 - distance) / (RoadScale.x / 2) * 1.5f;
            if (Distance_Reward < 0)
                Distance_Reward = 0;
        }
        else
        {
            float a = (road_z - z) / (road_x - x);
            float b = z - a * x;
            float distance = Mathf.Abs(a * car_x - car_z + b) / Mathf.Sqrt(a * a + 1);
            Distance_Reward = (RoadScale.x / 2 - distance) / (RoadScale.x / 2) * 1.5f;
            if (Distance_Reward < 0)
                Distance_Reward = 0;
        }
        float speed_rate = carData.speed / m_car.MaxSpeed;
        if (carData.speed < 1)
            return 0;

        Speed_Reward = speed_rate > 1.5f? 1.5f : (carData.speed < 0? 0f : speed_rate);

        return Distance_Reward + Speed_Reward;
    }

    private float AccelSimulation(string KEY)
    {
        float target = KEY.Equals("FORWARD") ? 1 : (KEY.Equals("BACKWARD") ? -1 : 0);
        accel = Mathf.MoveTowards(accel,
                      target, sensitivity * Time.deltaTime);
        return (Mathf.Abs(accel) < dead) ? 0f : target * 0.7f;  // accel;
    }

    private float SteeringSimulation(string KEY)
    {
        float target = KEY.Equals("LEFT") ? -1 : KEY.Equals("RIGHT") ? 1 : 0;
        steering = Mathf.MoveTowards(steering,
                      target, sensitivity * Time.deltaTime);
        return (Mathf.Abs(steering) < dead) ? 0f : target * 0.7f;  // steering;
    }

    IEnumerator ScreenShot()
    {
        yield return new WaitForEndOfFrame();
        texture2d.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        texture2d.Apply();
        Texture2D newTexture2d = new Texture2D(50, 50, TextureFormat.RGB24, false);
        for (int i = 0; i < 50; i++)
            for (int j = 0; j < 50; j++)
                newTexture2d.SetPixel(i, j, texture2d.GetPixel(i * width / 50, j * height / 50));
        newTexture2d.Apply();
        carData = new CarData(newTexture2d.EncodeToPNG(), GameObject.Find("Car").GetComponent<CarController>().CurrentSpeed, GameObject.Find("Car").GetComponent<CarController>().Backward);
        Destroy(newTexture2d);
        newTexture2d = null;
        GC.Collect();
    }

    public void ResetCar(int CurrentRoad)
    {
        GameObject.Find("Car").GetComponent<CarController>().ResetCar();
        GameObject.Find("Car").transform.position = new Vector3(
                                                            GameObject.Find("road (" + CurrentRoad + ")").transform.position.x,
                                                            0.3075473F,
                                                            GameObject.Find("road (" + CurrentRoad + ")").transform.position.z);
        GameObject.Find("Car").transform.rotation = GameObject.Find("road (" + CurrentRoad + ")").transform.rotation;
    }
}
