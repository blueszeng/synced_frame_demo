﻿using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using KBEngine;
using System;

public class RoomListPanel : PanelBase
{
    private Text idText;
    private Text winText;
    private Text lostText;
    private Transform content;
    private GameObject roomPrefab;
    private Button closeBtn;
    private Button newBtn;
    private Button reflashBtn;

    private SPACE_LIST room_list = new SPACE_LIST();
    #region 生命周期
    /// <summary> 初始化 </summary>
    public override void Init(params object[] args)
    {
        base.Init(args);
        skinPath = "RoomListPanel";
        layer = PanelLayer.Panel;
    }

    public void Start()
    {
        KBEngine.Event.registerOut("onReqSpaceList", this, "onReqSpaceList");
        KBEngine.Event.registerOut("onEnterSpaceResult", this, "onEnterSpaceResult");
        KBEngine.Event.registerOut("onCreateSpaceResult", this, "onCreateSpaceResult");
        KBEngine.Event.registerOut("AvatarReady", this, "AvatarReady");
    }

    void OnDestroy()
    {
        KBEngine.Event.deregisterOut(this);
    }

    public override void OnShowing()
    {
        base.OnShowing();
        //获取Transform
        Transform skinTrans = skin.transform;
        Transform listTrans = skinTrans.Find("ListImage");
        Transform winTrans = skinTrans.Find("WinImage");
        //获取成绩栏部件
        idText = winTrans.Find("IDText").GetComponent<Text>();
        winText = winTrans.Find("WinText").GetComponent<Text>();
        lostText = winTrans.Find("LostText").GetComponent<Text>();
        //获取列表栏部件
        Transform scroolRect = listTrans.Find("ScrollRect");
        content = scroolRect.Find("Content");
        roomPrefab = content.Find("RoomPrefab").gameObject;
        roomPrefab.SetActive(false);

        closeBtn = listTrans.Find("CloseBtn").GetComponent<Button>();
        newBtn = listTrans.Find("NewBtn").GetComponent<Button>();
        reflashBtn = listTrans.Find("ReflashBtn").GetComponent<Button>();
        //按钮事件
        reflashBtn.onClick.AddListener(OnReflashClick);
        newBtn.onClick.AddListener(OnNewClick);
        closeBtn.onClick.AddListener(OnCloseClick);

        //发送查询
        KBEngine.Event.fireIn("reqSpaceList", new object[] { });

    }

    public override void OnClosing()
    {
        KBEngine.Event.deregisterOut(this);
    }

    #endregion

    public void AvatarReady(KBEngine.Avatar avatar)
    {
        Debug.Log("AvatarReady.id:" + avatar.id);
        GameData.Instance.localPlayer = avatar;
        RecvGetAchieve();

        //发送查询
        KBEngine.Event.fireIn("reqSpaceList", new object[] { });
    }

    //收到GetAchieve协议
    public void RecvGetAchieve()
    {
        //处理
        string name = GameData.Instance.localPlayer.name;
        var win = GameData.Instance.localPlayer.component1.winScore;
        var lost = GameData.Instance.localPlayer.component1.lossScore;

        idText.text = "用户名:"+GameData.Instance.AccountName+"\nID:" + name +GameData.Instance.localPlayer.id;
        winText.text = win.ToString();
        lostText.text = lost.ToString();
    }


    //收到GetRoomList协议
    public void onReqSpaceList(SPACE_LIST roomList)
    {
        //清理
        ClearRoomUnit();
        room_list = roomList;

        for (int i = 0; i < roomList.values.Count; i++)
        {
            SPACE_INFO room = roomList.values[i];
            Debug.Log("id:" + i + ",space_key:" + room.space_key + ",space_state:" + room.space_state + ",space_creater:" + room.space_creater);
            GenerateRoomUnit(i, room.player_count, room.space_state);
        }
    }

    public void ClearRoomUnit()
    {
        room_list.values.Clear();

        for (int i = 0; i < content.childCount; i++)
            if (content.GetChild(i).name.Contains("Clone"))
                Destroy(content.GetChild(i).gameObject);
    }


