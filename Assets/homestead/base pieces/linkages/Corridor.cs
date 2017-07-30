using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Corridor : Powerline
{
    protected override Vector3 EndCapLocalPosition { get { return Vector3.zero; } }
    protected override Quaternion EndCapLocalRotation { get { return Quaternion.identity; } }
    protected override Vector3 EndCapWorldScale { get { return Vector3.one; } }
}
