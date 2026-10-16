using System;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed class PrototypeUnit : MonoBehaviour
    {
        [SerializeField] private UnitDefinition definition;

        public UnitDefinition Definition => definition;
        public RouteMotion Motion { get; private set; }
        public string UnitId { get; private set; }

        public void Initialize(IReadOnlyList<Vector3> worldWaypoints)
        {
            if (Motion != null)
                throw new InvalidOperationException("Prototype unit is already initialized.");
            if (definition == null)
                throw new InvalidOperationException(name + ": missing UnitDefinition.");
            var motion = new RouteMotion(worldWaypoints, definition.MoveSpeed);
            UnitId = definition.UnitId;
            Motion = motion;
            transform.position = motion.Position;
        }

        public void Simulate(float simulationDelta)
        {
            if (Motion == null)
                throw new InvalidOperationException("Initialize the prototype unit before simulation.");
            bool wasArrived = Motion.HasArrived;
            Motion.Advance(simulationDelta);
            transform.position = Motion.Position;
            if (!wasArrived && Motion.HasArrived)
                Debug.Log($"ROUTE_ARRIVED: {UnitId}; distance={Motion.DistanceTravelled:F3}. No gate damage implemented.");
        }
    }
}
