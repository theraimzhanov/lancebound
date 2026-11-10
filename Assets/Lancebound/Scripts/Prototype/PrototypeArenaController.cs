using Lancebound.Match;
using UnityEngine;

namespace Lancebound.Prototype
{
    /// <summary>
    /// Zero-asset playable prototype. Add this component to an empty scene and press Play.
    /// It creates the arena, riders, camera and mobile controls at runtime.
    /// </summary>
    public sealed class PrototypeArenaController : MonoBehaviour
    {
        private const float LaneLength = 32f;
        private const float ImpactLongitudinalDistance = 0.35f;
        private const float RideSpeed = 9f;

        private JoustMatch match;
        private Transform player;
        private Transform opponent;
        private Transform playerLance;
        private Transform opponentLance;
        private Transform arenaCamera;
        private float countdown;
        private float playerAim;
        private float bracePressedAt = -100f;
        private string announcement = "Prepare your lance";
        private Vector2 dragStart;
        private bool dragging;

        private void Awake()
        {
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            Application.targetFrameRate = 60;
            BuildArena();
            StartMatch();
        }

        private void Update()
        {
            ReadAimInput();

            if (Input.GetKeyDown(KeyCode.Space))
                bracePressedAt = Time.time;

            if (match.Phase == MatchPhase.Countdown)
            {
                countdown -= Time.deltaTime;
                announcement = countdown > 0f ? $"Round {match.Round} — {Mathf.CeilToInt(countdown)}" : "CHARGE!";
                if (countdown <= 0f)
                    match.BeginCharge();
            }
            else if (match.Phase == MatchPhase.Charging)
            {
                MoveRiders();
            }
        }

        private void MoveRiders()
        {
            player.position += Vector3.forward * (RideSpeed * Time.deltaTime);
            opponent.position += Vector3.back * (RideSpeed * Time.deltaTime);
            playerLance.localRotation = Quaternion.Euler(90f + playerAim, 0f, 0f);

            // Riders occupy separate lanes, so lateral distance must not prevent an impact.
            // Detect the moment their positions meet along the direction of travel instead.
            var longitudinalDistance = Mathf.Abs(player.position.z - opponent.position.z);
            if (longitudinalDistance <= ImpactLongitudinalDistance || player.position.z >= opponent.position.z)
                ResolvePass();
        }

        private void LateUpdate()
        {
            if (arenaCamera == null || player == null || opponent == null)
                return;

            var focus = Vector3.Lerp(player.position, opponent.position, 0.5f);
            var desiredPosition = focus + new Vector3(-10.5f, 6.8f, -7.5f);
            arenaCamera.position = Vector3.Lerp(arenaCamera.position, desiredPosition, 5f * Time.unscaledDeltaTime);
            arenaCamera.rotation = Quaternion.Slerp(
                arenaCamera.rotation,
                Quaternion.LookRotation(focus + Vector3.up - arenaCamera.position),
                7f * Time.unscaledDeltaTime);
        }

        private void ResolvePass()
        {
            var aimAccuracy = 1f - Mathf.Clamp01(Mathf.Abs(playerAim) / 28f);
            var braceAccuracy = 1f - Mathf.Clamp01(Mathf.Abs(Time.time - bracePressedAt - 0.25f) / 1.1f);
            var playerZone = aimAccuracy > 0.82f ? HitZone.Helmet : aimAccuracy > 0.38f ? HitZone.Torso : HitZone.Miss;

            var aiAccuracy = Random.Range(0.58f, 0.93f);
            var aiBrace = Random.Range(0.55f, 0.9f);
            var aiZone = aiAccuracy > 0.8f ? HitZone.Helmet : HitZone.Torso;

            match.ResolvePass(
                new ImpactInput(aimAccuracy, braceAccuracy, RideSpeed * 2f, 13.5f, playerZone),
                new ImpactInput(aiAccuracy, aiBrace, RideSpeed * 2f, 13.5f, aiZone),
                Random.Range(0.95f, 1.05f));

            announcement = BuildResultText();
            Time.timeScale = 0.2f;
            Invoke(nameof(RestoreTime), 0.12f);
        }

        private string BuildResultText()
        {
            var playerHit = match.LastPlayerImpact;
            if (playerHit.Unhorsed) return "UNHORSED! Victory!";
            if (match.LastOpponentImpact.Unhorsed) return "You were unhorsed";
            if (playerHit.LanceBroken) return $"Lance shattered! +{playerHit.Points}";
            if (playerHit.Points == 0) return "Missed — recover for the next pass";
            return $"Clean hit! +{playerHit.Points}";
        }

        private void RestoreTime()
        {
            Time.timeScale = 1f;
        }

