﻿using KBEngine;
using SyncFrame;
using System;
using System.Collections;
using System.Collections.Generic;
using TrueSync;
using UnityEngine;

public class PlayerBehaviour : MonoBehaviour {

    public TSRigidBody tsRigidBody;

    public TSTransform tsTransform;

    public KBEngine.Avatar owner;

    public UInt32 tsFrameId = 0;

    public UInt16 KeyValue = 0;

    public int deaths = 0;

    public Dictionary<KeyCode, int> keyBit = new Dictionary<KeyCode, int>()
    {
        { KeyCode.W,0},
        { KeyCode.S,1},
        { KeyCode.A,2},
        { KeyCode.D,3},
        { KeyCode.E,4},
        { KeyCode.F,5},
        { KeyCode.Z,6},
        { KeyCode.Q,7},
        { KeyCode.R,8},
        { KeyCode.LeftShift,9},
        { KeyCode.Space,10},

        { KeyCode.Mouse0,11},
        { KeyCode.Mouse1,12}
    };

    //-------------------------------//
    public Actions actions;
    public PlayerController controller;

    public int RollIndex = 0;
    public List<string> weapon_list = new List<string>();

    public GameObject projectilePrefab;
    private FP cooldown = 0;
    //-------------------------------------
    private FP accellRate = 4;

    private FP steerRate = 200;
    //---------------------------------------

    private void OnGUI()
    {

        if (owner.isPlayer())
        {
            GUI.contentColor = Color.green;
            GUI.Label(new Rect(0, Screen.height - 20, 400, 100), "id: " + owner.id);
            GUI.Label(new Rect(0, Screen.height - 35, 400, 100), "frameCount: " + tsFrameId);
            GUI.Label(new Rect(0, Screen.height - 50, 400, 100), "position: " + tsRigidBody.position);
            GUI.Label(new Rect(0, Screen.height - 65, 400, 100), "velocity: " + tsRigidBody.velocity);
            GUI.Label(new Rect(0, Screen.height - 80, 400, 100), "angularVelocity: " + tsRigidBody.angularVelocity);
            GUI.Label(new Rect(0, Screen.height - 95, 400, 100), "KeyValue: " + KeyValue);
        }
        else
        {
            GUI.contentColor = Color.yellow;
            GUI.Label(new Rect(Screen.width - 200, Screen.height - 20, 400, 100), "id: " + owner.id);
            GUI.Label(new Rect(Screen.width - 200, Screen.height - 35, 400, 100), "frameCount: " + tsFrameId);
            GUI.Label(new Rect(Screen.width - 200, Screen.height - 50, 400, 100), "position: " + tsRigidBody.position);
            GUI.Label(new Rect(Screen.width - 200, Screen.height - 65, 400, 100), "velocity: " + tsRigidBody.velocity);
            GUI.Label(new Rect(Screen.width - 200, Screen.height - 80, 400, 100), "angularVelocity: " + tsRigidBody.angularVelocity);
            GUI.Label(new Rect(Screen.width - 200, Screen.height - 95, 400, 100), "KeyValue: " + KeyValue);
        }

    }

    public void OnSyncedStart()
    {
        tsRigidBody = GetComponent<TSRigidBody>();

        tsTransform = GetComponent<TSTransform>();

        actions = GetComponent<Actions>();

        controller = GetComponent<PlayerController>();

        foreach (PlayerController.Arsenal a in controller.arsenal)
        {
            weapon_list.Add(a.name);
        }
        if(projectilePrefab == null)
        {
            projectilePrefab = (GameObject)Resources.Load("Perfabs/bullet");
        }
        
        Debug.Log("PlayerContorl::start:" + tsRigidBody + ",tsTransform:" + tsTransform + ",projectilePrefab:"+ projectilePrefab);
    }

    public void OnSyncedInput()
    {
        if(!owner.isPlayer())
        {
            return;
        }
        KeyValue = 0;

        foreach(var item in keyBit)
        {
            if (Input.GetKey(item.Key)
//                 && (item.Key == KeyCode.W 
//                 || item.Key == KeyCode.LeftShift
//                 //|| item.Key == KeyCode.Mouse0
//                 || item.Key == KeyCode.Mouse1
                /*)*/)
            {
                KeyValue |= (UInt16)(1L << item.Value);
            }
//             else if (Input.GetKeyDown(item.Key))
//             {
//                 KeyValue |= (UInt16)(1L << item.Value);
//             }

        }

        FP accell = Input.GetAxis("Vertical");
        FP steer = Input.GetAxis("Horizontal");


        KBEngine.Event.fireIn("reqFrameChange", FrameProto.encode(new FrameFPS(CMD.FPS, KeyValue,accell,steer)));
    }

