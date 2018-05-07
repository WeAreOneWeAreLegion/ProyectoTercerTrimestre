using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoSingleton<GameManager> {

    public enum TypeOfTag { Player, Enemy, Wall, Door, DoorTrigger, Bell, HittableObject }
    public enum ButtonRequest { A, B, X, Y, RB, LB, RT, LT }

    #region Public Variables
    [Header("\t--Game Designers Variables--")]
    [Tooltip("Muestra los fps, en caso contrario los esconde automaticamente")]
    public bool showFPS;

    [Header("Materials Variables")]
    [Tooltip("Distancia extra para no hacer invisible los objetos cuando estan a la misma distancia frontal que el personaje principal")]
    [Range(0,20)] public float transparencyOffsetForward = 1f;
    [Tooltip("Distancia extra para no hacer invisible los objetos cuando estan a la misma distancia lateral que el personaje principal")]
    [Range(0,20)] public float transparencyOffsetLateral = 1f;
    [Tooltip("La cantidad de transparencia a tener los objetos que la camara esconde")]
    [Range(0,1)] public float objectsHidenByCameraTransparency = 0.2f;
    [Tooltip("La cantidad de transparencia a tener los muros que la camara esconde")]
    [Range(0,1)] public float wallsHidenByCameraTransparency = 0f;

    [Header("Colectionable Variables")]
    [Tooltip("La cantidad total de gemas en partida ---- Esta variable pasara a automatizarse en breves")]
    public int maxNumOfGems = 4;

    [Header("\t    --Own Script Variables--")]
    [Header("Player Variables")]
    [Tooltip("Referencia del player")]
    public PlayerController player;

    [Header("Main Light")]
    [Tooltip("Referencia de la luz direccional")]
    public Light directionalLight;

    [Header("Rooms Variables")]
    [Tooltip("Referencia a todas las habitacions")]
    public List<GameObject> roomsList = new List<GameObject>();
    [Tooltip("Referencia a todas las puertas")]
    public List<ConectionScript> doorsList = new List<ConectionScript>();

    [Header("HUD Variables")]
    //Main Canvas
    [Tooltip("Referencia al canvas principal")]
    public Canvas mainCanvas;
    //Prefabs
    [Tooltip("Prefab del hud del enemigo --------- Esta variables pasara a ser borrada en breves")]
    public GameObject enemyHUDPrefab;
    //References
    [Tooltip("Referencia al hud del player")]
    public PlayerHUD playerHUD;
    [Tooltip("Referencia al hud de pausa")]
    public GameObject pausePanel;
    //Componens
    [Tooltip("Referencia al texto de vida")]
    public Text hpText;
    [Tooltip("Referencia a la sombra del texto de vida")]
    public Text hpTextShadow;
    [Tooltip("Referencia al texto de gemas")]
    public Text gemsText;
    [Tooltip("Referencia a la sombra del texto de gemas")]
    public Text gemsTextShadow;
    [Tooltip("Referencia al texto de control vertical actual")]
    public Text currentYControlMode;
    [Tooltip("Referencia al texto de fps")]
    public Text fpsText;

    [Header("Tags List")]
    [Tooltip("0.Player, 1.Enemy, 2.Wall, 3.Door 4.DoorTrigger 5.Bell")]
    public List<string> tagList = new List<string>();
    #endregion

    #region Private Variables
    private int currentNumOfGems;
    private bool combateMode;
    private float deltaTime;
    private bool isGamePaused;

    //Aixo hauria de estar en un gestor de huds mes que en el gameManager
    private List<EnemyHUD> enemyHUDList = new List<EnemyHUD>();
    private List<EnemyHUD> enemyHUDWaitingList = new List<EnemyHUD>();

    //Pause Panel Variables
    //private int actionIndex = 0; ------------ Provisional, don't erase
    #endregion

    private void Awake()
    {
        Time.timeScale = 1;
    }

    private void Start()
    {

        foreach (ConectionScript c in FindObjectsOfType<ConectionScript>())
            doorsList.Add(c);

        pausePanel.SetActive(false);

        //Set-up HUD
        if (!showFPS)
            fpsText.gameObject.SetActive(false);
        else
            fpsText.gameObject.SetActive(true);


        ModifyYModeInfo();
    }

    private void Update()
    {
        if (showFPS)
            ShowFPS();

        if (InputsManager.Instance.GetStartButtonDown())
            PauseGame();

        if (isGamePaused)
            PauseActions();
    }

    #region Door Methods
    public void UnblockPlayerDoors()
    {
        combateMode = false;

        player.CalmMode();

        Ray ray;
        RaycastHit hit;

        ray = new Ray(GetPlayer().position, Vector3.down);
        Physics.Raycast(ray, out hit, 100, LayerMask.GetMask("FloorLayer"));

        foreach (ConectionScript d in doorsList)
        {
            if (d.IsDoorFromRoom(hit.transform.parent.gameObject))
            {
                d.UnblockDoor();
            }
        }
    }

    public void BlockPlayerDoors()
    {
        combateMode = true;

        player.CombateMode();

        Ray ray;
        RaycastHit hit;

        ray = new Ray(GetPlayer().position, Vector3.down);
        Physics.Raycast(ray, out hit, 100, LayerMask.GetMask("FloorLayer"));

        foreach (ConectionScript d in doorsList)
        {
            if (d.IsDoorFromRoom(hit.transform.parent.gameObject))
            {
                d.BlockDoor();
            }
        }
    }
    #endregion

    #region Light Methods
    public void SwitchMainLight()
    {
        directionalLight.enabled = !directionalLight.enabled;
        if (directionalLight.enabled)
            RenderSettings.ambientIntensity = 1;
        else
            RenderSettings.ambientIntensity = 0;
    }
    #endregion

    #region HUD Methods
    public void ModifyHp(int currentHp)
    {
        hpText.text = currentHp.ToString();
        hpTextShadow.text = hpText.text;
    }

    public void ActivePlayerHUD(ButtonRequest req)
    {
        playerHUD.ShowSpecificButton(req);
    }

    public void DisablePlayerHUD()
    {
        playerHUD.HideImage();
    }

    public void CreateEnemyHUD(Transform target, int initialHp)
    {
        if (enemyHUDWaitingList.Count == 0)
        {
            GameObject go = Instantiate(enemyHUDPrefab, mainCanvas.transform) as GameObject;
            EnemyHUD e = go.GetComponent<EnemyHUD>();
            e.SetUp(target, initialHp);
            enemyHUDList.Add(e);
        }
        else
        {
            EnemyHUD e = enemyHUDWaitingList[0];
            enemyHUDWaitingList.Remove(e);
            e.gameObject.SetActive(true);
            e.SetUp(target, initialHp);
            enemyHUDList.Add(e);
        }
    }

    public void ModifyEnemyHp(Transform target, int currentHp)
    {
        enemyHUDList.Find(x => x.GetTarget() == target).ModifyHp(currentHp);
    }

    public void DestroyEnemyHUD(Transform target)
    {
        enemyHUDList.ForEach(x => 
        {
            if (x.GetTarget() == target)
            {
                x.gameObject.SetActive(false);
                enemyHUDWaitingList.Add(x);
                enemyHUDList.Remove(x);
            }
        });
    }

    public void IncreaseNumOfGems()
    {
        currentNumOfGems++;
        gemsText.text = currentNumOfGems.ToString()+"/"+maxNumOfGems.ToString();
        gemsTextShadow.text = gemsText.text;
    }
    #endregion

    #region Getter Methods
    public bool GetIsPlayerPanelActive()
    {
        return playerHUD.GetIsActive();
    }

    public bool GetIsInCombateMode()
    {
        return combateMode;
    }

    public bool IsDirectLightActivated()
    {
        return directionalLight.enabled;
    }

    public string GetTagOfDesiredType(TypeOfTag t)
    {
        return tagList[(int)t];
    }

    public Transform GetPlayer()
    {
        if (player == null)
            player = FindObjectOfType<PlayerController>();

        return player.transform;
    }

    public Vector2 GetCanvasResolution()
    {
        return mainCanvas.GetComponent<CanvasScaler>().referenceResolution;
    }
    #endregion

    //Setters

    #region FPS Method
    private void ShowFPS()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;

        //float msec = deltaTime * 1000.0f;
        float fps = 1.0f / deltaTime;
        fpsText.text = "FPS: " + ((int)fps).ToString();
    }
    #endregion

    #region Pause Methods
    private void PauseActions()
    {
        if (InputsManager.Instance.GetChangeColorButtonInputDown())
        {
            InputsManager.Instance.InvertY();
            ModifyYModeInfo();
        }
    }

    private void ModifyYModeInfo ()
    {
        if (InputsManager.Instance.invertVerticalRotation)
        {
            //Inverted (Modo actual)
            currentYControlMode.text = "Not-Inverted";
        }
        else
        {
            //Not Inverted (Luigi's Mansion)
            currentYControlMode.text = "Inverted";
        }
    }

    private void PauseGame()
    {
        if (isGamePaused)
        {
            Time.timeScale = 1;
            pausePanel.SetActive(false);
        }
        else
        {
            Time.timeScale = 0;
            pausePanel.SetActive(true);
        }

        isGamePaused = !isGamePaused;
    }

    private void QuitGame()
    {
        Application.Quit();
    }
    #endregion

}
