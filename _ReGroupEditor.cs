using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class _ReGroupEditor: EditorWindow
{
    public GameObject regroupFill;  //需要重新分组的物体，只有这一个
    public int ElementSizeWidth;    //物体宽
    public int ElementSizeHeigth;   //物体高

    _ReGroupEditor()
    {
        this.titleContent = new GUIContent("重新分组");
    }

    protected void OnEnable()
    {

    }
    //添加菜单栏用于打开窗口
    [MenuItem("从模型到Asset/重新分组")]
    static void showWindow()
    {
        EditorWindow.GetWindow(typeof(_ReGroupEditor));
    }
    void OnGUI()
    {
        GUILayout.Space(20);
        regroupFill=(GameObject) EditorGUILayout.ObjectField("ReGroupGameObject", regroupFill,typeof(GameObject));
        ElementSizeWidth = EditorGUILayout.IntField("元素长度", ElementSizeWidth);
        ElementSizeHeigth = EditorGUILayout.IntField("元素宽度", ElementSizeHeigth);
        GUILayout.Space(20);
        if (GUILayout.Button("重新分组"))//
        {
            ReGroup();
        }
    }

    /// <summary>
    /// 四分法，每一块大小相等
    /// 
    /// 无法排除多次点击影响
    /// </summary>
    private void ReGroup()
    {
        if (regroupFill)
        {
            #region//参数处理
            Transform[] transforms = regroupFill.transform.GetComponentsInChildren<Transform>();
            Dictionary<Vector3,GameObject> positionsGameObject = new Dictionary<Vector3,GameObject>();//已有元素位置,物体
            List<float> line = new List<float>();//行
            List<float> column = new List<float>();//列
            for(int i = 1; i < transforms.Length; i++)//取出所有元素的位置对应到行列中
            {
                positionsGameObject.Add(transforms[i].position,transforms[i].gameObject);
                InsertInToListLineColumn(line, column, transforms[i].position);
            }          
            //此时并未排除元素分散的影响

            //添加空位，让list内元素间距符合给定的间距
            ContinuityList(line, ElementSizeWidth);
            ContinuityList(column, ElementSizeHeigth);

            int deep = SearchDeep(line.Count * column.Count);

            //保存位置信息的矩阵
            PositionObject[,] positionObjects = new PositionObject[(int)Math.Pow(2, deep ), (int)Math.Pow(2, deep )];


            //这里应该在根据矩阵行列数再进行一次line和column的填充
            AddDistanceWithCount(line, ElementSizeWidth, (int)Math.Pow(2, deep));
            AddDistanceWithCount(column, ElementSizeHeigth, (int)Math.Pow(2, deep));
            #endregion


            #region    //填充矩阵
            //这里为了保证矩阵样式和模型分布相同（矩阵没有旋转），所以重新选择行和列进行填充
            for (int i = 0; i < positionObjects.GetLength(0); i++)//行
            {
                for (int j = 0; j < positionObjects.GetLength(0); j++)//列
                {
                    Vector3 temp = new Vector3(line[i], 0, column[column.Count-1-j]);//行从小到大，列从大到小
                    positionObjects[j, i].position = temp;//i，j互换
                    Bounds bounds = new Bounds(temp,new Vector3(ElementSizeWidth,0, ElementSizeHeigth));
                    positionObjects[j, i].bounds = bounds;
                    if (positionsGameObject.ContainsKey(temp))
                    {
                        positionObjects[j,i].gameObject = positionsGameObject[temp];
                    }
                    else
                    {
                        positionObjects[j,i].gameObject = null;
                    }                   
                }
            }
            //输出查看
            //Show(positionObjects);
            #endregion


            #region//矩阵分块，命名，等一系列操作

            
            //<层数,这层内每一块包含的叶子节点矩阵>，这么做一定有冗余数据，但是有效的做法
            Dictionary<int, List<PositionObject[,]>> deepSubMatrix = new Dictionary<int, List<PositionObject[,]>>();
            //对第0层进行赋值
            foreach(var item in deepSubMatrix)
            {
                Debug.LogError(item.Key);
            }

            //先完成对位置和层数的划分，再进行物体的分组
            //这里的重新分块不希望使用递归，因为叶子节点可能数量巨大
            //从0层开始像最后一层逐渐分组
            int deepTime = 0;
            while (deepTime != deep)
            {
                List<PositionObject[,]> tempPositionObjectList = new List<PositionObject[,]>();
                if (deepTime == 0)//第0层只有一个节点，内容就是全部
                {
                    tempPositionObjectList.Add(positionObjects);//添加原始数组
                    deepSubMatrix.Add(deepTime, tempPositionObjectList);//加入字典
                }
                else
                {
                    foreach(PositionObject[,] item in deepSubMatrix[deepTime-1])//遍历这一次所有子块，进行再分
                    {
                        if (item.Length != 1)//说明不是最低层叶子节点，即代表该块可再分
                        {
                            //左上
                            PositionObject[,] lu = new PositionObject[item.GetLength(0) / 2, item.GetLength(0) / 2];
                            //右上
                            PositionObject[,] ru = new PositionObject[item.GetLength(0) / 2, item.GetLength(0) / 2];
                            //左下
                            PositionObject[,] ld = new PositionObject[item.GetLength(0) / 2, item.GetLength(0) / 2];
                            //右下
                            PositionObject[,] rd = new PositionObject[item.GetLength(0) / 2, item.GetLength(0) / 2];

                            for (int i=0;i< item.GetLength(0)/2; i++)//行
                            {
                                for(int j=0;j< item.GetLength(0)/2; j++)//列
                                {
                                    lu[i, j] = item[i, j];
                                    ru[i, j] = item[i, item.GetLength(0) / 2 + j];
                                    ld[i, j] = item[item.GetLength(0) / 2 + i, j];
                                    rd[i, j] = item[item.GetLength(0) / 2 + i, item.GetLength(0) / 2 + j];
                                }
                            }
                            //将4块加入list，这里要注意一定是4块
                            // 想要修改顺序就修改这里
                            tempPositionObjectList.Add(lu);
                            tempPositionObjectList.Add(ru);
                            tempPositionObjectList.Add(rd);
                            tempPositionObjectList.Add(ld);
                                                      
                        }
                        else
                        {
                            Debug.LogError("该块不可再分");
                        }
                    }
                    deepSubMatrix.Add(deepTime, tempPositionObjectList);
                }
                deepTime += 1;
            }

            //输出，展示已保存的字典
            //Show(deepSubMatrix);

            //开始操作场景内物体

            //<逻辑父节点,该节点相关参数>//符合4^N
            Dictionary<int,List<PositionObject>> fathers = new Dictionary<int, List<PositionObject>>();
            Dictionary<GameObject, PositionObject[,]> gameobjectSons = new Dictionary<GameObject, PositionObject[,]>();

            //生成逻辑父节点
            for (int groupSignal = 0; groupSignal < deep; groupSignal++)
            {
                List<PositionObject> tempList = new List<PositionObject>();
                foreach (PositionObject[,] item in deepSubMatrix[groupSignal])
                {
                    
                    GameObject gameObject = new GameObject(groupSignal.ToString());
                    Bounds bounds = new Bounds();
                    bounds.size = item.GetLength(0) * item[0, 0].bounds.size;//大小=一行的数量*原有大小
                    Vector3 midVector3 = Vector3.zero;
                    foreach (PositionObject posObj in item)
                    {
                        midVector3.x += posObj.position.x;
                        midVector3.z += posObj.position.z;
                    }
                    bounds.center = midVector3 / item.Length;
                    gameObject.transform.position = bounds.center;
                    PositionObject positionObject = new PositionObject();
                    positionObject.bounds = bounds;
                    positionObject.position = bounds.center;
                    positionObject.gameObject = gameObject;
                    tempList.Add(positionObject);
                    if (groupSignal == deep - 1)//表示最后一层叶子节点
                    {
                        gameobjectSons.Add(gameObject, item);
                    }

                }
                fathers.Add(groupSignal, tempList);

            }
            //为父节点添加逻辑关系
            MakeUpFather(fathers,deep+1);

            //为原有物体设置父物体（逻辑）
            foreach (var objList in gameobjectSons)//直接查找最底层
            {

                foreach(var item in objList.Value)
                {
                    if (positionsGameObject.ContainsKey(item.position))
                    {
                        positionsGameObject[item.position].transform.SetParent(objList.Key.transform);
                    }
                }
            }




            #endregion


        }
    }

    /// <summary>
    /// 向两个行列list中添加元素，去重，排序
    /// 希望输入的最小元素都是大小相等的
    /// </summary>
    /// <param name="line">行</param>
    /// <param name="colume">列</param>
    /// <param name="pos">位置</param>
    private void InsertInToListLineColumn(List<float> line, List<float> colume, Vector3 pos)
    {
        if (!line.Contains(pos.x))//判断是否已有
        {
            if (line.Count == 0)//判断空表
            {
                line.Add(pos.x);
            }
            else//找出相应位置，插入
            {
                int count = 0;
                while (line[count] < pos.x)
                {
                    count++;
                    if (count > line.Count - 1)
                    {
                        break;
                    }
                }
                line.Insert(count, pos.x);

            }
        }

        if (!colume.Contains(pos.z))//判断是否已有
        {

            if (colume.Count == 0)//判断空表
            {
                colume.Add(pos.z);
            }
            else//找出相应位置，插入
            {
                int count = 0;
                while (colume[count] < pos.z)
                {
                    count++;
                    if (count > colume.Count - 1)
                    {
                        break;
                    }
                }
                colume.Insert(count, pos.z);
            }
        }



    }

    /// <summary>
    /// 添加空位使list符合distance并连续
    /// </summary>
    /// <param name="operationData">操作List,长度大于2</param>
    /// <param name="distance">间距</param>
    private void ContinuityList(List<float> operationData,int distance)
    {        
        List<float> result = new List<float>();//修改过的list，避免直接修改原有数据
        for(int i = 0; i < operationData.Count; i++)
        {
            if (result.Count == 0)
            {
                result.Add(operationData[i]);
            }
            else 
            {
                if ((int)result[result.Count-1] + distance < (int)operationData[i])
                {
                    while ((int)result[result.Count-1] + distance < operationData[i])
                    {
                        result.Add(result[result.Count-1] + distance);
                    }
                    result.Add(operationData[i]);
                }
                else
                {
                    result.Add(operationData[i]);
                }
            }
        }
        operationData.Clear();

        foreach(var item  in result)//把修改过的list内容替换掉原有数据
        {
            operationData.Add(item);
        }
        result.Clear();
    }

    /// <summary>
    /// 按照4^N得出范围
    /// </summary>
    /// <param name="target">元素个数</param>
    private int SearchDeep(int target)
    {
        int result = 0;
        while(Math.Pow(4, result) < target)
        {
            result++;
        }
        return result;
    }

    /// <summary>
    /// 按数量补充List
    /// </summary>
    /// <param name="operationData">操作List</param>
    /// <param name="distance">间距</param>
    /// <param name="count">数量</param>
    private void AddDistanceWithCount(List<float> operationData, int distance,int count)
    {
        while (operationData.Count < count)
        {
            operationData.Add(operationData[operationData.Count-1] + distance);
        }
    }

    #region//输出展示Debug相关
    /// <summary>
    /// 输出位置矩阵
    /// </summary>
    /// <param name="positionObjects">想要展示的矩阵</param>
    /// <param name="signal">标记</param>
    private void Show(PositionObject[,] shower,int signal)
    {
        for (int i = 0; i < shower.GetLength(0); i++)//列
        {
            string a = "Sign:"+signal+" "+i + "行 ";
            for (int j = 0; j < shower.GetLength(0); j++)//行
            {


                if (shower[i, j].gameObject)
                {
                    a += " " + 1;// shower[i, j].position.ToString() + "A";
                }
                else
                {
                    a += " " + 0;// shower[i, j].position.ToString() + "B";
                }

            }
            Debug.LogError(a);
        }
    }

    /// <summary>
    /// 输出分块信息
    /// </summary>
    /// <param name="shower"></param>
    private void Show(Dictionary<int, List<PositionObject[,]>> shower)
    { 
        foreach(var item in shower)
        {
            Debug.LogError("第" + item.Key + "层"+"===================================================");
            int num = 1;
            foreach(var temp in item.Value)
            {
                Debug.LogError("第" + num + "块" + "-------"+ "第" + item.Key + "层");
                Show(temp, num);
                num++;
            }

        }
    }
    #endregion

    /// <summary>
    /// 为父节点添加逻辑关系
    /// </summary>
    /// <param name="fathers">操作字典 </param>
    /// <param name="deep">深度 </param>
    private void MakeUpFather(Dictionary<int, List<PositionObject>> fathers,int start)
    {
       
       for(int i = 0; i < fathers.Count; i++)
       {
            if (i == 0)
            {
                
            }
            else
            {
                int signalFather = 0;
                int signalNum = 0;
                foreach(PositionObject item in fathers[i])
                {
                    item.gameObject.transform.SetParent(fathers[i - 1][signalFather].gameObject.transform);
                   
                    if ((signalNum+1) % 4 == 0)
                    {
                        signalFather++;
                    }
                    signalNum++;
                }
            }
            

       }
    }

}


/// <summary>
/// 保存元素位置，和物体引用，无物体则未null
/// </summary>
public struct PositionObject
{
    /// <summary>
    /// 元素位置
    /// </summary>
    public Vector3 position;
    /// <summary>
    /// 物体引用
    /// </summary>
    public GameObject gameObject;
    /// <summary>
    /// 边框
    /// </summary>
    public Bounds bounds;
}