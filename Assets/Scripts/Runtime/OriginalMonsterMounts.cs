using System;
using System.Collections.Generic;
using UnityEngine;
namespace YesterdayConvoy
{
    internal static class OriginalMonsterMounts
    {
        [Serializable] sealed class Catalog {public int version;public Point[] points;}
        [Serializable] sealed class Point {public string cardId;public float bodyPixelsPerUnit;public Vector3 bodyLocalPoint;}
        static Dictionary<string,Point> points;
        internal static bool TryHit(string cardId,SpriteRenderer body,out Vector3 position)
        {
            position=default;if(body==null||body.sprite==null||string.IsNullOrEmpty(cardId))return false;
            if(points==null)
            {
                var data=Resources.Load<TextAsset>("OriginalMonsterMounts/HitPoints");
                var catalog=data==null?null:JsonUtility.FromJson<Catalog>(data.text);
                points=new Dictionary<string,Point>(StringComparer.Ordinal);
                if(catalog?.version==1&&catalog.points!=null)foreach(var point in catalog.points)points.Add(point.cardId,point);
                if(data!=null)Resources.UnloadAsset(data);
            }
            if(!points.TryGetValue(cardId,out var source)||Mathf.Abs(body.sprite.pixelsPerUnit-source.bodyPixelsPerUnit)>.001f)return false;
            var local=source.bodyLocalPoint;if(body.flipX)local.x=-local.x;
            position=body.transform.TransformPoint(local);return true;
        }
    }
}
