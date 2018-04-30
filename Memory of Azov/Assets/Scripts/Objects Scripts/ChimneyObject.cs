using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChimneyObject : LightenableObject
{

    #region Public Variables
    [Header("\t    Own Script Variables")]
    [Tooltip("Mesh de la flama")]
    public MeshRenderer flareRenderer;
    [Tooltip("Material para el mesh de la flama cuando la chimenea este activa")]
    public Material lightenedMaterial;
    [Tooltip("Luz a activar cuando la chimenea este activa")]
    public Light flareLight;
    #endregion

    #region Private Variables
    private bool insideRadius;

    private Transform target;
    #endregion

    private void Start()
    {
        target = GameManager.Instance.GetPlayer();
    }

    private void Update()
    {
        CheckPlayerDistance();
        CheckLighten();
    }

    #region Lighten Methods
    private void CheckPlayerDistance()
    {
        if (Vector3.Distance(target.position, flareRenderer.transform.position) < target.GetComponent<PlayerController>().lanternDamageLength)
        {
            target.GetComponent<PlayerController>().OnLightenPuzzleEnter(this.gameObject);
        }
        else
        {
            target.GetComponent<PlayerController>().OnLightenPuzzleExit(this.gameObject);
        }
    }

    private void CheckLighten()
    {
        if (insideRadius)
        {
            flareRenderer.material = lightenedMaterial;
            flareLight.gameObject.SetActive(true);
            enabled = false;
        }
    }

    public override void InsideLanternRange()
    {
        insideRadius = true;
    }

    public override bool IsInSight()
    {
        return insideRadius;
    }
    #endregion
}
