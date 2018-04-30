using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransparentObject : MonoBehaviour {

    #region Public Variables
    [Header("\tGame Designers Variables")]
    [Tooltip("Genero fantasma?")]
    public bool spawnGhost;
    [Tooltip("Genero gema?")]
    public bool spawnGem;
    [Tooltip("Llamo a todos los fantasmas de la sala?")]
    public bool spawnAllGhosts;

    [Header("\t    Own Script Variables")]
    [Tooltip("Soy un muro?")]
    public bool isWall;
    [Tooltip("Soy una puerta? (Esta variable impide que haga shake el objeto tambien)")]
    public bool isDoor;
    [Tooltip("Soy estatico? (No shake)")]
    public bool isStatic;
    public Renderer myRenderer;
    public Animation myAnimation;
    public AnimationClip shakeAnimation;
    public AnimationClip openAnimation;

    private Transform player;
    #endregion

    #region Private Variables
    private bool isMaterialHidden = false;
    private float transparencyValue;

    private bool isHorizontalDoor;

    private Material myMat;
    #endregion

    private void Start()
    {
        transparencyValue = isWall ? GameManager.Instance.wallsHidenByCameraTransparency : GameManager.Instance.objectsHidenByCameraTransparency;

        if (myAnimation == null && !isWall)
        {
            myAnimation = GetComponent<Animation>();
        }

        if (!isWall && !isDoor)
        {
            myAnimation.AddClip(shakeAnimation, "Shake");
            myAnimation.AddClip(openAnimation, "Open");
        }

        if (isDoor)
        {
            isHorizontalDoor = transform.parent.eulerAngles.y != 0 && transform.parent.eulerAngles.y != 180;
        }

        if (myRenderer == null)
            myRenderer = GetComponent<Renderer>();

        myMat = myRenderer.material;

        player = GameManager.Instance.GetPlayer();
    }

    private void Update()
    {
        if ((isDoor && !isHorizontalDoor))
        {
            return;
        }
        if (isMaterialHidden)
        {
            if (Mathf.Abs((Camera.main.transform.position - transform.position).z) + GameManager.Instance.transparencyOffsetForward > Mathf.Abs((Camera.main.transform.position - player.position).z) ||
                (Mathf.Abs((transform.position - player.position).x) > GameManager.Instance.transparencyOffsetLateral && !isDoor))
            {
                ShowMaterial();
            }
        }
        else
        {
            if (Mathf.Abs((Camera.main.transform.position - transform.position).z) + GameManager.Instance.transparencyOffsetForward < Mathf.Abs((Camera.main.transform.position - player.position).z) &&
                Mathf.Abs((transform.position - player.position).x) < GameManager.Instance.transparencyOffsetLateral)
            {
                HideMaterial();
            }
        }
    }

    #region Hidden Methods
    public bool IsMaterialHidden()
    {
        return isMaterialHidden;
    }

    public void HideMaterial()
    {
        StandardShadersUtil.ChangeRenderMode(myMat, StandardShadersUtil.BlendMode.Fade);
        myMat.color = new Color(myMat.color.r, myMat.color.g, myMat.color.b, transparencyValue);
        myRenderer.material = myMat;
        isMaterialHidden = true;
    }

    public void ShowMaterial()
    {
        StandardShadersUtil.ChangeRenderMode(myMat, StandardShadersUtil.BlendMode.Opaque);
        myMat.color = new Color(myMat.color.r, myMat.color.g, myMat.color.b, 1f);
        myRenderer.material = myMat;
        isMaterialHidden = false;
    }
    #endregion

    #region Action Methods
    public void ShakeObjectAnimation(bool isFirstCall = true)
    {
        if (isWall || isDoor || isStatic)
        {
            return;
        }

        if (myAnimation.isPlaying)
        {
            return;
        }

        if (spawnGhost)
        {
            Ray ray = new Ray(transform.position + Vector3.up, Vector3.down);

            RaycastHit hit;

            Physics.Raycast(ray, out hit, 100, LayerMask.GetMask("FloorLayer"));

            GameObject go = EnemyManager.Instance.GetEnemy(hit.transform != null ? hit.transform.parent : this.transform);

            go.transform.position = new Vector3(transform.position.x, hit.point.y + EnemyManager.Instance.enemyFloorYOffset, transform.position.z);
            go.transform.forward = transform.forward;

            spawnGhost = false;

            if (isFirstCall && spawnAllGhosts)
            {
                hit.transform.parent.GetComponent<RoomScript>().ShowAllEnemiesFromRoom();
            }
        }

        if (spawnGem)
        {
            GameObject go = ObjectsManager.Instance.GetItem(this.transform, ObjectsManager.ItemRequest.Gem);

            go.transform.position = transform.position;
            go.GetComponent<GemObject>().DiscoveredByFeature();

            spawnGem = false;
        }

        myAnimation.clip = myAnimation.GetClip("Shake");
        myAnimation.Play();
    }

    public void OpenObject()
    {
        //If an object must be open like a room feature.
    }
    #endregion

}
