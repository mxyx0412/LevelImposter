﻿using LevelImposter.Core;
using TMPro;
using UnityEngine;

namespace LevelImposter.Builders
{
    public class RoomNameBuilder : IElemBuilder
    {
        private int _nameCount = 0;

        public void Build(LIElement elem, GameObject obj)
        {
            if (elem.type != "util-room")
                return;

            // Check Visibility
            bool isMinimapVisible = elem.properties.isRoomNameVisible ?? true;
            if (!isMinimapVisible)
                return;

            // ShipStatus
            var shipStatus = LIShipStatus.Instance?.ShipStatus;
            if (shipStatus == null)
                throw new MissingShipException();

            MapBehaviour mapBehaviour = MinimapBuilder.GetMinimap();

            // Clone
            Transform roomNames = mapBehaviour.transform.GetChild(mapBehaviour.transform.childCount - 1);
            GameObject roomNameClone = roomNames.GetChild(0).gameObject;

            // Object
            float mapScale = shipStatus.MapScale;
            GameObject roomName = UnityEngine.Object.Instantiate(roomNameClone, roomNames);
            roomName.name = elem.name;
            roomName.layer = (int)Layer.UI;
            roomName.transform.localPosition = new Vector3(
                elem.x / mapScale,
                elem.y / mapScale,
                -1
            );

            // Text
            UnityEngine.Object.Destroy(roomName.GetComponent<TextTranslatorTMP>());
            TextMeshPro roomText = roomName.GetComponent<TextMeshPro>();
            roomText.text = elem.name.Replace("\\n", "\n");
            roomText.fontSizeMin = roomText.fontSizeMax;
            roomText.alignment = TextAlignmentOptions.Bottom;
            roomText.enabled = true;
            _nameCount++;

            // Transform
            RectTransform rectTransform = roomName.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(10, 0);
        }

        public void PostBuild()
        {
            MapBehaviour mapBehaviour = MinimapBuilder.GetMinimap();
            Transform roomNames = mapBehaviour.transform.GetChild(mapBehaviour.transform.childCount - 1);

            while (roomNames.childCount > _nameCount)
                UnityEngine.Object.DestroyImmediate(roomNames.GetChild(0).gameObject);
        }
    }
}
