﻿using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using LevelImposter.DB;
using PowerTools;

namespace LevelImposter.Core
{
    public class SabDoorBuilder : IElemBuilder
    {
        private int _doorId = 0;

        public void Build(LIElement elem, GameObject obj)
        {
            if (!elem.type.StartsWith("sab-door"))
                return;
            if (LIShipStatus.Instance?.ShipStatus == null)
                throw new Exception("ShipStatus not found");

            // Prefab
            var prefab = AssetDB.GetObject(elem.type);
            if (prefab == null)
                return;
            var prefabRenderer = prefab.GetComponent<SpriteRenderer>();
            var prefabDoor = prefab.GetComponent<PlainDoor>();

            // Default Sprite
            SpriteRenderer spriteRenderer = obj.GetComponent<SpriteRenderer>();
            Animator animator = obj.AddComponent<Animator>();
            SpriteAnim spriteAnim = obj.AddComponent<SpriteAnim>();
            obj.layer = (int)Layer.ShortObjects;
            bool isSpriteAnim = false;
            if (!spriteRenderer)
            {
                spriteRenderer = obj.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = prefabRenderer.sprite;
                isSpriteAnim = true;
            }
            else
            {
                spriteRenderer.enabled = false;
                spriteAnim.enabled = false;
                animator.enabled = false;
            }
            spriteRenderer.material = prefabRenderer.material;

            // Dummy Components
            BoxCollider2D dummyCollider = obj.AddComponent<BoxCollider2D>();
            dummyCollider.isTrigger = true;
            dummyCollider.enabled = false;

            // Colliders
            Collider2D[] colliders = obj.GetComponentsInChildren<Collider2D>();
            foreach (Collider2D collider in colliders)
                collider.enabled = false;

            // Door
            var doorType = elem.properties.doorType;
            bool isManualDoor = doorType == "polus" || doorType == "airship";
            ShipStatus shipStatus = LIShipStatus.Instance.ShipStatus;
            PlainDoor doorComponent;
            if (isManualDoor)
            {
                doorComponent = obj.AddComponent<PlainDoor>();
                shipStatus.Systems[SystemTypes.Doors] = new DoorsSystemType().Cast<ISystemType>();
            }
            else
            {
                doorComponent = obj.AddComponent<AutoOpenDoor>();
                shipStatus.Systems[SystemTypes.Doors] = new AutoDoorsSystemType().Cast<ISystemType>();
            }
            doorComponent.Room = RoomBuilder.GetParentOrDefault(elem);
            doorComponent.Id = _doorId++;
            doorComponent.myCollider = dummyCollider;
            doorComponent.animator = spriteAnim;
            doorComponent.OpenSound = prefabDoor.OpenSound;
            doorComponent.CloseSound = prefabDoor.CloseSound;
            shipStatus.AllDoors = MapUtils.AddToArr(shipStatus.AllDoors, doorComponent);

            // SpriteAnim
            if (isSpriteAnim)
            {
                doorComponent.OpenDoorAnim = prefabDoor.OpenDoorAnim;
                doorComponent.CloseDoorAnim = prefabDoor.CloseDoorAnim;
            }

            // Console
            if (isManualDoor)
            {
                var prefab2 = AssetDB.GetObject($"sab-door-{doorType}"); // "sab-door-polus" or "sab-door-airship"
                DoorConsole? prefab2Console = prefab2?.GetComponent<DoorConsole>();
                DoorConsole consoleComponent = obj.AddComponent<DoorConsole>();
                consoleComponent.MinigamePrefab = prefab2Console?.MinigamePrefab;
            }
        }

        public void PostBuild() {}
    }
}