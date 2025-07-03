using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Rogue
{
    public class WeaponAuthoring : MonoBehaviour
    {
        public WeaponAssetData WeaponAssetData;
        private class Baker : Baker<WeaponAuthoring>
        {
            public override void Bake(WeaponAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
                AddComponent<Weapon>(entity, new Weapon
                {
                    Id = authoring.WeaponAssetData.Id,
                    Level = authoring.WeaponAssetData.Level,
                    AnimId = authoring.WeaponAssetData.AnimId,
                    Zoom = authoring.WeaponAssetData.Zoom,
                    Damage = authoring.WeaponAssetData.Damage,
                    Range = authoring.WeaponAssetData.Range,
                    Cooldown = authoring.WeaponAssetData.Cooldown,
                    Attribute = authoring.WeaponAssetData.Attribute,
                    CriticalChance = authoring.WeaponAssetData.CriticalChance,
                    CriticalDamage = authoring.WeaponAssetData.CriticalDamage,
                    BulletId = authoring.WeaponAssetData.BulletId,
                    BulletNum = authoring.WeaponAssetData.BulletNum,
                    TrajectoryNum = authoring.WeaponAssetData.TrajectoryNum,
                });

                // 添加武器冷却组件
                var cooldown = new WeaponCooldown();
                cooldown.StartCooldown(0f); // 初始可以立即射击
                AddComponent(entity, cooldown);
            }
        }
    }

    public struct Weapon : IComponentData
    {

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
}
