using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[HelpURL("http://mobfarmgames.weebly.com/ap_pool.html")]
public class AP_Pool : MonoBehaviour { 

	public PoolBlock poolBlock;

	[HideInInspector] public Stack<PoolItem> pool;
	[HideInInspector] public List<PoolItem> masterPool; // only used when using EmptyBehavior.ReuseOldest

	int addedObjects;
	int failedSpawns;
	int reusedObjects;
	int peakObjects;
	int origSize;
	int initSize;
	int dynamicSize;
	bool loaded;

	[System.Serializable]
	public class PoolBlock {
		[Tooltip("对象池的起始大小。")]
		public int size = 32;
		[Tooltip("请求对象且池为空时的行为。\n\n" +
            "Grow - 如果池小于 maxSize，将实例化池中的新对象。如果池必须增加许多项，这将是最慢的，但是它是最灵活的。 \nlimited by Max Size.\n\n" +
            "Fail - 目前不会创建任何派生工具。性能最佳，但灵活性最低。\n\n" +
            "Reuse Oldest - 将找到最旧的活动对象，并将其重新用作新的生成对象。对于某些应用程序很有用，但是对于大型池而言可能会非常慢，因为它必须检查每一项以找到最旧的项。如果池仅偶尔为空，请考虑使用“增长”，或者从更大的池开始。")] // 对于大池，速度比增长慢，但是对于小池，速度更快
        public AP_enum.EmptyBehavior emptyBehavior;
		[Tooltip("intEmptyBehavior.Grow可以扩大池的最大大小。未选择EmptyBehavior.Grow时，此字段是隐藏的。")]
		public int maxSize = 64; // 池大小的绝对限制，用于空行为增长模式
        [Tooltip("当请求对象且池为空且池的最大大小已达到时的行为。\n\n" +
            "Fail - 没有对象将被衍生。\n\n" +
            "Reuse Oldest - 将重用最老的活动对象。")]
		public AP_enum.MaxEmptyBehavior maxEmptyBehavior; // mode when pool is at the max size
		[Tooltip("该池包含的对象。")]
		public GameObject prefab;
		[Tooltip("When the scene is stopped, creates a report showing pool usage:\n\n" +
			"Start Size - Size of the pool when the scene started.\n\n" +
			"End Size - Size of the pool when the scene ended.\n\n" +
			"Added Objects - Number of objects added to the pool beyond the Start Size.\n\n" +
			"Failed Spawns - Number of spawns failed due to no objects available in the pool.\n\n" +
			"Reused Objects - Number of objects reused before they were added back to the pool.\n\n" +
			"Most Objects Active - The most pool objects ever active at the same time.")]
		public bool printLogOnQuit;

		public PoolBlock ( int size, AP_enum.EmptyBehavior emptyBehavior, int maxSize, AP_enum.MaxEmptyBehavior maxEmptyBehavior, GameObject prefab, bool printLogOnQuit ) {
			this.size = size;
			this.emptyBehavior = emptyBehavior;
			this.maxSize = maxSize;
			this.maxEmptyBehavior = maxEmptyBehavior;
			this.prefab = prefab;
			this.printLogOnQuit = printLogOnQuit;
		}
	}

	[System.Serializable]
	public class PoolItem {
		public GameObject obj;
		public AP_Reference refScript;

		public PoolItem ( GameObject obj, AP_Reference refScript ) {
			this.obj = obj;
			this.refScript = refScript;
		}
	}

	void OnValidate () {
		if ( loaded == false ) { // only run during editor
			if ( poolBlock.maxSize <= poolBlock.size ) { poolBlock.maxSize = poolBlock.size * 2; } 
		}
	}

	void Awake () {
		loaded = true;

		// required to allow creation or modification of pools at runtime. (Timing of script creation and initialization can get wonkey)
		if ( poolBlock == null ) {
			poolBlock = new PoolBlock( 0, AP_enum.EmptyBehavior.Grow, 0, AP_enum.MaxEmptyBehavior.Fail, null, false );
		} else {
			poolBlock = new PoolBlock( poolBlock.size, poolBlock.emptyBehavior, poolBlock.maxSize, poolBlock.maxEmptyBehavior, poolBlock.prefab, poolBlock.printLogOnQuit );
		}
		pool = new Stack<PoolItem>();
		masterPool = new List<PoolItem>();

		origSize = Mathf.Max( 0, poolBlock.size); 
		poolBlock.size = 0;

		for ( int i=0; i < origSize; i++ ) {
			CreateObject( true );
		}
	}

	void Start () {
		Invoke( "StatInit", 0 ); // for logging after dynamic creation of pool objects from other scripts
	}

	void StatInit () { // for logging after dynamic creation of pool objects from other scripts
		initSize = poolBlock.size - origSize;
	}
		 
