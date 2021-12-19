using System;
using System.Collections.Generic;
using TwitchLib.Client.Models;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Extensions;
using System.Text.RegularExpressions;
using System.Linq;

namespace MiniBoty
{
    class BotBody
    {
        public static string[] Descriptions = new string[] { "Changes bot tag command", "Changes bot activity, if active:'true' - bot is moderating",
            "Changes default timeout time for being paste or overlengthed",
            "Influences how message should be similar to '!mb ban message' to be punished", "Influences how much repetition percentage must be in a message to be timeouted",
            "Changes maximum user message length", "Changes how much messages will be writed in buffer(max 500, 0 is off), and when you type '!mb 600 message' it will punish all messages in buffer",
            "By default(true) sends all service messages to your whisper, if false - to chat" };

        public static string[] names = new string[] { "tag", "active", "paste-timeout", "l-dist-trigger", "paste_trigger", "overlength", "msg_prebuffer_size", "allow_vip" };
        public static object[] DefaultParameters = new object[] { "mb", true, 5, 9, 65, 210, 200, true };
        public static object[] CurrentParameters = new object[names.Length];
        public static List<Parameter> qParameters = new List<Parameter>() { new TagParam(), new IsActiveParam(), new LevensteinDistanceTriggParam(), new PasteTriggParam() };
        public static Parameter[] Parameters =
        {
            new TagParam(), new IsActiveParam(), new LevensteinDistanceTriggParam(), new PasteTriggParam()
        };

        private static string ListOfCommands = 
            "|!<tag> - replies to you with current state(Active/Unactive): !mb| " +
            "\n|!<tag> <'ban' or [number]> <message> - adds <message> to blocklist for [number] seconds or 'ban' permanently: !mb 600 bebra pushkina| " +
            "\n|!<tag> 'get' 'parameters' - shows list of all settings: !mb get parameters| " +
            "\n|!<tag> 'get' <parameter> - shows current state and description of <parameter>: !mb get paste_timeout|" +
            "\n|!<tag> 'set' <parameter> - changes <parameter> state: !mb set active false";

        private ConnectionCredentials connectionCredentials;
        private TwitchClient client;
        private SRPData parameters;
        private SRPData banned_users;
        private SRPData error_log;
        LogWriter timeoutedLog;

        //public static object[,] BigMessageBuffer = new object[100, 2];

        public void Connect(string channelName)
        {
            try
            {
                connectionCredentials = new ConnectionCredentials(bebra.BotUsername, bebra.Token);
                Console.WriteLine("Stage 1: Checking data for validity...");
            }
            catch (Exception)
            {
                Console.Clear();
                Console.WriteLine("|ERROR| 'BotUsername' or 'BotToken' values are incorrect ");
                Program.successfulConnection = false;
                return;
            }
            if (string.IsNullOrEmpty(channelName) || channelName.Length < 3)
            {
                Console.Clear();
                Console.WriteLine("|ERROR| 'ChannelName' cannot be empty or less then 3 characters");
                Program.successfulConnection = false;
                return;
            }

            client = new TwitchClient();
            client.Initialize(connectionCredentials, channelName);
            Console.WriteLine("Stage 2: " + bebra.BotUsername.ToUpper() + " Inializing...");


            if (Program.isLogging)
            {
                client.OnLog += Client_OnLog;
            }
            client.OnMessageReceived += Client_OnMessageReceived;

            Console.WriteLine("Stage 3: Connecting to '" + channelName + "'...");
            client.OnConnected += Client_OnConnected;
            client.Connect();
        }


        private void Client_OnLog(object sender, OnLogArgs e)
        {
            Console.WriteLine($"{e.DateTime}: {e.BotUsername} - {e.Data}");
        }

