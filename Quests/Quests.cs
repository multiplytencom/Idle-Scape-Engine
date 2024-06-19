using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Quests : MainRefs
{
    public Vector2 scrollPos;
    public string questSystemID = Guid.NewGuid().ToString();
    [HideInInspector] public List<AbstractQuestBlock> questModulesList;
    public List<QuestData> questDatasList = new List<QuestData>();
    [HideInInspector] public IGeneralFunctionalWindow questWindow;
    [HideInInspector] public int insertBlockIndex;
    public string startTag;
    public List<QuestTemplate> questTemplatesList = new List<QuestTemplate>();
    public bool isQuestSystemActive = true;
    public bool optimize = true;


    private void Awake()
    {
        CheckAllDataBlocks();
    }

    protected override void Start()
    {
        base.Start();
        var debugTools = FindObjectOfType<DebugTools>();
        if (debugTools && debugTools.isDebugActive && debugTools.turnOffAllQuests) isQuestSystemActive = false;
        if (!isQuestSystemActive) return;
        GetRef<QuestsManager>().AddQuestSystemToPair(this);
        GetRef<AbstractSavingManager>().GetSavingData<QuestsSavingData>().AddQuestDatas(questDatasList, this);
        if (!questDatasList.Any()) return;

        if (startTag == "")
        {
            foreach (var item in questDatasList)
            {
                if (GetRef<AbstractSavingManager>().GetSavingData<QuestsSavingData>().questPair[item.blockPart.GetFullTag(this)] == QuestProgressType.inProgress)
                {
                    item.questBlock.OnQuestStartProcedure(item.blockPart);
                    GetRef<QuestsManager>().QuestLaunched(item);
                }
            }

            if (GetRef<AbstractSavingManager>().GetSavingData<QuestsSavingData>().questPair[questDatasList[0].blockPart.GetFullTag(this)] == QuestProgressType.none)
                StartQuest(questDatasList[0].blockPart.tag);
        }
        else StartQuest(startTag);
    }

    private void OnDestroy()
    {
        var questManager = GetRef<QuestsManager>();
        if (questManager) questManager.RemoveQuestSystemFromPair(this);
    }

    private void Update()
    {
        if (!isQuestSystemActive) return;

        foreach (var item in questDatasList)
        {
            if (GetRef<AbstractSavingManager>().GetSavingData<QuestsSavingData>().questPair[item.blockPart.GetFullTag(this)] == QuestProgressType.inProgress)
            {
                if (item.questBlock.IsGoalReached(item.blockPart))
                {
                    GetRef<QuestsManager>().QuestCompleted(item);
                    GetRef<AbstractSavingManager>().GetSavingData<QuestsSavingData>().ChangeQuestState(item.blockPart.GetFullTag(this), QuestProgressType.completed);
                    if (!item.blockPart.canLaunchTags) continue;

                    foreach (var tag in item.blockPart.startQuestsWithTagsList)
                    {
                        StartQuest(tag);
                    }
                }
            }
        }
    }

    public QuestData AddNewDataToList(List<QuestData> list, int moduleIndex, int indexAfter = -2)
    {
        var data = questModulesList[moduleIndex];
        var dataBlock = data.AddNewItem(this);
        var questData = new QuestData { questBlock = data, blockPart = dataBlock, id = dataBlock.id };
        if (indexAfter == -2) list.Add(questData);
        else list.Insert(indexAfter + 1, questData);
        return questData;
    }

    public List<int> GetIdsList()
    {
        var list = questDatasList.Select(a => a.id).ToList();
        return list;
    }

    public void GetQuestModulsList()
    {
        if (questModulesList == null) questModulesList = new List<AbstractQuestBlock>();
        questModulesList.Clear();
        questModulesList = GetComponentsInChildren<AbstractQuestBlock>().ToList();
        foreach (var item in questModulesList)
            if (item.questSystemID == "") item.questSystemID = questSystemID;
    }

    public void CheckDataBlock(List<QuestData> list, int index)
    {
        if (index > list.Count - 1) return;
        if (list[index].blockPart == null)
            list[index].blockPart = list[index].questBlock.GetBlockPart(list[index].id);
    }

    public void CheckAllDataBlocks()
    {
        for (int i = 0; i < questDatasList.Count; i++)
        {
            CheckDataBlock(questDatasList, i);
            questDatasList[i].questBlock.questSystemID = questSystemID;
        }

        for (int i = 0; i < questTemplatesList.Count; i++)
        {
            var template = questTemplatesList[i];

            for (int i1 = 0; i1 < template.questDatasList.Count; i1++)
            {
                CheckDataBlock(template.questDatasList, i1);
                template.questDatasList[i1].questBlock.questSystemID = questSystemID;
            }
        }
    }

    public void RemoveQuestDataFromList(List<QuestData> list, QuestData data)
    {
        list.Remove(data);
        data.questBlock.RemoveItem(data.blockPart);
    }

    public void GetGUI(QuestData questData)
    {
        questData.questBlock.GetGUI(questData.blockPart, questWindow);
    }

    public void StartQuest(string tag)
    {
        if (tag == "") return;
        var index = GetIndexByTag(tag);
        var fullTag = questDatasList[index].blockPart.GetFullTag(this);
        if (GetRef<AbstractSavingManager>().GetSavingData<QuestsSavingData>().questPair[fullTag] != QuestProgressType.none) return;
        GetRef<AbstractSavingManager>().GetSavingData<QuestsSavingData>().ChangeQuestState(questDatasList[index].blockPart.GetFullTag(this), QuestProgressType.starting);
        questDatasList[index].questBlock.OnQuestStartProcedure(questDatasList[index].blockPart);
        GetRef<AbstractSavingManager>().GetSavingData<QuestsSavingData>().ChangeQuestState(questDatasList[index].blockPart.GetFullTag(this), QuestProgressType.inProgress);
        GetRef<QuestsManager>().QuestLaunched(questDatasList[index]);
    }

    public void FinishQuest(string tag)
    {
        if (tag == "") return;
        questDatasList[GetIndexByTag(tag)].blockPart.isGoalReached = true;
    }

    public int GetIndexByTag(string tag)
    {
        for (int i = 0; i < questDatasList.Count; i++)
        {
            if (questDatasList[i].blockPart.tag == tag)
            {
                return i;
            }
        }

        return -1;
    }

    public QuestData GetQuestDataByTag(string tag)
    {
        return questDatasList.Find(a => a.blockPart.tag == tag);
    }

    public void MatchTags(List<QuestData> refList, List<QuestData> targetList)
    {
        for (int i = 0; i < refList.Count; i++)
        {
            var item = refList[i];
            var list = item.blockPart.startQuestsWithTagsList;
            if (list != null)
                targetList[i].blockPart.startQuestsWithTagsList = FindTagsInTargetList(list, refList, targetList);

            list = item.blockPart.GetSetDataTags(null);
            if (list.Any())
                targetList[i].blockPart.GetSetDataTags(FindTagsInTargetList(list, refList, targetList));
        }
    }

    private List<string> FindTagsInTargetList(List<string> tags, List<QuestData> refList, List<QuestData> targetList)
    {
        List<int> indexes = new List<int>();

        foreach (var tag in tags)
        {
            var index = refList.FindIndex(a => a.blockPart.tag == tag);
            if (index != -1) indexes.Add(index);
        }

        List<string> newTags = new List<string>();

        foreach (var index in indexes)
        {
            newTags.Add(targetList[index].blockPart.tag);
        }

        return newTags;
    }

    public bool ClickRectForRewind(Rect clickedRect, Rect rewindToRect)
    {
        Event e = Event.current;
        if (CheckRectClicked(clickedRect))
        {
            RewindToRect(e.mousePosition.y, rewindToRect.yMax);
            return true;
        }

        return false;
    }

    public bool CheckRectClicked(Rect rect)
    {
        Event e = Event.current;
        if (e.type == EventType.MouseUp && e.button == 0
                && e.mousePosition.y > rect.yMin
                && e.mousePosition.y < rect.yMax)
            return true;
        return false;
    }

    public void RewindToRect(float currentPosY, float targetPosY)
    {
        var diff = targetPosY - currentPosY;
        var pos = scrollPos;
        pos.y += diff;
        scrollPos = pos;
    }

    public void ApplicationQuit()
    {
        foreach (var item in questDatasList)
        {
            item.questBlock.RevertTagsOnExit(item.blockPart);
        }
    }
}

[Serializable]
public class QuestData
{
    public AbstractQuestBlock questBlock;
    public AbstractQuestBlockPart blockPart;
    public int id;
    public Rect rect;
}

public enum QuestProgressType
{
    none, inProgress, completed, starting
}

[Serializable]
public class QuestTemplate
{
    public List<QuestData> questDatasList = new List<QuestData>();
    public string name = "Template name";
}
