using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Rogue
{
    public partial struct SpawnSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Config>();
            state.RequireForUpdate<ExecuteSpawn>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Enabled = false;
            var config = SystemAPI.GetSingleton<Config>();
            var rand = new Random(123);

            //spawn player
            {
                var playerEntity = state.EntityManager.Instantiate(config.PlayerPrefab);
                var playerTransform = state.EntityManager.GetComponentData<LocalTransform>(playerEntity);
                playerTransform.Position = new float3(0, 0, 0);
                state.EntityManager.SetComponentData(playerEntity, playerTransform);
            }

            // spawn enemies
            {
                var enemyTransform = state.EntityManager.GetComponentData<LocalTransform>(config.EnemyPrefab);
                var halfAreaSize = config.EnemySpawnAreaSize * 0.5f;
                var fixedZ = enemyTransform.Position.z; // 使用预制体的z坐标作为固定深度

                for (int i = 0; i < config.NumEnemies; i++)
                {
                    var enemyEntity = state.EntityManager.Instantiate(config.EnemyPrefab);

                    // 在x-y平面的400x400区域内随机生成位置
                    var randomX = rand.NextFloat(-halfAreaSize, halfAreaSize);
                    var randomY = rand.NextFloat(-halfAreaSize, halfAreaSize);
                    var spawnPosition = new float3(randomX, randomY, fixedZ);

                    // 设置敌人位置
                    var transform = enemyTransform;
                    transform.Position = spawnPosition;
                    state.EntityManager.SetComponentData(enemyEntity, transform);

                    // 设置敌人移动参数（确保方向仅在x-y平面）
                    var randomDirection = rand.NextFloat3Direction();
                    var planarDirection = math.normalize(new float3(randomDirection.x, randomDirection.y, 0));

                    var enemyMovement = new EnemyMovement
                    {
                        Direction = planarDirection,
                        Speed = config.EnemyMoveSpeed,
                        DirectionChangeTimer = 0f,
                        DirectionChangeInterval = config.EnemyDirectionChangeInterval + rand.NextFloat(-0.5f, 0.5f) // 添加一些随机变化
                    };
                    state.EntityManager.SetComponentData(enemyEntity, enemyMovement);

                    // 设置敌人状态为移动
                    var enemy = state.EntityManager.GetComponentData<Enemy>(enemyEntity);
                    enemy.State = EnemyState.IDLE;
                    state.EntityManager.SetComponentData(enemyEntity, enemy);
                }
            }
        }
    }
}
