using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {

    public enum State { Playing, CrossDoor, Cinematic }

    #region Public Variables
    [Header("\tGame Designers Variables")]

    [Header("Character Behaviour")]
    public bool independentFacing;

    [Header("Speed Variables")]
    [Tooltip("Velocidad a la que se mueve el personaje")]
    [Range(0,20)] public float speed = 10;
    [Tooltip("Velocidad a la que rota el personaje")]
    [Range(50,150)] public float rotationSpeed = 100;
    [Tooltip("Velocidad de rotacion automatica cuando te mueves")]
    [Range(0,15)] public float autoFacingSpeed = 7f;
    [Tooltip("Velocidad de rotacion de la linterna (Hacia arriba i hacia abajo)")]
    [Range(0,90)] public float lanternRotationSpeed = 50;
    [Range(0,180)] public float angleToStartMoving = 10f;

    [Header("Lantern Variables")]
    [Tooltip("Daño que hace la linterna")]
    [Range(0,100)] public int lanternDamage = 15;
    [Tooltip("Radio en el cual la linterna empezara a hacer daño (Lineas exteriores Blancas)")]
    [Range(10,40)] public float lanternDamageRadius = 21;
    [Tooltip("Distancia en el cual la linterna empezara a hacer daño (Linea central Blanca)")]
    [Range(1,15)] public float lanternDamageLength = 10;
    [Tooltip("Distancia en el cual la linterna se acortara cuando esta cargado al maximo")]
    [Range(1,15)] public float lanternChargingLength = 1;
    [Tooltip("Este valor se aplica de forma positiva. *Literna apuntando arriba*")]
    [Range(0,60)] public float topLanternAngle = 45;
    [Tooltip("Este valor se aplica de forma negativa. *Literna apuntando abajo*")]
    [Range(0, 30)] public float bottomLanternAngle = 5;
    [Tooltip("El radio que tiene el personaje para aumentar o reducir la luz de las linternas automaticamente, solo se aplica cuando miras a camara")]
    [Range(0,30)] public float angleOfLightDecrease;
    [Tooltip("La velocidad a la que la luz pasa de su valor al valor minimo o maximo hacia la camara")]
    [Range(0,15)] public float cameraLightFadeSpeed = 7f;
    [Tooltip("La velocidad a la que la luz pasa de su valor al valor minimo o maximo hacia la camara")]
    [Range(0, 15)] public float chargedLightFadeSpeed = 1f;
    [Tooltip("Tiempo que tarda la linterna en estar cargado al maximo")]
    [Range(0, 5)] public float timeForMaxCharged = 2f;
    [Tooltip("Tiempo para reutilizar el flash")]
    [Range(0, 5)] public float timeBetweenFlashes = 1f;

    [Header("Increase/Decrease Variables")]
    [Tooltip("Cuanto se reducira la luz cuando el personaje este enfocando hacia la camara")]
    [Range(1,6)] public float lightDecreaseFactor;
    [Tooltip("Cuanto se reducira la luz cuando el personaje este enfocando hacia la camara")]
    [Range(1, 4)] public float lightIncreaseFactor = 1.6f;

    [Header("Action Variables")]
    [Tooltip("Distancia a la cual el player realizara una accion (Linea central Verde)")]
    [Range(0, 5)] public float actionDistance = 2.5f;
    [Tooltip("Distancia a la cual el player realizara una accion (Linea central Verde)")]
    [Range(0, 2)] public float timeBetweenAim = 0.5f;

    [Header("Health Variables")]
    [Tooltip("Vida inicial del personaje")]
    public int initialHp = 100;

    [Header("\t    Own Script Variables")]
    [Tooltip("Transform linterna")]
    public Transform lantern;
    [Tooltip("Luz linterna")]
    public Light lanternLight;
    [Tooltip("Particulas luz linterna")]
    public ParticleSystem chargindParticles;
    [Tooltip("Luz gradiante de la linterna cargando")]
    public Gradient lightsChargingGradientColor;
    [Tooltip("Luz gradiante de la linterna cargada")]
    public Gradient lightsChargedGradientColor;
    #endregion

    #region Private Variables
    private int currentHp;
    private float xLanternRotationValue;
    private float yRotationValue;
    private bool areLightsDecreased;
    private bool areLightsIncreased;
    private bool canMove;
    private bool stopByAiming;
    private bool isLightCharging;
    private bool isMoving;
    private bool autoFace;
    private Vector3 direction;
    private Vector3 faceDirection;

    private State currentState = State.Playing;
    private CharacterController myCharController;
    private AudioSource myAudioSource;

    private List<GameObject> ghostsInRadius = new List<GameObject>();
    private List<GameObject> lightenObjectInRadius = new List<GameObject>();
    private List<GameObject> lightenedPuzzlesInRadius = new List<GameObject>();

    //Timer variables
    private float lightChargingTimer;
    private float delayBetweenChargedShotTimer;
    private float aimTimer;

    //Input variables
    private float xMove, zMove;
    private float xRotation, yRotation;

    //Light lerp values
    private bool lightEnabled = true;
    private bool isLightHighIntensity = false;
    private bool isMaxIntensity = false;

    private float initialLanternLightRange;
    private float initialLanternDamageRange;
    private float lightLerpSpeed;
    private float initialLightIntensity;
    private float lerpValueLightIntensity;

    //CrossDoor variables
    private bool isCrossingDoor;
    private Vector3 pointToGoCrossDoor;
    private Vector3 directionToGoCrossDoor;

    private ConectionScript currentDoorCrossing;
    #endregion

    private void Awake()
    {
        InputsManager.Instance.LockMouse();
    }

    private void Start()
    {
        //Tag 
        if (tag != GameManager.Instance.GetTagOfDesiredType(GameManager.TypeOfTag.Player))
            tag = GameManager.Instance.GetTagOfDesiredType(GameManager.TypeOfTag.Player);

        //Components
        myCharController = GetComponent<CharacterController>();
        myAudioSource = GetComponent<AudioSource>();

        //Variables initalization
        currentHp = initialHp;
        delayBetweenChargedShotTimer = 1;
        faceDirection = transform.forward;

        //Lights
        initialLightIntensity = lerpValueLightIntensity = lanternLight.intensity;
        initialLanternLightRange = lanternLight.range;
        initialLanternDamageRange = lanternDamageLength;
        chargindParticles.gameObject.SetActive(false);

        //HUD initialization
        GameManager.Instance.ModifyHp(currentHp);
    }

    private void Update ()
    {
        if (Time.timeScale == 0)
            return;

        if (currentState == State.Playing)
        {
            Inputs();

            Move();
            RotateByJoystick();
            RotateByMove();

            CheckLight();
            LightDamaging();

            Timers();
            MoveChecker();
            ActionChecker();
        }
        else if (currentState == State.CrossDoor)
        {
            CrossDoor();
        }
        else if (currentState == State.Cinematic)
        {

        }
    }

    //Private Methods
    #region Playing Methods

    private void Inputs()
    {
        //Actions always performable
        if (Input.GetKeyDown(KeyCode.Escape))
            InputsManager.Instance.UnlockMouse();

        if (Input.GetMouseButtonDown(0))
            InputsManager.Instance.LockMouse();

        if (!canMove)
        {
            return;
        }

        //Actions limited by canMove
        if (InputsManager.Instance.GetActionButtonInputDown())
            Action();

        if (!canMove) //Doble check if action done
        {
            return;
        }

        if (InputsManager.Instance.GetIntensityButtonDown() && delayBetweenChargedShotTimer >= 1 && !isLightCharging)
            ChargeLight();

        if (InputsManager.Instance.GetIntensityButtonUp() && isLightCharging)
            ReleaseLight();

        if (InputsManager.Instance.GetSwitchButtonInput())
        {
            lightEnabled = true;
            SwitchLight();
        }
        else
        {
            lightEnabled = false;
            SwitchLight();
        }

        xMove = InputsManager.Instance.GetMovementX();
        zMove = InputsManager.Instance.GetMovementY();

        xRotation = InputsManager.Instance.GetRotationX();
        yRotation = InputsManager.Instance.GetRotationY();
    }

    private void Move()
    {
        direction = Vector3.zero;

        direction.x = xMove;
        direction.z = zMove;

        if (!independentFacing && canMove)
        {
            //Face where you go

            if (direction != Vector3.zero)
            {
                if (!(!isMoving && autoFace))
                {
                    faceDirection = direction;

                    if (Vector3.Angle(faceDirection, transform.forward) > 180 - angleToStartMoving)
                    {
                        //Debug.Log("Autofacing from: "+transform.forward+" to: "+faceDirection);
                        autoFace = true;
                    }
                    else
                    {
                        autoFace = false;
                    }
                }
            }
            else
            {
                isMoving = false;
                if (!autoFace)
                {
                    faceDirection = transform.forward;
                }
            }

            if (((Vector3.Angle(faceDirection, transform.forward) <= angleToStartMoving && !isMoving && !autoFace) || isMoving) && direction != Vector3.zero)
            {

                myCharController.Move(((transform.forward * direction.magnitude * speed) + Physics.gravity) * Time.deltaTime);
                isMoving = true;
            }

        }
        else if (independentFacing && canMove)
        {
            myCharController.Move(((direction * speed) + Physics.gravity) * Time.deltaTime);
        }
        else if (!canMove)
        {
            transform.forward = Vector3.Slerp(transform.forward, faceDirection, autoFacingSpeed * Time.deltaTime);
        }


    }

    private void RotateByJoystick()
    {
        if (xMove == 0 && zMove == 0 || independentFacing)
            transform.Rotate(Vector3.up, xRotation * rotationSpeed * Time.deltaTime);

        xLanternRotationValue = Mathf.Clamp(xLanternRotationValue + yRotation * lanternRotationSpeed * Time.deltaTime, -topLanternAngle, bottomLanternAngle);

        lantern.localRotation = Quaternion.Euler(xLanternRotationValue, 0, 0);
    }

    private void RotateByMove()
    {
        if ((!independentFacing && canMove && (xMove != 0 || zMove != 0)) || autoFace)
        {
            float extraSpeed = autoFace ? 3.5f : 3f;

            if (Vector3.Angle(faceDirection, transform.forward) > angleToStartMoving)
            {
                transform.Rotate(Vector3.up, (-Mathf.Sign(Vector3.SignedAngle(faceDirection, transform.forward, transform.up))) * rotationSpeed * extraSpeed * Time.deltaTime);
            }
            else if (faceDirection != transform.forward || autoFace)
            {
                transform.forward = faceDirection;
                autoFace = false;
            }
        }

        xMove = zMove = 0;
    }

    private void CheckLight()
    {
        if (isLightCharging)
        {
            lanternLight.range = Mathf.Lerp(initialLanternLightRange, lanternChargingLength, lightChargingTimer);
            lanternDamageLength = Mathf.Lerp(initialLanternDamageRange, lanternChargingLength, lightChargingTimer);
        }
        else
        {
            if (lanternLight.range != initialLanternLightRange)
            {
                lanternLight.range = initialLanternLightRange;
                lanternDamageLength = initialLanternDamageRange;
            }
        }

        if (Mathf.Sign(transform.forward.z) != Mathf.Sign(Camera.main.transform.forward.z))
        {
            if (Mathf.Abs(Vector3.Angle((Camera.main.transform.position - lantern.transform.position).normalized, lantern.forward)) < angleOfLightDecrease && !areLightsDecreased)
            {
                lerpValueLightIntensity = initialLightIntensity / lightDecreaseFactor;

                lightLerpSpeed = cameraLightFadeSpeed;

                areLightsDecreased = true;
                areLightsIncreased = false;
            }
            else if (Mathf.Abs(Vector3.Angle((Camera.main.transform.position - lantern.transform.position).normalized, lantern.forward)) > angleOfLightDecrease && areLightsDecreased)
            {
                lerpValueLightIntensity = initialLightIntensity;

                lightLerpSpeed = cameraLightFadeSpeed;

                areLightsDecreased = false;
            }
        }

        if (isLightHighIntensity && !areLightsIncreased && !areLightsDecreased)
        {
            //Increase intensity
            lerpValueLightIntensity = initialLightIntensity * lightIncreaseFactor * (lightChargingTimer >= 1 ? 1 : 0.5f);
            areLightsIncreased = true;

            InstaLightIntensity();
            isLightHighIntensity = false;
            isMaxIntensity = true;
            return;
        }
        else if (!isLightHighIntensity && areLightsIncreased)
        {
            //Decrease intesnity
            lerpValueLightIntensity = initialLightIntensity;

            lightLerpSpeed = chargedLightFadeSpeed;

            areLightsIncreased = false;
        }

        lanternLight.intensity = Mathf.Lerp(lanternLight.intensity, lerpValueLightIntensity, Time.deltaTime * lightLerpSpeed);
    }

    private void LightDamaging()
    {
        if (!lightEnabled || (ghostsInRadius.Count == 0 && lightenObjectInRadius.Count == 0 && lightenedPuzzlesInRadius.Count == 0))
        {
            return;
        }

        ghostsInRadius.ForEach(x => { if (!x.activeInHierarchy) ghostsInRadius.Remove(x); });

        foreach (GameObject g in ghostsInRadius)
        {
            Enemy gc = g.GetComponent<Enemy>();

            if (Mathf.Abs(Vector3.Angle((g.transform.position - lantern.transform.position).normalized, lantern.forward)) < lanternDamageRadius && (!gc.IsInSight() || gc.IsStunned() != isMaxIntensity))
            {
                gc.InsideLanternRange(lanternDamage, isMaxIntensity);
            }
            else if (Mathf.Abs(Vector3.Angle((g.transform.position - lantern.transform.position).normalized, lantern.forward)) > lanternDamageRadius && gc.IsInSight())
            {
                gc.OutsideLanternRange();
            }
        }

        foreach (GameObject p in lightenObjectInRadius)
        {
            LightenableObject wp = p.GetComponent<LightenableObject>();

            if (Mathf.Abs(Vector3.Angle((wp.transform.position - lantern.transform.position).normalized, lantern.forward)) < lanternDamageRadius && !wp.IsInSight())
            {
                wp.InsideLanternRange();
            }
            else if (Mathf.Abs(Vector3.Angle((wp.transform.position - lantern.transform.position).normalized, lantern.forward)) > lanternDamageRadius && wp.IsInSight())
            {
                wp.OutsideLanternRange();
            }
        }

        if (isMaxIntensity)
        {
            Debug.Log("Flashing");
            foreach (GameObject l in lightenedPuzzlesInRadius)
            {
                LightenableObject co = l.GetComponent<LightenableObject>();

                if (Mathf.Abs(Vector3.Angle((co.transform.position - lantern.transform.position).normalized, lantern.forward)) < lanternDamageRadius && !co.IsInSight())
                {
                    co.InsideLanternRange();
                }
                else if (Mathf.Abs(Vector3.Angle((co.transform.position - lantern.transform.position).normalized, lantern.forward)) > lanternDamageRadius && co.IsInSight())
                {
                    co.OutsideLanternRange();
                }
            }

            isMaxIntensity = false;
        }

    }

    private void Timers()
    {
        if (!canMove && stopByAiming)
        {
            Debug.Log("Time stopped "+aimTimer);
            aimTimer += Time.deltaTime;

            if (aimTimer >= timeBetweenAim)
            {
                Debug.Log("Time finished");
                canMove = true;
                stopByAiming = false;

                aimTimer = 0;
            }
        }

        if (isLightCharging && lightChargingTimer < 1)
        {
            lightChargingTimer += Time.deltaTime / timeForMaxCharged;

            if (lightChargingTimer >= 1)
            {
                var col = chargindParticles.colorOverLifetime;
                col.color = lightsChargedGradientColor;

                lightChargingTimer = 1;
            }
        }

        if (!isLightCharging && delayBetweenChargedShotTimer < 1)
        {
            delayBetweenChargedShotTimer += Time.deltaTime / timeBetweenFlashes;

            if (delayBetweenChargedShotTimer >= 1)
            {
                delayBetweenChargedShotTimer = 1;
            }
        }
    }

    private void MoveChecker()
    {
        if (!canMove)
        {
            if (!myAudioSource.isPlaying && !stopByAiming)
            {
                MoveAgain();
            }
        }
    }

    private void ActionChecker()
    {
        if (GameManager.Instance.GetIsInCombateMode())
        {
            return;
        }

        bool actionDetected = false;

        RaycastHit hit;

        actionDetected = Physics.Raycast(transform.position, transform.forward, out hit, actionDistance);

        if (actionDetected)
        {
            if (hit.transform.tag != GameManager.Instance.GetTagOfDesiredType(GameManager.TypeOfTag.Wall) &&
                hit.transform.tag != GameManager.Instance.GetTagOfDesiredType(GameManager.TypeOfTag.Player) &&
                hit.transform.tag != GameManager.Instance.GetTagOfDesiredType(GameManager.TypeOfTag.Door))
            {
                if (hit.transform.tag == GameManager.Instance.GetTagOfDesiredType(GameManager.TypeOfTag.DoorTrigger))
                {
                    if (hit.transform.GetComponent<ConectionScript>().GetIsHiddenDoor())
                    {
                        return;
                    }
                }

                if (!GameManager.Instance.GetIsPlayerPanelActive())
                {
                    GameManager.Instance.ActivePlayerHUD(GameManager.ButtonRequest.A);
                }
            }
            else
            {
                if (GameManager.Instance.GetIsPlayerPanelActive())
                {
                    GameManager.Instance.DisablePlayerHUD();
                }
            }
        }
        else
        {
            if (GameManager.Instance.GetIsPlayerPanelActive())
            {
                GameManager.Instance.DisablePlayerHUD();
            }
        }
    }

    private void Action()
    {
        if (GameManager.Instance.GetIsInCombateMode())
        {
            return;
        }

        bool actionDetected = false;

        RaycastHit hit;

        actionDetected = Physics.Raycast(transform.position, transform.forward, out hit, actionDistance);

        if (actionDetected)
        {
            string hitTag = hit.transform.tag;

            if (hitTag == GameManager.Instance.GetTagOfDesiredType(GameManager.TypeOfTag.Wall))
            {
                return;
            }

            //Perform action
            if (hitTag == GameManager.Instance.GetTagOfDesiredType(GameManager.TypeOfTag.Door) ||
                hitTag == GameManager.Instance.GetTagOfDesiredType(GameManager.TypeOfTag.DoorTrigger))
            {
                faceDirection = -hit.normal;
            }
            else
            {
                faceDirection = (new Vector3(hit.transform.position.x, transform.position.y, hit.transform.position.z) - transform.position).normalized;
            }

            if (hitTag == GameManager.Instance.GetTagOfDesiredType(GameManager.TypeOfTag.DoorTrigger))
            {
                //Open door
                transform.forward = -hit.normal;
                if (hit.transform.GetComponent<ConectionScript>().OpenDoorAnimation())
                {
                    currentDoorCrossing = hit.transform.GetComponent<ConectionScript>();

                    transform.position = currentDoorCrossing.GetDoorOpeningPos(transform.position);
                    ChangePlayerState(State.CrossDoor);
                    SetPointToMoveCrossDoor(currentDoorCrossing.GetDoorClosingPos(transform.position));

                    myAudioSource.clip = SoundManager.Instance.GetSoundByRequest(SoundManager.SoundRequest.P_OpenDoor);
                    myAudioSource.Play();

                    return;
                }
            }

            if (hitTag == GameManager.Instance.GetTagOfDesiredType(GameManager.TypeOfTag.HittableObject))
            {
                TransparentObject to = hit.transform.parent.GetComponent<TransparentObject>();
                to.ShakeObjectAnimation();
            }
            else if (hit.transform.GetComponent<TransparentObject>() != null)
            {
                TransparentObject to = hit.transform.GetComponent<TransparentObject>();
                to.ShakeObjectAnimation();
            }

            if (hitTag == GameManager.Instance.GetTagOfDesiredType(GameManager.TypeOfTag.Bell) && 
                hit.transform.GetComponent<DoorBell>().enabled)
            {
                hit.transform.GetComponent<DoorBell>().OpenDoor();
                StopPlusSoundRequired(SoundManager.SoundRequest.P_ButtonPush);
                return;
            }

            StopPlusSoundRequired(SoundManager.SoundRequest.P_Knock);
        }

    }

    private void InstaLightIntensity()
    {
        lanternLight.intensity = lerpValueLightIntensity;
    }

    private void ChargeLight()
    {
        CombateMode();
        aimTimer = timeBetweenAim / 2;

        isLightCharging = true;
        lightChargingTimer = 0;

        chargindParticles.gameObject.SetActive(true);
        var col = chargindParticles.colorOverLifetime;
        col.color = lightsChargingGradientColor;
    }

    private void ReleaseLight()
    {
        CalmMode();
        aimTimer = timeBetweenAim / 2;

        isLightCharging = false;
        if (lightChargingTimer > 0.2f)
        {
            isLightHighIntensity = true;
        }
        delayBetweenChargedShotTimer = 0;

        chargindParticles.gameObject.SetActive(false);
    }

    private void SwitchLight()
    {
        if (lightEnabled)
        {
            lanternLight.gameObject.SetActive(false);
            lightEnabled = false;
        }
        else
        {
            lanternLight.gameObject.SetActive(true);
            lightEnabled = true;
        }
    }

    private void StopPlusSoundRequired(SoundManager.SoundRequest sr)
    {
        StopMovement();

        myAudioSource.clip = SoundManager.Instance.GetSoundByRequest(sr);

        myAudioSource.Play();
    }

    private void MoveAgain()
    {
        canMove = true;
        lightEnabled = false;
        SwitchLight();
    }

    private void StopMovement()
    {
        canMove = false;
        lightEnabled = true;
        SwitchLight();
    }

    private void StopMovementByAim()
    {
        faceDirection = transform.forward;

        stopByAiming = true;
        canMove = false;
        aimTimer = 0;
    }
    #endregion

    #region Cross Door Methods

    public void CrossDoor()
    {
        if (isCrossingDoor)
        {
            myCharController.Move(directionToGoCrossDoor * speed * Time.deltaTime);
            if (Vector3.Distance(pointToGoCrossDoor, transform.position) < 0.5f)
            {
                DoorCrossed();
            }
        }
    }

    private void DoorCrossed()
    {
        isCrossingDoor = false;
        currentDoorCrossing.CloseDoorAnimation();
        Camera.main.GetComponent<CameraBehaviour>().EndCrossDoorMovement();
        ChangePlayerState(State.Playing);
    }

    public void StartCrossingDoor()
    {
        isCrossingDoor = true;
    }

    public void SetPointToMoveCrossDoor(Vector3 otherSideDoor)
    {
        pointToGoCrossDoor = otherSideDoor;
        directionToGoCrossDoor = (otherSideDoor - transform.position).normalized;
    }

    #endregion

    #region Public Methods
    public void ChangePlayerState(State newState)
    {
        currentState = newState;
    }

    public void CombateMode()
    {
        independentFacing = true;
        StopMovementByAim();
    }

    public void CalmMode()
    {
        independentFacing = false;
        StopMovementByAim();
    }

    public void IncreaseHealth(int heal)
    {
        currentHp += heal;

        if (currentHp > initialHp)
        {
            currentHp = initialHp;
        }

        GameManager.Instance.ModifyHp(currentHp);
    }

    public void RecieveDamage(int damage)
    {
        currentHp -= damage;

        if (currentHp < 0)
        {
            currentHp = 0;
        }

        GameManager.Instance.ModifyHp(currentHp);
    }

    public Vector3 GetOutsideLightPoint(Vector3 referencePoint)
    {


        return Vector3.zero;
    }

    public bool IsHighLigthening()
    {
        return isMaxIntensity;
    }
    #endregion

    #region List Calls Methods
    public void OnGhostEnter(GameObject g)
    {
        if (!ghostsInRadius.Contains(g))
        {
            ghostsInRadius.Add(g);
        }
    }

    public void OnGhostExit(GameObject g)
    {
        if (ghostsInRadius.Contains(g))
        {
            ghostsInRadius.Remove(g);
        }
    }

    public void OnLightenObjectEnter(GameObject p)
    {
        if (!lightenObjectInRadius.Contains(p))
        {
            lightenObjectInRadius.Add(p);
        }
    }

    public void OnLightenObjectExit(GameObject p)
    {
        if (lightenObjectInRadius.Contains(p))
        {
            lightenObjectInRadius.Remove(p);
        }
    }

    public void OnLightenPuzzleEnter(GameObject l)
    {
        if (!lightenedPuzzlesInRadius.Contains(l))
        {
            lightenedPuzzlesInRadius.Add(l);
        }
    }

    public void OnLightenPuzzleExit(GameObject l)
    {
        if (lightenedPuzzlesInRadius.Contains(l))
        {
            lightenedPuzzlesInRadius.Remove(l);
        }
    }
    #endregion

    #region Unity Gizmos Method
    private void OnDrawGizmosSelected()
    {
        //Lantern Gizmos
        Gizmos.DrawLine(lanternLight.transform.position, lanternLight.transform.position + (lantern.forward * lanternDamageLength));
        Gizmos.DrawLine(lanternLight.transform.position, lanternLight.transform.position + (Quaternion.Euler(0, lanternDamageRadius,0) * lantern.forward * lanternDamageLength));
        Gizmos.DrawLine(lanternLight.transform.position, lanternLight.transform.position + (Quaternion.Euler(0, -lanternDamageRadius,0) * lantern.forward * lanternDamageLength));
        Gizmos.DrawLine(lanternLight.transform.position, lanternLight.transform.position + (Quaternion.Euler(lanternDamageRadius, 0, 0) * lantern.forward * lanternDamageLength));
        Gizmos.DrawLine(lanternLight.transform.position, lanternLight.transform.position + (Quaternion.Euler(-lanternDamageRadius, 0, 0) * lantern.forward * lanternDamageLength));

        //Action Gizmos
        Gizmos.color = Color.green;

        Gizmos.DrawLine(transform.position, transform.position + transform.forward * actionDistance);
    }
    #endregion

}
