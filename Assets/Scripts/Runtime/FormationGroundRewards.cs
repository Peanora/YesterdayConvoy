using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleController
    {
        const float RewardPop = .205f, RewardFlightEnd = 1.09f, RewardArrivalTime = .46f;
        const float RewardLeft = 11.28f, RewardRight = 14.37f, RewardNear = .38f, RewardFar = 2.88f, RewardFloor = .12f;
        static readonly Color[] PrismLight =
        {
            new Color(.40f,.94f,.98f), new Color(.54f,.65f,1), new Color(.82f,.57f,1),
            new Color(1,.55f,.81f), new Color(1,.78f,.49f)
        };
        static readonly Vector2[] LootScatter =
        {
            new Vector2(.38f,.50f), new Vector2(.62f,.50f), new Vector2(.14f,.17f),
            new Vector2(.86f,.17f), new Vector2(.14f,.83f), new Vector2(.86f,.83f),
            new Vector2(.38f,.17f), new Vector2(.62f,.17f), new Vector2(.14f,.50f),
            new Vector2(.86f,.50f), new Vector2(.38f,.83f), new Vector2(.62f,.83f)
        };
        sealed class GroundBall
        {
            public FormationCampaignRun.RewardDrop Drop;
            public Transform Root, Body, Orbit;
            public MeshRenderer Shell;
            public Transform Shadow;
            public float Radius, Age;
            public int Grade, Slot;
            public GroundRewardFlight Flight;
        }
        sealed class GroundRewardFlight
        {
            public GroundBall Ball;
            public FormationCampaignRun.BallPayout Payout;
            public Vector2 Origin;
            public Vector2[] RecruitTargets;
            public float Age;
            public bool Arrived;
            public int ArrivalFirstItem, ArrivalItems, ArrivalGold;
        }
        readonly List<GroundBall> groundBalls = new List<GroundBall>();
        readonly List<GroundRewardFlight> rewardFlights = new List<GroundRewardFlight>();
        readonly List<Mesh> rewardMeshes = new List<Mesh>();
        readonly Dictionary<string,Sprite> componentDropSprites = new Dictionary<string,Sprite>();
        readonly Dictionary<string, Mesh> rewardShells = new Dictionary<string, Mesh>();
        readonly Dictionary<(int Grade,int Layer),Mesh> rewardRings = new Dictionary<(int,int),Mesh>();
        GameObject groundRewardRoot, groundRewardFrame;
        Material rewardMaterial, rewardDecal;
        Mesh rewardShadowMesh;
        MaterialPropertyBlock rewardTint;
        GroundBall pressedGroundBall;
        GroundBall openingGroundBall => rewardFlights.Find(flight=>!flight.Arrived)?.Ball;
        Vector2 rewardPointerStart;
        int groundRewardPage;
        int GroundRewardPages => Mathf.Max(1,Mathf.CeilToInt((Campaign?.PendingBalls.Count??0)/(float)LootScatter.Length));
        bool HasGroundRewards => Campaign?.PendingBalls.Count > 0;
        int PendingGroundRewardItems => rewardFlights.Sum(flight=>flight.Arrived?0:flight.Payout.Items.Count);
        internal bool ActiveGroundRewardHasRecruits => rewardFlights.Any(flight=>!flight.Arrived&&flight.Payout.Recruits.Count>0);
        internal bool ActiveGroundRewardHasTwoStar => rewardFlights.Any(flight=>!flight.Arrived&&flight.Payout.Recruits.Any(recruit=>recruit.Star==2));
        bool GroundRewardsVisible => Campaign != null && (!Campaign.IsPractice || Campaign.ShowsHopeRewards) && !CombatViewActive && !ViewingDefenders;
        bool CanPickGroundRewards => GroundRewardsVisible && !CampaignInteractionBlocked && !formationMenuOpen &&
            !shopOddsVisible && !rewardHistoryOpen;

        internal static float RewardSizeRatio(string color) => color switch
        {
            "白" => .72f, "金" => 1.30f, "彩" => 1.58f, _ => 1f
        };

        Vector2 RewardProjection(Vector3 point) =>
            TacticalViewport.Current.FromScreen(battleCamera.WorldToScreenPoint(point));

        internal bool TryGroundRewardPoint(int id, out Vector2 point)
        {
            var ball = groundBalls.Find(candidate => candidate.Drop.Id == id);
            bool visible = ball != null && ball.Flight == null && GroundRewardsVisible && ball.Root.gameObject.activeInHierarchy;
            point = visible ? RewardProjection(ball.Body.position) : default;
            return visible;
        }

        internal bool ProcessFormationRewardPointer(EventType type, Vector2 point)
        {
            if (type == EventType.MouseDown) pressedGroundBall = null;
            bool canPick = CanPickGroundRewards && !FormationSupplyBlocksPoint(point) &&
                !FormationTraitBlocksPoint(point) && !FormationInspectionBlocksPoint(point) && !OperationsBlocksPoint(point) &&
                !FormationEquipmentDetailsRect.Contains(point) &&
                (!(MarketVisible || ShopPanelTransitioning) || !FormationMarketRect.Contains(point));
            if (type == EventType.MouseDown)
            {
                if (!canPick) return false;
                pressedGroundBall = GroundBallAt(point);
                if (pressedGroundBall == null) return false;
                CancelFormationDrag();
                rewardPointerStart = point;
                return true;
            }
            if (pressedGroundBall == null) return false;
            if (type == EventType.MouseUp)
            {
                var selected = pressedGroundBall;
                pressedGroundBall = null;
                if (canPick && Vector2.Distance(point, rewardPointerStart) < 8 && GroundBallAt(point) == selected)
                    TryBeginGroundReward(selected.Drop.Id);
            }
            return true;
        }

        GroundBall GroundBallAt(Vector2 point)
        {
            GroundBall nearest = null;
            float best = float.MaxValue;
            foreach (var ball in groundBalls)
            {
                if (ball.Flight != null || !ball.Root.gameObject.activeSelf) continue;
                var center = RewardProjection(ball.Body.position);
                float radius = Vector2.Distance(center, RewardProjection(ball.Body.position + battleCamera.transform.right * ball.Radius));
                float distance = (point - center).sqrMagnitude;
                if (distance <= Mathf.Pow(Mathf.Max(10, radius + 6), 2) && distance < best)
                { nearest = ball; best = distance; }
            }
            return nearest;
        }

        bool TryBeginGroundReward(int id)
        {
            if (!CanPickGroundRewards) return false;
            var ball = groundBalls.Find(candidate => candidate.Drop.Id == id);
            if (ball == null || ball.Flight != null) return false;
            var payout = Campaign.BeginBall(id);
            if (payout == null) return false;
            var targets = ball.Drop.IsOpening && payout.Recruits.Count > 0
                ? Session.OpeningRecruitCenters(payout.Recruits).Select(cell => RewardProjection(ActorWorldPoint(cell))).ToArray()
                : ReserveRewardTargets(payout.Recruits.Count);
            ball.Flight = new GroundRewardFlight
            {
                Ball = ball,
                Payout = payout,
                Origin = RewardProjection(ball.Body.position + Vector3.up * .12f),
                RecruitTargets = targets
            };
            rewardFlights.Add(ball.Flight);
            if (payout.Items.Count > 0)
                RevealEquipmentSlot(Campaign.Inventory.Count + PendingGroundRewardItems - 1);
            CloseFormationTraitDetails();
            return true;
        }

        Vector2[] ReserveRewardTargets(int count)
        {
            int earlierRecruits = rewardFlights.Sum(flight => flight.Arrived ? 0 : flight.Payout.Recruits.Count);
            var vacant = Enumerable.Range(0,10).Where(slot => Session.Seats.At(false,slot) == 0).ToArray();
            var targets = new Vector2[count];
            for (int index = 0; index < count; index++)
                targets[index] = FormationReserveRect(vacant.Length == 0 ? 9 : vacant[Mathf.Min(earlierRecruits+index,vacant.Length-1)]).center;
            return targets;
        }

        void TickGroundRewards(float delta)
        {
            TickGroundRewardFlights(delta);
            if (!GroundRewardsVisible)
            {
                if (groundRewardRoot != null) groundRewardRoot.SetActive(false);
                return;
            }
            EnsureGroundRewards();
            groundRewardRoot.SetActive(true);
            groundRewardPage = Mathf.Clamp(groundRewardPage,0,GroundRewardPages-1);
            var visibleDrops = Campaign.PendingBalls.Skip(groundRewardPage*LootScatter.Length).Take(LootScatter.Length).ToArray();
            for (int index=groundBalls.Count-1;index>=0;index--)
            {
                var ball = groundBalls[index];
                if (visibleDrops.Contains(ball.Drop)) continue;
                RemoveGroundBall(ball);
            }
            foreach (var drop in visibleDrops)
            {
                if (groundBalls.Count >= LootScatter.Length) break;
                if (groundBalls.Any(ball => ball.Drop.Id == drop.Id)) continue;
                if (rewardFlights.Any(flight=>!flight.Arrived&&flight.Ball.Drop.Id==drop.Id)) continue;
                for (int slot = 0; slot < LootScatter.Length; slot++)
                    if (!groundBalls.Any(ball => ball.Slot == slot)) { AddGroundBall(drop, slot); break; }
            }
            var mouse = TacticalViewport.Current.FromScreen(Input.mousePosition);
            var hovered = CanPickGroundRewards ? GroundBallAt(mouse) : null;
            float clock = Time.unscaledTime;
            foreach (var ball in groundBalls)
            {
                if (!ball.Root.gameObject.activeSelf) continue;
                ball.Age += delta;
                bool opening = ball.Flight != null;
                float flightAge = ball.Flight?.Age ?? 0;
                bool hover = ball == hovered;
                float pop = Mathf.Clamp01(flightAge / RewardPop);
                float age = Mathf.Max(0, ball.Age);
                float fall = age < .30f ? .97f * (1 - Mathf.Pow(age / .30f, 2)) :
                    .22f * Mathf.Abs(Mathf.Sin(Mathf.Clamp01((age - .30f) / .34f) * 6.283f)) *
                    (1 - Mathf.Clamp01((age - .30f) / .34f));
                ball.Body.gameObject.SetActive(!opening || flightAge < RewardPop);
                ball.Shadow.gameObject.SetActive(ball.Body.gameObject.activeSelf);
                ball.Body.localPosition = Vector3.up * (ball.Radius + fall + (hover ? .035f : 0) + .19f * pop * pop);
                ball.Body.localScale = Vector3.one * (ball.Radius * (1 - .19f * Mathf.Sin(pop * 2.6f)));
                ball.Body.rotation = ball.Drop.Component != null ? battleCamera.transform.rotation :
                    Quaternion.Euler(24 + fall * 150, clock * 34 + ball.Drop.Id * 43 + pop * 52, -16);
                if (ball.Orbit != null)
                    ball.Orbit.localRotation = Quaternion.Euler(0, clock * (ball.Grade == 2 ? 58 : 42), 0);
                rewardTint.SetFloat("_Boost", .95f + (hover ? .22f : 0) + pop * .85f);
                if (ball.Shell != null) ball.Shell.SetPropertyBlock(rewardTint);
            }
        }

        void TickGroundRewardFlights(float delta)
        {
            if (formationMenuOpen || rewardFlights.Count == 0) return;
            bool changed = false;
            foreach (var flight in rewardFlights)
            {
                flight.Age += delta;
                if (flight.Arrived || flight.Age < RewardFlightEnd) continue;
                flight.Arrived = true;
                flight.Age = RewardFlightEnd;
                var ball = flight.Ball;
                RemoveGroundBall(ball);
                int firstItem = Campaign.Inventory.Count;
                var reward = Session.CompleteBall(ball.Drop.Id);
                if (reward == null) continue;
                flight.ArrivalGold = reward.Gold;
                flight.ArrivalFirstItem = firstItem;
                flight.ArrivalItems = Campaign.Inventory.Count - firstItem;
                if (flight.ArrivalItems > 0)
                    RevealEquipmentSlot(firstItem + flight.ArrivalItems - 1);
                notice = (ball.Drop.IsOpening ? "开局补给" : ball.Drop.Component != null ? "装备补足" : ball.Drop.Color+"球") + " · " + BallRewardText(reward);
                changed = true;
            }
            if (changed)
            {
                RefreshCampaignPreparation();
                approvedShop?.SyncShopBenchModels();
            }
            rewardFlights.RemoveAll(flight=>flight.Arrived&&flight.Age>=RewardFlightEnd+RewardArrivalTime);
        }

        void RemoveGroundBall(GroundBall ball)
        {
            if (!groundBalls.Remove(ball)) return;
            if (pressedGroundBall == ball) pressedGroundBall = null;
            ball.Root.gameObject.SetActive(false);
            Destroy(ball.Root.gameObject);
        }

        void EnsureGroundRewards()
        {
            if (groundRewardRoot != null) return;
            var shader = Resources.Load<Shader>("FormationRewardCrystal");
            rewardMaterial = new Material(shader) { name = "Formation reward crystal" };
            rewardDecal = new Material(Shader.Find("Sprites/Default")) { name = "Formation reward contact" };
            rewardTint = new MaterialPropertyBlock();
            groundRewardRoot = new GameObject("Ground reward drops");
            groundRewardRoot.transform.SetParent(transform, false);
            groundRewardFrame = new GameObject("Reward bay / ground markings");
            groundRewardFrame.transform.SetParent(groundRewardRoot.transform, false);
            Color frame = new Color(.74f,.59f,.33f);
            PlaneRewardQuad(RewardLeft, RewardNear, RewardRight, RewardFar, new Color(.11f,.10f,.08f,.35f), false);
            foreach (float col in new[] { RewardLeft, RewardRight })
            {
                PlaneRewardQuad(col - .023f, RewardNear, col + .023f, RewardFar, frame, true);
                foreach (float end in new[] { RewardNear, RewardFar })
                    PlaneRewardQuad(col - .038f, end - .17f, col + .038f, end + .17f, BallAccent("金"), true);
            }
            foreach (float row in new[] { RewardNear, RewardFar })
                PlaneRewardQuad(RewardLeft, row - .025f, RewardRight, row + .025f, frame, true);
            rewardShadowMesh = MakeRewardShadow();
        }

        void ClearGroundRewards()
        {
            pressedGroundBall = null;
            rewardFlights.Clear();
            foreach (var ball in groundBalls)
            {
                ball.Root.gameObject.SetActive(false);
                Destroy(ball.Root.gameObject);
            }
            groundBalls.Clear();
            if (groundRewardRoot != null) groundRewardRoot.SetActive(false);
        }

        void DisposeGroundRewards()
        {
            if (groundRewardRoot != null) Destroy(groundRewardRoot);
            foreach (var mesh in rewardMeshes) Destroy(mesh);
            foreach (var sprite in componentDropSprites.Values) Destroy(sprite);
            if (rewardMaterial != null) Destroy(rewardMaterial);
            if (rewardDecal != null) Destroy(rewardDecal);
        }

        Vector3 GroundRewardPoint(float col,float row,float height)
        {
            var point = WorldPoint(new Vector2(col,row));
            point.y = height;
            return point;
        }

        void AddGroundBall(FormationCampaignRun.RewardDrop drop,int index)
        {
            var offset = LootScatter[index % LootScatter.Length];
            var root = new GameObject(drop.Component != null ? "装备补足 · "+drop.Component : drop.Color+"球").transform;
            root.SetParent(groundRewardRoot.transform, false);
            root.position = GroundRewardPoint(Mathf.Lerp(RewardLeft, RewardRight, offset.x),
                Mathf.Lerp(RewardFar, RewardNear, offset.y), RewardFloor + .012f);
            var body = new GameObject("3D crystal").transform;
            body.SetParent(root,false);
            float radius = .228f * RewardSizeRatio(drop.Color);
            body.localPosition = Vector3.up * (radius + .96f);
            body.localScale = Vector3.one * radius;
            MeshRenderer shell = null;
            if (drop.Component == null) shell = RewardMeshChild("Faceted shell",body,RewardShell(drop.Color),rewardMaterial);
            else
            {
                var art = SupplyItemArt(drop.Component);
                if (art != null)
                {
                    var display = new GameObject("拾取装备") {layer=FormationLeftLayer};
                    display.transform.SetParent(body,false);
                    var renderer = display.AddComponent<SpriteRenderer>();
                    if (!componentDropSprites.TryGetValue(drop.Component,out var sprite))
                        componentDropSprites[drop.Component] = sprite = Sprite.Create(art,new Rect(0,0,art.width,art.height),new Vector2(.5f,.5f),art.width*.47f);
                    renderer.sprite = sprite;
                }
            }
            int grade = drop.Color == "彩" ? 2 : drop.Color == "金" ? 1 : 0;
            Transform orbit = null;
            if (grade != 0)
            {
                orbit = new GameObject("Crystal orbit").transform;
                orbit.SetParent(body,false);
                for (int indexRing = 0; indexRing < grade; indexRing++)
                {
                    var key = (grade,indexRing);
                    if (!rewardRings.TryGetValue(key,out var ring))
                        rewardRings[key] = ring = RewardRing(1.24f + indexRing * .10f,grade);
                    var arc = RewardMeshChild("Orbit ring",orbit,ring,rewardMaterial);
                    arc.transform.localRotation = Quaternion.Euler(indexRing == 0 ? 43 : -49, indexRing * 61, indexRing * 30);
                }
            }
            var shadow = RewardMeshChild("Ground contact", root, rewardShadowMesh, rewardDecal).transform;
            shadow.localScale = Vector3.one * (radius * 1.65f);
            groundBalls.Add(new GroundBall { Drop = drop, Root = root, Body = body, Orbit = orbit, Shell = shell,
                Shadow = shadow, Radius = radius, Age = -.045f * index, Grade = grade, Slot = index });
        }

        MeshRenderer RewardMeshChild(string name,Transform parent,Mesh mesh,Material material)
        {
            var child = new GameObject(name) { layer = FormationLeftLayer };
            child.transform.SetParent(parent,false);
            child.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }

        Mesh StoreRewardMesh(List<Vector3> vertices,List<Color> colors,List<int> indices,bool normals)
        {
            var mesh = new Mesh { name = "Formation reward geometry" };
            mesh.SetVertices(vertices); mesh.SetColors(colors); mesh.SetTriangles(indices,0);
            if (normals) mesh.RecalculateNormals();
            rewardMeshes.Add(mesh);
            return mesh;
        }

        void PlaneRewardQuad(float left,float near,float right,float far,Color color,bool solid)
        {
            float height = RewardFloor + (solid ? .009f : .004f);
            var vertices = new List<Vector3> { GroundRewardPoint(left,near,height), GroundRewardPoint(left,far,height),
                GroundRewardPoint(right,far,height), GroundRewardPoint(right,near,height) };
            var mesh = StoreRewardMesh(vertices,Enumerable.Repeat(color,4).ToList(),new List<int>{0,1,2,0,2,3},solid);
            var renderer = RewardMeshChild("Ground line",groundRewardFrame.transform,mesh,solid ? rewardMaterial : rewardDecal);
            if (solid)
            {
                rewardTint.SetFloat("_Boost",1.25f);
                renderer.SetPropertyBlock(rewardTint);
            }
        }

        Mesh MakeRewardShadow()
        {
            var vertices = new List<Vector3> { Vector3.zero };
            var colors = new List<Color> { new Color(.015f,.013f,.015f,.68f) };
            var indices = new List<int>();
            for (int index = 0; index <= 24; index++)
            {
                float angle = index * Mathf.PI / 12;
                vertices.Add(new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle)));
                colors.Add(new Color(.015f,.013f,.015f,0));
                if (index > 0) { indices.Add(0);indices.Add(index + 1);indices.Add(index); }
            }
            return StoreRewardMesh(vertices,colors,indices,false);
        }

        static float LootNoise(int seed) => Mathf.Repeat(Mathf.Sin(seed * 127.1f + 311.7f) * 43758.5453f,1);

        Mesh RewardShell(string color)
        {
            if (rewardShells.TryGetValue(color,out var shared)) return shared;
            float phi = (1 + Mathf.Sqrt(5)) / 2;
            var points = new[] {
                new Vector3(-1,phi,0),new Vector3(1,phi,0),new Vector3(-1,-phi,0),new Vector3(1,-phi,0),
                new Vector3(0,-1,phi),new Vector3(0,1,phi),new Vector3(0,-1,-phi),new Vector3(0,1,-phi),
                new Vector3(phi,0,-1),new Vector3(phi,0,1),new Vector3(-phi,0,-1),new Vector3(-phi,0,1) };
            int[] faces = {0,11,5,0,5,1,0,1,7,0,7,10,0,10,11,1,5,9,5,11,4,11,10,2,10,7,6,7,1,8,
                3,9,4,3,4,2,3,2,6,3,6,8,3,8,9,4,9,5,2,4,11,6,2,10,8,6,7,9,8,1};
            Color baseColor = color switch { "蓝" => new Color(.07f,.52f,.88f), "金" => new Color(.91f,.58f,.13f),
                "彩" => new Color(.47f,.40f,.79f), _ => new Color(.73f,.82f,.90f) };
            var vertices = new List<Vector3>();
            var colors = new List<Color>();
            var indices = new List<int>();
            void Subdivide(Vector3 first,Vector3 second,Vector3 third,int depth)
            {
                if (depth > 0)
                {
                    var edgeAB = (first + second).normalized;
                    var edgeBC = (second + third).normalized;
                    var edgeCA = (third + first).normalized;
                    Subdivide(first,edgeAB,edgeCA,depth-1);Subdivide(second,edgeBC,edgeAB,depth-1);
                    Subdivide(third,edgeCA,edgeBC,depth-1);Subdivide(edgeAB,edgeBC,edgeCA,depth-1);
                    return;
                }
                int index = vertices.Count;
                float variation = LootNoise(index + 7);
                Color face = color == "彩" ? Color.Lerp(baseColor,PrismLight[(index / 3) % PrismLight.Length],.72f) : baseColor;
                face = Color.Lerp(face, variation > .2f ? Color.white : Color.black, variation > .2f ? variation * .28f : .22f);
                vertices.Add(first);vertices.Add(second);vertices.Add(third);
                colors.Add(face);colors.Add(face);colors.Add(face);
                indices.Add(index);indices.Add(index+1);indices.Add(index+2);
            }
            for (int index = 0; index < faces.Length; index += 3)
                Subdivide(points[faces[index]].normalized,points[faces[index+1]].normalized,points[faces[index+2]].normalized,2);
            return rewardShells[color] = StoreRewardMesh(vertices,colors,indices,true);
        }

        Mesh RewardRing(float radius,int grade)
        {
            var points = new List<Vector3>();
            var colors = new List<Color>();
            var indices = new List<int>();
            int arcs = grade == 2 ? 5 : 3;
            for (int arc = 0; arc < arcs; arc++)
            {
                int start = points.Count;
                for (int segment = 0; segment <= 12; segment++)
                {
                    float angle = (arc + segment / 15f) * Mathf.PI * 2 / arcs;
                    var direction = new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                    points.Add(direction * (radius - .025f));points.Add(direction * (radius + .025f));
                    Color color = grade == 2 ? PrismLight[arc] : new Color(1,.80f,.39f);
                    colors.Add(color);colors.Add(color);
                    if (segment == 0) continue;
                    int offset = start + segment * 2;
                    indices.AddRange(new[]{offset-2,offset,offset-1,offset-1,offset,offset+1});
                }
            }
            return StoreRewardMesh(points,colors,indices,true);
        }

        void DrawFormationRewardFlights()
        {
            if (CombatViewActive || ViewingDefenders || formationMenuOpen || Event.current.type != EventType.Repaint) return;
            if (Campaign?.OpeningRewardsPending == true && !CampaignInteractionBlocked)
            {
                var anchor = RewardProjection(GroundRewardPoint((RewardLeft + RewardRight) * .5f, RewardFar, .3f));
                var label = new Rect(anchor.x - 64, anchor.y - 22, 128, 19);
                Fill(label, new Color(.10f,.15f,.19f,.89f));
                EconomyLabel(label, "开局补给 · 点击拾取", 11, EconomyIce, true, TextAnchor.MiddleCenter);
            }
            if (rewardFlights.Count == 0) return;
            int nextItem = Campaign.Inventory.Count;
            foreach (var flight in rewardFlights)
            {
                if (flight.Arrived)
                {
                    DrawRewardArrival(flight);
                    continue;
                }
                if (flight.Age >= .155f) DrawGroundRewardFlight(flight,nextItem);
                nextItem += flight.Payout.Items.Count;
            }
        }

        void DrawRewardArrival(GroundRewardFlight flight)
        {
            float age = flight.Age - RewardFlightEnd;
            Color arrival = new Color(1,.84f,.46f,.92f * (1 - age / RewardArrivalTime));
            var wallet = FormationCoinRect;
            if (flight.ArrivalGold > 0)
            {
                ApprovedUi.Border(wallet,arrival,2);
                TraitLabel(new Rect(wallet.x+8,wallet.y-22-age*26,wallet.width-16,22),
                    "+"+flight.ArrivalGold,15,arrival,true,TextAnchor.MiddleRight);
            }
            for (int index = 0; index < flight.ArrivalItems; index++)
            {
                var slot = EquipmentTileOnScreen(flight.ArrivalFirstItem + index);
                if (EquipmentTilesViewport.Overlaps(slot)) ApprovedUi.Border(slot,arrival,2);
            }
        }

        void DrawGroundRewardFlight(GroundRewardFlight flight,int firstItem)
        {
            DrawGroundRewardBurst(flight,flight.Age - .155f);
            var payout = flight.Payout;
            int coins = Mathf.Min(5,payout.Gold);
            for (int index = 0; index < coins; index++)
                DrawRewardFlight(flight,FormationCoinRect.center,null,true,.035f * index,index-(coins-1)*.5f);
            for (int index = 0; index < payout.Items.Count; index++)
                DrawRewardFlight(flight,EquipmentFlightTarget(firstItem+index),payout.Items[index],false,.065f*index,index);
            for (int index = 0; index < payout.Recruits.Count; index++)
                DrawRewardFlight(flight,flight.RecruitTargets[index],null,false,index*.055f,index-1,payout.Recruits[index]);
            if (payout.Refreshes > 0)
            {
                var refresh = MarketVisible ? FormationRefreshRect.center : FormationCoinRect.center;
                DrawRewardFlight(flight,refresh,"刷新",false,.075f,0);
            }
            if (payout.Choice != null)
                DrawRewardFlight(flight,RewardChoiceRect.center,"自选",false,.035f,0);
        }

        void DrawGroundRewardBurst(GroundRewardFlight flight,float age)
        {
            var ball = flight.Ball;
            float duration = .41f + ball.Grade * .09f;
            float time = Mathf.Clamp01(age / duration);
            if (time >= 1) return;
            float ease = 1 - Mathf.Pow(1-time,3);
            var center = flight.Origin;
            var hue = BallAccent(ball.Drop.Color);
            int count = 16 + ball.Grade * 7;
            for (int index = 0; index < count; index++)
            {
                float angle = index * 2.399963f;
                float distance = (9 + LootNoise(index+ball.Drop.Id) * (29+ball.Grade*8)) * ease;
                var position = center + new Vector2(Mathf.Cos(angle)*distance,Mathf.Sin(angle)*distance*.68f+11*time*time);
                var tint = ball.Grade == 2 ? PrismLight[index % PrismLight.Length] : hue;
                tint.a = (1-time) * (.67f + .33f * LootNoise(index));
                float size = (1.6f + LootNoise(index+8)*3.5f) * (1-.65f*time);
                DrawLootStreak(position,position + new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*size*2,tint,size);
            }
            if (ball.Grade > 0)
                for (int index = 0; index < 32; index++)
                {
                    if (index % 8 == 7) continue;
                    float angle = index * Mathf.PI / 16;
                    float span = 11 + (34+ball.Grade*7)*ease;
                    Vector2 Spot(float theta) => center + new Vector2(Mathf.Cos(theta)*span,Mathf.Sin(theta)*span*.55f);
                    var tint = ball.Grade == 2 ? PrismLight[index / 7 % PrismLight.Length] : hue;
                    tint.a = (1-time)*.78f;
                    DrawLootStreak(Spot(angle),Spot(angle+Mathf.PI/16),tint,1.5f);
                }
        }

        void DrawRewardFlight(GroundRewardFlight flight,Vector2 target,string key,bool coin,float delay,float fan,FormationCampaignRun.RewardRecruit recruit = null)
        {
            float flightDuration = RewardFlightEnd - RewardPop - delay;
            float time = (flight.Age - RewardPop - delay) / flightDuration;
            if (time < 0) return;
            time = Mathf.Clamp01(time);
            var origin = flight.Origin;
            var first = origin + new Vector2((coin ? 37 : -35)+12*fan,-79-12*fan);
            var last = target + (coin ? new Vector2(-56+8*fan,-95) : new Vector2(-100-9*fan,65));
            Vector2 Spot(float progress)
            {
                float curve = Mathf.Clamp01(progress);
                float inverse = 1 - curve;
                return inverse*inverse*inverse*origin + 3*inverse*inverse*curve*first +
                    3*inverse*curve*curve*last + curve*curve*curve*target;
            }
            if (time < 1)
                for (int index = 9; index > 0; index--)
                {
                    float strength = 1 - index / 10f;
                    var hue = coin ? new Color(1,.80f,.35f) : new Color(.52f,.88f,1);
                    hue.a = strength * strength * .84f;
                    DrawLootStreak(Spot(time-index*.027f),Spot(time-(index-1)*.027f),hue,strength * 4);
                }
            var point = Spot(time);
            float size = coin ? Mathf.Lerp(20,12,time) : Mathf.Lerp(36,29,time);
            var rect = new Rect(point.x-size/2,point.y-size/2,size,size);
            if (recruit != null)
            {
                Fill(rect,TraitPanel);ApprovedUi.Border(rect,BallAccent(recruit.Star==2?"金":"蓝"),2);
                var face = FormationUnitArt.For(recruit.CardId)?.Portrait;
                if (face != null) ApprovedShopPresentation.DrawTestPortrait(new Rect(rect.x+2,rect.y+2,rect.width-4,rect.height-4),face);
                else TraitLabel(rect,recruit.Name,9,TraitInk,false,TextAnchor.MiddleCenter);
                TraitLabel(new Rect(rect.x,rect.y,rect.width,13),recruit.Star+"★",10,BallAccent("金"),true);
            }
            else if (coin && formationCoin != null) GUI.DrawTexture(rect,formationCoin,ScaleMode.ScaleToFit,true);
            else if (key == "刷新" || key == "自选")
                TraitLabel(new Rect(point.x-24,point.y-13,48,26),key,13,BallAccent("金"),true,TextAnchor.MiddleCenter);
            else DrawSupplyItem(rect,key);
        }

        static void DrawLootStreak(Vector2 start,Vector2 end,Color color,float thickness)
        {
            Vector2 edge = end - start;
            if (edge.sqrMagnitude < .001f) return;
            var matrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(edge.y,edge.x)*Mathf.Rad2Deg,start);
            Fill(new Rect(start.x,start.y - thickness/2,edge.magnitude,thickness),color);
            GUI.matrix = matrix;
        }
    }
}
