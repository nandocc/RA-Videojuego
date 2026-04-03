using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using Vuforia;

public class ARGameFlowManager : MonoBehaviour
{
    public static bool AsteroidThreatActive { get; private set; }

    [Header("References")]
    [SerializeField] private VuforiaGroundPlaneFallback placementController;
    [SerializeField] private ProjectileShooter projectileShooter;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private AsteroidSpawner asteroidSpawner;
    [SerializeField] private bool requireSurfacePlacement = true;

    [Header("Difficulty")]
    [SerializeField] private float speedMultiplier = 1.65f;
    [SerializeField] private float turnSpeedMultiplier = 1.45f;
    [SerializeField] private Vector2 mapMinXZ = new Vector2(-12f, -7f);
    [SerializeField] private Vector2 mapMaxXZ = new Vector2(12f, 7f);
    [SerializeField] private Vector2 respawnDelayRange = new Vector2(1.1f, 2.4f);
    [SerializeField] private float asteroidStartDelaySeconds = 10f;

    [Header("Player Label (Top Right)")]
    [SerializeField] private bool showPlayerLabel;
    [SerializeField] private string playerName = "Tu Nombre";
    [SerializeField] private string playerId = "AQUI-TU-MATRICULA";
    [Header("Flow")]
    [SerializeField] private float introMessageDuration = 5f;
    [SerializeField] private float gameDurationSeconds = 60f;

    private ShipWanderAR[] _wanderers;
    private ShipHealth[] _ships;
    private bool _gameStarted;
    private bool _gameOver;
    private bool _gameCompleted;
    private int _score;
    private int _aliveShips;
    private float _playerHealthCurrent = 100f;
    private float _playerHealthMax = 100f;
    private float _asteroidCountdownEndTime;
    private bool _asteroidThreatStarted;
    private float _introEndTime;
    private float _gameEndTime;
    private readonly Dictionary<ShipHealth, float> _respawnAt = new Dictionary<ShipHealth, float>();

    private GUIStyle _labelStyle;
    private GUIStyle _panelStyle;
    private GUIStyle _buttonStyle;
    private GUIStyle _reticleStyle;
    private Texture2D _whiteTex;
    private readonly GUIContent _playerLabelContent = new();
    private readonly GUIContent _introContent = new();
    private readonly GUIContent _timerContent = new();

    private void Awake()
    {
        ResetFlowState();

        if (placementController == null)
            placementController = FindFirstObjectByType<VuforiaGroundPlaneFallback>();
        if (projectileShooter == null)
            projectileShooter = FindFirstObjectByType<ProjectileShooter>();
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (asteroidSpawner == null)
            asteroidSpawner = FindFirstObjectByType<AsteroidSpawner>();

        var hasImageTarget = FindFirstObjectByType<ImageTargetBehaviour>() != null;
        if (hasImageTarget)
            requireSurfacePlacement = false;
        else if (placementController == null || !placementController.gameObject.activeInHierarchy)
            requireSurfacePlacement = false;

        _wanderers = FindObjectsByType<ShipWanderAR>(FindObjectsSortMode.None);
        _ships = FindObjectsByType<ShipHealth>(FindObjectsSortMode.None);

        SetGameplayEnabled(false);
        _aliveShips = CountAliveShips();

        if (!requireSurfacePlacement)
            StartGameIfNeeded();
    }

    private void Start()
    {
        // Guard against scene serialization states where Awake order leaves gameplay not started.
        if (!_gameStarted && !requireSurfacePlacement)
            StartGameIfNeeded();
    }

