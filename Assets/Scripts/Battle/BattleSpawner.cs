using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SpawnRequest
{
    public Team team;
    public GameObject prefabs;
    public Character characterData;//Serializable 数据
}


public class BattleSpawner : MonoBehaviour
{
    [SerializeField] private BattleFormation formation;
    [SerializeField] private BattleManager battle;
    [SerializeField] private List<SpawnRequest> initialEnemies = new();
    [SerializeField] private List<SpawnRequest> initialPlayers = new();
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private Canvas worldUICanvas;


    [SerializeField] private EncounterDataBase encounterDatabase;
    [SerializeField] private GameObject enemyPrefab;

    private bool _spawnInitialDone = false;

    private readonly System.Collections.Generic.Queue<SpawnRequest> _enemyReserve = new();

    public EncounterData CurrentEncounterData { get; private set; }

    private void Awake()
    {
        Debug.Log($"[BattleSpawner] Awake instanceID={GetInstanceID()} active={gameObject.activeInHierarchy}");
        //槽位释放后补位：死一个补一个
        formation.OnSlotChanged += HandleSlotChanged;
    }
    private void Start()
    {
        if (spawnOnStart)
        {
            SpawnInitial();
        }
    }
    private void OnDestroy()
    {
        formation.OnSlotChanged -= HandleSlotChanged;
    }
    private void HandleSlotChanged(Team team, int slotIndex2, BaseController prev, BaseController cur) {
        //只有Enemy slot 变空时补位
        if (team == Team.Enemy && prev != null && cur == null)
        {
            TryFillOneEnemy();
        }
    }
    private void EnqueueOrSpawn(SpawnRequest req)
    {
        //Debug.Log($"[Reserve ENQ] spawnerId={GetInstanceID()} {req.characterData.Name} queue={_enemyReserve.Count + 1}");
        if (req.team != Team.Enemy && req.team != Team.Player) return;

        var slotIndex = formation.FindFirstEmpty(req.team);
        if (slotIndex < 0)//他小于0 其实是跟formation.FindFirstEmpty 的返回约定有关 因为在没有空位的情况下返回值是-1
        {
            if (req.team == Team.Enemy)
            {
                Debug.Log($"[Reserve ENQ] {req.characterData.Name} (no slot) queue={_enemyReserve.Count + 1}");
                _enemyReserve.Enqueue(req);
            }
            else
            {
                Debug.LogWarning($"[Player Spawn] No slot for {req.characterData.Name}");
            }
            return;
        }
         bool ok = SpawnInToSlot(req, slotIndex);
        if (!ok && req.team == Team.Enemy)
        {
            Debug.Log($"[Reserve ENQ] {req.characterData.Name} (spawn failed) queue={_enemyReserve.Count + 1}");
            _enemyReserve.Enqueue(req);
        } 
    }
    public void TryFillOneEnemy()
    {
        if (_enemyReserve.Count == 0) return;

        int empty = formation.FindFirstEmpty(Team.Enemy);
        if (empty < 0) return;

        var req=_enemyReserve.Dequeue();
        SpawnInToSlot(req, empty);
    }
    private bool SpawnInToSlot(SpawnRequest req, int slotIndex)
    {
        var go = Instantiate(req.prefabs);
        var ctrl = go.GetComponent<BaseController>();
        ctrl.Init(req.characterData);

        //先注入ui依赖（只对需要worldui的控制器做）
        if(go.TryGetComponent<EnemyController>(out var enemyCtrl))
        {
            enemyCtrl.InjectWorldUICanvas(worldUICanvas);
        }
        //占位
        bool ok = formation.TryOccupy(req.team, slotIndex, ctrl);
        if (!ok)
        {
            Destroy(go);
            //下面是敌人逻辑，为了改成通用逻辑进行注释，并且固定在EnqueueOrSpawn方法当中
            /*//槽位被抢了；极端情况，敌人回队列 
            if (req.team == Team.Enemy) _enemyReserve.Enqueue(req);
            return;*/
            return false;
        }
        //站位
        var anchor = formation.GetAnchor(req.team, slotIndex);
        ctrl.transform.position = anchor.position;
        //站位完成后进行初始化血条
        if (enemyCtrl != null)
        {
            enemyCtrl.EnsureHpBarInitialized();
        }
 
        //注册进战斗/时间轴
        battle.RegisterController(ctrl);

        return true;
    }
    public void SpawnInitial()
    {
        if (_spawnInitialDone) return;
        _spawnInitialDone = true;

        _enemyReserve.Clear();
        CurrentEncounterData = null;

        Debug.Log($"[SpawnInitial] called frame={Time.frameCount} queue={_enemyReserve.Count}");
        //Player: 按顺序塞（0-3）//原先的测试配置
        SpawnPlayers();
       
        /* //Enemy:
         foreach(var req in initialEnemies)
         {
             EnqueueOrSpawn(req);
         }*/
        //Enemy:优先使用EncounterData
        if (!TrySpawnEnemiesFromEncounter())
        {
            foreach(var req in initialEnemies)
            {
                if (req == null)
                    continue;
                SpawnRequest copiedRq = new SpawnRequest
                {
                    team = req.team,
                    prefabs = req.prefabs,
                    characterData = req.characterData!= null ? req.characterData.Copy() : null
                };
                EnqueueOrSpawn(copiedRq);
            }
        }
    }
    private void SpawnPlayers()
    {
        List<Character> initialPartyChracters = new List<Character>();

        foreach (var req in initialPlayers)
        {
            if (req == null || req.characterData == null)
                continue;
            initialPartyChracters.Add(req.characterData);
        }
       // PartyRuntimeState.InitializeIfEmpty(initialPartyChracters);

        int count = Mathf.Min(
     Mathf.Min(PartyRuntimeState.PartyMembers.Count, initialPlayers.Count),
     4);

        for (int i = 0; i < count; i++)
        {
            Character sourceCharacter = PartyRuntimeState.PartyMembers[i];

            Debug.Log(
    $"[SpawnPlayers] name={sourceCharacter.Name}, hp={sourceCharacter.Hp}/{sourceCharacter.MaxHp}, portrait={(sourceCharacter.Portrait == null ? "NULL" : sourceCharacter.Portrait.name)}"
);

            if (sourceCharacter == null)
                continue;

            Character battleCharacter = sourceCharacter.Copy();

            SpawnRequest req = new SpawnRequest
            {
                team = Team.Player,
                prefabs = initialPlayers[i].prefabs,
                characterData = battleCharacter
            };

            SpawnInToSlot(req, i);
        }

    }
    /* public void SpawnPlayerInitial(List<SpawnRequest> playerTeam)
     {
         for(int i = 0; i < playerTeam.Count; i++)
         {
             SpawnInToSlot(playerTeam[i], i);
         }
     }*/
    private bool TrySpawnEnemiesFromEncounter()
    {
        Debug.Log($"[BattleSpawner] TrySpawnEnemiesFromEncounter encounterId={FieldBattleContext.CurrentEncounterId}");
        if (encounterDatabase == null)
        {
            Debug.LogWarning($"[BattleSpawner] EncounterDataBase is null.use InitialEnemies fallback");
            return false;
        }
        string encounterId = FieldBattleContext.CurrentEncounterId;
        if (string.IsNullOrEmpty(encounterId))
        {
            Debug.Log("[BattleSpawner no CurrentEncoutnerId. use InitialEnemies fallback]");
            return false;
        }
        EncounterData encounterData = encounterDatabase.FindeById(encounterId);

        if (encounterData == null)
        {
            Debug.LogWarning($"[BattleSpawner] EncounterData not found: {encounterId}. Use initialEnemies fallback.");
            return false;
        }

        if (!encounterData.ValidateConfig())
        {
            Debug.LogWarning($"[BattleSpawner] EncounterData is invalid: {encounterId}. Use initialEnemies fallback.");
            return false;
        }

        if (enemyPrefab == null)
        {
            Debug.LogWarning("[BattleSpawner] enemyPrefab is null. Use initialEnemies fallback.");
            return false;
        }
        CurrentEncounterData = encounterData;

        int spawnedEnemyCount = 0;
        foreach(var enemyCharacter in encounterData.EnemyChatacters)
        {
            if (enemyCharacter == null)
                continue;

            SpawnRequest req = new SpawnRequest()
            {
                team = Team.Enemy,
                prefabs = enemyPrefab,
                characterData = enemyCharacter.Copy()
            };
            
            EnqueueOrSpawn(req);
            spawnedEnemyCount++;
        }

        if (spawnedEnemyCount <= 0)
        {
            CurrentEncounterData = null;
            Debug.LogWarning($"[BattleSpawner] EncounterData has no spawnable enemies: {encounterId}. Use initialEnemies fallback.");
            return false;
        }

        Debug.Log($"[BattleSpawner] Spawned encounter enemies: {encounterId}, count={spawnedEnemyCount}");

        return true;
    }
}
