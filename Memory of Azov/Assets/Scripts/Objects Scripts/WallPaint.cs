using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallPaint : LightenableObject
{
    #region Public Variables
    [Header("\tGame Designers Variables")]
    [Tooltip("Tiempo que tardara a hacer aparecer el objeto en caso de que este siendo enfocado")]
    [Range(1,5)] public float timeToSpawnObject = 3f;

    [Header("\t    Own Script Variables")]
    [Header("Drop Variables")]
    [Tooltip("Tipo de drop")]
    public ObjectsManager.ItemRequest itemToDrop = ObjectsManager.ItemRequest.Gem;
    [Tooltip("Particulas al ser enfocado")]
    public GameObject particles;
    #endregion

    #region Private Variables
    private bool insideRadius;
    private float timer;

    private Transform target;
    #endregion

	private void Start ()
    {
        target = GameManager.Instance.GetPlayer();
        particles.SetActive(false);
    }
	
	private void Update ()
    {
        CheckPlayerDistance();
        Lightened();
    }

    #region Lighten Methods
    private void CheckPlayerDistance()
    {
        if (Vector3.Distance(target.position, transform.position) < target.GetComponent<PlayerController>().lanternDamageLength)
        {
            target.GetComponent<PlayerController>().OnLightenObjectEnter(this.gameObject);
        }
        else
        {
            target.GetComponent<PlayerController>().OnLightenObjectExit(this.gameObject);
            OutsideLanternRange();
        }
    }

    private void Lightened()
    {
        if (insideRadius)
        {
            if (!particles.activeInHierarchy)
                particles.SetActive(true);

            timer += Time.deltaTime;

            if (timer >= timeToSpawnObject)
            {
                //Spawn object
                GameObject go = ObjectsManager.Instance.GetItem(transform, itemToDrop);

                if (go != null)
                {
                    go.transform.position = transform.position - transform.forward;
                    go.transform.forward = -transform.forward;
                }

                gameObject.SetActive(false);
            }
        }
        else
        {
            if (timer > 0)
            {
                if (particles.activeInHierarchy)
                    particles.SetActive(false);

                timer = 0;
            }
        }
    }

    public override void InsideLanternRange()
    {
        if (GameManager.Instance.GetIsInCombateMode())
        {
            return;
        }
        insideRadius = true;
    }

    public override void OutsideLanternRange()
    {
        insideRadius = false;
    }

    public override bool IsInSight()
    {
        return insideRadius;
    }
    #endregion
}
