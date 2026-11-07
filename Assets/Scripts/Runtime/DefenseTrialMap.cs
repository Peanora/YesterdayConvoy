using UnityEngine;
namespace YesterdayConvoy
{
    public sealed class DefenseTrialMap : MonoBehaviour
    {
        InitialDemo scene; GameObject original; bool originalActive,redActive,blueActive,restored,owned;
        AuthoredBattlefield battlefield; Camera borrowedCamera; Vector3 red,blue,cameraPosition;
        internal StrongholdMapEnvironment Environment=>battlefield==null?null:battlefield.GetComponent<StrongholdMapEnvironment>();
        public void Initialize(InitialDemo bindings,DefenderTrialDefinition data)
        {
            scene=bindings;original=GameObject.Find("Industrial Map");
            red=scene.RedGate.position;blue=scene.BlueGate.position;
            redActive=scene.RedGate.gameObject.activeSelf;blueActive=scene.BlueGate.gameObject.activeSelf;
            borrowedCamera=Camera.main;cameraPosition=borrowedCamera.transform.position;
            if(data.Tactical&&(StrongholdChallengeLayout.IsStronghold(data.mapId)||data.mapId==BlackForestLayout.Id))
            {
                battlefield=FindAnyObjectByType<AuthoredBattlefield>(FindObjectsInactive.Include);
                if(battlefield==null){
                    if(data.mapId==BlackForestLayout.Id)throw new System.InvalidOperationException("Open the independently authored BlackForestCrossing scene; terrain is never generated on Play.");
                    battlefield=Instantiate(Resources.Load<GameObject>("Maps/BattleShopEnvironment")).GetComponent<AuthoredBattlefield>();owned=true;
                }
                battlefield.gameObject.SetActive(true);
                if(original!=null){originalActive=original.activeSelf;original.SetActive(false);}
                scene.RedGate.gameObject.SetActive(false);scene.BlueGate.gameObject.SetActive(false);
                borrowedCamera.transform.position=borrowedCamera.transform.rotation*new Vector3(0,-1.7f,-35)+new Vector3(0,0,.7f);
            }
            else
            {
                scene.RedGate.position=data.route[0];scene.BlueGate.position=data.route[data.route.Length-1];
                borrowedCamera.transform.position-=borrowedCamera.transform.forward*2.8f;
            }
        }
        public void Restore()
        {
            if(restored)return;restored=true;
            if(scene!=null){if(scene.RedGate!=null){scene.RedGate.position=red;scene.RedGate.gameObject.SetActive(redActive);}if(scene.BlueGate!=null){scene.BlueGate.position=blue;scene.BlueGate.gameObject.SetActive(blueActive);}}
            if(original!=null&&battlefield!=null)original.SetActive(originalActive);
            if(borrowedCamera!=null)borrowedCamera.transform.position=cameraPosition;
            if(battlefield!=null){if(owned)Destroy(battlefield.gameObject);else battlefield.gameObject.SetActive(false);}
            gameObject.SetActive(false);
        }
        void OnDestroy()=>Restore();
    }
}
