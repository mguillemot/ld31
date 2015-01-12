using System;
using UnityEngine;

public class Spawner : MonoBehaviour
{

    [Serializable]
    public class SpawnChoice
    {
        public GameObject MonsterPrefab;
        public float Weight;
    }

    public Area Area;
    public SpawnChoice[] Choices;
    public float SpawnDelayMin;
    public float SpawnDelayMax;

    private bool hasSpawn;
    private Monster spawn;
    private float nextSpawnAt;

    void Start()
    {
        Spawn();
    }

    void Update()
    {
        if (hasSpawn && !spawn)
        {
            hasSpawn = false;
            spawn = null;
            nextSpawnAt = Time.time + UnityEngine.Random.Range(SpawnDelayMin, SpawnDelayMax);
        }

        //Debug.Log("Spawn?" + hasSpawn + " nextAt=" + nextSpawnAt + " now=" + Time.time);

        if (!hasSpawn && nextSpawnAt < Time.time)
        {
            Spawn();
        }
    }

    public void Spawn()
    {
        var spawnTable = new ProbabilityTable<GameObject>();
        foreach (var choice in Choices)
        {
            spawnTable.Add(choice.MonsterPrefab, choice.Weight);
        }
        var prefab = spawnTable.ChooseOne();
        var spawnObject = (GameObject) Instantiate(prefab);
        spawnObject.transform.parent = Field.instance.transform;
        if (Area != Area.NONE)
        {
            var pos = Field.instance.posPerArea[Area].RandomElement();
            spawnObject.transform.localPosition = new Vector3(pos.GetX() * GameConstants.CellSize, pos.GetY() * GameConstants.CellSize);
        }
        else
        {
            spawnObject.transform.localPosition = transform.localPosition;
        }
        spawn = spawnObject.GetComponent<Monster>();
        hasSpawn = true;
        Field.instance.monsters.Add(spawn);
    }

}
