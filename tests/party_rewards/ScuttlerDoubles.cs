using System;using System.Collections;using UnityEngine;
namespace UnityEngine.SceneManagement {public struct Scene {public int handle;}public static class SceneManager {public static int Handle=1;public static Scene GetActiveScene()=>new(){handle=Handle};}}
public class ScuttlerControl:MonoBehaviour {
	public bool startIdle;

	public bool startRunning;

	public string killedPDBool = "killedOrangeScuttler";

	public string killsPDBool = "killsOrangeScuttler";

	public string newDataPDBool = "newDataOrangeScuttler";

	public string runAnim = "Run";

	public string landAnim = "Land";

	public GameObject corpsePrefab;

	public GameObject splatEffectChild;

	public GameObject journalUpdateMsgPrefab;

	public AudioSource audioSourcePrefab;

	public AudioEvent bounceSound;

	public TriggerEnterEvent heroAlert;

	public bool healthScuttler;

	public GameObject strikeNailPrefab;

	public GameObject slashImpactPrefab;

	public GameObject fireballHitPrefab;

	public AudioEvent deathSound1;

	public AudioEvent deathSound2;

	public GameObject pool;

	public GameObject screenFlash;

	public Color bloodColor;

	private Transform hero;

	private float maxSpeed;

	private float acceleration = 0.3f;

	private bool landed;

	private Coroutine runRoutine;

	private Coroutine bounceRoutine;

	private float rayLength;

	private Vector2 rayOrigin;

	private tk2dSpriteAnimator anim;

	private Rigidbody2D body;

	private AudioSource source;

	private bool alive = true;

	private bool reverseRun;

	private float activateDelay = 0.25f;

	private float activateTime;

public void Hit(HitInstance hit)=>On.ScuttlerControl.Run_Hit(this,hit);public void Native_Hit(HitInstance hit)=>throw new NotImplementedException();
public IEnumerator Heal()=>On.ScuttlerControl.Run_Heal(this);public IEnumerator Native_Heal()=>throw new NotImplementedException();
public void Rearm(float at=0){alive=true;activateTime=at;gameObject.SetActive(true);}public bool Alive=>alive;
}
public class tk2dSpriteAnimator:MonoBehaviour {}
public class TriggerEnterEvent:MonoBehaviour {}
public enum AttackTypes {Nail,Generic,Spell,Acid,Splatter,RuinsWater,SharpShadow,NailBeam}
public struct HitInstance {public AttackTypes AttackType;public float Direction;public GameObject Source;}
public struct AudioEvent {public void SpawnAndPlayOneShot(AudioSource prefab,Vector3 position){}}
public class GameCameras:UnityEngine.Object {public PlayMakerFSM cameraShakeFSM;}
public class GlobalPrefabDefaults {public static GlobalPrefabDefaults Instance=new();public void SpawnBlood(Vector3 at,short min,short max,float a,float b,float c,float d,Color? color){}}
public static class FlingUtils {public static int ChildrenCalls,LastMin,LastMax;public struct ChildrenConfig {public GameObject Parent;public int AmountMin,AmountMax;public float SpeedMin,SpeedMax,AngleMin,AngleMax,OriginVariationX,OriginVariationY;}public static void FlingChildren(ChildrenConfig c,Transform at,Vector3 offset){ChildrenCalls++;LastMin=c.AmountMin;LastMax=c.AmountMax;}}
public static class EventRegister {public static int BlueEvents;public static void SendEvent(string name){if(name=="ADD BLUE HEALTH"){BlueEvents++;KO.HollowKnight8.Plugin.Self.Session.Data.healthBlue++;}}}
