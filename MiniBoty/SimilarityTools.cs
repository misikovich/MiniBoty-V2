using System;
using System.Collections.Generic;
using System.Linq;
namespace MiniBoty
{
    class SimilarityTools
    {
        
        private static int percentageTrigger = (int)BotBody.DefaultParameters[4];
        private static int overLength = (int)BotBody.DefaultParameters[5];
        private static int msgPrebuffer = (int)BotBody.DefaultParameters[6];
        private static TimeSpan timeoutTime = TimeSpan.FromSeconds((int)BotBody.DefaultParameters[2]);
        private static int levenMinDist = (int)BotBody.DefaultParameters[3];

        private static int percents = 0;
        private static List<BlockedMessage> punish_list = new();
        private static List<BlockedMessage> allow_list = new();
        private static List<Message> buffer = new();
        public static SRPData PunishList_file;


        public static object[] ReturnIssues(string message, string sender, string message_id)
        ///return: [(bool)punish?, (string)sender, (string)message, (string)reason, (string)detailed, (string)messageID, (string)punish_type, (TimeSpan)punish_time]
        {
            percentageTrigger = (int)BotBody.CurrentParameters[4];
            overLength = (int)BotBody.CurrentParameters[5];
            msgPrebuffer = (int)BotBody.CurrentParameters[6];
            timeoutTime = TimeSpan.FromSeconds((int)BotBody.CurrentParameters[2]);
            levenMinDist = (int)BotBody.CurrentParameters[3];



            //ClearText(ref message);
            object[] buffer = new object[8];
            bool legit = true;

            buffer[0] = false; //punish
            buffer[1] = sender;
            buffer[2] = message;
            buffer[3] = string.Empty; //description
            buffer[4] = string.Empty; //detailed desc
            buffer[5] = message_id;
            buffer[6] = string.Empty; //punishment type
            buffer[7] = timeoutTime; //timeout time

            if (IsOverLength(message))
            {
                buffer[0] = true;
                buffer[3] += "Overlength (>" + overLength + " symbols); ";
                buffer[4] += message.Length + " symbols; ";
                buffer[6] = "timeout";
                legit = false;
            }
            if (DetectRepeatings(message))
            {
                buffer[0] = true;
                buffer[3] += "Too much repeatings (>" + percentageTrigger + "% of message); ";
                buffer[4] += percents + "%; ";
                buffer[6] = "timeout";
                legit = false;
            }
            if (LevenshteinCompute(message, levenMinDist, punish_list, ref buffer))
            {
                buffer[0] = true;
                buffer[3] += "Your message is in blocklist; ";
                if (message.Length > 24)
                {
                    buffer[4] += $"Message: '{message.ToString().Substring(0, 24)}...'; ";
                }
                else
                {
                    buffer[4] += $"Message: '{message}'; ";
                }

                legit = false;
            }
            if (legit)
            {
                NewMessage(message, sender, message_id);
                buffer[3] = "legit";
                buffer[4] = "legit";
            }
            return buffer;
        }

