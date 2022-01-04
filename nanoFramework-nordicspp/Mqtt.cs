using nanoFramework.M2Mqtt;
using nanoFramework.M2Mqtt.Messages;
using System;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace nanoframework_nordicspp
{
   
    public static class MQTT
    {
        private static MqttClient client = null;
        private const string BrokerAddress = "broker.emqx.io";
        private const string _clientID = "12345";
        private static int count = 0;
        private static void Init()
        {
            // Using TLS/SSL
            //X509Certificate userTrustRoot = new X509Certificate(Resource.GetBytes(Resource.BinaryResources.usertrust));
            //client = new MqttClient(BrokerAddress,
            //8883,
            //true,
            //userTrustRoot,
            //null,
            //MqttSslProtocols.TLSv1_2);

            // Non TLS
            client = new MqttClient(BrokerAddress,
            1883,
            false,
            null,
            null,
            MqttSslProtocols.None);

            client.MqttMsgPublishReceived += Client_MqttMsgPublishReceived;
            client.MqttMsgSubscribed += Client_MqttMsgSubscribed;
        }

        public static void Publish(byte[] message)
        {
            count++;
            if(client == null)
            {
                Debug.WriteLine("Init MQTT");
                Init();
            }

            if( ! client.IsConnected)
            {
                Debug.WriteLine("Connect to MQTT");
                var conn = client.Connect(_clientID);
                
             
            }
            Debug.WriteLine($"Publish to MQTT Count {count}");
            client.Publish("/Esp32/Test"+_clientID, message);

        }


        private static void Client_MqttMsgSubscribed(object sender, MqttMsgSubscribedEventArgs e)
        {
            Debug.WriteLine("Client_MqttMsgSubscribed ");
        }

        private static void Client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            string topic = e.Topic;

            string message = Encoding.UTF8.GetString(e.Message, 0, e.Message.Length);

            Debug.WriteLine("Publish Received Topic:" + topic + " Message:" + message);
        }
    }
}