    private void OnEnable()
    {
        ShipHealth.ShipDestroyed += OnShipDestroyed;
        if (placementController != null)
            placementController.OnPlacementApplied += StartGameIfNeeded;
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += OnPlayerHealthChanged;
            playerHealth.OnPlayerDied += OnPlayerDied;
        }
    }

    private void OnDisable()
    {
        ShipHealth.ShipDestroyed -= OnShipDestroyed;
        if (placementController != null)
            placementController.OnPlacementApplied -= StartGameIfNeeded;
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= OnPlayerHealthChanged;
            playerHealth.OnPlayerDied -= OnPlayerDied;
        }
    }

    private void StartGameIfNeeded()
    {
        if (_gameStarted)
            return;

        _gameOver = false;
        _gameCompleted = false;
        _gameStarted = true;
        _asteroidThreatStarted = false;
        AsteroidThreatActive = false;
        AsteroidHazard.ResetLaunchScheduler();
        ApplyDifficultySettings();
        SetGameplayEnabled(true);

        if (playerHealth != null)
        {
            playerHealth.ResetHealth();
            _playerHealthCurrent = playerHealth.CurrentHealth;
            _playerHealthMax = playerHealth.MaxHealth;
        }

        if (asteroidSpawner != null && projectileShooter != null)
        {
            asteroidSpawner.SetTarget(projectileShooter.GetPlayerVisualTransform());
            asteroidSpawner.SetGameplayEnabled(false);
            _asteroidCountdownEndTime = Time.time + Mathf.Max(0f, asteroidStartDelaySeconds);
        }

        _introEndTime = Time.time + introMessageDuration;
        _gameEndTime = Time.time + gameDurationSeconds;
    }

    private void OnShipDestroyed(ShipHealth ship, int points)
    {
        _score += points;
        if (ship != null)
        {
            var respawnDelay = Random.Range(respawnDelayRange.x, respawnDelayRange.y);
            _respawnAt[ship] = Time.time + Mathf.Max(0.2f, respawnDelay);
        }
        _aliveShips = CountAliveShips();
    }

    private int CountAliveShips()
    {
        var count = 0;
        foreach (var ship in _ships)
        {
            if (ship != null && ship.gameObject.activeInHierarchy)
                count++;
        }
        return count;
    }

    private void SetGameplayEnabled(bool enabled)
    {
        if (projectileShooter != null)
            projectileShooter.SetGameplayEnabled(enabled);

        if (asteroidSpawner != null)
            asteroidSpawner.SetGameplayEnabled(enabled);

        foreach (var wanderer in _wanderers)
        {
            if (wanderer != null)
                wanderer.enabled = enabled;
        }
    }

    private void InitStyles()
    {
        if (_labelStyle != null)
            return;

        _labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 28,
            alignment = TextAnchor.UpperLeft,
            normal = { textColor = Color.white }
        };

        _panelStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 30,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white },
            wordWrap = true
        };

        _buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 28
        };

        _reticleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 42,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.cyan }
        };

        if (_whiteTex == null)
        {
            _whiteTex = new Texture2D(1, 1);
            _whiteTex.SetPixel(0, 0, Color.white);
            _whiteTex.Apply();
        }
    }

    private void Update()
    {
        if (_gameOver || _gameCompleted)
            return;

        HandleGameTimer();
        HandleAsteroidCountdown();
        HandleRespawns();

        if (_gameStarted || !requireSurfacePlacement || placementController == null)
            return;

        if (!TryGetTapPosition(out var tapPosition))
            return;

        if (placementController.PlaceAtScreenPosition(tapPosition))
            StartGameIfNeeded();
    }

    private void HandleRespawns()
    {
        if (!_gameStarted || _respawnAt.Count == 0)
            return;

        var now = Time.time;
        var due = new List<ShipHealth>();
        foreach (var pair in _respawnAt)
        {
            if (pair.Key == null)
            {
                due.Add(pair.Key);
                continue;
            }

            if (now >= pair.Value)
                due.Add(pair.Key);
        }

        if (due.Count == 0)
            return;

        foreach (var ship in due)
        {
            _respawnAt.Remove(ship);
            if (ship == null)
                continue;

            ship.Respawn();
            var wander = ship.GetComponent<ShipWanderAR>();
            if (wander != null)
                wander.WarpToRandomPoint();
        }

        _aliveShips = CountAliveShips();
    }

    private void ApplyDifficultySettings()
    {
        foreach (var wanderer in _wanderers)
        {
            if (wanderer == null)
                continue;

            wanderer.SetMovementArea(mapMinXZ, mapMaxXZ);
            var boostedSpeed = Random.Range(2.2f, 3.8f) * speedMultiplier;
            var boostedTurn = Random.Range(6.0f, 11.0f) * turnSpeedMultiplier;
            wanderer.SetMotion(boostedSpeed, boostedTurn);
            wanderer.WarpToRandomPoint();
        }
    }

    private void OnGUI()
    {
        InitStyles();

        var pad = 20f;
        if (showPlayerLabel)
        {
            _playerLabelContent.text = $"{playerName}\n{playerId}";
            var rightSize = _labelStyle.CalcSize(_playerLabelContent);
            GUI.Label(new Rect(Screen.width - rightSize.x - pad, pad, rightSize.x, rightSize.y + 10), _playerLabelContent, _labelStyle);
        }

        GUI.Label(new Rect(pad, pad, 420, 80), $"Puntos: {_score}", _labelStyle);
        GUI.Label(new Rect(pad, pad + 36, 420, 80), $"Naves activas: {_aliveShips}", _labelStyle);
        DrawHealthBar(pad, pad + 320, 420, 26);

        if (_gameStarted && !_gameOver && !_gameCompleted)
        {
            var remaining = Mathf.Max(0f, _gameEndTime - Time.time);
            var timerText = $"Tiempo: {Mathf.CeilToInt(remaining)}s";
            _timerContent.text = timerText;
            GUI.Label(new Rect(Screen.width * 0.5f - 160f, 24f, 320f, 40f), _timerContent, _panelStyle);
        }

        if (_gameStarted && !_gameOver && !_asteroidThreatStarted && asteroidSpawner != null)
        {
            var remaining = Mathf.Max(0f, _asteroidCountdownEndTime - Time.time);
            var text = $"Asteroides en: {Mathf.CeilToInt(remaining)}s";
            GUI.Label(new Rect(Screen.width * 0.5f - 170f, 100f, 340f, 50f), text, _panelStyle);
        }

        if (_gameStarted && !_gameOver && !_gameCompleted && Time.time < _introEndTime)
        {
            var t = Mathf.CeilToInt(_introEndTime - Time.time);
            _introContent.text = $"Sobrevive a los asteroides y dispara a los enemigos ({t})";
            GUI.Label(new Rect(30f, 150f, Screen.width - 60f, 90f), _introContent, _panelStyle);
        }

        if (_gameStarted || !requireSurfacePlacement)
        {
            if (_gameCompleted)
            {
                DrawCompletedPanel();
                return;
            }

            if (_gameOver)
                DrawGameOverPanel();
            return;
        }

        var cx = Screen.width * 0.5f - 40f;
        var cy = Screen.height * 0.5f - 40f;
        GUI.Label(new Rect(cx, cy, 80f, 80f), "+", _reticleStyle);

        var w = Mathf.Min(900, Screen.width - 80);
        var h = 320f;
        var x = (Screen.width - w) * 0.5f;
        var y = (Screen.height - h) * 0.5f;

        GUI.Box(new Rect(x, y, w, h), string.Empty, _panelStyle);
        GUI.Label(
            new Rect(x + 30, y + 25, w - 60, 170),
            "Apunta la cruz al suelo o una mesa.\nToca la pantalla para colocar el mapa y empezar.\nTambien puedes usar el boton de configurar.",
            _panelStyle);

        if (GUI.Button(new Rect(x + 40, y + h - 95, w - 80, 60), "Configurar Superficie y Empezar", _buttonStyle))
        {
            if (placementController != null && placementController.PlaceAtScreenCenter())
                StartGameIfNeeded();
        }
    }

    private static bool TryGetTapPosition(out Vector2 screenPosition)
    {
        screenPosition = default;

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            screenPosition = Mouse.current.position.ReadValue();
            return true;
        }

        return false;
    }

    private void OnPlayerHealthChanged(float current, float max)
    {
        _playerHealthCurrent = current;
        _playerHealthMax = Mathf.Max(1f, max);
    }

    private void OnPlayerDied()
    {
        if (!_gameStarted)
            return;

        _gameOver = true;
        AsteroidThreatActive = false;
        AsteroidHazard.ResetLaunchScheduler();
        SetGameplayEnabled(false);
    }

    private void DrawHealthBar(float x, float y, float width, float height)
    {
        var ratio = Mathf.Clamp01(_playerHealthCurrent / Mathf.Max(1f, _playerHealthMax));

        var labelHeight = _labelStyle.lineHeight + 4f;
        GUI.Label(new Rect(x, y - labelHeight - 4f, width, labelHeight),
            $"Health: {Mathf.CeilToInt(_playerHealthCurrent)} / {Mathf.CeilToInt(_playerHealthMax)}",
            _labelStyle);

        var barRect = new Rect(x, y, width, height);
        GUI.Box(barRect, GUIContent.none);

        var prevColor = GUI.color;
        GUI.color = Color.green;
        var fillRect = new Rect(barRect.x + 2f, barRect.y + 2f, (barRect.width - 4f) * ratio, barRect.height - 4f);
        GUI.DrawTexture(fillRect, _whiteTex, ScaleMode.StretchToFill, true);
        GUI.color = prevColor;
    }

    private void DrawGameOverPanel()
    {
        var w = Mathf.Min(680, Screen.width - 80);
        var h = 220f;
        var x = (Screen.width - w) * 0.5f;
        var y = (Screen.height - h) * 0.5f;
        GUI.Box(new Rect(x, y, w, h), string.Empty, _panelStyle);
        GUI.Label(new Rect(x + 20, y + 20, w - 40, 80), "GAME OVER\nTu nave fue destruida", _panelStyle);
        GUI.Label(new Rect(x + 20, y + 120, w - 40, 40), $"Puntaje final: {_score}", _panelStyle);

        if (GUI.Button(new Rect(x + 40, y + h - 70, w - 80, 50), "Volver a Jugar", _buttonStyle))
            RestartGame();
    }

    private void DrawCompletedPanel()
    {
        var w = Mathf.Min(680, Screen.width - 80);
        var h = 220f;
        var x = (Screen.width - w) * 0.5f;
        var y = (Screen.height - h) * 0.5f;
        GUI.Box(new Rect(x, y, w, h), string.Empty, _panelStyle);
        GUI.Label(new Rect(x + 20, y + 20, w - 40, 80), "Juego Completado\nSobreviviste 1 minuto", _panelStyle);
        GUI.Label(new Rect(x + 20, y + 120, w - 40, 40), $"Puntaje final: {_score}", _panelStyle);

        if (GUI.Button(new Rect(x + 40, y + h - 70, w - 80, 50), "Volver a Jugar", _buttonStyle))
            RestartGame();
    }

    private void HandleAsteroidCountdown()
    {
        if (!_gameStarted || _gameOver || _asteroidThreatStarted || asteroidSpawner == null)
            return;

        if (Time.time < _asteroidCountdownEndTime)
            return;

        asteroidSpawner.SetGameplayEnabled(true);
        _asteroidThreatStarted = true;
        AsteroidThreatActive = true;
    }

    private void HandleGameTimer()
    {
        if (!_gameStarted || _gameOver || _gameCompleted)
            return;

        if (Time.time >= _gameEndTime)
        {
            _gameCompleted = true;
            AsteroidThreatActive = false;
            SetGameplayEnabled(false);
        }
    }

    private void RestartGame()
    {
        AsteroidThreatActive = false;
        AsteroidHazard.ResetLaunchScheduler();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void ResetFlowState()
    {
        _gameStarted = false;
        _gameOver = false;
        _gameCompleted = false;
        _asteroidThreatStarted = false;
        AsteroidThreatActive = false;
        AsteroidHazard.ResetLaunchScheduler();
        _respawnAt.Clear();
        _introEndTime = 0f;
        _gameEndTime = 0f;
    }
}