        private static void AnalyzeBuffer(List<Message> messages)
        {
        }
        public static void AddBlockMessage(string text, string punishment, TimeSpan time)
        {
            if (text.Length < 4)
            {
                return;
            }
            if (punishment.Length < 3)
            {
                punishment = "timeout";
                time = TimeSpan.FromSeconds(5);
            }
            if (punishment == "timeout" && time == TimeSpan.FromSeconds(0))
            {
                return;
            }
            punish_list.Add(new BlockedMessage { 
                NumberID = punish_list.Count,
                Text = text,
                PunishType = punishment,
                PunishTime = time
            });

            AnalyzeBuffer(buffer);

            string numberOfElements = "number_of_elements";
            int _numberID = punish_list[punish_list.Count - 1].NumberID;
            string[] vnames = new string[] { numberOfElements, $"{_numberID}_text", $"{_numberID}_punishment_type", $"{_numberID}_timeout_time" };
            string[] values = new string[] { punish_list.Count.ToString(), text, punishment, time.TotalSeconds.ToString()};
            PunishList_file.SaveData(vnames, values);
            Console.WriteLine("|MESSAGE ANALYZER| Added: '" + text + $"' {punishment.ToUpperInvariant()} " + time);
        }

        
        private static int FindIndex(List<BlockedMessage> list, string toFind)
        {
            string[] splittedNew = toFind.ToLower().Split(' ');
            foreach (BlockedMessage blockedMessage in list)
            {
                int matches = 0;
                string[] splittedSaved = blockedMessage.Text.ToLower().Split(' ');
                if (splittedNew[0] != splittedSaved[0])
                {
                    continue;
                }
                matches = 1;
                for (int i = 1; i < splittedNew.Length && i < splittedSaved.Length; i++)
                {
                    if (splittedSaved[i] == splittedNew[i])
                    {
                        matches++;
                    }
                }
                if (splittedNew.Length - matches < 2)
                {
                    return blockedMessage.NumberID;
                }
            }
            return -1;
        }
        public static bool DeleteBlockedMessage(string part_of_message, bool clearAll)
        {
            int index = FindIndex(punish_list, part_of_message);
            if (index == -1)
            {
                Console.WriteLine("Index not found");
                return false;
            }

            int ID = punish_list[index].NumberID;
            int total = PunishList_file.ParseDataInt("number_of_elements");
            if (ID == total - 1) //if last element in file
            {
                PunishList_file.DeleteParameter(new string[] { ID + "_text", ID + "_punishment_type", ID + "_timeout_time" });
                PunishList_file.SaveData(new string[] { "number_of_elements" }, new string[] { (total - 1).ToString() });
                Console.WriteLine($"Deleted: {punish_list[index].Text} {punish_list[index].PunishType} {punish_list[index].PunishTime} ID: {punish_list[index].NumberID}");
                punish_list.RemoveAt(index);
                return true;
            }
            else if (ID >= 0 && ID < total - 1 )
            {
                
                PunishList_file.SaveData(new string[] { "number_of_elements", ID + "_text", ID + "_punishment_type", ID + "_timeout_time" }, 
                    new string[] {
                        (total - 1).ToString(),
                    PunishList_file.ParseDataString((total - 1) + "_text"),
                    PunishList_file.ParseDataString((total - 1) + "_punishment_type"),
                    PunishList_file.ParseDataString((total - 1) + "_timeout_time")
                    });
                PunishList_file.DeleteParameter(new string[] { (total - 1) + "_text", (total - 1) + "_punishment_type", (total - 1) + "_timeout_time" });
                Console.WriteLine($"Deleted: {punish_list[index].Text} {punish_list[index].PunishType} {punish_list[index].PunishTime} ID: {punish_list[index].NumberID}");
                punish_list.RemoveAt(index);
                return true;
            }
            else
            {
                Console.WriteLine("Failed to save, ID: "+ ID);
                return false;
            }

        }
        public static void CreateBlockListFile(string filename)
        {
            PunishList_file = new SRPData(filename, ':');
        }
        public static void OnLoad(string channel)
        {
            PunishList_file = new SRPData($"punishments_{channel}.txt", ':');
            int n = PunishList_file.ParseDataInt("number_of_elements");
            for (int i = 0; i < n; i++)
            {
                AddBlockMessage(
                    PunishList_file.ParseDataString(i+"_text"),
                    PunishList_file.ParseDataString(i+"_punishment_type"),
                    TimeSpan.FromSeconds(PunishList_file.ParseDataInt(i + "_timeout_time")));
            }
        }
        private static void NewMessage(string message, string username, string id)
        {
            buffer.Add(new Message { Text = message, Username = username, MessageID = id});
            if (buffer.Count > (int)BotBody.CurrentParameters[6])
            {
                buffer.RemoveAt((int)BotBody.CurrentParameters[6]);
            }
        }
        private class Message
        {
            public string Text { get; set; }
            public string Username { get; set; }
            public string MessageID { get; set; }
        }
        private class BlockedMessage
        {
            public int NumberID { get; set; }
            public string Text { get; set; }
            public string PunishType { get; set; }
            public TimeSpan PunishTime { get; set; }
        }

