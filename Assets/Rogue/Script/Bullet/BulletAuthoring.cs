using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Rogue
{
    // public class BulletAuthoring : MonoBehaviour
    // {
    //     private class Baker : Baker<BulletAuthoring>
    //     {
    //         public override void Bake(BulletAuthoring authoring)
    //         {
    //             var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
    //             AddComponent<Bullet>(entity, new Bullet
    //             {
    //                 Id = authoring.bulletAssetData.Id,
    //                 BulletType = authoring.bulletAssetData.BulletType,
    //                 Zoom = authoring.bulletAssetData.Zoom,
    //                 AtkBet = authoring.bulletAssetData.AtkBet,
    //                 SpiltRadius = authoring.bulletAssetData.SpiltRadius,
    //                 AimOffset = authoring.bulletAssetData.AimOffset,
    //                 CreateBulletID = authoring.bulletAssetData.CreateBulletID,
    //                 IsAtkDestroy = authoring.bulletAssetData.IsAtkDestroy,
    //                 AtkFrame = authoring.bulletAssetData.AtkFrame,
    //                 BulletCollisionR = authoring.bulletAssetData.BulletCollisionR,
    //                 BulletSpeed = authoring.bulletAssetData.BulletSpeed,
    //                 BulletAcceleration = authoring.bulletAssetData.BulletAcceleration,
    //                 BulletDelay = authoring.bulletAssetData.BulletDelay,
    //                 BulletInterval = authoring.bulletAssetData.BulletInterval,
    //                 WavingAngle = authoring.bulletAssetData.WavingAngle,
    //                 WavingRadius = authoring.bulletAssetData.WavingRadius,
    //                 PokeWidth = authoring.bulletAssetData.PokeWidth,
    //                 PokeLength = authoring.bulletAssetData.PokeLength,
    //                 BulletSurroundR = authoring.bulletAssetData.BulletSurroundR,
    //                 BulletSurroundAngle = authoring.bulletAssetData.BulletSurroundAngle,
    //                 BulletSurroundSpeed = authoring.bulletAssetData.BulletSurroundSpeed,
    //                 BulletSurroundDelay = authoring.bulletAssetData.BulletSurroundDelay,
    //                 ParabolaAngle = authoring.bulletAssetData.ParabolaAngle,
    //                 ParabolaSpeed = authoring.bulletAssetData.ParabolaSpeed,
    //                 ParabolaDelay = authoring.bulletAssetData.ParabolaDelay,
    //                 FixedDamage = authoring.bulletAssetData.FixedDamage,
    //                 FixedDelay = authoring.bulletAssetData.FixedDelay,
    //             });

    //             // 添加子弹移动组件
    //             AddComponent(entity, new BulletMovement
    //             {
    //                 Direction = Vector2.right, // 将在发射时设置
    //                 StartPosition = Vector2.zero,
    //                 BulletType = authoring.bulletAssetData.BulletType
    //             });

    //             // 添加子弹生命周期组件
    //             var bulletLifetime = new BulletLifetime();
    //             bulletLifetime.Initialize(authoring.bulletAssetData.BulletLifeTime);
    //             AddComponent(entity, bulletLifetime);

    //             // 添加子弹伤害组件
    //             AddComponent(entity, new BulletDamage
    //             {
    //                 Damage = authoring.bulletAssetData.damage,
    //                 CriticalChance = authoring.bulletAssetData.criticalChance,
    //                 CriticalDamage = authoring.bulletAssetData.criticalDamage,
    //                 HasHit = false,
    //                 Owner = Entity.Null // 将在发射时设置
    //             });

    //         }
    //     }
    // }
    public enum BulletType
    {

        Waving,//定点挥砍
        Poke,//定点戳刺
        Liner,//直线
        Surround,//环绕
        Parabola,//抛物线
        Fixed//定点伤害
    }
    public struct Bullet : IComponentData
    {

        public int BulletId;//子弹id
        public BulletType BulletType;//子弹类型

        // public float AtkBet;//攻击倍率

        public float SpiltRadius;//分裂半径

        public int CreateBulletID;//子弹销毁后创建子弹id
        public int IsAtkDestroy;//是否攻击后销毁

        // [Header("扇形")]
        // public float WavingAngle;//扇形角度

        // public float WavingRadius;//扇形半径

        // [Header("戳刺")]
        // public float PokeWidth;//戳刺宽度

        // public float PokeLength;//戳刺长度

        // [Header("环绕")]
        // public float BulletSurroundR;//环绕半径

        // public float BulletSurroundAngle;//环绕角度

        // public float BulletSurroundSpeed;//环绕速度

        // public float BulletSurroundDelay;//环绕延迟

        // [Header("抛物线")]
        // public float ParabolaAngle;//抛物线角度

        // public float ParabolaSpeed;//抛物线速度

        // public float ParabolaDelay;//抛物线延迟

        // [Header("定点伤害")]
        // public float FixedDamage;//定点伤害

        // public float FixedDelay;//定点伤害延迟

    }
    // public struct BulletWaving : IComponentData
    // {
    //     public float WavingAngle;//扇形角度
    //     public float WavingRadius;//扇形半径
    // }
    // public struct BulletPoke : IComponentData
    // {
    //     public float PokeWidth;//戳刺宽度
    // }
}
