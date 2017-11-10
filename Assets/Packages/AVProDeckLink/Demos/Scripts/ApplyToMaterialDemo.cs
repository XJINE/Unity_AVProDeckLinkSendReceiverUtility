using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//-----------------------------------------------------------------------------
// Copyright 2014-2016 RenderHeads Ltd.  All rights reserved.
//-----------------------------------------------------------------------------

namespace RenderHeads.Media.AVProDeckLink.Demos
{
    public class ApplyToMaterialDemo : MonoBehaviour
    {
        public List<GameObject> prefabs;
        public GameObject scene;

        [Range(20, 100)]
        public int SpawnLimit = 20;

        [Range(1, 10)]
        public float SpawnsPerSecond = 1;

        private List<GameObject> spawnedObjects;

        private float _timeSinceLastSpawn = 0f;

        private float angle = 15f;

        // Use this for initialization
        void Start()
        {
            spawnedObjects = new List<GameObject>();
        }

        // Update is called once per frame
        void Update()
        {
            float spawnRate = 1f / SpawnsPerSecond;
            _timeSinceLastSpawn += Time.deltaTime;

            if (prefabs.Count < 1)
            {
                return;
            }

            if (_timeSinceLastSpawn >= spawnRate)
            {
                _timeSinceLastSpawn -= spawnRate;
                int spawnIdx = Mathf.Min((int)(Random.value * (float)prefabs.Count), prefabs.Count - 1);

                GameObject spawned = Instantiate(prefabs[spawnIdx]);

                int spawnHeightOffset = Random.value > 0.5f ? 1 : -1;

                float yRotation = Random.value * 2 * angle - angle;

                Quaternion rot = Quaternion.AngleAxis(yRotation, Vector3.up); 

                spawned.transform.position = new Vector3(Camera.main.transform.position.x, Camera.main.transform.position.y + spawnHeightOffset, Camera.main.transform.position.z);
                spawned.GetComponent<Rigidbody>().velocity = rot * Camera.main.transform.forward * 20;

                if(scene != null)
                {
                    spawned.transform.parent = scene.transform;
                }

                spawnedObjects.Add(spawned);

                if (spawnedObjects.Count > SpawnLimit)
                {
                    GameObject removed = spawnedObjects[0];
                    spawnedObjects.Remove(removed);
                    Destroy(removed);
                }
            }
        }
    }
}