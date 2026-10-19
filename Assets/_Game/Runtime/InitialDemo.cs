using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Authored scene bindings. Combat state lives exclusively in ManualBattle.</summary>
    public sealed class InitialDemo : MonoBehaviour
    {
        [SerializeField] private Transform monster;
        [SerializeField] private Transform redGate, blueGate;
        [SerializeField] private Vector3[] waypoints;
        [SerializeField] private CardDefinition card;
        [SerializeField] private Font interfaceFont;
        public Transform Monster => monster;
        public Transform RedGate => redGate;
        public Transform BlueGate => blueGate;
        public CardDefinition Card => card;
        public Font InterfaceFont => interfaceFont;
        public System.Collections.Generic.IReadOnlyList<Vector3> Waypoints => waypoints;
    }
}
