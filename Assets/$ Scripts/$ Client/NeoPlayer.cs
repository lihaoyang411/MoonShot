
using UnityEngine;
using TeamBlack.MoonShot.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TeamBlack.Util;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TeamBlack.MoonShot
{
    public class NeoPlayer : MonoBehaviour
    {

        // The NeoPlayer is deceptively simple
        // It has three functions:
        // - Track ALL entities & faction variables
        // - Recieve and route entity update packets
        // - Recieve and route and player input (interaction requests)

        // It never sends requests

        // Each List<Entity> represents a faction
        // - Faction 0 should always be the neutral faction
        // - Some faction > 0 is the player's faction (id)
        // - Other factions are opponents
        [Header("Generic Entity Prefabs")]
        public GameObject OreEntityPrefab;
        public GameObject MineEntityPrefab;
        public GameObject TimeBombEntityPrefab;
        public GameObject CementBombEntityPrefab;
        public GameObject GasBombEntityPrefab;
        public GameObject HealStationEntityPrefab;
        public GameObject SonicBombEntityPrefab;
        public GameObject AntimatterBombEntityPrefab;
        public GameObject SandBagsEntityPrefab;

        [Header("Faction 1 Entity Prefabs")]
        public GameObject Faction1Hub;
        public GameObject Faction1Digger;
        public GameObject Faction1Miner;
        public GameObject Faction1Soldier;
        public GameObject Faction1Hauler;

        [Header("Faction 2 Entity Prefabs")]
        public GameObject Faction2Hub;
        public GameObject Faction2Digger;
        public GameObject Faction2Miner;
        public GameObject Faction2Soldier;
        public GameObject Faction2Hauler;

        private ServerPathfinder _pathfinder;
        public ServerPathfinder PathFinder
        {
            get
            {
                if (_pathfinder == null)
                {
                    _pathfinder = new ServerPathfinder();
                }
                _pathfinder.SetByteMap(MapManager.Map);
                return _pathfinder;
            }
        }


        [Header("(TEMP) Direct References")]
        public MapManager MapManager;
        public Text CreditCounter;
        public AudioSource CreditUpSound;

        const int MAX_FACTIONS = 4;

        public Entity[,] FactionEntities = new Entity[MAX_FACTIONS, Constants.Global.MAX_ENTITIES];

        public Entity Hub => FactionEntities[myFactionID, 0];
        
        
        private NetClient _client;
        public NetClient Client => (_client) ? _client : (_client = GetComponent<NetClient>());
        public Listened<List<Entity>> Selected = new Listened<List<Entity>>(new List<Entity>());
        private int _frontSelectIndex = 0;
        public int FrontSelectIndex
        {
            get
            {
                return _frontSelectIndex;
            }
            set
            {
                _frontSelectIndex = value % Selected.Value.Count;
            }
        }
        public Entity FrontSelected => (Selected.Value.Count <= _frontSelectIndex) ? 
            null : Selected.Value[_frontSelectIndex];


        IEnumerable<Unit> MyFactionUnits()
        {
            for (int i = 0; i < FactionEntities.GetLength(1); i++)
            {
                var curr = FactionEntities[myFactionID, i]?.GetComponent<Unit>();
                if (curr == null) continue;
                yield return curr;
            }
        }
        
        [Header("Debug")]
        public Listened<int> Credits = new Listened<int>(-1);

        public UnityEvent CreditListener = new UnityEvent();
        // I.e. Which units can I send requests to?
        public int myFactionID = -1;

        private void Awake()
        {
            // We need a file to track constants
            // Also entity ids should be intsx
            // 9 = arbitrary player max

            // When making a new selection, reset index
            Selected.Listen(() => _frontSelectIndex = 0);
            Credits.Listen(CreditListener.Invoke);
            // Faction initializers
            Client.AddCallback(NewPackets.PacketType.InitSelf, AddMyFaction);

            // Game responses
            Client.AddCallback(NewPackets.PacketType.UpdateFaction, RecieveFactionUpdate);
            Client.AddCallback(NewPackets.PacketType.UpdateEntity, RecieveEntityUpdate);

            Client.AddCallback(NewPackets.PacketType.TileUpdate, MapManager.RecieveBlockUpdate);
            Client.AddCallback(NewPackets.PacketType.MapUpdate, MapManager.RecieveMapUpdate);
            Client.AddCallback(NewPackets.PacketType.ChunkUpdate, MapManager.RecieveChunkUpdate);
            
            Client.AddCallback(NewPackets.PacketType.GameOver, GameOver);
            //Client.AddCallback(NewPackets.PacketType.TileUpdate, MapManager.RecieveBlockUpdate);
        }

        #region NetCallbacks
        public void GameOver(byte[] packet)
        {
            NewPackets.GameOver gameOver = new NewPackets.GameOver(packet);
            if(gameOver.WinningFaction == myFactionID)
                GameObject.FindObjectOfType<GameOver>().Display(true);
            else
                GameObject.FindObjectOfType<GameOver>().Display(false);
        }

        public void RecieveEntityUpdate(byte[] packet)
        {
            // Get the faction index from the header
            byte factionIndex = 0;
            packet = NewPackets.PopByteHeader(packet, out factionIndex);

            // Deserialize
            NewPackets.EntityUpdate entityUpdate = new NewPackets.EntityUpdate(packet);

            if (FactionEntities[factionIndex, entityUpdate.unitIndex] == null)
            {
                // Implicitly populate faction entities
                switch (entityUpdate.unitType)
                {
                    // Units
                    case Constants.Entities.
                         Miner.ID:
                        FactionEntities[factionIndex, entityUpdate.unitIndex] = 
                            GameObject.Instantiate(factionIndex == 1 ? Faction1Miner : Faction2Miner).GetComponent<Entity>();
                        break;
                    case Constants.Entities.
                         Digger.ID:
                        FactionEntities[factionIndex, entityUpdate.unitIndex] =
                            GameObject.Instantiate(factionIndex == 1 ? Faction1Digger : Faction2Digger).GetComponent<Entity>();
                        break;
                    case Constants.Entities.
                         Soldier.ID:
                        FactionEntities[factionIndex, entityUpdate.unitIndex] =
                            GameObject.Instantiate(factionIndex == 1 ? Faction1Soldier : Faction2Soldier).GetComponent<Entity>();
                        break;
                    case Constants.Entities.
                         Hauler.ID:
                        FactionEntities[factionIndex, entityUpdate.unitIndex] =
                            GameObject.Instantiate(factionIndex == 1 ? Faction1Hauler : Faction2Hauler).GetComponent<Entity>();
                        break;

                    //...
                    // Deployables
                    case Constants.Entities.
                         QuickSand.ID:
                        FactionEntities[factionIndex, entityUpdate.unitIndex] = GameObject.Instantiate(CementBombEntityPrefab).GetComponent<Entity>();
                        break;
                    case Constants.Entities.
                         TimeBomb.ID:
                        FactionEntities[factionIndex, entityUpdate.unitIndex] = GameObject.Instantiate(TimeBombEntityPrefab).GetComponent<Entity>();
                        break;
                    case Constants.Entities.
                         ProximityMine.ID:
                        FactionEntities[factionIndex, entityUpdate.unitIndex] = GameObject.Instantiate(MineEntityPrefab).GetComponent<Entity>();
                        break;
                    case Constants.Entities.
                         GasBomb.ID:
                        FactionEntities[factionIndex, entityUpdate.unitIndex] = GameObject.Instantiate(GasBombEntityPrefab).GetComponent<Entity>();
                        break;
                    case Constants.Entities.
                         HealStation.ID:
                        FactionEntities[factionIndex, entityUpdate.unitIndex] = GameObject.Instantiate(HealStationEntityPrefab).GetComponent<Entity>();
                        break;
                    case Constants.Entities.
                         SonicBomb.ID:
                        FactionEntities[factionIndex, entityUpdate.unitIndex] = GameObject.Instantiate(SonicBombEntityPrefab).GetComponent<Entity>();
                        break;
                    case Constants.Entities.
                         AntimatterBomb.ID:
                        FactionEntities[factionIndex, entityUpdate.unitIndex] = GameObject.Instantiate(AntimatterBombEntityPrefab).GetComponent<Entity>();
                        break;
                    case Constants.Entities.
                         SandBags.ID:
                        FactionEntities[factionIndex, entityUpdate.unitIndex] = GameObject.Instantiate(SandBagsEntityPrefab).GetComponent<Entity>();
                        break;

                    // Hub
                    case Constants.Entities.Hub.ID:
                        FactionEntities[factionIndex, entityUpdate.unitIndex] =
                            GameObject.Instantiate(factionIndex == 1 ? Faction1Hub : Faction2Hub).GetComponent<Entity>();
                        break;

                    // Ore
                    case Constants.Entities.Ore.ID:
                        FactionEntities[factionIndex, entityUpdate.unitIndex] = GameObject.Instantiate(OreEntityPrefab).GetComponent<Entity>();
                        break;

                    default:
                        Debug.Log("PLAYER ERROR: NO CASE FOR THAT ENTITY!");
                        break;
                }

                // Manually set indices for requests
                FactionEntities[factionIndex, entityUpdate.unitIndex].factionID = factionIndex;
                FactionEntities[factionIndex, entityUpdate.unitIndex].entityID = entityUpdate.unitIndex;
            }

            // Route new state to correct unit int correct faction
            FactionEntities[factionIndex, entityUpdate.unitIndex].UpdateState(entityUpdate);

            //print("GOT ENTITY UPDATE");
        }



        public void RecieveFactionUpdate(byte[] packet)
        {

            byte factionIndex = 255; // invoke runtime error?
            packet = NewPackets.PopByteHeader(packet, out factionIndex);

            if (factionIndex == myFactionID)
            {
                NewPackets.FactionUpdate fUp = new NewPackets.FactionUpdate(packet);

                if(1 < fUp.credits - Credits)
                    GameObject.FindObjectOfType<AudioManager>().PlaySell();
                if (Credits > fUp.credits)
                    GameObject.FindObjectOfType<AudioManager>().PlayBuy();

                Credits.Value = fUp.credits;
                CreditCounter.text = "" + Credits;
            }
        }

        public void AddMyFaction(byte[] packet)
        {
            byte factionIndex = 255; // invoke runtime error?
            NewPackets.PopByteHeader(packet, out factionIndex);

            // This is my faction!
            myFactionID = factionIndex;

            print("ADDING MY FACTION " + myFactionID);

            if(FactionEntities[factionIndex, 0] != null)
            {
                Debug.Log("SETTING CAM POSITION");
                Camera.main.transform.position = FactionEntities[factionIndex, 0].transform.position - new Vector3(0,0,-10);
            }
        }
        
        #endregion
        
        #region Singleton

        private static NeoPlayer _instance;
        // FIXME: make this better, allow no duplicates
        public static NeoPlayer Instance => _instance ? _instance : (_instance = FindObjectOfType<NeoPlayer>());

        #endregion
    }
}
