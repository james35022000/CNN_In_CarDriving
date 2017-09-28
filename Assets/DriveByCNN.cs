using System;
using System.Collections;
using System.Collections.Generic;
using UnityStandardAssets.Vehicles.Car;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class DriveByCNN : MonoBehaviour
{

    static Socket listener;
    static string LocalHost = "127.0.0.1";
    static int port = 8787;
    static string data = "0000";
    static int width = Screen.width;
    static int height = Screen.height;
    static Texture2D texture2d;
    CarData carData;


    private CarController m_car;
    private int CurrentRoad = 11;

    private float accel = 0;
    private float steering = 0;
    private float sensitivity = 1f;
    private float dead = 0.001f;

    int screanShotCnt = 0;

    private class CarData
    {
        public CarData(byte[] screenShot, float speed)
        {
            this.screenShot = screenShot;
            this.speed = speed;
        }

        public byte[] screenShot;
        public float speed;
    }

    // Use this for initialization
    void Start()
    {
        (new Thread(ServerListening)).Start();
        m_car = GameObject.Find("Car").GetComponent<CarController>();
        texture2d = new Texture2D(width, height, TextureFormat.RGB24, false);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        //if (screanShotCnt % 3 == 0)
            StartCoroutine(ScreenShot());
        //screanShotCnt++;
        ControlCar(data);
        if (CurrentRoad >= 490)
            GameObject.Find("Road").GetComponent<RoadGenScript>().ClickBtn();
        CurrentRoad = GetCurrentRoad();
    }
    
    private void ServerListening()
    {
        while (true)
        {
            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                listener.Bind(new IPEndPoint(IPAddress.Parse(LocalHost), port));
                listener.Listen(100);
                while (true)
                {
                    Debug.Log("Waiting for client connect.");
                    Socket handler = listener.Accept();
                    byte[] bytes = new byte[1024];
                    int count = handler.Receive(bytes);
                    data = Encoding.ASCII.GetString(bytes, 0, count);
                    if (data == "CNN_CarDriving_Client")
                    {
                        Debug.Log("Connect!");
                        while (true)
                        {
                            Thread.Sleep(100);
                            handler.Send(carData.screenShot);
                            carData.screenShot = null;
                            //Thread.Sleep(50);
                            handler.Receive(bytes);
                            handler.Send(Encoding.ASCII.GetBytes(carData.speed + ""));
                            count = handler.Receive(bytes);
                            data = Encoding.ASCII.GetString(bytes, 0, count);
                            Debug.Log(carData.speed + " : " + data);
                        }
                    }
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                }
            }
            catch (Exception e)
            {
                listener.Shutdown(SocketShutdown.Both);
                listener.Close();
                Debug.Log(e.Message.ToString());
            }
        }
    }

    private void ControlCar(string direction)
    {
        steering = SteeringSimulation("NULL");
        accel = AccelSimulation("NULL");
        if(direction[0] == '1')
        {
            steering = SteeringSimulation("LEFT");
        }
        if(direction[1] == '1')
        {
            steering = SteeringSimulation("RIGHT");
        }
        if(direction[2] == '1')
        {
            accel = AccelSimulation("FORWARD");
        }
        if(direction[3] == '1')
        {
            accel = AccelSimulation("BACKWARD");
        }

        m_car.Move(steering, accel, accel, 0f);
        /*switch(direction)
        {
            case "0000":
                break;
            case "0001":
                break;
            case "0010":
                break;
            case "0100":
                break;
            case "1000":
                break;
            case "0101":
                break;
            case "1001":
                break;
            case "0110":
                break;
            case "1010":
                break;
        }*/
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
        carData = new CarData(newTexture2d.EncodeToPNG(), GameObject.Find("Car").GetComponent<CarController>().CurrentSpeed);
        Destroy(newTexture2d);
        newTexture2d = null;
        GC.Collect();
    }
}
