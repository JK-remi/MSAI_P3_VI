using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class UdpReceiver
{
    private Thread receiveThread;
    private UdpClient client;
    private int port;
    private bool isRunning = false;

    // 데이터를 수신했을 때 발생하는 이벤트
    public delegate void DataReceivedHandler(string data);
    public event DataReceivedHandler OnDataReceived;

    public UdpReceiver(int port)
    {
        this.port = port;
    }

    public void Start()
    {
        if (!isRunning)
        {
            isRunning = true;
            receiveThread = new Thread(new ThreadStart(ReceiveData));
            receiveThread.IsBackground = true;
            receiveThread.Start();
        }
    }

    public void Stop()
    {
        isRunning = false;
        if (client != null)
        {
            client.Close();
            client = null;
        }
        if (receiveThread != null)
        {
            receiveThread.Abort();
            receiveThread = null;
        }
    }

    private void ReceiveData()
    {
        client = new UdpClient(port);
        while (isRunning)
        {
            try
            {
                IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = client.Receive(ref anyIP);
                string receivedData = Encoding.UTF8.GetString(data);
                if (OnDataReceived != null)
                {
                    OnDataReceived(receivedData);
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError(ex.ToString());
            }
        }
    }

    public void Dispose()
    {
        Stop();
    }
}
