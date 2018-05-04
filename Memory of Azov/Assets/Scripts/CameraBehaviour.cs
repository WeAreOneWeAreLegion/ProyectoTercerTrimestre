using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class CameraBehaviour : MonoSingleton<CameraBehaviour> {

    public enum CamState { Following, CrossDoor, Cinematic }

    #region Public Variables
    [Header("\tGame Designers Variables")]
    [Tooltip("Distancia que mantiene la camara respecto al target actual")]
    [Range(5,120)] public float cameraDistance = 10f;
    [Tooltip("Altura que mantiene la camara respecto al target actual")]
    [Range(0,4)] public float cameraYOffset = 2f;
    [Tooltip("Angulo que mantiene la camara respecto al target actual")]
    [Range(0,90)] public float cameraAngle = 25f;
    [Tooltip("Velocidad de la camara al apuntar al target actual")]
    [Range(0,40)] public float cameraFollowSpeed = 15f;
    [Tooltip("La longitud en la cual el player parará de moverse al chochar una pared lateral (Linea Blanca)")]
    [Range(0,10)] public float sideDistance = 4.5f;
    [Tooltip("La longitud en la cual el player parará de moverse al chochar una trasera lateral (Linea Azul)")]
    [Range(0,10)] public float backwardsDistance = 4.5f;

    [Header("CrossDoor Variables")]
    [Tooltip("Cuantos segundos tardara la camara en moverse de la entrada de la puerta a la salida de la puerta")]
    [Range(0, 40)] public float crossDoorCameraSpeed = 0.2f;

    [Header("\t    Own Script Variables")]
    [Tooltip("Target a seguir")]
    public Transform target;
    [Tooltip("Ray lateral para detectar paredes y por tanto clampear la camara")]
    [Range(50,150)] public float raySidesDistance = 100f;
    #endregion

    #region Private Variables
    private float xLeftWall;
    private float xRightWall;
    private float zBackWall;
    private bool wallsFound;
    private bool backwardDistanceSet;
    private float backwardFirstDistance;

    private Vector3 moveAtPoint;

    private CamState currentState;
    private Vector3 backwards = Vector3.zero;

    private TransparentObject currentWall;
    #endregion

    private void Start()
    {
        if (target == null)
        {
            target = GameManager.Instance.GetPlayer();
        }
    }

    private void Update ()
    {
        if (Time.timeScale == 0)
            return;
        if (currentState == CamState.Following)
            CameraMovement();
        else if (currentState == CamState.CrossDoor)
            CrossDoorMovement();
        else if (currentState == CamState.Cinematic)
        {   
            //Cinematic
        }
    }

    #region Following Methods
    private void CameraMovement()
    {
        if (cameraAngle != transform.rotation.eulerAngles.x)
        {
            transform.rotation = Quaternion.Euler(Vector3.right*cameraAngle);
        }

        if (!wallsFound)
        {
            RaycastHit hit;

            Ray ray = new Ray(target.transform.position, -transform.right);
            Physics.Raycast(ray, out hit, raySidesDistance, LayerMask.GetMask("WallLayer"));
            //Set left wall
            xLeftWall = hit.transform.position.x;

            ray = new Ray(target.transform.position, transform.right);
            Physics.Raycast(ray, out hit, raySidesDistance, LayerMask.GetMask("WallLayer"));
            //Set right wall
            xRightWall = hit.transform.position.x;

            ray = new Ray(target.transform.position, -transform.forward);
            Physics.Raycast(ray, out hit, raySidesDistance, LayerMask.GetMask("WallLayer"));
            //Set back wall
            try
            {
                zBackWall = hit.transform.position.z;
            }
            catch
            {
                EditorApplication.isPaused = true;
            }

            wallsFound = true;
        }

        bool rightCollision = Mathf.Abs(target.transform.position.x - xRightWall) < sideDistance;
        bool leftCollision = Mathf.Abs(target.transform.position.x - xLeftWall) < sideDistance;
        bool backCollision = Mathf.Abs(target.transform.position.z - zBackWall) < backwardsDistance;

        float yPosition = (target.position + (Vector3.up * cameraYOffset) - (transform.forward * cameraDistance)).y;

        if (!rightCollision && !leftCollision && !backCollision)
        {
            //TotalLerp
            transform.position = Vector3.Lerp(transform.position, target.position + (Vector3.up * cameraYOffset) - (transform.forward * cameraDistance), cameraFollowSpeed * Time.deltaTime);
        }
        else if (backCollision && (leftCollision || rightCollision))
        {
            if (leftCollision)
            {
                transform.position = Vector3.Lerp(transform.position, new Vector3(xLeftWall + sideDistance, yPosition, zBackWall - (transform.forward * cameraDistance).z + backwardsDistance), cameraFollowSpeed * Time.deltaTime);
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, new Vector3(xRightWall - sideDistance, yPosition, zBackWall - (transform.forward * cameraDistance).z + backwardsDistance), cameraFollowSpeed * Time.deltaTime);
            }
            //Clamp to corner position
        }
        else if (leftCollision || rightCollision)
        {
            if (leftCollision)
            {
                transform.position = Vector3.Lerp(transform.position, new Vector3(xLeftWall + sideDistance, yPosition, (target.position - (transform.forward * cameraDistance)).z), cameraFollowSpeed * Time.deltaTime);
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, new Vector3(xRightWall - sideDistance, yPosition, (target.position - (transform.forward * cameraDistance)).z), cameraFollowSpeed * Time.deltaTime);
            }
        }
        else //Backwards hit
        {
            transform.position = Vector3.Lerp(transform.position, new Vector3(target.position.x, yPosition, zBackWall - (transform.forward * cameraDistance).z + backwardsDistance), cameraFollowSpeed * Time.deltaTime);
        }
    }
    #endregion

    #region Cross Door Methods
    private void CrossDoorMovement()
    {
        transform.position = Vector3.Lerp(transform.position, moveAtPoint + (Vector3.up * cameraYOffset) - (transform.forward * cameraDistance), Time.deltaTime / crossDoorCameraSpeed);
    }

    public void MoveAtPoint(Vector3 referencePoint, bool isRightSide)
    {
        moveAtPoint = referencePoint + (Vector3.down * referencePoint.y) + (Vector3.up * GameManager.Instance.GetPlayer().position.y) + ((isRightSide ? -1 : 1) * Vector3.right * sideDistance);
    }

    public void EndCrossDoorMovement()
    {
        ChangeCameraBehaviourState(CamState.Following);
        ResetWallDetection();
    }
    #endregion

    #region Public Methods
    public void ChangeCameraBehaviourState(CamState newState)
    {
        currentState = newState;
    }

    public void ResetWallDetection()
    {
        wallsFound = false;
    }
    #endregion

    #region Unity Inspector/Gizmos Methods
    //Private Unity Methods
    private void OnValidate()
    {
        if (target == null)
            return;

        if (cameraAngle != transform.eulerAngles.x)
        {
            transform.rotation = Quaternion.Euler(Vector3.right * cameraAngle);
        }
        if (cameraDistance != (target.position - transform.position).magnitude)
        {
            transform.position = target.position + (Vector3.up * cameraYOffset) - (transform.forward * cameraDistance);
        }
        if (cameraYOffset != (transform.position - target.position + (transform.forward * cameraDistance)).y)
        {
            transform.position = target.position + (Vector3.up * cameraYOffset) - (transform.forward * cameraDistance);
        }
    }

    private void OnDrawGizmosSelected()
    {
        backwards = transform.position - target.position;
        backwards.y = 0;
        backwards.Normalize();

        Gizmos.color = Color.white;
        Gizmos.DrawLine(target.transform.position, target.transform.position + (transform.right * sideDistance));
        Gizmos.DrawLine(target.transform.position, target.transform.position - (transform.right * sideDistance));
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(target.transform.position, target.transform.position + (backwards * backwardsDistance));
        Gizmos.color = Color.red;
        Gizmos.DrawLine(target.transform.position, target.transform.position + ((transform.position - target.position).normalized * cameraDistance));
    }
    #endregion
}
