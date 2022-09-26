using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using LevelImposter.Shop;
using LevelImposter.DB;
using System.Diagnostics;

namespace LevelImposter.Core
{
    public class LIShipStatus : MonoBehaviour
    {
        public const int Y_OFFSET = 25;
        public const float PLAYER_POS = -5.0f;

        public static LIShipStatus Instance { get; private set; }

        public ShipStatus shipStatus { get; private set; }
        public LIMap currentMap { get; private set; }

        private BuildRouter buildRouter = new BuildRouter();
        private Stopwatch stopWatch = new Stopwatch();
        private readonly List<string> priorityTypes = new()
        {
            "util-minimap",
            "util-room"
        };
        private readonly Dictionary<string, string> exileIDs = new()
        {
            { "Skeld", "ss-skeld" },
            { "MiraHQ", "ss-mira" },
            { "Polus", "ss-polus" },
            { "Airship", "ss-airship" }
        };

        public LIShipStatus(IntPtr intPtr) : base(intPtr)
        {
        }

        public void Awake()
        {
            Destroy(GetComponent<TagAmbientSoundPlayer>());

            shipStatus = GetComponent<ShipStatus>();
            Instance = this;
            if (MapLoader.currentMap != null)
                LoadMap(MapLoader.currentMap);
            else
                LILogger.Info("No map content, no LI data will load");
        }

        public void Start()
        {
            if (MapLoader.currentMap != null)
                HudManager.Instance.ShadowQuad.material.SetInt("_Mask", 7);
        }
        
        public void ResetMap()
        {
            while (transform.childCount > 0)
                DestroyImmediate(transform.GetChild(0).gameObject);

            FollowerCamera camera = Camera.main.GetComponent<FollowerCamera>();
            camera.shakeAmount = 0;
            camera.shakePeriod = 0;

            shipStatus.AllDoors = new UnhollowerBaseLib.Il2CppReferenceArray<PlainDoor>(0);
            shipStatus.DummyLocations = new UnhollowerBaseLib.Il2CppReferenceArray<Transform>(0);
            shipStatus.SpecialTasks = new UnhollowerBaseLib.Il2CppReferenceArray<PlayerTask>(0);
            shipStatus.CommonTasks = new UnhollowerBaseLib.Il2CppReferenceArray<NormalPlayerTask>(0);
            shipStatus.LongTasks = new UnhollowerBaseLib.Il2CppReferenceArray<NormalPlayerTask>(0);
            shipStatus.NormalTasks = new UnhollowerBaseLib.Il2CppReferenceArray<NormalPlayerTask>(0);
            shipStatus.SystemNames = new UnhollowerBaseLib.Il2CppStructArray<StringNames>(0);
            shipStatus.Systems = new Il2CppSystem.Collections.Generic.Dictionary<SystemTypes, ISystemType>();
            shipStatus.MedScanner = null;
            shipStatus.Type = ShipStatus.MapType.Ship;
            shipStatus.WeaponsImage = null;

            shipStatus.InitialSpawnCenter = new Vector2(0, -Y_OFFSET);
            shipStatus.MeetingSpawnCenter = new Vector2(0, -Y_OFFSET);
            shipStatus.MeetingSpawnCenter2 = new Vector2(0, -Y_OFFSET);

            shipStatus.Systems.Add(SystemTypes.Electrical, new SwitchSystem().Cast<ISystemType>());
            shipStatus.Systems.Add(SystemTypes.MedBay, new MedScanSystem().Cast<ISystemType>());
            //shipStatus.Systems.Add(SystemTypes.Doors, new DoorsSystemType().Cast<ISystemType>()); // <-- Doors w/ Task
            //shipStatus.Systems.Add(SystemTypes.Doors, new AutoDoorsSystemType().Cast<ISystemType>()); // <-- Doors w/o Task
            shipStatus.Systems.Add(SystemTypes.Comms, new HudOverrideSystemType().Cast<ISystemType>());
            shipStatus.Systems.Add(SystemTypes.Security, new SecurityCameraSystemType().Cast<ISystemType>());
            shipStatus.Systems.Add(SystemTypes.Laboratory, new ReactorSystemType(60f, SystemTypes.Laboratory).Cast<ISystemType>()); // <- Seconds, SystemType
            shipStatus.Systems.Add(SystemTypes.Ventilation, new VentilationSystem().Cast<ISystemType>());
            shipStatus.Systems.Add(SystemTypes.Sabotage, new SabotageSystemType(new IActivatable[] {
                shipStatus.Systems[SystemTypes.Electrical].Cast<IActivatable>(),
                shipStatus.Systems[SystemTypes.Comms].Cast<IActivatable>(),
                shipStatus.Systems[SystemTypes.Laboratory].Cast<IActivatable>()
            }).Cast<ISystemType>());

            MapUtils.systemRenames.Clear();
            MapUtils.taskRenames.Clear();
        }

        public void LoadMap(LIMap map)
        {
            LILogger.Info("Loading " + map.name + " [" + map.id + "]");
            currentMap = map;
            ResetMap();
            LoadMapProperties(map);

            // Asset DB
            if (!AssetDB.isReady)
                LILogger.Warn("Asset DB is not ready yet!");

            // Priority First
            foreach (string type in priorityTypes)
                foreach (LIElement elem in map.elements)
                    if (elem.type == type)
                        AddElement(elem);
            // Everything Else
            foreach (LIElement elem in map.elements)
                if (!priorityTypes.Contains(elem.type))
                    AddElement(elem);

            buildRouter.PostBuild();
            LILogger.Info("Map load completed");
        }

        public void LoadMapProperties(LIMap map)
        {
            shipStatus.name = map.name;

            if (!string.IsNullOrEmpty(map.properties.bgColor))
            {
                Color bgColor;
                ColorUtility.TryParseHtmlString(map.properties.bgColor, out bgColor);
                Camera.main.backgroundColor = bgColor;
            }

            if (!string.IsNullOrEmpty(map.properties.exileID))
            {
                if (exileIDs.ContainsKey(map.properties.exileID))
                {
                    ShipStatus ship = AssetDB.ss[exileIDs[map.properties.exileID]].ShipStatus;
                    shipStatus.ExileCutscenePrefab = ship.ExileCutscenePrefab;
                }
                else
                {
                    LILogger.Warn("Unknown exile ID: " + map.properties.exileID);
                }
            }
        }

        public void AddElement(LIElement element)
        {
            stopWatch.Restart();
            stopWatch.Start();

            LILogger.Info("Adding " + element.ToString());
            try
            {
                GameObject gameObject = buildRouter.Build(element);
                gameObject.transform.SetParent(transform);
                gameObject.transform.localPosition -= new Vector3(0, Y_OFFSET, -((element.y - Y_OFFSET) / 1000.0f) + PLAYER_POS);
            }
            catch (Exception e)
            {
                LILogger.Error("Error while building " + element.name + ":\n" + e);
            }

            stopWatch.Stop();
            if (stopWatch.ElapsedMilliseconds > 1000)
            {
                float seconds = Mathf.Round(stopWatch.ElapsedMilliseconds / 100f) / 10f;
                LILogger.Warn("Took " + seconds + "s to build " + element.name);
            }
        }
    }
}
