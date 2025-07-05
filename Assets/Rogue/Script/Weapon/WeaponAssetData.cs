using UnityEngine;

[CreateAssetMenu(fileName = "WeaponAssetData", menuName = "Scriptable Objects/Weapon")]
public class WeaponAssetData : ScriptableObject
{

    [Header("额外")]
    public string Desc;//描述

    [Header("基础")]
    public int Id;//武器id

    public int Level;//等级
    public int AnimId;//动画id
    public int Zoom;//缩放倍率


    [Header("伤害")]
    public float Damage;//伤害
    public int Range;//攻击范围

    public float Cooldown;//冷却时间

    public int Attribute;//属性

    public float CriticalChance;//暴击几率
    public float CriticalDamage;//暴击伤害

    [Header("子弹")]
    public int BulletId;//子弹id

    public int BulletNum;//子弹数量,每次攻击生成子弹数量

    public int TrajectoryNum;//弹道数量

}
