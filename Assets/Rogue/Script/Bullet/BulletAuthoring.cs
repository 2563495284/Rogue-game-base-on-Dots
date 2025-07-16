using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Rogue
{
    public class BulletAuthoring : MonoBehaviour
    {
        public BulletAssetData BulletAssetData;
        private class Baker : Baker<BulletAuthoring>
        {
            public override void Bake(BulletAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
                // AddComponent<LocalTransform>(entity);
                AddComponent(entity, new Bullet
                {
                    BulletId = authoring.BulletAssetData.BulletId,
                    BulletType = authoring.BulletAssetData.BulletType,
                    SpiltRadius = authoring.BulletAssetData.SpiltRadius,
                    CreateBulletID = authoring.BulletAssetData.CreateBulletID,
                    IsAtkDestroy = authoring.BulletAssetData.IsAtkDestroy
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
        public int BulletId;//子弹id
        public BulletType BulletType;//子弹类型
        public bool IsFlipX;

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
