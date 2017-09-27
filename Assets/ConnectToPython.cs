using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class ConnectToPython : MonoBehaviour
{

    static Socket listener;
    static string LocalHost = "127.0.0.1";
    static int port = 8787;
    static string data = null;
    static int width = Screen.width;
    static int height = Screen.height;
    static Texture2D texture2d;

    public TestDataGen.TestData[] testData = null;
    

    // Use this for initialization
    void Start()
    {
        (new Thread(ServerListening)).Start();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void AddTestData(TestDataGen.TestData[] t)
    {
        testData = t;
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
                        while (handler.Connected)
                        {
                            if (testData != null)
                            {
                                for (int i = 0; i < 400; i++)
                                {
                                    if (testData[i] == null)
                                        break;
                                    handler.Send(testData[i].screenShot);
                                    testData[i].screenShot = null;
                                    handler.Receive(bytes);
                                    handler.Send(Encoding.ASCII.GetBytes(testData[i].speed + " " + testData[i].direction));
                                    handler.Receive(bytes);
                                    testData[i] = null;
                                }
                                testData = null;
                                GC.Collect();
                            }
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

    private static void ControlCar(string direction)
    {

    }
}
