using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestieCrawler
{
        [Serializable]
        public class Creature : INPC, ISource {
        int WHMapID;
        int ID;
        public string Name;
        List<int> QuestsInvolved = new List<int>();
 
        //Deep Info
        bool DeepFetchDone = false;
        public List<int> D_Drops = new List<int>(); //ItemIDS
        public List<Coord> D_Locations = new List<Coord>();
        bool FactionFetchDone = false;
        public Dictionary<string, int> D_React = new Dictionary<string, int>();

        //Not needed i think
        /*List<int> D_Starts = new List<int>(); //QuestIDS
        List<int> D_Ends = new List<int>(); //QuestIDS
        Dictionary<string, int> D_React = new Dictionary<string, int>();*/
        public Creature(int WHMapID, int ID, string Name){
                this.WHMapID = WHMapID;
                this.ID = ID;
                this.Name = Name;
        }

        public void RemoveDuplicates()
        {
            if (D_Drops != null)
            {
                List<int> Distinct = new List<int>();
                foreach (int o in D_Drops)
                {
                    bool found = false;
                    foreach (int o2 in Distinct)
                    {

                        // 
                        if (o == o2)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        Distinct.Add(o);
                    }
                }
                D_Drops = Distinct;
            }
        }

        public int NpcID() { return ID; }
        public int WoWheadMapID(){ return WHMapID; }
        public List<int> RequirementFor(){return QuestsInvolved;}
 
        public List<Coord> NPCLocations(){ return DeepFetchDone ? D_Locations : null;}

        public void FetchFaction(Web web)
        {
            if (FactionFetchDone != null && FactionFetchDone == true) { return; }
            if (D_React == null) { D_React = new Dictionary<string, int>(); }
            web.Navigate("http://db.vanillagaming.org/?npc=" + ID);
            try
            {
                IWebElement table = web.driver.FindElement(By.ClassName("infobox"));
                IWebElement targetElement = table.FindElement(By.CssSelector("div[id*='markup-']"));
                ReadOnlyCollection<IWebElement> t = targetElement.FindElements(By.CssSelector("div"));
                //string s = t[2].GetAttribute("innerHTML");
                foreach (IWebElement e in t)
                {
                    string s = e.GetAttribute("innerHTML");
                    if (s.ToLower().Contains("react:"))
                    {
                        foreach (IWebElement Span in e.FindElements(By.CssSelector("span")))
                        {
                            string c = Span.GetAttribute("class");
                            if (c == "q10") //Hostile
                            {
                                D_React.Add(Span.Text, -1);
                            }
                            else if (c == "q2") //Friendly
                            {
                                D_React.Add(Span.Text, 1);
                            }
                            else if (c == "q") //Neutral
                            {
                                D_React.Add(Span.Text, 0);
                            }
                        }
                    }
                }
            }
            catch
            {

            }
            FactionFetchDone = true;
        }

        public void FetchWebInformation(Web web, bool forceUpdate = false){
                if(DeepFetchDone && !forceUpdate){return;}
                web.Navigate("http://db.vanillagaming.org/?npc="+ID);


                //Object Read = (Object)web.GetVar("g_listviews['drop']['data'].length");
                /*if (Read != null)
                {
                    int ItemLength = int.Parse(Read.ToString());

                    for (int i = 0; i < ItemLength; i++)
                    {
                        int ItemID = int.Parse(web.GetVar("g_listviews['drop']['data']['" + i + "']['id']").ToString());
                        string ItemName = web.GetVar("g_listviews['drop']['data']['" + i + "']['name']").ToString().Remove(1);
                        if(!Database.DB.Items.ContainsKey(ItemID)){
                            Database.DB.Items.Add(ItemID,new Item(ItemID, ItemName));
                        }
                    }
                }*/
                if (D_Locations == null)
                {
                    D_Locations = new List<Coord>();
                }
                Object Read = (Object)web.GetVar("myMapper.zone");
                if (Read != null)
                {
                    int MapID = int.Parse(Read.ToString());
                    Read = (Object)web.GetVar("myMapper.getCoords()");
                    if (Read != null)
                    {
                        ReadOnlyCollection<Object> read = (ReadOnlyCollection<Object>)Read;
                        foreach (dynamic o in read)
                        {
                            double X = o[0];
                            double Y = o[1];
                            D_Locations.Add(new Coord(MapID, X, Y));
                        }
                    }
                }
                
                 /*
                Object Chck = web.GetVar("g_listviews['starts']['data'].length");
                int Length = int.Parse((Chck != null ? Chck : 0).ToString());
                //Todo: Do conversions!
                for (int i = 0; i < Length; i++)
                {
                    Object c = web.GetVar("g_listviews['starts']['data']['" + i + "']['category']");
                    int category = int.Parse((c!=null?c:-1).ToString());

                    c = web.GetVar("g_listviews['starts']['data']['" + i + "']['category2']");
                    int category2 = int.Parse((c != null ? c : -1).ToString());

                    c =web.GetVar("g_listviews['starts']['data']['" + i + "']['id']");
                    int QuestID = int.Parse((c != null ? c : -1).ToString());

                    c = web.GetVar("g_listviews['starts']['data']['" + i + "']['level']");
                    int level = int.Parse((c != null ? c : -1).ToString());

                    c=web.GetVar("g_listviews['starts']['data']['" + i + "']['name']");
                    string name = (c != null ? c : "").ToString();

                    c=web.GetVar("g_listviews['starts']['data']['" + i + "']['reqlevel']");
                    int reqLevel = int.Parse((c != null ? c : -1).ToString());

                    c=web.GetVar("g_listviews['starts']['data']['" + i + "']['side']");
                    int side = int.Parse((c != null ? c : -1).ToString());

                    c=web.GetVar("g_listviews['starts']['data']['" + i + "']['type']");
                    int type = int.Parse((c != null ? c : -1).ToString());

                    c=web.GetVar("g_listviews['starts']['data']['" + i + "']['xp']");
                    int xp = int.Parse((c != null ? c : -1).ToString());
                    if(!Database.DB.Quests.ContainsKey(QuestID)){
                        Database.DB.Quests.Add(QuestID, new Quest(category,category2,QuestID,level,name,reqLevel,side,xp)); //TODO
                    }
                    if (Database.DB.Quests.ContainsKey(QuestID))
                    {
                        //Database.DB.Quests[QuestID].StarterNPC.Add(ID); TODO
                    }
                }
                Chck = web.GetVar("g_listviews['ends']['data'].length");
                Length = int.Parse((Chck != null ? Chck : 0).ToString());
                //Todo: Do conversions!
                for (int i = 0; i < Length; i++)
                {
                    Object c = web.GetVar("g_listviews['ends']['data']['" + i + "']['category']");
                    int category = int.Parse((c != null ? c : -1).ToString());

                    c = web.GetVar("g_listviews['ends']['data']['" + i + "']['category2']");
                    int category2 = int.Parse((c != null ? c : -1).ToString());

                    c = web.GetVar("g_listviews['ends']['data']['" + i + "']['id']");
                    int QuestID = int.Parse((c != null ? c : -1).ToString());

                    c = web.GetVar("g_listviews['ends']['data']['" + i + "']['level']");
                    int level = int.Parse((c != null ? c : -1).ToString());

                    c = web.GetVar("g_listviews['ends']['data']['" + i + "']['name']");
                    string name = (c != null ? c : "").ToString();

                    c = web.GetVar("g_listviews['ends']['data']['" + i + "']['reqlevel']");
                    int reqLevel = int.Parse((c != null ? c : -1).ToString());

                    c = web.GetVar("g_listviews['ends']['data']['" + i + "']['side']");
                    int side = int.Parse((c != null ? c : -1).ToString());

                    c = web.GetVar("g_listviews['ends']['data']['" + i + "']['type']");
                    int type = int.Parse((c != null ? c : -1).ToString());

                    c = web.GetVar("g_listviews['ends']['data']['" + i + "']['xp']");
                    int xp = int.Parse((c != null ? c : -1).ToString());
                    if (!Database.DB.Quests.ContainsKey(QuestID))
                    {
                        Database.DB.Quests.Add(QuestID, new Quest(category, category2, QuestID, level, name, reqLevel, side, xp)); //TODO
                    }
                    if (Database.DB.Quests.ContainsKey(QuestID))
                    {
                        //Database.DB.Quests[QuestID].EndNPC.Add(ID); TODO
                    }
                }
                //TODO Do stuff
                //Get Coords myMapper.getCoords() DONE
                //Get Drops DONE
                //Get Starts DONE
                //Get Ends DONE
                //Get Drops DONE
                //Get Alligiance (Horde/Ally/Both) DONE*/
                DeepFetchDone = true;
        }
 
        public bool FetchDone(){return DeepFetchDone;}
 
        //ISource
        public Type GetBaseType(){return typeof(INPC);}
        public int GetID(){return ID;}
 
 
}
}
