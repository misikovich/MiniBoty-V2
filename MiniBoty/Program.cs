using System;
using System.IO;

namespace MiniBoty
{
    class Program
    {
        public static bool successfulConnection;
        private static bool successfulSetup;
        public static string channel;
        public static bool isLogging = false;

        static void Main(string[] args)
        {
            BotBody bot = new BotBody();
            Random random = new Random();
            string tempFileName = "rnd" + random.Next(10000);
            SRPData Data = new SRPData(tempFileName, ':'); //костыль
            

            if (args.Length == 0)
            {
                Data = new SRPData($"Config.txt", ':'); //load config file
                string loadedChannelName = Data.ParseDataString("channel_name");
                do
                {
                    if (!string.IsNullOrEmpty(loadedChannelName))
                    {
                        Console.Write($"Connect to '{loadedChannelName}'? (Press Enter to yes or write manually): ");
                        channel = Console.ReadLine();

                        if (string.IsNullOrEmpty(channel))
                        {
                            channel = loadedChannelName;
                        }
                        successfulSetup = true;
                    }
                    else
                    {
                        Console.Write("Channel to connect: ");
                        channel = Console.ReadLine();

                        if (string.IsNullOrEmpty(channel))
                        {
                            successfulSetup = false;
                            Console.Write("|ERROR| Channel name can`t be empty!");
                        }
                        else
                        {
                            successfulSetup = true;
                        }
                    }
                } while (!successfulSetup);
            }
            else if (args.Length == 1)
            {
                File.Delete(tempFileName);
                channel = args[0];
                Console.WriteLine(args[0]);
            }
            else
            {
                File.Delete(tempFileName);
                isLogging = (bool)Convert.ChangeType(args[1], isLogging.GetType());
                channel = args[0];
                Console.WriteLine(args[0]);
            }

            do //trying to connect
            {
                if (args.Length == 0)
                {
                    Data.SaveData(new string[] { "channel_name" }, new string[] { channel });
                }
                Console.Clear();
                Console.WriteLine("Connecting to " + channel + "...");
                successfulConnection = true;
            } while (!successfulConnection);

            Console.Clear();
            Console.WriteLine("Connecting to " + channel);

            bot.Connect(channel);
            Console.ReadLine();
            do
            {
                Console.WriteLine("End");
                //object[] buffer = new object[5];
                //Console.WriteLine("New message: ");
                //buffer = SimilarityTools.ReturnIssues(Console.ReadLine(), channel);
                //Console.WriteLine("--------------");
                /*for (int i = 0; i < buffer.Length; i++)
                {
                    switch (i)
                    {
                        case 0:
                            Console.WriteLine("Punish?: " + buffer[0]);
                            break;
                        case 1:
                            Console.WriteLine("Sender: " + buffer[1]);
                            break;
                        case 2:
                            Console.WriteLine("Message: " + buffer[2]);
                            break;
                        case 3:
                            Console.WriteLine("Reason: " + buffer[3]);
                            break;
                        case 4:
                            Console.WriteLine("Detailed: " + buffer[4]);
                            break;
                        default:
                            Console.WriteLine(buffer[i]);
                            break;
                    }
                }*/
            } while (!successfulConnection);
        }
    }
}