    void StateChange(string message)
    {
        actions.SendMessage(message, SendMessageOptions.DontRequireReceiver);
    }

    void ChangeWeapon()
    {
        RollIndex = (++RollIndex) % weapon_list.Count;
        string name = weapon_list[RollIndex];
        controller.SetArsenal(name);
    }

    public void Respawn()
    {       
        deaths++;
    }

    void FireBullet()
    {
        if(cooldown <= 0)
        {
            cooldown = 2;
            TSVector position = tsTransform.position + tsTransform.forward.normalized + new TSVector(1, 1, 0) ;
            FPS_Manager.SyncedInstantiate(projectilePrefab, position, TSQuaternion.identity);


            Projectile projectile = projectilePrefab.GetComponent<Projectile>();

            projectile.direction = tsTransform.forward;
            //projectile.owner = owner;


            //Debug.Log("FireBullet:projectile.direction:" + projectile.direction);
        }

       
    }
    public void UpdateAnimator(List<KeyCode> keys,bool walk)
    {
        if (keys.Contains(KeyCode.W) && keys.Contains(KeyCode.LeftShift))
        {
            StateChange("Run");
        }
        else if (keys.Contains(KeyCode.W)||walk)
        {
            StateChange("Walk");
        }
        else if (keys.Contains(KeyCode.Space))
        {
            StateChange("Jump");
        }
        else if (keys.Contains(KeyCode.Mouse0))
        {
            StateChange("Attack");
            FireBullet();

        }
        else if (keys.Contains(KeyCode.Mouse1))
        {
            StateChange("Aiming");
        }
        else if (keys.Contains(KeyCode.Z))
        {
            StateChange("Sitting");
        }
        else if (keys.Contains(KeyCode.Q))
        {
            StateChange("Death");
        }
        else if (keys.Contains(KeyCode.E))
        {
            StateChange("Damage");
        }
        else if (keys.Contains(KeyCode.F))
        {
            ChangeWeapon();
        }
        else //if (keys.Contains(KeyCode.R))
        {
            StateChange("Stay");
        }
    }

    public void UpdateMovement(FP accell, FP steer)
    {
        accell *= accellRate * FPS_Manager.instance.Config.lockedTimeStep;
        steer *= steerRate * FPS_Manager.instance.Config.lockedTimeStep;

        //tsTransform.Translate(0, 0, accell, Space.Self);
        tsTransform.Rotate(0, steer, 0);

        tsRigidBody.velocity += tsTransform.forward * accell*10;
        //tsRigidBody.AddForce(tsTransform.forward * accell);

        //tsRigidBody.AddForce(TSVector.forward * FPS_Manager.instance.Config.lockedTimeStep);
    }

    public void OnSyncedUpdate(UInt32 frameid, ENTITY_DATA operation)
    {
        tsFrameId = frameid;

        FrameFPS data = new FrameFPS();
        data.PareseFrom(operation);
        if(data.e.entityid != owner.id)
        {
            return;
        }

        List<KeyCode> keyValues = new List<KeyCode>();
        foreach (var item in keyBit)
        {         
            if ((data.keyValue &(UInt16)(1L << item.Value)) > 0 )
            {
                keyValues.Add(item.Key);
                Debug.Log("key:" + item.Key);
            }
        }
        bool walk = (data.accell != 0 || data.steer != 0);
        UpdateAnimator(keyValues,walk);
        UpdateMovement(data.accell, data.steer);
        cooldown -= FPS_Manager.instance.Config.lockedTimeStep;


    }

    /**
* @brief Tints box's material with gray color when it collides with the ball.
**/
    public void OnSyncedCollisionEnter(TSCollision other)
    {

        if (other.gameObject.name == "Box(Clone)")
        {
            Debug.Log("OnSyncedCollisionEnter:other:" + other.transform.name);
            other.gameObject.GetComponent<Renderer>().material.color = Color.gray;
        }
    }

    /**
    * @brief Increases box's local scale by 1% while collision with a ball remains active.
    **/
    public void OnSyncedCollisionStay(TSCollision other)
    {

        if (other.gameObject.name == "Box(Clone)")
        {
            Debug.Log("OnSyncedCollisionStay:other:" + other.transform.name);
            other.gameObject.transform.localScale *= 1.01f;
        }
    }

    /**
    * @brief Resets changes in box's properties when there is no more collision with the ball.
    **/
    public void OnSyncedCollisionExit(TSCollision other)
    {

        if (other.gameObject.name == "Box(Clone)")
        {
            Debug.Log("OnSyncedCollisionExit:other:" + other.transform.name);
            other.gameObject.transform.localScale = Vector3.one;
            other.gameObject.GetComponent<Renderer>().material.color = Color.blue;
        }
    }

}