        private void ReadAimInput()
        {
            if (Input.touchCount == 0)
            {
                if (Input.GetMouseButtonDown(0) && Input.mousePosition.x >= Screen.width * 0.35f)
                {
                    dragStart = Input.mousePosition;
                    dragging = true;
                }
                else if (Input.GetMouseButton(0) && dragging)
                {
                    playerAim = Mathf.Clamp((Input.mousePosition.y - dragStart.y) * -0.08f, -28f, 28f);
                }
                else if (Input.GetMouseButtonUp(0))
                {
                    dragging = false;
                }

                return;
            }

            var touch = Input.GetTouch(0);
            if (touch.position.x < Screen.width * 0.35f)
                return;

            if (touch.phase == TouchPhase.Began)
            {
                dragStart = touch.position;
                dragging = true;
            }
            else if (dragging && (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary))
            {
                playerAim = Mathf.Clamp((touch.position.y - dragStart.y) * -0.08f, -28f, 28f);
            }
            else if (touch.phase is TouchPhase.Ended or TouchPhase.Canceled)
            {
                dragging = false;
            }
        }

        private void OnGUI()
        {
            var scale = Mathf.Max(1f, Screen.height / 720f);
            var oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            var width = Screen.width / scale;
            var height = Screen.height / scale;

            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.86f, 0.38f) }
            };
            GUI.Label(new Rect(0, 18, width, 48), $"{match.PlayerScore}  —  {match.OpponentScore}", titleStyle);
            GUI.Label(new Rect(0, 60, width, 42), announcement, titleStyle);

            if (match.Phase == MatchPhase.Charging &&
                GUI.Button(new Rect(width - 190, height - 150, 160, 110), "BRACE"))
            {
                bracePressedAt = Time.time;
            }

            if (match.Phase == MatchPhase.RoundResult &&
                GUI.Button(new Rect(width / 2f - 100, height - 105, 200, 72), "NEXT PASS"))
            {
                match.AdvanceRound();
                ResetPass();
            }

            if (match.Phase == MatchPhase.MatchResult &&
                GUI.Button(new Rect(width / 2f - 100, height - 105, 200, 72), "REMATCH"))
            {
                StartMatch();
            }

            GUI.Label(new Rect(24, height - 80, 380, 50), "Drag vertically on the right to aim");
            GUI.matrix = oldMatrix;
        }

        private void StartMatch()
        {
            Time.timeScale = 1f;
            match = new JoustMatch();
            ResetPass();
        }

        private void ResetPass()
        {
            player.position = new Vector3(-1.15f, 1f, -LaneLength * 0.5f);
            opponent.position = new Vector3(1.15f, 1f, LaneLength * 0.5f);
            player.rotation = Quaternion.identity;
            opponent.rotation = Quaternion.Euler(0f, 180f, 0f);
            playerAim = 12f;
            bracePressedAt = -100f;
            countdown = 3f;
            announcement = "Prepare your lance";
        }

        private void BuildArena()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Tournament Ground";
            ground.transform.localScale = new Vector3(2.2f, 1f, 4.2f);
            ground.GetComponent<Renderer>().material.color = new Color(0.28f, 0.36f, 0.18f);

            var divider = GameObject.CreatePrimitive(PrimitiveType.Cube);
            divider.name = "Tilt Barrier";
            divider.transform.position = new Vector3(0f, 0.55f, 0f);
            divider.transform.localScale = new Vector3(0.18f, 1.1f, LaneLength + 4f);
            divider.GetComponent<Renderer>().material.color = new Color(0.35f, 0.12f, 0.07f);

            player = CreateRider("Player", new Color(0.12f, 0.3f, 0.75f), false, out playerLance);
            opponent = CreateRider("Opponent", new Color(0.7f, 0.14f, 0.1f), true, out opponentLance);

            var cameraObject = new GameObject("Main Camera");
            var arenaCamera = cameraObject.AddComponent<Camera>();
            this.arenaCamera = cameraObject.transform;
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(-11f, 7.5f, -9f);
            cameraObject.transform.rotation = Quaternion.Euler(18f, 42f, 0f);
            arenaCamera.fieldOfView = 58f;

            var lightObject = new GameObject("Sun");
            var sun = lightObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.25f;
            lightObject.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
            RenderSettings.ambientLight = new Color(0.38f, 0.4f, 0.46f);
        }

        private static Transform CreateRider(string name, Color color, bool reversed, out Transform lance)
        {
            var root = new GameObject(name).transform;
            root.rotation = reversed ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;

            var horse = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            horse.name = "Horse";
            horse.transform.SetParent(root, false);
            horse.transform.localScale = new Vector3(0.75f, 0.85f, 1.25f);
            horse.GetComponent<Renderer>().material.color = new Color(0.22f, 0.1f, 0.055f);

            var rider = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            rider.name = "Knight";
            rider.transform.SetParent(root, false);
            rider.transform.localPosition = new Vector3(0f, 1.35f, 0f);
            rider.transform.localScale = new Vector3(0.45f, 0.62f, 0.45f);
            rider.GetComponent<Renderer>().material.color = color;

            var lanceObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lanceObject.name = "Lance";
            lanceObject.transform.SetParent(root, false);
            lanceObject.transform.localPosition = new Vector3(0f, 1.55f, 2.15f);
            lanceObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            lanceObject.transform.localScale = new Vector3(0.075f, 2.4f, 0.075f);
            lanceObject.GetComponent<Renderer>().material.color = new Color(0.55f, 0.3f, 0.12f);
            lance = lanceObject.transform;
            return root;
        }
    }
}
