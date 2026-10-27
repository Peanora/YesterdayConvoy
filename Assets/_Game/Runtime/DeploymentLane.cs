using System;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Immutable red-gate path. Junctions never change a deployed entity's lane.</summary>
    public sealed class DeploymentLane
    {
        public string Id { get; }
        public string ExitId { get; }
        public IReadOnlyList<Vector3> Points { get; }
        public Vector3 Entry => Points[0];
        public Vector3 Exit => Points[Points.Count-1];
        public DeploymentLane(string id,string exitId,IReadOnlyList<Vector3> points)
        {
            if(string.IsNullOrWhiteSpace(id)||string.IsNullOrWhiteSpace(exitId)) throw new ArgumentException("Missing gate identity.");
            _=new RouteMotion(points,1);
            var copy=new Vector3[points.Count];for(int i=0;i<copy.Length;i++)copy[i]=points[i];
            Id=id;ExitId=exitId;Points=Array.AsReadOnly(copy);
        }
    }
}
