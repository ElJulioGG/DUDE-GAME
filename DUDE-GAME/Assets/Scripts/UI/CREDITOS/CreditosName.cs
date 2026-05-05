using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
[CreateAssetMenu(fileName = "New Credit", menuName = "Credits/Credit Entry")]
public class CreditosName : ScriptableObject
{
    public string creditTitle;
    public string personName;
    public Sprite personImage;

}
