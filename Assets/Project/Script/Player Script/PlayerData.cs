using System;   
using UnityEngine;

public class PlayerData : MonoBehaviour
{
    [Header("Status")]
    public bool isAlive = true;

    [SerializeField]
    private float thirst = 100f;

    public float Thirst
    {
        get => thirst;
        set => thirst = Mathf.Clamp(value, 0, 100);
    }

    [Header("Inventory")]
    ///<summary>
   ///  Teacher Card is the Master key in many game , it can open all the classroom in the hallway.
   /// </summary>
    [SerializeField] private bool hasTeacherCard;

    public bool HasTeacherCard => hasTeacherCard;

    [Header("Progress")]
    ///<summary>
   ///  is loading the Map deadline which is the main goal.
   /// </summary>
    [SerializeField] private bool isRenderingMap;

    public bool IsRenderingMap
    {
        get => isRenderingMap;
        set => isRenderingMap = value;
    }

    [Header("Position")]
     ///<summary>
   ///  current position on the Map of the Player
   ///</summary>
    public int currentNodeId;

    [Header("Danger")]
    public bool isBeingSeen;
    public bool isBeingChased;
}







   
  