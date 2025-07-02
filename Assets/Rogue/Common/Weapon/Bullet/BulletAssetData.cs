using UnityEngine;

namespace Rogue
{


    [CreateAssetMenu(fileName = "BulletAssetData", menuName = "Scriptable Objects/Bullet")]
    public class BulletAssetData : ScriptableObject
    {
        [Header("额外")]
        public string Name;//名称

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
