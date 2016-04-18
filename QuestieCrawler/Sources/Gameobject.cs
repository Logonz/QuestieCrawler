using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestieCrawler
{
    [Serializable]
    public class Gameobject : IObject, ISource {
        int ID;
        List<int> QuestsInvolved = new List<int>();
 
        //Deep Info
        bool DeepFetchDone = false;
        int WHMapID;
        public string Name; //Done
        public List<int> D_Drops = new List<int>(); //ItemIDS;
        public List<Coord> D_Locations = new List<Coord>();
 
        bool Container;
        public List<int> Contains() { return DeepFetchDone ? D_Drops : null; }

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


        public bool IsContainer() { return D_Drops.Count > 0 && DeepFetchDone ? true : false; }

        public int ObjectID() { return ID; }

        public Gameobject(int ID, string Name)
        {
            this.ID = ID;
            this.Name = Name;
        }
 
        public int WoWheadMapID(){ return WHMapID; }

        public List<Coord> ObjectLocations() { return DeepFetchDone ? D_Locations : null; }
        public List<int> RequirementFor(){return QuestsInvolved;}
 
        public void FetchWebInformation(Web web, bool forceUpdate = false){
                if(DeepFetchDone && !forceUpdate){return;}
                web.Navigate("http://db.vanillagaming.org/?object=" + ID);
            if(D_Locations == null)
            {
                D_Locations = new List<Coord>();
            }

                Object Read = (Object)web.GetVar("myMapper.zone");
                if (Read == null) { return; }
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
                //TODO Do stuff
                //Get Coords myMapper.getCoords()
                //Get locations, get what it contains
                //GetWHMapID
                //Name
                DeepFetchDone = true;
        }
 
        public bool FetchDone(){return DeepFetchDone;}
 
        //ISource
        public Type GetBaseType(){return typeof(IObject);}
        public int GetID(){return ID;}
}
}
