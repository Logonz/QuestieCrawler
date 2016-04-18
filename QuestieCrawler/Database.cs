using MySql.Data.MySqlClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QuestieCrawler
{
    [Serializable]
    public static class Database
    {
        public static List<Web> WebClients = new List<Web>();
        static bool Loading = false;
        static string FileName = "MyFile3.bin";
        public static SubDatabase DB = new SubDatabase();
        public static void Save()
        {
            while (Loading) { }
            IFormatter f = new BinaryFormatter();
            Stream s = new FileStream(FileName,
                                     FileMode.Create,
                                     FileAccess.Write, FileShare.None);
            f.Serialize(s, Database.DB);
            s.Close();
        }


        public static void Load()
        {
            Loading = true;
            if (File.Exists(FileName))
            {
                IFormatter formatter = new BinaryFormatter();
                Stream stream = new FileStream(FileName,
                                         FileMode.Open,
                                         FileAccess.ReadWrite, FileShare.None);
                Database.DB = (SubDatabase)formatter.Deserialize(stream);
                stream.Close();
            }
            for (int i = 0; i < 1; i++)
            {
               // Thread t = new Thread(
                //() => Database.CreateClient());
                //t.Start();
            }
            Loading = false;
        }

        public static void CreateClient()
        {
          Database.WebClients.Add(new Web());
        }
    }
    enum Races : int
    {
        Human = 0,
        Orc = 1,
        Dwarf = 2,
        NightElf = 3,
        Undead = 4,
        Tauren = 5,
        Gnome = 6,
        Troll = 7,
    }

    [Serializable]
    public class SubDatabase
    {
        public Dictionary<int, Creature> Creatures = new Dictionary<int, Creature>();
        public Dictionary<int, Gameobject> Gameobjects = new Dictionary<int, Gameobject>();
        public Dictionary<int, Item> Items = new Dictionary<int, Item>();
        public Dictionary<int, Quest> Quests = new Dictionary<int, Quest>();


        public void DeepScanMysql(Form1 Progress, bool forceUpdate = false)
        {
            Progress.Buttons(false);
            Progress.SetProgressBarMaximum(Creatures.Count + Gameobjects.Count + Items.Count + Quests.Count, true);

            MySql.Data.MySqlClient.MySqlConnection dbConn = new MySql.Data.MySqlClient.MySqlConnection("Persist Security Info=False;server=localhost;database=mangos;uid=root;password=");


            try
            {
                dbConn.Open();
            }
            catch (Exception erro)
            {

            }
            foreach (KeyValuePair<int, Quest> c in Quests)
            {
                MySqlCommand cmd = dbConn.CreateCommand();
                cmd.CommandText = "SELECT * FROM `quest_template` where entry="+c.Key+";";
                MySqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {

                    int id = int.Parse(reader["entry"].ToString());
                    int minLevel = int.Parse(reader["minlevel"].ToString());
                    int questLevel = int.Parse(reader["questlevel"].ToString());
                    string title = reader["title"].ToString();
                    string details = reader["details"].ToString();
                    //details = reader["requestitemstext"].ToString();
                    int requiredRaces = int.Parse(reader["requiredraces"].ToString());
                    int requiredClasses = int.Parse(reader["requiredclasses"].ToString());
                    int PrevQuestId = int.Parse(reader["prevquestid"].ToString());

                    c.Value.details = details;
                    c.Value.QuestID = id;
                    c.Value.reqLevel = minLevel;
                    c.Value.level = questLevel;
                    c.Value.name = title;
                    c.Value.reqRaces = requiredRaces;
                    c.Value.RequiresQuest = PrevQuestId;
                    c.Value.reqClasses = requiredClasses;

                    int ProvidedItem = int.Parse(reader["srcitemid"].ToString());

                    //Items Check
                    for (int i = 1; i <= 4; i++)
                    {
                        int ReqItemID = int.Parse(reader["reqitemid" + i].ToString());
                        int ReqCount = int.Parse(reader["reqitemcount" + i].ToString());
                        //We dont care if we've gotten the item provided by the questgiver.
                        if (ReqItemID != 0 && ReqCount > 0 && ReqItemID != ProvidedItem)
                        {
                            if (!Items.ContainsKey(ReqItemID) && ReqItemID != 0)
                            {
                                Items.Add(ReqItemID, new Item(ReqItemID, null));
                            }
                            c.Value.AddQuestObjective(new Objective(Items[ReqItemID], ReqCount));
                        }
                    }
                    


                    //Creature Check
                    for (int i = 1; i <= 4; i++)
                    {
                        //If ReqSpellCast is != 0, the objective is to cast on target, else kill.
                        //NOTE: If ReqSpellCast is != 0 and the spell has effects Send Event or Quest Complete, this field may be left empty.
                        int ReqSpellCast = int.Parse(reader["ReqSpellCast"+i].ToString());
                        //> 0 = Creature
                        //< 0 = Gameobject
                        int ReqCreatureID = int.Parse(reader["reqcreatureorgoid" + i].ToString());
                        int ReqCount = int.Parse(reader["reqcreatureorgocount" + i].ToString());
                        if (ReqCreatureID > 0 && ReqCount > 0)
                        {
                            if (!Creatures.ContainsKey(ReqCreatureID) && ReqCreatureID != 0)
                            {
                                Creatures.Add(ReqCreatureID, new Creature(-1, ReqCreatureID,null));
                            }
                            c.Value.AddQuestObjective(new Objective(Creatures[ReqCreatureID], ReqCount));
                        }
                        else if (ReqCreatureID < 0 && ReqCount > 0)
                        {
                            ReqCreatureID = Math.Abs(ReqCreatureID);
                            if (!Gameobjects.ContainsKey(ReqCreatureID))
                            {
                                Gameobjects.Add(ReqCreatureID, new Gameobject(ReqCreatureID, null));
                            }
                            c.Value.AddQuestObjective(new Objective(Gameobjects[ReqCreatureID], ReqCount));
                        }
                    }

                    /*if (x > 0)
                    {
                        List<Races> ReqRaces = new List<Races>();

                        BitArray b = new BitArray(new int[] { x });
                        for (int i = 0; i < 8; i++)
                        {
                            if (b.Get(i))
                            {
                                ReqRaces.Add((Races)i);
                            }
                        }
                    }
                    else
                    {

                    }*/
                        
                    //qs.Add(new Quest(0,0,id,questLevel,title,minLevel,requiredRaces,0));
                    //Quest q = new Quest(0, 0, id, level, name, reqlvl, side, xp);
                }
                reader.Close();


                //Starters GameObjects
                cmd = dbConn.CreateCommand();
                cmd.CommandText = "SELECT * FROM `gameobject_questrelation` where quest=" + c.Key + ";";
                reader = cmd.ExecuteReader(); //id quest
                while(reader.Read())
                {
                    int ID = int.Parse(reader["id"].ToString());
                    if(!Gameobjects.ContainsKey(ID))
                    {
                        Gameobjects.Add(ID,new Gameobject(ID, null));
                    }
                    if(c.Value.Starter == null)
                    {
                        c.Value.Starter = new List<Objective>();
                    }
                    c.Value.Starter.Add(new Objective(Gameobjects[ID]));
                }
                reader.Close();

                //Starters Creatures
                cmd = dbConn.CreateCommand();
                cmd.CommandText = "SELECT * FROM `creature_questrelation` where quest=" + c.Key + ";";
                reader = cmd.ExecuteReader(); //id quest
                while (reader.Read())
                {
                    int ID = int.Parse(reader["id"].ToString());
                    if (!Creatures.ContainsKey(ID))
                    {
                        Creatures.Add(ID, new Creature(-1, ID, null));
                    }
                    if (c.Value.Starter == null)
                    {
                        c.Value.Starter = new List<Objective>();
                    }
                    c.Value.Starter.Add(new Objective(Creatures[ID]));
                }
                reader.Close();

                //Finishers GameObjects
                cmd = dbConn.CreateCommand();
                cmd.CommandText = "SELECT * FROM `gameobject_involvedrelation` where quest=" + c.Key + ";";
                reader = cmd.ExecuteReader(); //id quest
                while (reader.Read())
                {
                    int ID = int.Parse(reader["id"].ToString());
                    if (!Gameobjects.ContainsKey(ID))
                    {
                        Gameobjects.Add(ID, new Gameobject(ID, null));
                    }
                    if (c.Value.Finisher == null)
                    {
                        c.Value.Finisher = new List<Objective>();
                    }
                    c.Value.Finisher.Add(new Objective(Gameobjects[ID]));
                }
                reader.Close();

                //Finishers Creatures
                cmd = dbConn.CreateCommand();
                cmd.CommandText = "SELECT * FROM `creature_involvedrelation` where quest=" + c.Key + ";";
                reader = cmd.ExecuteReader(); //id quest
                while (reader.Read())
                {
                    int ID = int.Parse(reader["id"].ToString());
                    if (!Creatures.ContainsKey(ID))
                    {
                        Creatures.Add(ID, new Creature(-1, ID, null));
                    }
                    if (c.Value.Finisher == null)
                    {
                        c.Value.Finisher = new List<Objective>();
                    }
                    c.Value.Finisher.Add(new Objective(Creatures[ID]));
                }
                reader.Close();

                if(c.Value.GetQuestObjectives().Count == 0)
                {

                }
                c.Value.RemoveDuplicates();

                Progress.SetProgressBarMaximum(Creatures.Count + Gameobjects.Count + Items.Count + Quests.Count);
                Progress.StepProgressBar();
            }
             /*MySqlCommand cmd1 = dbConn.CreateCommand();
             cmd1.CommandText = "SELECT * FROM `gameobject_template`;";
             MySqlDataReader reader1 = cmd1.ExecuteReader();

             while (reader1.Read())
             {
                 string name = reader1["name"].ToString();
                 int id = int.Parse(reader1["entry"].ToString());
                 if(!Gameobjects.ContainsKey(id))
                 {
                     Gameobjects.Add(id, new Gameobject(id, name));
                 }
             }
             reader1.Close();*/

            foreach (KeyValuePair<int, Item> i in Items)
            {
                MySqlCommand cmd = dbConn.CreateCommand();
                cmd.CommandText = "SELECT * FROM `gameobject_loot_template` where item=" + i.Key + ";";
                MySqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    int GOid = int.Parse(reader["entry"].ToString());
                    if(!Gameobjects.ContainsKey(GOid))
                    {
                        Gameobjects.Add(GOid, new Gameobject(GOid, ""));
                        Gameobjects[GOid].D_Drops.Add(i.Key);
                    }
                }
                reader.Close();

                Progress.SetProgressBarMaximum(Creatures.Count + Gameobjects.Count + Items.Count + Quests.Count);
                Progress.StepProgressBar();
            }

            //Test Resets sources
            foreach(KeyValuePair<int, Item> i in Items)
            {
                i.Value.Sources = new List<ISource>();
            }

            foreach (KeyValuePair<int, Gameobject> i in Gameobjects)
            {
                //Run through all items
                MySqlCommand cmd = dbConn.CreateCommand();
                cmd.CommandText = "SELECT * FROM `gameobject_template` where entry=" + i.Key + ";";
                MySqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    i.Value.Name = reader["name"].ToString();
                }
                reader.Close();


                cmd = dbConn.CreateCommand();
                cmd.CommandText = "SELECT * FROM `gameobject_loot_template` where entry=" + i.Key + ";";
                reader = cmd.ExecuteReader(); //id quest
                while (reader.Read())
                {
                  int ItemID = int.Parse(reader["item"].ToString());
                  if (!Items.ContainsKey(ItemID))
                  {
                      Items.Add(ItemID, new Item(ItemID, null));
                  }
                  Items[ItemID].AddSource(i.Value);
                  i.Value.D_Drops.Add(ItemID);
                }
                reader.Close();

                i.Value.RemoveDuplicates();

                Progress.SetProgressBarMaximum(Creatures.Count + Gameobjects.Count + Items.Count + Quests.Count);
                Progress.StepProgressBar();
            }

            foreach (KeyValuePair<int, Creature> i in Creatures)
            {
                //Run through all items
                MySqlCommand cmd = dbConn.CreateCommand();
                cmd.CommandText = "SELECT * FROM `creature_template` where entry=" + i.Key + ";";
                MySqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    i.Value.Name = reader["name"].ToString();
                }
                reader.Close();

                cmd = dbConn.CreateCommand();
                cmd.CommandText = "SELECT * FROM `creature_loot_template` where entry=" + i.Key + ";";
                reader = cmd.ExecuteReader(); //id quest
                while (reader.Read())
                {
                    int ItemID = int.Parse(reader["item"].ToString());
                    if (!Items.ContainsKey(ItemID))
                    {
                        Items.Add(ItemID, new Item(ItemID, null));
                    }
                    i.Value.D_Drops.Add(ItemID);
                    Items[ItemID].AddSource(i.Value);
                }
                reader.Close();

                i.Value.RemoveDuplicates();

                Progress.SetProgressBarMaximum(Creatures.Count + Gameobjects.Count + Items.Count + Quests.Count);
                Progress.StepProgressBar();
            }

            foreach (KeyValuePair<int, Item> i in Items)
            {
                //Run through all items
                MySqlCommand cmd = dbConn.CreateCommand();
                cmd.CommandText = "SELECT * FROM `item_template` where entry=" + i.Key + ";";
                MySqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    i.Value.Name = reader["name"].ToString();
                }
                reader.Close();

                /*foreach (KeyValuePair<int, Creature> z in Creatures)
                {
                    foreach(int s in z.Value.D_Drops)
                    {
                        if(s == z.Key)
                        {
                            i.Value.AddSource(z.Value);
                        }
                    }
                }

                foreach (KeyValuePair<int, Gameobject> z in Gameobjects)
                {
                    foreach (int s in z.Value.D_Drops)
                    {
                        if (s == z.Key)
                        {
                            i.Value.AddSource(z.Value);
                        }
                    }
                }*/

                i.Value.RemoveDuplicates();

                //Progress.SetProgressBarMaximum(Creatures.Count + Gameobjects.Count + Items.Count + Quests.Count);
                //Progress.StepProgressBar();
            }
            Database.Save();
            Progress.Buttons(true);
        }

        public void FetchCreatureCoordinates(Form1 Progress, bool forceUpdate = false)
        {
            Progress.AddProgressBarMaximum(Creatures.Count);
            Web web = new Web();
            foreach (KeyValuePair<int, Creature> i in Creatures)
            {
                if (i.Value.FetchDone() && !forceUpdate)
                {
                    Progress.StepProgressBar();
                    foreach(Coord c in i.Value.D_Locations)
                    {
                        c.Convert();
                    }
                    continue;
                }
                i.Value.FetchWebInformation(web);
                foreach (Coord c in i.Value.D_Locations)
                {
                    c.Convert();
                }
                Progress.StepProgressBar();
            }
            web.Quit();
            Database.Save();
        }
        public void FetchGameobjectCoordinates(Form1 Progress, bool forceUpdate = false)
        {
            Progress.AddProgressBarMaximum(Gameobjects.Count);
            Web web = new Web();
            foreach (KeyValuePair<int, Gameobject> i in Gameobjects)
            {
                if (i.Value.FetchDone() && !forceUpdate)
                {
                    Progress.StepProgressBar();
                    foreach (Coord c in i.Value.D_Locations)
                    {
                        c.Convert();
                    }
                    continue;
                }
                i.Value.FetchWebInformation(web);
                foreach (Coord c in i.Value.D_Locations)
                {
                    c.Convert();
                }
                Progress.StepProgressBar();
            }
            web.Quit();
            Database.Save();
        }
        
        public void FetchFactions(Form1 Progress)
        {
            Web web = new Web(true);
            Progress.SetProgressBarMaximum(Creatures.Count, true);
            foreach(KeyValuePair<int, Creature> c in Creatures)
            {
                c.Value.FetchFaction(web);
                Progress.StepProgressBar();
            }
            Database.Save();
        }


        public void WriteLua(string Path)
        {
            StreamWriter SW = new StreamWriter(Path+"DB_Quest.lua");
            SW.WriteLine("DB_Quests={");
            foreach(KeyValuePair<int, Quest> q in Quests)
            {
                if (q.Value.name == "CLUCK!") { continue; }
                SW.WriteLine("[" + q.Value.QuestID + "]={");
                SW.WriteLine("\tID = " + q.Value.QuestID + ",");
                SW.WriteLine("\treqQuest = " + Math.Abs(q.Value.RequiresQuest) + ",");
                string T = q.Value.name.Replace("'", "\\'");
                SW.WriteLine("\tTitle = '" + T + "',");
                //SW.WriteLine("\t['"+q.Value.details.Replace("'","\\'")+"'] = {},");
                if (q.Value.GetQuestObjectives().Count > 0)
                {
                    SW.WriteLine("\tObjectives = {");
                    foreach (Objective o in q.Value.GetQuestObjectives())
                    {
                        SW.WriteLine("\t\t{type='" + o.GetObjectiveType().ToString().Replace("QuestieCrawler.", "").Remove(0, 1) + "', ID=" + o.GetSource().GetID() + ", Count=" + o.GetRequiredAmount() + "},");
                    }
                    SW.WriteLine("\t},");
                }
                SW.WriteLine("\tStarter = {");
                foreach(Objective s in q.Value.Starter)
                {
                    SW.WriteLine("\t\t{type='" + s.GetObjectiveType().ToString().Replace("QuestieCrawler.", "").Remove(0,1) + "', ID=" + s.GetSource().GetID()+"},");
                }
                SW.WriteLine("\t},");
                SW.WriteLine("\tFinisher = {");
                foreach (Objective s in q.Value.Finisher)
                {
                    SW.WriteLine("\t\t{type='" + s.GetObjectiveType().ToString().Replace("QuestieCrawler.", "").Remove(0, 1) + "', ID=" + s.GetSource().GetID() + "},");
                }
                SW.WriteLine("\t},");
                SW.WriteLine("\tminLevel = " + q.Value.reqLevel + ",");
                SW.WriteLine("\tLevel = " + q.Value.level + ",");
                SW.WriteLine("\treqRace = " + q.Value.reqRaces + ",");
                SW.WriteLine("\treqClass = " + q.Value.reqClasses + ",");
                SW.WriteLine("},");
            }
            SW.WriteLine("}");
            SW.Close();


            SW = new StreamWriter(Path + "DB_Items.lua");
            SW.WriteLine("DB_Items={");
            foreach (KeyValuePair<int, Item> q in Items)
            {
                if (q.Value.Name == null) { continue; }
                SW.WriteLine("[" + q.Value.GetID() + "]={");
                SW.WriteLine("\tID = " + q.Value.GetID() + ",");
                string T = q.Value.Name.Replace("'", "\\'");
                SW.WriteLine("\tName = '" + T + "',");
                if (q.Value.GetSources().Count > 0)
                {
                    SW.Write("\tSources = {\n\t\t");
                    foreach (ISource o in q.Value.GetSources())
                    {
                        if (o.GetID() == q.Value.GetID()) { continue; }
                        SW.Write("[" + o.GetID() + "] = {type='" + o.GetType().ToString().Replace("QuestieCrawler.", "").Replace("Creature", "NPC") + "', ID=" + o.GetID() + "},");
                    }
                    SW.WriteLine("\n\t},");
                }
                SW.WriteLine("},");
            }
            SW.WriteLine("}");
            SW.Close();

            SW = new StreamWriter(Path + "DB_Creatures.lua");
            SW.WriteLine("DB_Creatures={");
            foreach (KeyValuePair<int, Creature> q in Creatures)
            {
                if (q.Value.Name == null) { continue; }
                SW.WriteLine("[" + q.Value.GetID() + "]={");
                SW.WriteLine("\tID = " + q.Value.GetID() + ",");
                string T = q.Value.Name.Replace("'", "\\'");
                SW.WriteLine("\tName = '" + T + "',");
                if (q.Value.D_Locations.Count > 0)
                {
                    SW.Write("\tLocations = {\n\t\t");
                    foreach (Coord o in q.Value.D_Locations)
                    {
                        SW.Write("{c=" + o.C.ToString().Replace(',', '.') + ",z=" + o.Z.ToString().Replace(',', '.') + ",x=" + (o.X/100).ToString().Replace(',', '.') + ",y=" + (o.Y/100).ToString().Replace(',', '.') + "},");
                    }
                    SW.WriteLine("\n\t},");
                }
                SW.Write("\tReact = {\n\t\t");
                foreach (KeyValuePair<string,int> o in q.Value.D_React)
                {
                    SW.Write(o.Key+"="+o.Value+",");
                }
                SW.WriteLine("\n\t},");
                SW.WriteLine("},");
            }
            SW.WriteLine("}");
            SW.Close();

            SW = new StreamWriter(Path + "DB_Gameobjects.lua");
            SW.WriteLine("DB_Gameobjects={");
            foreach (KeyValuePair<int, Gameobject> q in Gameobjects)
            {
                if (q.Value.Name == null) { continue; }
                SW.WriteLine("[" + q.Value.GetID() + "]={");
                SW.WriteLine("\tID = " + q.Value.GetID() + ",");
                string T = q.Value.Name.Replace("'", "\\'");
                SW.WriteLine("\tName = '" + T + "',");
                if (q.Value.D_Locations.Count > 0)
                {
                    SW.Write("\tLocations = {\n\t\t");
                    foreach (Coord o in q.Value.D_Locations)
                    {
                        SW.Write("{c=" + o.C.ToString().Replace(',', '.') + ",z=" + o.Z.ToString().Replace(',', '.') + ",x=" + (o.X / 100).ToString().Replace(',', '.') + ",y=" + (o.Y / 100).ToString().Replace(',', '.') + "},");
                    }
                    SW.WriteLine("\n\t},");
                }
                SW.WriteLine("},");
            }
            SW.WriteLine("}");
            SW.Close();

            SW = new StreamWriter(Path + "DB_Zones.lua");
            SW.WriteLine("DB_Zones={");
            foreach(KeyValuePair<int, string> i in Coord.NameConvertion)
            {
                SW.WriteLine("\t['" + i.Value.Replace("'", "\\'") + "']={");
                SW.WriteLine("\t\tName='" + i.Value.Replace("'", "\\'") + "',");
                SW.WriteLine("\t\tWoWHeadID=" + i.Key+",");
                SW.WriteLine("\t\tC=" + Coord.Convertion[i.Key][0]+",");
                SW.WriteLine("\t\tZ=" + Coord.Convertion[i.Key][1]+",");
                SW.WriteLine("\t},");
            }
            SW.WriteLine("}");
            SW.Close();

            MySql.Data.MySqlClient.MySqlConnection dbConn = new MySql.Data.MySqlClient.MySqlConnection("Persist Security Info=False;server=localhost;database=mangos;uid=root;password=");
            dbConn.Open();
            Dictionary<int, Quest> LookUp = new Dictionary<int, Quest>();
            MySqlCommand cmd = dbConn.CreateCommand();
            cmd.CommandText = "SELECT * FROM `quest_template`;";
            MySqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    int id = int.Parse(reader["entry"].ToString());
                    string title = reader["title"].ToString();
                    string details = reader["details"].ToString();
                    LookUp.Add(id, new Quest(id));
                    LookUp[id].details = details;
                    LookUp[id].name = title;
                }
                dbConn.Close();
            SW = new StreamWriter(Path + "DB_QuestLookup.lua");
            List<string> Written = new List<string>();
            SW.WriteLine("DB_QuestLookup={");
            foreach (KeyValuePair<int, Quest> q in LookUp)
            {
                if (Written.Contains(q.Value.name)) { continue; }
                SW.WriteLine("\t['" + q.Value.name.Replace("'", "\\'") + "']={");
                Written.Add(q.Value.name);
                string temp = Regex.Replace(q.Value.details, @"\r\n?|\n", "\\n");
                SW.WriteLine("\t\t['" + temp.Replace("'", "\\'").Replace("$B", "\\n") + "']=" + q.Value.QuestID + ",");
                foreach (KeyValuePair<int, Quest> q2 in LookUp)
                {
                    if (q.Value.name == q2.Value.name && q.Value.QuestID != q2.Value.QuestID && q.Value.details != q2.Value.details)
                    {
                        temp = Regex.Replace(q.Value.details, @"\r\n?|\n", "\\n");
                        SW.WriteLine("\t\t['" + temp.Replace("'", "\\'").Replace("$B", "\\n") + "']=" + q.Value.QuestID + ",");
                    }
                }

                SW.WriteLine("\t},");
            }
            SW.WriteLine("}");
            SW.Close();
        }
        public void WriteCon(string Path)
        {
            StreamWriter SW = new StreamWriter(Path + "DB_Quest_min.lua");
            SW.Write("Quests={");
            foreach (KeyValuePair<int, Quest> q in Quests)
            {
                SW.Write("['" + q.Value.QuestID + "']={");
                SW.Write("ID=" + q.Value.QuestID + ",");
                SW.Write("reqQuest=" + q.Value.RequiresQuest + ",");
                string T = q.Value.name.Replace("'", "\\'");
                SW.Write("Title='" + T + "',");
                if (q.Value.GetQuestObjectives().Count > 0)
                {
                    SW.Write("Objectives={");
                    foreach (Objective o in q.Value.GetQuestObjectives())
                    {
                        SW.Write("{type='" + o.GetObjectiveType().ToString().Replace("QuestieCrawler.", "").Remove(1, 1) + "',ID=" + o.GetSource().GetID() + ",Count=" + o.GetRequiredAmount() + "},");
                    }
                    SW.Write("},");
                }
                SW.Write("Starter={");
                foreach (Objective s in q.Value.Starter)
                {
                    SW.Write("{type='" + s.GetObjectiveType().ToString().Replace("QuestieCrawler.", "").Remove(0, 1) + "',ID=" + s.GetSource().GetID() + "},");
                }
                SW.Write("},");
                SW.Write("Finisher={");
                foreach (Objective s in q.Value.Finisher)
                {
                    SW.Write("{type='" + s.GetObjectiveType().ToString().Replace("QuestieCrawler.", "").Remove(0, 1) + "',ID=" + s.GetSource().GetID() + "},");
                }
                SW.Write("},");
                SW.Write("minLevel=" + q.Value.reqLevel);
                SW.Write("Level=" + q.Value.level);
                SW.Write("reqRace=" + q.Value.reqRaces);
                SW.Write("reqClass=" + q.Value.reqLevel);
                SW.Write("},");
            }
            SW.Write("}");
            SW.Close();
        }
            
        static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        public void DeepScanAll(Form1 Progress, bool forceUpdate = false)
        {

            Web web = new Web();
            Progress.Buttons(false);
            Progress.SetProgressBarMaximum(Creatures.Count + Gameobjects.Count + Items.Count + Quests.Count, true);
            //web.Init();
            foreach (KeyValuePair<int, Creature> c in Creatures)
            {
                c.Value.FetchWebInformation(web, forceUpdate);
                Progress.StepProgressBar();
                Progress.SetProgressBarMaximum(Creatures.Count + Gameobjects.Count + Items.Count + Quests.Count);
            }
            Database.Save();
            foreach (KeyValuePair<int, Quest> c in Quests)
            {
                c.Value.FetchWebInformation(web, forceUpdate);
                Progress.StepProgressBar();
                Progress.SetProgressBarMaximum(Creatures.Count + Gameobjects.Count + Items.Count + Quests.Count);
            }

            Database.Save();
            foreach (KeyValuePair<int, Gameobject> c in Gameobjects)
            {
                c.Value.FetchWebInformation(web, forceUpdate);
                Progress.StepProgressBar();
                Progress.SetProgressBarMaximum(Creatures.Count + Gameobjects.Count + Items.Count + Quests.Count);
            }
            Database.Save();
            foreach (KeyValuePair<int, Item> c in Items)
            {
                c.Value.FetchWebInformation(web, forceUpdate);
                Progress.StepProgressBar();
                Progress.SetProgressBarMaximum(Creatures.Count + Gameobjects.Count + Items.Count + Quests.Count);
            }
            Database.Save();
            Progress.Buttons(true);
        }
    }
}