        private static bool LevenshteinCompute(string message, int min_dist_level, List<BlockedMessage> list, ref object[] buffer)
        {
            if (list.Count == 0) { return false; }

            foreach (BlockedMessage item in list)
            {
                if (PhraseAnalyze(item.Text, message))
                {
                    buffer[6] = item.PunishType;
                    buffer[7] = item.PunishTime;
                    return true;
                }


                double percent = (message.Length / item.Text.Length) * 100;

                if (percent < 80 || percent > 120)
                {
                    continue;
                }

                int leven_dist = Compute(item.Text.ToLower(), message.ToLower());
                if (leven_dist < min_dist_level)
                {
                    buffer[6] = item.PunishType;
                    buffer[7] = item.PunishTime;
                    return true;
                }
            }
            return false;
        }
        private static bool PhraseAnalyze(string blocked, string user)
        {
            if (blocked.Length > user.Length)
            {
                return false;
            }
            if (blocked.Split().Length > user.Split().Length)
            {
                return false;
            }
            blocked = blocked.ToLower();
            user = user.ToLower();
            string[] b_arr = blocked.Split();
            string[] u_arr = user.Split();

            int matches = 0;
            for (int u = 0; u < u_arr.Length; u++)
            {
                matches = 0;
                for (int b = 0; b < b_arr.Length; b++)
                {
                    if (u + b >= u_arr.Length)
                    {
                        break;
                    }
                    if (u_arr[u + b] == b_arr[b])
                    {
                        matches++;
                    }
                }
                if (matches == b_arr.Length)
                {
                    return true;
                }
            }
            return false;
        }
        private static bool DetectRepeatings(string text)
        {
            var splitted = string.Join(" ", text.Split(' ').Distinct());
            //Console.WriteLine("Splitted: " + splitted);
            //Console.WriteLine("Splitted length: " + splitted.Length + "\nText length: " + text.Length);
            //int numRepeatings = text.Length / splitted.Length;
            //Console.WriteLine("Number of repeatings: " + numRepeatings);
            int percentage = 100 - (100 * splitted.Length) / text.Length;
            percents = percentage;
            if (percentage >= (int)BotBody.CurrentParameters[4] && splitted.Split().Length > 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        private static bool IsOverLength(string text) 
        {
            if (text.Length > (int)BotBody.CurrentParameters[5])
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static int Compute(string s, string t)
        {
            if (string.IsNullOrEmpty(s))
                return t.Length;
            if (string.IsNullOrEmpty(t))
                return s.Length;

            int n = s.Length; // length of s
            int m = t.Length; // length of t

            if (n == 0)
                return m;
            if (m == 0)
                return n;

            int[] p = new int[n + 1]; //'previous' cost array, horizontally
            int[] d = new int[n + 1]; // cost array, horizontally

            // indexes into strings s and t
            int i; // iterates through s
            int j; // iterates through t

            for (i = 0; i <= n; i++)
                p[i] = i;

            for (j = 1; j <= m; j++)
            {
                char tJ = t[j - 1]; // jth character of t
                d[0] = j;

                for (i = 1; i <= n; i++)
                {
                    int cost = s[i - 1] == tJ ? 0 : 1; // cost
                                                       // minimum of cell to the left+1, to the top+1, diagonally left and up +cost                
                    d[i] = Math.Min(Math.Min(d[i - 1] + 1, p[i] + 1), p[i - 1] + cost);
                }

                // copy current distance counts to 'previous row' distance counts
                int[] dPlaceholder = p; //placeholder to assist in swapping p and d
                p = d;
                d = dPlaceholder;
            }

            // our last action in the above loop was to switch d and p, so p now 
            // actually has the most recent cost counts
            return p[n];
        }
    }
}