        private void Client_OnConnected(object sender, OnConnectedArgs e)
        {
            string channel = Program.channel;
            Console.Clear();
            Console.WriteLine($"|START| Connected to channel '{channel.ToUpper()}' as {e.BotUsername.ToUpper()}");

            timeoutedLog = new LogWriter($"timeout_log_{channel}.txt");
            banned_users = new SRPData($"banned_users_{channel}.txt", ':');
            parameters = new SRPData($"params_{channel}.txt", ':');
            error_log = new SRPData($"errors.txt", ':');

            SimilarityTools.OnLoad(channel);
            SetParameters(parameters);

            Program.successfulConnection = true;

            /*string reply;
            if ((bool)CurrentParameters[1]) { reply = "Active"; } else { reply = "Unactive"; }
            client.SendMessage(Program.channel, "Connected, " + reply);*/
        }
        private static void SetParameters(SRPData data)
        {
            for (int i = 0; i < names.Length; i++)
            {
                string parameter = data.ParseDataString(names[i]);
                if (parameter == null)
                {
                    CurrentParameters[i] = DefaultParameters[i];
                }
                else
                {
                    CurrentParameters[i] = Convert.ChangeType(parameter, DefaultParameters[i].GetType());
                }
            }
            
        }
        private static string TryChangeParameter(SRPData data, string parameter, object value)
        {
            if (parameter == "defaults" && DefaultParameters != CurrentParameters)
            {
                CurrentParameters = DefaultParameters;
                data.SaveData(names, Array.ConvertAll(CurrentParameters, x => x.ToString()));
                return $"Set defaults: {Get("parameters")}";
            }
            if (string.IsNullOrEmpty(value.ToString()) || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return "Value can not be empty!";
            }

            int index = Array.IndexOf(names, parameter);
            
            if (index != -1)
            {

                try
                {
                    var typed_value = Convert.ChangeType(value, DefaultParameters[index].GetType());
                    string last_val = CurrentParameters[index].ToString();
                    CurrentParameters[index] = typed_value;
                    data.SaveData(new string[] { parameter }, new string[] { typed_value.ToString() });
                    return $"Changed '{parameter}': {last_val} --> {typed_value.ToString()}";
                }
                catch (Exception)
                {
                    string typename = DefaultParameters[index].GetType().Name;
                    switch (typename)
                    {
                        case "Int32":
                            typename = "Number";
                            break;
                        case "Bool":
                            typename = "True/False";
                            break;
                        case "String":
                            typename = "Text";
                            break;
                    }
                    return $"Invalid object type({typename}) for " + parameter;
                }
            }
            else
            {
                return $"No parameter '{parameter}' found, use '!{CurrentParameters[0]} get parameters' to see the list of parameters";
            }
        }
        public  void SendReply(string message, string messageid)
        {
            client.SendReply(client.JoinedChannels[0], messageid, message);
        }
        private void SendWhisper(string message, string username)
        {
            client.SendWhisper(username, message);
        }
        private void DeleteChatMessage(ChatMessage chatMessage)
        {
            client.DeleteMessage(Program.channel, chatMessage);
        }
        private static string Get(string parameter)
        {
            parameter = parameter.ToLower();
            string ret = "";
            switch (parameter)
            {
                case "parameters":
                    for (int i = 0; i < names.Length; i++)
                    {
                        ret += names[i] + ": '" + CurrentParameters[i].ToString() + "'; \n"; // bot_tag: 'mb'
                    }
                    break;
                case "blocklist":
                    break;
                case "commands":
                    ret = ListOfCommands;
                    break;
                default:
                    int index = Array.IndexOf(names, parameter);
                    if (index != -1)
                    {
                        ret = names[index] + ": '" + CurrentParameters[index].ToString() + "' -- " + Descriptions[index]; // bot_tag: 'mb'
                    }
                    else
                    {
                        ret = $"No parameter '{parameter}' found";
                    }
                    break;
            }
            return ret;
        }
        private void MessageProcessing(OnMessageReceivedArgs e)
        {
            bool allow_vip = (bool)CurrentParameters[7];
            if (!e.ChatMessage.IsBroadcaster && !e.ChatMessage.IsModerator && e.ChatMessage.Username.ToLower() != bebra.BotUsername && e.ChatMessage.Username.ToLower() != "misikovich" && (bool)CurrentParameters[1])
            {
                if (allow_vip && e.ChatMessage.IsVip)
                {
                    return;
                }
                object[] buffer = SimilarityTools.ReturnIssues(e.ChatMessage.Message, e.ChatMessage.Username, e.ChatMessage.Id);
                ///return: [(bool)punish?, (string)sender, (string)message, (string)reason, (string)detailed, (string)messageID, (string)punish_type, (TimeSpan)punish_time]
                if ((bool)buffer[0])
                {
                    client.DeleteMessage(e.ChatMessage.Channel, (string)buffer[5]);

                    string shorted;
                    if (((string)buffer[3]).Length > 64)
                    {
                        shorted = $"{((string)buffer[2]).ToString().Substring(0, 64)}...";
                    }
                    else
                    {
                        shorted = $"{((string)buffer[2])}";
                    }
                    if (shorted.Length < 64)
                    {
                        for (int i = shorted.Length; i < 67; i++)
                        {
                            shorted += " ";
                        }
                    }
                    string nameLined = buffer[1].ToString();
                    if (nameLined.Length < 16)
                    {
                        for (int i = nameLined.Length; i < 16; i++)
                        {
                            nameLined += " ";
                        }
                    }
                    Console.WriteLine($"|INFO||RESTRICT| {nameLined.ToUpper()} {buffer[6].ToString().ToUpper()} {(TimeSpan)buffer[7]}: '{shorted}' Reason: {buffer[3]}");

                    if ((string)buffer[6] == "timeout")
                    {
                        client.TimeoutUser(e.ChatMessage.Channel, (string)buffer[1], (TimeSpan)buffer[7], (string)buffer[3] + "|  " + (string)buffer[4]);
                    }
                    else if ((string)buffer[6] == "ban")
                    {
                        client.BanUser(e.ChatMessage.Channel, (string)buffer[1], (string)buffer[3] + "-|- " + (string)buffer[4]);
                        string total_banned = (banned_users.ParseDataInt("total_banned") + 1).ToString();
                        banned_users.SaveData(new string[] { "total_banned", total_banned }, new string[] { total_banned, buffer[1].ToString() });
                    }
                    //Console.WriteLine($"!{((string)buffer[6]).ToUpperInvariant()} {DateTime.Now.ToLongTimeString()} Channel: {e.ChatMessage.Channel} Username: {(string)buffer[1]} Time: {((TimeSpan)buffer[7]).ToString()} s Reason: {(string)buffer[3]} -|- {(string)buffer[4]}");
                    timeoutedLog.LogWrite(buffer);
                }
            }
            else if (e.ChatMessage.IsBroadcaster || e.ChatMessage.IsModerator || e.ChatMessage.Username.ToLower() == bebra.BotUsername || e.ChatMessage.Username.ToLower() == "misikovich") //is previleged?
            {
                
                if (e.ChatMessage.Message[0] == '!') //is command?
                {
                    Console.WriteLine("|INPUT| User: '" + e.ChatMessage.Username + "' - privileged");
                    string[] splitted = e.ChatMessage.Message.Split(); //split command to array
                    string[] onlyCharSplit = Regex.Replace(e.ChatMessage.Message, @"[^0-9a-zA-Z!]+", "").Split();
                    if (onlyCharSplit.Length == 1 && onlyCharSplit[0] == "!" + CurrentParameters[0]) //!mb
                    {
                        string reply;
                        if ((bool)CurrentParameters[1]) { reply = "Active"; } else { reply = "Unactive"; }
                        Console.WriteLine("|OUT| Sended: " + reply);
                        SendReply(reply, e.ChatMessage.Id);
                        return;
                    }

                    Console.Write("|INPUT| ");
                    foreach (var item in splitted)
                    {
                        Console.Write($"'{item}' ");
                    }
                    Console.WriteLine();

                    List<string> argument3_splitted = new List<string>();
                    if (splitted.Length >= 3 && splitted[0] == "!" + CurrentParameters[0])
                    {
                        bool whisperMode = false;
                        for (int i = 2; i < splitted.Length; i++)
                        {
                            argument3_splitted.Add(splitted[i]);
                        }

                        if (int.TryParse(splitted[1], out int arg_1)) //check if 1st argument is a number //!mb 600 funny paste
                        {
                            SimilarityTools.AddBlockMessage(String.Join(' ', argument3_splitted.ToArray()), "timeout", TimeSpan.FromSeconds(arg_1));
                            SendReply("Added", e.ChatMessage.Id);
                            DeleteChatMessage(e.ChatMessage);
                        }
                        else if (splitted[1].ToLower() == "ban") //!mb ban funny paste 
                        {
                            SimilarityTools.AddBlockMessage(String.Join(' ', argument3_splitted.ToArray()), "ban", TimeSpan.FromSeconds(0));
                            SendReply("Added", e.ChatMessage.Id);
                            DeleteChatMessage(e.ChatMessage);
                        }
                        else if (splitted[1].ToLower() == "set") //!mb set tag miniboty
                        {
                            string argument3 = string.Empty;
                            if (splitted.Length > 3)
                            {
                                argument3 = splitted[3].ToLower();
                            }

                            if (whisperMode)
                            {
                                SendWhisper(TryChangeParameter(parameters, splitted[2].ToLower(), argument3), e.ChatMessage.Username);
                            }
                            else
                            {
                                SendReply(TryChangeParameter(parameters, splitted[2].ToLower(), argument3), e.ChatMessage.Id);
                            }
                            DeleteChatMessage(e.ChatMessage);
                        }
                        else if (splitted[1].ToLower() == "get")
                        {
                            if (whisperMode)
                            {
                                SendWhisper(Get(splitted[2].ToLower()), e.ChatMessage.Username);
                            }
                            else
                            {
                                SendReply(Get(splitted[2].ToLower()), e.ChatMessage.Id);
                            }
                            DeleteChatMessage(e.ChatMessage);
                        }
                        else if (splitted[1].ToLower() == "add")
                        {
                            int interval;
                            if (int.TryParse(splitted[2], out interval))
                            {

                            }
                        }
                        else if (splitted[1].ToLower() == "del")
                        {
                            if (SimilarityTools.DeleteBlockedMessage(String.Join(' ', argument3_splitted.ToArray()), false))
                            {
                                SendReply("Deleted", e.ChatMessage.Id);
                            }
                            else
                            {
                                SendReply("Not found", e.ChatMessage.Id);
                            }
                        }
                        else
                        {
                            SendReply($"Invalid argument '{splitted[1].ToLower()}'", e.ChatMessage.Id);
                            SendReply("Your command: '" + String.Join(' ', splitted) + "' \nHere is list of commands: " + ListOfCommands, e.ChatMessage.Id);
                        }
                    }
                }
            }
        }
        private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            try
            {
                MessageProcessing(e);
            }
            catch (Exception exception)
            {
                int totalcount = error_log.ParseDataInt("TotalCount");
                if (totalcount <= 0)
                {
                    totalcount = 0;
                }
                else
                {
                    totalcount++;
                }
                string[] savedataNames = new string[] { "TotalCount", "Time", "Channel", "Exception", "Source", "StackTrace", "LastMessage"};
                string[] savedataValues = new string[] { totalcount.ToString(), DateTime.Now.ToString(), Program.channel, exception.Message, exception.Source, exception.StackTrace, e.ChatMessage.Message};
                error_log.SaveData(savedataNames, savedataValues);

                Console.Clear();
                Console.WriteLine("|CRITICAL ERROR| " + exception.Message);

                for (int i = 0; i < savedataNames.Length; i++)
                {
                    Console.WriteLine($"{savedataNames[i]}: {savedataValues[i]}");
                }

                Console.Beep();
                Console.Beep();

                System.Diagnostics.Process.Start($"miniboty.exe {Program.channel} {Program.isLogging.ToString().ToLower()}");
                Environment.Exit(0);
            }
        }
    }
}
