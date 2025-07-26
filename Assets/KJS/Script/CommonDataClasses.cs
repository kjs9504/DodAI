using System;
using System.Collections.Generic;

// 모든 공통 데이터 클래스들을 한 곳에 정의
[Serializable]
public class Position
{
    public float x;
    public float y;
    public float z;
}

[Serializable]
public class AcceptedTaskData
{
    public long id;
    public string todo, date, time, acceptedAt;
    public long? userId;
    public string emotion; // 백엔드에서 보내는 emotion 필드
}

[Serializable]
public class FruitData
{
    public long id;
    public long acceptedTaskId;
    public string todo; // JSON에서 오는 todo 필드
    public string date; // JSON에서 오는 date 필드
    public float posX, posY, posZ;
    public string createdAt;
    public string emotion;
    public Position position;
}

[Serializable]
public class TreeData
{
    public string date;
    public string emotion;
    public string goodPoints;
    public string solution;
}

[Serializable]
public class WeekData
{
    public string weekStart;
    public string weekEnd;
    public TreeData tree;
    public List<FruitData> fruits;
}

[Serializable]
public class WeeksData
{
    public List<WeekData> weeks;
}

// 기존 구조와의 호환성을 위한 클래스들
[Serializable]
public class AcceptedListData
{
    public List<AcceptedTaskData> tasks;
}

[Serializable]
public class FruitListData
{
    public List<FruitData> fruits;
}

[Serializable]
public class TreeDataList
{
    public List<TreeData> trees;
} 