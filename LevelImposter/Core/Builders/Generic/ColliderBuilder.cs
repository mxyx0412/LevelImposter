﻿using System;
using Il2CppSystem.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LevelImposter.Core
{
    /// <summary>
    /// Configures the Collider2D on the GameObject
    /// </summary>
    public class ColliderBuilder : IElemBuilder
    {
        public void Build(LIElement elem, GameObject obj)
        {
            if (elem.properties.colliders == null)
                return;

            // Iterate through colliders
            foreach (LICollider colliderData in elem.properties.colliders)
            {
                // PolygonCollider2D
                if (colliderData.isSolid)
                {
                    PolygonCollider2D collider = obj.AddComponent<PolygonCollider2D>();
                    collider.pathCount = 1;
                    collider.SetPath(0, GetPoints(colliderData));
                }
                // EdgeCollider2D
                else
                {
                    EdgeCollider2D collider = obj.AddComponent<EdgeCollider2D>();
                    collider.SetPoints(GetPoints(colliderData));
                }

                // Shadow Object
                if (colliderData.blocksLight)
                {
                    GameObject shadowObj = new("Shadow " + colliderData.id);
                    shadowObj.transform.SetParent(obj.transform);
                    shadowObj.transform.localPosition = Vector3.zero;
                    shadowObj.transform.localRotation = Quaternion.Euler(Vector3.zero);
                    shadowObj.transform.localScale = Vector3.one;
                    shadowObj.layer = (int)Layer.Shadow;

                    EdgeCollider2D shadowCollider = shadowObj.AddComponent<EdgeCollider2D>();
                    shadowCollider.SetPoints(GetPoints(colliderData, colliderData.isSolid));
                }
            }
        }

        private List<Vector2> GetPoints(LICollider collider, bool wrap = false)
        {
            var list = new List<Vector2>(collider.points.Length);
            foreach (Point point in collider.points)
                list.Add(new Vector2(point.x, -point.y));
            if (wrap && list.Count > 0)
                list.Add(list[0]);
            return list;
        }

        public void PostBuild() { }
    }
}
