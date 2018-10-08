using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MQ
{
    class Program
    {
        static void Main(string[] args)
        {
            if (Console.ReadLine() == "1")
            {
                LogsProd();
               // Prod();
            }
            else
            {
                LogsCust();
               // Cust();
            }
            
        }
        //消费者
        public static void Cust()
        {
          
            ConnectionFactory factory = new ConnectionFactory();
            factory.HostName = "127.0.0.1";
            //默认端口
            factory.Port = 5672;
            using (IConnection conn = factory.CreateConnection())
            {
                using (IModel channel = conn.CreateModel())
                {
                    //在MQ上定义一个持久化队列，如果名称相同不会重复创建
                    channel.QueueDeclare("MyRabbitMQ", true, false, false, null);

                    //输入1，那如果接收一个消息，但是没有应答，则客户端不会收到下一个消息
                    channel.BasicQos(0, 1, false);
                   
                    Console.WriteLine("Listening...");

                    //在队列上定义一个消费者
                    EventingBasicConsumer consumer = new EventingBasicConsumer(channel);
                    //消费队列，并设置应答模式为程序主动应答
                    channel.BasicConsume("MyRabbitMQ", false, consumer);
                    //消息持久化(保障RabbitMQ服务端出错 消息队列仍不会丢失)
                    var Properties=channel.CreateBasicProperties();
                    Properties.Persistent = true;
                        consumer.Received += (mode, ea) =>
                        {
                            //阻塞函数，获取队列中的消息

                            byte[] bytes = ea.Body;
                            string str = Encoding.UTF8.GetString(bytes);
                            Thread.Sleep(1000);
                            Console.WriteLine("队列消息:" + str.ToString());
                            //回复确认
                            channel.BasicAck(ea.DeliveryTag, false);
                        };
                    Console.ReadKey();
                    
                }
            }
          
        }
        //生产者
        public static void Prod()
        {
            ConnectionFactory connection = new ConnectionFactory
            {
                HostName = "127.0.0.1",
                Port = 5672
            };
            using (IConnection conn = connection.CreateConnection())
            {
                using (IModel im = conn.CreateModel())
                {
                    im.QueueDeclare("MyRabbitMQ", true, false, false, null);
                    while (true)
                    {
                        string message = string.Format("Message_{0}", new Random().Next(1, 100000));
                        byte[] buffer = Encoding.UTF8.GetBytes(message);
                        IBasicProperties properties = im.CreateBasicProperties();
                        properties.DeliveryMode = 2;
                        im.BasicPublish("", "MyRabbitMQ", properties, buffer);
                        Thread.Sleep(1000);
                        Console.WriteLine("消息发送成功:" + message);
                    }
                }
            }
          
        }
        /// <summary>
        /// 消息交换机
        /// </summary>
        public static void LogsProd()
        {
            ConnectionFactory connection = new ConnectionFactory
            {
                HostName = "127.0.0.1",

            };
            using (IConnection conn = connection.CreateConnection())
            using (var channel= conn.CreateModel())
                {
                channel.ExchangeDeclare("logs", ExchangeType.Fanout);
                while (true)
                {
                    var message = string.Format("随机数为{0},当前时间为:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),new Random().Next(1,10000));
                    var body = Encoding.UTF8.GetBytes(message);
                    Thread.Sleep(1000);
                    channel.BasicPublish("logs", "", null,body);
                    Console.WriteLine("输出的logs为:{0}",message);
                }
            }
           
        }
        /// <summary>
        /// 交换机消息接收
        /// </summary>
        public static void LogsCust()
        {
            ConnectionFactory factory = new ConnectionFactory
            {
                HostName = "127.0.0.1",
            };
            using (IConnection conn = factory.CreateConnection())
            using (IModel channel = conn.CreateModel())
            {
                    //在MQ上定义一个持久化队列，如果名称相同不会重复创建
                    channel.ExchangeDeclare("logs", ExchangeType.Fanout);
                    var queryname = channel.QueueDeclare().QueueName;//非持久，排他，自动删除队列

                    channel.QueueBind(queryname, "logs", "");
                    Thread.Sleep(1000);
                    Console.WriteLine("开始接收Logs信息...");
                    EventingBasicConsumer consumer = new EventingBasicConsumer(channel);
                    consumer.Received += (model, ea) =>
                    {
                        var body = ea.Body;
                        var message = string.Format("接收消息的时间为：{0},接收数据为：{1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),Encoding.UTF8.GetString(body));
                        Console.WriteLine(message);
                    };
                    channel.BasicConsume(queryname,true, consumer);
                    Console.ReadKey();
            }
            
        }

    }
}
