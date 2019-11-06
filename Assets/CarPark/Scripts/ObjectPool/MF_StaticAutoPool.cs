using UnityEngine;
using System.Collections;

namespace AP_enum {
	public enum EmptyBehavior { Grow, Fail, ReuseOldest }
	public enum MaxEmptyBehavior { Fail, ReuseOldest }
}

[HelpURL("http://mobfarmgames.weebly.com/mf_staticautopool.html")]
public class MF_AutoPool {

	static AP_Manager opmScript;

    // InitializeSpawn
    // 初始化包含预制件的对象池，并可以向该游泳池的大小添加项目。
    // 此方法是可选的，用于确保在调用此方法时创建所有池引用链接。在大量使用池的情况下，这可以通过在场景的早期创建所有参考来减少延迟尖峰。

    // 可能会提前调用，并且不会创建派生工具，但是会创建池引用，并且如果引用已创建或已经存在，则返回true。
    // 如果要在特定池的第一个生成之前链接池引用，则使用。 （除了最苛刻的场景外，可能没有必要。）
    // 另外，可用于在运行时动态创建池。
    public static bool InitializeSpawn ( GameObject prefab ) { 
		return InitializeSpawn ( prefab, 0f, 0 );
	}
    // 分配的参数可用于在运行时创建池
    // 如果addPool为<1，它将用于将现有的池增加一个百分比。 否则它将舍入到最接近的整数并增加该数量
    // minPool是必须在该池中的min对象。 如果当前池+ addPool <minPool，将使用minPool
    public static bool InitializeSpawn ( GameObject prefab, float addPool, int minPool ) { 
		return InitializeSpawn( prefab, addPool, minPool, AP_enum.EmptyBehavior.Grow, AP_enum.MaxEmptyBehavior.Fail, false ); 
	}
	public static bool InitializeSpawn ( GameObject prefab, float addPool, int minPool, AP_enum.EmptyBehavior emptyBehavior, AP_enum.MaxEmptyBehavior maxEmptyBehavior ) { 
		return InitializeSpawn( prefab, addPool, minPool, emptyBehavior, maxEmptyBehavior, true ); 
	}
	static bool InitializeSpawn ( GameObject prefab, float addPool, int minPool, AP_enum.EmptyBehavior emptyBehavior, AP_enum.MaxEmptyBehavior maxEmptyBehavior, bool modBehavior ) { 
		if ( prefab == null ) { return false; } // 未定义对象

        if ( opmScript == null ) { // 尚未找到对象池管理器脚本
            opmScript = Object.FindObjectOfType<AP_Manager>(); // 在场景中找到它
			if ( opmScript == null ) { Debug.Log( "No Object Pool Manager found in scene." ); return false; } // 找不到对象池管理器
        }
        // 找到一个对象池管理器
        return opmScript.InitializeSpawn( prefab, addPool, minPool, emptyBehavior, maxEmptyBehavior, modBehavior ); 
	}

    // 创建obj预制件。返回生成对象
    // 从包含prefab的池中生成一个对象。如果未使用pos和rot，则位置和旋转将是池对象的位置和旋转。可选地，可以为包含子项的预制件指定子项索引。该孩子将被启用，而其他孩子将保持禁用状态。此功能可用于模拟不同类型的对象。
    public static GameObject Spawn ( GameObject prefab ) { // 在对象池的位置和旋转位置生成
        return Spawn ( prefab, null, Vector3.zero, Quaternion.identity, false );
	}
	public static GameObject Spawn ( GameObject prefab, int? child ) { // child allows a single object to hold multiple versions of objects, and only activate a specific child. null = don't use children
		return Spawn ( prefab, child, Vector3.zero, Quaternion.identity, false );
	}
	public static GameObject Spawn ( GameObject prefab, Vector3 pos, Quaternion rot ) { // specify a specific position and rotation
		return Spawn ( prefab, null, pos, rot, true );
	}
	public static GameObject Spawn ( GameObject prefab, int? child, Vector3 pos, Quaternion rot ) {
		return Spawn ( prefab, child, pos, rot, true );
	}
	static GameObject Spawn ( GameObject prefab, int? child, Vector3 pos, Quaternion rot, bool usePosRot ) {
		FindOPM();
		if ( opmScript == null ) { // didn't find an object pool manager
			return null;
		} else { // found an object pool manager
			return opmScript.Spawn( prefab, child, pos, rot, usePosRot );
		} 
	}

	public static bool Despawn ( GameObject obj ) {
		if ( obj == null ) { return false; }
		return Despawn( obj.GetComponent<AP_Reference>(), -1f );
	}
	public static bool Despawn ( GameObject obj, float time ) {
		if ( obj == null ) { return false; }
		return Despawn( obj.GetComponent<AP_Reference>(), time );
	}
	public static bool Despawn ( AP_Reference script ) {
		return Despawn( script, -1f );
	} 
	public static bool Despawn ( AP_Reference script, float time ) {
		if ( script == null ) { return false; }
		return script.Despawn( time );
	}

	public static int GetActiveCount ( GameObject obj ) {
		FindOPM();
		if ( opmScript == null ) { // didn't find an object pool manager
			return 0;
		} else { 
			return opmScript.GetActiveCount( obj );
		}
	}

	public static int GetAvailableCount ( GameObject obj ) {
		FindOPM();
		if ( opmScript == null ) { // didn't find an object pool manager
			return 0;
		} else { 
			return opmScript.GetAvailableCount( obj );
		}
	}

	public static bool DespawnPool ( GameObject obj ) {
		FindOPM();
		if ( opmScript == null ) { // didn't find an object pool manager
			return false;
		} else { 
			return opmScript.DespawnPool( obj );
		}
	}

	public static bool DespawnAll () {
		FindOPM();
		if ( opmScript == null ) { // didn't find an object pool manager
			return false;
		} else { 
			return opmScript.DespawnAll();
		}
	}

	public static bool RemovePool ( GameObject obj ) {
		bool result = false;
		FindOPM();
		if ( opmScript == null ) { // didn't find an object pool manager
			return false;
		} else { 
			result = opmScript.RemovePool( obj );
			if ( result == true ) { opmScript.poolRef.Remove( obj ); }
			return result;
		}
	}

	public static bool RemoveAll () {
		FindOPM();
		if ( opmScript == null ) { // didn't find an object pool manager
			return false;
		} else { 
			return opmScript.RemoveAll();
		}
	}

	static void FindOPM () {
		if ( opmScript == null ) { // object pool manager script not located yet
			opmScript = Object.FindObjectOfType<AP_Manager>(); // find it in the scene
		}
	}

}