	public GameObject Spawn () { // use to call spawn directly from the pool, and also used by the "Spawn" button in the editor
		return Spawn( null, Vector3.zero, Quaternion.identity, false );
	}
	public GameObject Spawn ( int? child ) { // use to call spawn directly from the pool
		return Spawn( child, Vector3.zero, Quaternion.identity, false );
	}
	public GameObject Spawn ( Vector3 pos, Quaternion rot ) { // use to call spawn directly from the pool
		return Spawn( null, pos, rot, true );
	}
	public GameObject Spawn ( int? child, Vector3 pos, Quaternion rot ) { // use to call spawn directly from the pool
		return Spawn( child, pos, rot, true );
	}
	public GameObject Spawn ( int? child, Vector3 pos, Quaternion rot, bool usePosRot ) {
		GameObject obj = GetObject();
		if ( obj == null ) { return null; } // early out

		obj.SetActive(false); // reset item in case object is being reused, has no effect if object is already disabled
		obj.transform.parent = null;
		obj.transform.position = usePosRot ? pos : transform.position;
		obj.transform.rotation = usePosRot ? rot : transform.rotation;
	
		obj.SetActive(true);

		if ( child != null && child < obj.transform.childCount ) { // activate a specific child
			obj.transform.GetChild( (int)child ).gameObject.SetActive(true); 
		}

		if ( peakObjects < poolBlock.size - pool.Count ) { peakObjects = poolBlock.size - pool.Count; } // for logging
		return obj;
	}

	public void Despawn ( GameObject obj, AP_Reference oprScript ) { // return an object back to this pool
		if ( obj.transform.parent == transform ) { return; } // already in pool
		obj.SetActive(false);
		obj.transform.parent = transform;
		obj.transform.localPosition = Vector3.zero;
		obj.transform.localRotation = Quaternion.identity;
		oprScript.CancelInvoke();
		pool.Push( new PoolItem( obj, oprScript ) );
	}

	public GameObject GetObject () { // get object from pool, creating one if necessary and if settings allow
		GameObject result = null;
		if ( pool.Count == 0 ) {
			if ( poolBlock.emptyBehavior == AP_enum.EmptyBehavior.Fail ) { failedSpawns++; return null; }

			if ( poolBlock.emptyBehavior == AP_enum.EmptyBehavior.ReuseOldest ) {
				result = FindOldest();
				if ( result != null ) { reusedObjects++; }
			}

			if ( poolBlock.emptyBehavior == AP_enum.EmptyBehavior.Grow ) {
				if ( poolBlock.size >= poolBlock.maxSize ) {
					if ( poolBlock.maxEmptyBehavior == AP_enum.MaxEmptyBehavior.Fail ) { failedSpawns++; return null; }
					if ( poolBlock.maxEmptyBehavior == AP_enum.MaxEmptyBehavior.ReuseOldest ) {
						result = FindOldest();
						if ( result != null ) { reusedObjects++; }
					}
				} else {
					addedObjects++;
					return CreateObject();
				}
			}
		} else {
			pool.Peek().refScript.timeSpawned = Time.time;
			return pool.Pop().obj;
		}
		return result;
	}

	GameObject FindOldest () { // will also set timeSpawned for returned object
		GameObject result = null;
		int oldestIndex = 0;
		float oldestTime = Mathf.Infinity;
		if ( masterPool.Count > 0 ) {
			for ( int i = 0; i < masterPool.Count; i++ ) {
				if ( masterPool[i] == null || masterPool[i].obj == null ) { continue; } // make sure object still exsists
				if ( masterPool[i].refScript.timeSpawned < oldestTime ) { 
					oldestTime = masterPool[i].refScript.timeSpawned;
					result = masterPool[i].obj;
					oldestIndex = i;
				}
			}
			masterPool[ oldestIndex ].refScript.timeSpawned = Time.time;
		}
		return result;
	}

	public GameObject CreateObject () {
		return CreateObject ( false );
	}
	public GameObject CreateObject ( bool createInPool ) { // true when creating an item in the pool without spawing it
		GameObject obj = null;
		if ( poolBlock.prefab ) {
			obj = (GameObject) Instantiate( poolBlock.prefab, transform.position, transform.rotation );
			AP_Reference oprScript = obj.GetComponent<AP_Reference>();
			if ( oprScript == null ) { oprScript = obj.AddComponent<AP_Reference>(); }
			oprScript.poolScript = this;
			oprScript.timeSpawned = Time.time;
			masterPool.Add( new PoolItem( obj, oprScript ) );

			if ( createInPool == true ) {
				pool.Push( new PoolItem( obj, oprScript ) );
				obj.SetActive(false);
				obj.transform.parent = transform;
			}
			poolBlock.size++;
		}
		return obj;
	}

	public int GetActiveCount () {
		return poolBlock.size - pool.Count;
	}

	public int GetAvailableCount () {
		return pool.Count;
	}

	void OnApplicationQuit () { 
		if ( poolBlock.printLogOnQuit == true ) {
			PrintLog();
		}
	}

	public void PrintLog () {
		Debug.Log( transform.name + ":       初始大小: " + origSize + "    初始化已添加: " + initSize + "    增加数: " + addedObjects + "    结束大小: " + poolBlock.size + "\n" +
			"    生成失败数: " + failedSpawns + "    重用对象数: " + reusedObjects + "     对象同时存活最大数: " + peakObjects );
	}

}
