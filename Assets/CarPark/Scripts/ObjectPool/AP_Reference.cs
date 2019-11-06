using UnityEngine;
using System.Collections;

[HelpURL("http://mobfarmgames.weebly.com/ap_reference.html")]
public class AP_Reference : MonoBehaviour {

	[Tooltip("调用Despawn（）时，将禁用该对象，但是将等待延迟时间，然后才能在对象池中使用该对象。")]
	public float delay;

	[HideInInspector] public AP_Pool poolScript; // 存储该对象的对象池脚本的位置
    [HideInInspector] public float timeSpawned;

	public bool Despawn ( float del ) { // -1将使用此脚本中指定的延迟
		if ( del >= 0 ){ // 超越延迟
            if ( poolScript ) {
				Invoke( "DoDespawn", del );
				gameObject.SetActive(false);
				return true;
			} else {
				return false;
			}
		} else {
			return DoDespawn();
		}
	}

	bool DoDespawn() {
		if ( poolScript ) {
			poolScript.Despawn( gameObject, this );
			return true;
		} else {
			return false;
		}
	}

}