    //创建一个房间单元
    //参数 i，房间序号（从0开始）
    //参数num，房间里的玩家数
    //参数status，房间状态，1-准备中 2-战斗中
    public void GenerateRoomUnit(int i, int num, int status)
    {
        //添加房间
        content.GetComponent<RectTransform>().sizeDelta = new Vector2(0, (i + 1) * 110);
        GameObject o = Instantiate(roomPrefab);
        o.transform.SetParent(content);
        o.SetActive(true);
        //房间信息
        Transform trans = o.transform;
        Text nameText = trans.Find("nameText").GetComponent<Text>();
        Text countText = trans.Find("CountText").GetComponent<Text>();
        Text statusText = trans.Find("StatusText").GetComponent<Text>();
        nameText.text = "序号：" + (i + 1).ToString();
        countText.text = "人数：" + num.ToString();
        if (status == 0)
        {
            statusText.color = Color.green;
            statusText.text = "状态：空闲中";
        }
        else if(status == 1)
        {
            statusText.color = Color.red;
            statusText.text = "状态：开战中";
        }
        else
        {
            statusText.color = Color.black;
            statusText.text = "状态：等待中";
        }
        //按钮事件
        Button btn = trans.Find("JoinButton").GetComponent<Button>();
        btn.name = i.ToString();   //改变按钮的名字，以便给OnJoinBtnClick传参
        btn.onClick.AddListener(delegate()
        {
            OnJoinBtnClick(btn.name);
        }
        );
    }


    //刷新按钮
    public void OnReflashClick()
    {
        KBEngine.Event.fireIn("reqSpaceList", new object[] { });
    }

    //加入按钮
    public void OnJoinBtnClick(string name)
    {
        UInt64 roomKey = room_list.values[int.Parse(name)].space_key;
        KBEngine.Event.fireIn("reqLoginSpace", roomKey);

        Debug.Log("请求进入房间 " + name);

    }

    //加入按钮返回
    
    public SPACE_INFO findSpace(UInt64 spaceKey)
    {
        for(int i = 0; i < room_list.values.Count;i++)
        {
            if(room_list.values[i].space_key == spaceKey )
            {
                return room_list.values[i];
            }
        }

        return null;
    }
    public void onEnterSpaceResult(UInt64 spaceKey)
    {
        //处理
        if (spaceKey > 0)
        {
            GameData.Instance.CurrentRoom = findSpace(spaceKey);
            Debug.Log("spaceKey:" + spaceKey + ",CurrentRoom.space_creater:" + GameData.Instance.CurrentRoom.space_creater);

            PanelMgr.instance.OpenPanel<TipPanel>("", "成功进入房间!");
            PanelMgr.instance.OpenPanel<RoomPanel>("");
            Close();
        }
        else
        {
            GameData.Instance.CurrentRoom = null;
            PanelMgr.instance.OpenPanel<TipPanel>("", "进入房间失败");
        }
    }

    //新建按钮
    public void OnNewClick()
    {
        Debug.Log("RoomListPannel:OnNewClick");
        KBEngine.Event.fireIn("reqCreateSpace", new object[] { });
    }

    //新建按钮返回
    public void onCreateSpaceResult(UInt64 spaceKey)
    {
        //处理
        if (spaceKey > 0)
        {
            PanelMgr.instance.OpenPanel<TipPanel>("", "创建成功!");
            //PanelMgr.instance.OpenPanel<RoomPanel>("");
            //Close();
        }
        else
        {
            PanelMgr.instance.OpenPanel<TipPanel>("", "创建房间失败！");
        }
        KBEngine.Event.fireIn("reqSpaceList", new object[] { });
    }

    //退出按钮
    public void OnCloseClick()
    {
        //
    }

    //退出登录
    public void onLoginOut(byte result)
    {
        PanelMgr.instance.OpenPanel<TipPanel>("", "退出成功！");
        PanelMgr.instance.OpenPanel<LoginPanel>("", "");
    }

}