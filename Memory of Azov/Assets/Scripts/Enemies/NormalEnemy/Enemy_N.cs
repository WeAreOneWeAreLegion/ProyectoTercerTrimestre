using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_N : Enemy
{
    #region Override Methods
    protected override void Start()
    {
        base.Start();

        ChangeState(new AwakeState_N());
    }

    public override void ResetVariables()
    {
        base.ResetVariables();

        ChangeState(new AwakeState_N());
    }
    #endregion
}
