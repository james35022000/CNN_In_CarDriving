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
    static byte[][] screenshot = new byte[1][];

    // Use this for initialization
    void Start()
    {
        texture2d = new Texture2D(width, height, TextureFormat.RGB24, false);
        (new Thread(ServerListening)).Start();
    }

    // Update is called once per frame
    void Update()
    {
        texture2d.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        texture2d.Apply();
        screenshot[0] = texture2d.EncodeToPNG();
    }
    
    private static void ServerListening()
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
                        int cnt = 0;
                        while (true)
                        {
                            Thread.Sleep(1000);
                            Debug.Log("Take ScreenShot NO." + (++cnt).ToString());
                            handler.Send(screenshot[0]);
                            count = handler.Receive(bytes);
                            data = Encoding.ASCII.GetString(bytes, 0, count);
                            ControlCar(data);
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
