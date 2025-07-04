using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Rogue
{
    public class BulletAuthoring : MonoBehaviour
    {
        [Header("子弹配置")]
        public BulletAssetData bulletAssetData;

        private class Baker : Baker<BulletAuthoring>
        {
            public override void Bake(BulletAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
                AddComponent<Bullet>(entity);

                // 添加子弹移动组件
                AddComponent(entity, new BulletMovement
                {
                    Speed = authoring.bulletAssetData.BulletSpeed,
                    Direction = float3.zero, // 将在发射时设置
                    StartPosition = float3.zero
                });

                // 添加子弹生命周期组件
                var bulletLifetime = new BulletLifetime();
                bulletLifetime.Initialize(authoring.bulletAssetData.BulletLifeTime);
                AddComponent(entity, bulletLifetime);

                // 添加子弹伤害组件
                AddComponent(entity, new BulletDamage
                {
                    Damage = authoring.bulletAssetData.damage,
                    CriticalChance = authoring.bulletAssetData.criticalChance,
                    CriticalDamage = authoring.bulletAssetData.criticalDamage,
                    HasHit = false,
                    Owner = Entity.Null // 将在发射时设置
                });
            }
        }
    }
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

        [Header("基础")]
        public int Id;//子弹id
        public int BulletAnimId;//子弹动画id
        public BulletType BulletType;//子弹类型

        public float Zoom;//缩放

        public float AtkBet;//攻击倍率

        public float SpiltRadius;//分裂半径

        public float AimOffset;//瞄准方向位置偏移

        public int CreateBulletID;//子弹销毁后创建子弹id
        public int IsAtkDestroy;//是否攻击后销毁

        public int AtkFrame;//伤害帧

        public float BulletCollisionR;//子弹碰撞半径

        public float BulletSpeed;//子弹速度

        public float BulletAcceleration;//子弹加速度

        public float BulletDuration;//子弹持续时间

        public float BulletDelay;//子弹延迟

        public float BulletInterval;//子弹间隔

        [Header("扇形")]
        public float WavingAngle;//扇形角度

        public float WavingRadius;//扇形半径

        [Header("戳刺")]
        public float PokeWidth;//戳刺宽度

        public float PokeLength;//戳刺长度

        [Header("环绕")]
        public float BulletSurroundR;//环绕半径

        public float BulletSurroundAngle;//环绕角度

        public float BulletSurroundSpeed;//环绕速度

        public float BulletSurroundDelay;//环绕延迟

        [Header("抛物线")]
        public float ParabolaAngle;//抛物线角度

        public float ParabolaSpeed;//抛物线速度

        public float ParabolaDelay;//抛物线延迟

        [Header("定点伤害")]
        public float FixedDamage;//定点伤害

        public float FixedDelay;//定点伤害延迟



    }
}
