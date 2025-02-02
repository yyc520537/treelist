﻿using DevExpress.Utils;
using DevExpress.XtraDiagram;
using DevExpress.XtraTreeList;
using DevExpress.XtraTreeList.Columns;
using log4net;
using log4net.Repository.Hierarchy;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using treelist.model;
using static DevExpress.XtraEditors.Mask.Design.MaskSettingsForm.DesignInfo.MaskManagerInfo;

namespace treelist
{
    public partial class Form1 : DevExpress.XtraEditors.XtraForm
    {
        private int nodeId = 0; // 用于生成唯一的节点ID
        // 在Form1类中定义一个列表作为数据源
        BindingList<TreeListNodeModel> nodeList = new BindingList<TreeListNodeModel>();
        Dictionary<string, List<string>> fileDependencies = new Dictionary<string, List<string>>();//存储关联信息
        private static readonly ILog _logger = LogManager.GetLogger(typeof(Form1));
        public Form1()
        {
            InitializeComponent();
            InitializeTreeList();
            log4net.Config.XmlConfigurator.Configure(new FileInfo("log4net.config"));//使用配置文件
            InitializeLog4Net("日志添加字段");// 日志文件名添加自定义字段
            _logger.Info("程序开始运行.");  
        }

        /// <summary>
        /// 日志文件名添加自定义字段
        /// </summary>
        /// <param name="customFieldName"></param>
        static void InitializeLog4Net(string customFieldName)
        {
            log4net.Config.XmlConfigurator.Configure(); // 配置 log4net（确保你已经在配置文件中定义了 log4net 配置）

            var appender = (log4net.Appender.RollingFileAppender)log4net.LogManager.GetRepository()
                .GetAppenders().FirstOrDefault(a => a.Name == "FileAppender");
            if (appender != null)
            {
                appender.File = $"C:/Logs/{customFieldName}";
                appender.ActivateOptions(); // 重新激活配置以应用更改
            }
        }
        /// <summary>
        /// 设置文件名颜色显示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void treeList1_CustomDrawNodeCell(object sender, CustomDrawNodeCellEventArgs e)
        {
            if (e.Column.FieldName == "Name" && e.Node.GetValue(e.Column) != null)
            {
                string fullText = e.Node.GetValue(e.Column).ToString();
                // 假设fullText格式是"filename[revision][date]"
                int startIndex = fullText.IndexOf("[");
                if (startIndex != -1)
                {
                    // 设置默认的绘制，避免画背景等
                    e.Appearance.FillRectangle(e.Cache, e.Bounds);
                    // 绘制前半部分（黑色）
                    string mainText = fullText.Substring(0, startIndex);
                    e.Cache.DrawString(mainText, e.Appearance.Font, Brushes.Black, e.Bounds.Location);
                    // 计算前半部分的宽度，以便确定灰色文本的开始位置
                    SizeF mainTextSize = e.Cache.CalcTextSize(mainText, e.Appearance.Font).ToSize();
                    PointF revisionTextLocation = new PointF(e.Bounds.X + mainTextSize.Width, e.Bounds.Y);
                    // 绘制后半部分（灰色）
                    string revisionText = fullText.Substring(startIndex);
                    e.Cache.DrawString(revisionText, e.Appearance.Font, Brushes.Gray, revisionTextLocation);
                    // 设置Handled为true，避免默认绘制行为
                    e.Handled = true;
                }
            }
        }
        private void treeList1_FocusedNodeChanged(object sender, DevExpress.XtraTreeList.FocusedNodeChangedEventArgs e)
        {
            if (e.Node != null)
            {
                var fileName = ExtractFileName(e.Node.GetValue("Name").ToString());
                // 清除旧的图形元素
                diagramControl1.Items.Clear();
                visitedNodes.Clear();  // 也重置访问过的节点集合
                DisplayDependenciesForSelectedFile(fileName);
            }
        }

        /// <summary>
        /// TreeList属性设置
        /// </summary>
        private void InitializeTreeList()
        {
            treeList1.Columns.Add(new TreeListColumn{Caption = "Name", FieldName = "Name", VisibleIndex = 0 });
            treeList1.Columns.Add(new TreeListColumn { Caption = "Group", FieldName = "Group", VisibleIndex = 1 , Visible = false });
            treeList1.Columns["Group"].SortOrder = SortOrder.Ascending;
            treeList1.KeyFieldName = "ID";
            treeList1.ParentFieldName = "ParentID";
        }
        /// <summary>
        /// XML文件选择按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoadXmlButton_Click(object sender, EventArgs e)
        {
            try
            {
                
                _logger.Info("脚本导入正常.");
                FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    var stopwatch = Stopwatch.StartNew();
                    string selectedFolder = folderBrowserDialog.SelectedPath;
                    // 异步执行加载和解析操作Task.Run(() =>
                    LoadFiles(selectedFolder);
                    // 异步执行加载和解析操作
                    ProcessXmlDirectory(selectedFolder);
                    //Task.Run(() => DisplayDependenciesAsync(diagramControl1, fileDependencies));
                    stopwatch.Stop();
                    _logger.Info($"程序加载完成，耗时：{stopwatch.ElapsedMilliseconds}毫秒");
                }
            }
            catch (Exception ex)
            {

                _logger.Error("脚本导入失败.", ex);
            }
        }
        int nodeIdCounter = 1;
        /// <summary>
        /// treelist节点数据生成
        /// </summary>
        /// <param name="folderPath">json文件地址</param>
        private void LoadFiles(string folderPath)
        {
            try
            {
                // 步骤1: 解析 JSON 文件，创建节点到 TreeListNode 的映射
                var jsonFilePath = Path.Combine(folderPath, "C:\\work\\ScriptList.json");
                var jsonEcuMapping = LoadJsonData(jsonFilePath);
                // 创建节点数据
                foreach (var script in jsonEcuMapping)
                {
                    int scriptTypeId = nodeIdCounter++;
                    switch (script.scriptType)
                    {
                        case "B":
                            script.scriptType = "SystemBaseScript";
                            break;
                        case "F":
                            script.scriptType = "GeneralFunctionScript";
                            break;
                        case "S":
                            script.scriptType = "SoftwareDownloadScript";
                            break;
                        case "D":
                            script.scriptType = "DiagnosticScript";
                            break;
                        case "C":
                            script.scriptType = "ConfigScript";
                            break;
                        default:
                            script.scriptType = "SystemBaseScript";
                            break;
                    }
                    if (!nodeList.Any(n => n.Name == script.scriptType))
                    {
                        nodeList.Add(new TreeListNodeModel
                        {
                            ID = scriptTypeId,
                            ParentID = null, // 顶级节点
                            Name = script.scriptType
                        });
                    }
                    foreach (var scriptDto in script.scriptDTOS)
                    {
                        if (scriptDto.scGenericComponentDTOS != null)
                        {
                            foreach (var component in scriptDto.scGenericComponentDTOS)
                            {
                                int componentId = nodeIdCounter++;
                                if (!nodeList.Any(n => n.Name == component.ecu))
                                {
                                    nodeList.Add(new TreeListNodeModel 
                                    {
                                        ID = componentId,
                                        ParentID = nodeList.Where(n => n.Name == script.scriptType).Select(n => n.ID).FirstOrDefault(),
                                        Name = component.ecu
                                    });
                                }
                            }
                        }
                    }
                }
                // 加载脚本文件并添加到节点数据中
                var xmlFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
                    .Where(file => file.EndsWith(".otx", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".zotx", StringComparison.OrdinalIgnoreCase));
                foreach (var xmlFile in xmlFiles)
                {   
                    var xmlFileName = Path.GetFileName(xmlFile);
                    var scriptDto = jsonEcuMapping.SelectMany(s => s.scriptDTOS)
                        .FirstOrDefault(dto => dto.fileTitle.Equals(xmlFileName, StringComparison.OrdinalIgnoreCase));

                    var result = jsonEcuMapping
                        .SelectMany(s => s.scriptDTOS.Select(dto => new { ScriptType = s.scriptType, Dto = dto }))
                        .FirstOrDefault(x => x.Dto.fileTitle.Equals(xmlFileName, StringComparison.OrdinalIgnoreCase));
                    var scriptType = "";
                    ScriptDTO scriptDtoEntry = null;
                    if (result != null)
                    {
                        scriptType = result.ScriptType;
                        scriptDtoEntry = result.Dto;
                    }
                    if (result == null)
                    {
                        _logger.Warn(xmlFileName + "脚本不在json文件中.");
                        if (!nodeList.Any(n => n.Name == "Local"))
                        {
                            nodeList.Add(new TreeListNodeModel
                            {
                                ID = nodeIdCounter++,
                                ParentID = null, // 顶级节点
                                Name = "Local"
                            });
                        }
                        else
                        {
                            TreeListNodeModel aaa = new TreeListNodeModel
                            {
                                ID = nodeIdCounter++,
                                ParentID = nodeList.Where(n => n.Name == "Local").Select(n => n.ID).FirstOrDefault(), // 顶级节点
                                Name = xmlFileName
                            };
                            nodeList.Add(aaa);
                            LoadXmlAndPopulateTree1(xmlFile, aaa, nodeList);
                        }
                    }
                    if (scriptDto != null)
                    {
                        if (scriptDto.scGenericComponentDTOS != null)
                        {
                            _logger.Info("当前脚本ecu型号为：" + scriptDto.scGenericComponentDTOS.FirstOrDefault().ecu +"脚本名称："+ xmlFileName);
                            if (!nodeList.Any(n => n.Name == xmlFileName))
                            {
                                TreeListNodeModel aaa = new TreeListNodeModel
                                {
                                    ID = nodeIdCounter++,//xmlFileName + "["+scriptDto.gitRevision+"]"+ "["+scriptDto.gitDate+"]"
                                    ParentID = nodeList.Where(n => n.Name == scriptDto.scGenericComponentDTOS.FirstOrDefault().ecu).Select(n => n.ID).FirstOrDefault(),//scriptDto.scGenericComponentDTOS?.FirstOrDefault()?.ecu
                                    Name = xmlFileName + " [" + scriptDto.gitRevision + "] " + "[" + scriptDto.gitDate + "]",
                                    FileTitle = scriptDto.fileTitle,
                                    GitRevision = scriptDto.gitRevision,
                                    GitDate = scriptDto.gitDate,
                                    FileId = scriptDto.id,
                                    Title = scriptDto.title
                                };
                                nodeList.Add(aaa);
                                LoadXmlAndPopulateTree1(xmlFile, aaa, nodeList);
                            }
                        }
                        else
                        {
                            _logger.Warn("当前脚本无ecu型号.");
                            if (!nodeList.Any(n => n.Name == xmlFileName))
                            {
                                TreeListNodeModel aaa = new TreeListNodeModel
                                {
                                    ID = nodeIdCounter++,//xmlFileName + "["+scriptDto.gitRevision+"]"+ "["+scriptDto.gitDate+"]"
                                    ParentID = nodeList.Where(n => n.Name == scriptType).Select(n => n.ID).FirstOrDefault(),//scriptDto.scGenericComponentDTOS?.FirstOrDefault()?.ecu
                                    Name = xmlFileName + " [" + scriptDto.gitRevision + "] " + "[" + scriptDto.gitDate + "]",
                                    FileTitle = scriptDto.fileTitle,
                                    GitRevision = scriptDto.gitRevision,
                                    GitDate = scriptDto.gitDate,
                                    FileId = scriptDto.id,
                                    Title = scriptDto.title
                                };
                                nodeList.Add(aaa);
                                LoadXmlAndPopulateTree1(xmlFile, aaa, nodeList);
                            }
                        }
                    }
                }
                _logger.Info("文件导入正常.");
            }
            catch (Exception ex)
            {
                _logger.Error("Error.", ex); 
            }
            
            // 使用 nodeList 作为数据源
            this.Invoke(new Action(() =>
            {
                treeList1.DataSource = nodeList;
                treeList1.KeyFieldName = "ID";
                treeList1.ParentFieldName = "ParentID";
                //treeList1.ExpandAll();//用于treelist结构是否展开
            }));
        }

        /// <summary>
        /// json文件解析
        /// </summary>
        /// <param name="jsonFilePath">json文件地址</param>
        private List<ScriptList> LoadJsonData(string jsonFilePath)
        {
            try
            {
                _logger.Info("json文件解析成功.");
                string json = File.ReadAllText(jsonFilePath);
                return JsonConvert.DeserializeObject<List<ScriptList>>(json);
            }
            catch (Exception ex)
            {
                _logger.Error("json文件解析失败：", ex); 
                return null;
            }
        }
        #region 使用XmlDocument
        /// <summary>
        /// xml文件导入
        /// </summary>
        /// <param name="xmlFilePath">文件地址</param>
        private XmlNode LoadXmlDocument(string xmlFilePath)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlFilePath);
            return xmlDoc.DocumentElement;
        }

        /// <summary>
        /// XML文件结构解析
        /// </summary>
        /// <param name="xmlFilePath">文件地址</param>
        private void PopulateXmlToTree(XmlNode xmlNode, TreeListNodeModel parentNode, BindingList<TreeListNodeModel> nodeList)
        {
            if (xmlNode.Name.Equals("procedure", StringComparison.OrdinalIgnoreCase) ||
               xmlNode.Name.Equals("inParam", StringComparison.OrdinalIgnoreCase) ||
               xmlNode.Name.Equals("outParam", StringComparison.OrdinalIgnoreCase))
            {
                // 假设您已经将图标添加到 imageCollection1 并为它们设置了键名
                int procedureIconIndex = imageCollection1.Images.IndexOf(imageCollection1.Images["p.png"]);
                int paramIconIndex = imageCollection1.Images.IndexOf(imageCollection1.Images["v.png"]);
                int folderIconIndex = imageCollection1.Images.IndexOf(imageCollection1.Images["a.png"]);
                if (xmlNode.Name.Equals("procedure", StringComparison.OrdinalIgnoreCase))
                {
                    if (xmlNode.NodeType == XmlNodeType.Element)
                    {
                        var nodeName = xmlNode.Attributes?["name"]?.InnerText ?? xmlNode.Name;
                        var TreeListNodeModel = new TreeListNodeModel
                        {                                                                                                                                                                                                                                                                                                                                                   
                            ID = nodeIdCounter++,
                            ParentID = parentNode.ID,  // 使用父节点的ID作为ParentID
                            Name = nodeName,
                            Group = 1,
                            ImageIndex = procedureIconIndex
                        };
                        nodeList.Add(TreeListNodeModel);
                        foreach (XmlNode childNode in xmlNode.ChildNodes)
                        {
                            // 只处理元素类型的节点。
                            if (childNode.NodeType == XmlNodeType.Element)
                            {
                                PopulateXmlToTree(childNode, parentNode, nodeList);
                            }
                        }
                    }
                }
                else if (xmlNode.Name.Equals("inParam", StringComparison.OrdinalIgnoreCase) ||
                         xmlNode.Name.Equals("outParam", StringComparison.OrdinalIgnoreCase))
                {
                    if (xmlNode.NodeType == XmlNodeType.Element)
                    {
                        var nodeName = xmlNode.Attributes?["name"]?.InnerText ?? xmlNode.Name;
                        var TreeListNodeModel = new TreeListNodeModel
                        {
                            ID = nodeIdCounter++,
                            ParentID = parentNode.ID,  // 使用父节点的ID作为ParentID
                            Name = nodeName,
                            Group = 2,
                            ImageIndex = paramIconIndex
                        };
                        nodeList.Add(TreeListNodeModel);
                        foreach (XmlNode childNode in xmlNode.ChildNodes)
                        {
                            // 只处理元素类型的节点。
                            if (childNode.NodeType == XmlNodeType.Element)
                            {
                                PopulateXmlToTree(childNode, parentNode, nodeList);
                            }
                        }
                    }
                }
            }
            else
            {
                if (xmlNode.NodeType == XmlNodeType.Element)
                {
                    foreach (XmlNode childNode in xmlNode.ChildNodes)
                    {
                        // 只处理元素类型的节点。
                        if (childNode.NodeType == XmlNodeType.Element)
                        {
                            PopulateXmlToTree(childNode, parentNode, nodeList);
                        }
                    }
                }
            }
        }
        #endregion

        /// <summary>
        /// 测试按钮，用于修改nodelist列表中数据的值，观察treelist是否会实时更新
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            // 首先找到具有名称"Local"的节点的ID
            var localNodeId = nodeList.FirstOrDefault(n => n.Name == "Local")?.ID;
            if (localNodeId != null)
            {
                var pngFile = Path.Combine(Application.StartupPath, "C:\\Logs\\MyFlowShapes.png");
                diagramControl1.ExportDiagram(pngFile);//将绘制的脚本关联图保存
                SaveTreeListAsImage(treeList1, @"C:\Logs\treeList.png");
                _logger.Warn ("测试修改数据，treelist实时更新.");
                // 使用找到的ID来更新对应的节点
                var nodeToUpdate = nodeList.FirstOrDefault(n => n.ID == localNodeId);
                if (nodeToUpdate != null)
                {
                    nodeToUpdate.Name = "测试能否实时修改数据"; // 更新名称
                    nodeToUpdate.ImageIndex = imageCollection1.Images.IndexOf(imageCollection1.Images["p.png"]);
                    // 使用INotifyPropertyChanged接口，UI会自动响应这个变化
                }
            }
            else
            {
                var pngFile = Path.Combine(Application.StartupPath, "C:\\Logs\\FlowShapes.pngg");
                diagramControl1.ExportDiagram(pngFile);
                SaveTreeListAsImage(treeList1, @"C:\Logs\treeList.png");
                _logger.Warn("测试修改数据，treelist实时更新.");
                localNodeId = nodeList.FirstOrDefault(n => n.Name == "测试能否实时修改数据")?.ID;
                // 使用找到的ID来更新对应的节点
                var nodeToUpdate = nodeList.FirstOrDefault(n => n.ID == localNodeId);
                if (nodeToUpdate != null)
                {
                    nodeToUpdate.Name = "Local"; // 更新名称
                    nodeToUpdate.ImageIndex = imageCollection1.Images.IndexOf(imageCollection1.Images["v.png"]);
                    // 使用INotifyPropertyChanged接口，UI会自动响应这个变化
                }
            }
        }

        #region 使用XmlReader
        /// <summary>
        /// 使用XmlReader
        /// </summary>
        /// <param name="xmlFilePath"></param>
        /// <param name="parentNode"></param>
        /// <param name="nodeList"></param>
        private void ParseXmlUsingXmlReaderAndPopulateTree(string xmlFilePath, TreeListNodeModel parentNode, BindingList<TreeListNodeModel> nodeList)
        {
            using (XmlReader reader = XmlReader.Create(xmlFilePath))
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        string nodeName = reader.Name;
                        string nameAttribute = reader.GetAttribute("name");

                        if (nodeName.Equals("procedure", StringComparison.OrdinalIgnoreCase) ||
                            nodeName.Equals("inParam", StringComparison.OrdinalIgnoreCase) ||
                            nodeName.Equals("outParam", StringComparison.OrdinalIgnoreCase))
                        {
                            int iconIndex = GetIconIndex(nodeName);
                            int group = nodeName.Equals("procedure", StringComparison.OrdinalIgnoreCase) ? 1 : 2;

                            var newNode = new TreeListNodeModel
                            {
                                ID = nodeIdCounter++,
                                ParentID = parentNode?.ID, // 如果 parentNode 为 null，则这是顶级节点
                                Name = nameAttribute ?? nodeName,
                                Group = group,
                                ImageIndex = iconIndex
                            };
                            nodeList.Add(newNode);

                            // 如果有子节点，递归调用
                            if (!reader.IsEmptyElement)
                            {
                                PopulateXmlToTreeUsingReader(reader, newNode, nodeList);
                            }
                        }
                        else
                        {
                            // 跳过非目标节点，但处理其子节点
                            if (!reader.IsEmptyElement)
                            {
                                PopulateXmlToTreeUsingReader(reader, parentNode, nodeList);
                            }
                        }
                    }
                }
            }
        }

        private void PopulateXmlToTreeUsingReader(XmlReader reader, TreeListNodeModel currentParentNode, BindingList<TreeListNodeModel> nodeList)
        {
            int parentDepth = reader.Depth;  // 获取当前节点的深度

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    string nodeName = reader.Name;
                    string nameAttribute = reader.GetAttribute("name");
                    int iconIndex = GetIconIndex(nodeName);
                    int group = GetGroupByNodeName(nodeName);

                    // 创建新节点
                    var newNode = new TreeListNodeModel
                    {
                        ID = nodeIdCounter++,
                        ParentID = currentParentNode?.ID, // 这里我们不改变父节点
                        Name = nameAttribute ?? nodeName,
                        Group = group,
                        ImageIndex = iconIndex
                    };

                    // 对于procedure节点，我们添加新节点并将其作为后续inParam和outParam的父节点
                    if (nodeName.Equals("procedure", StringComparison.OrdinalIgnoreCase))
                    {
                        nodeList.Add(newNode);  // 添加 procedure 节点

                        // 处理子节点，传入当前的父节点，因为procedure是顶级节点
                        if (!reader.IsEmptyElement)
                        {
                            PopulateXmlToTreeUsingReader(reader, currentParentNode, nodeList);
                        }
                    }
                    // 对于inParam和outParam节点，我们使用相同的父节点
                    else if (nodeName.Equals("inParam", StringComparison.OrdinalIgnoreCase) ||
                             nodeName.Equals("outParam", StringComparison.OrdinalIgnoreCase))
                    {
                        nodeList.Add(newNode);  // 添加 inParam 或 outParam 节点
                                                // 不需要递归，因为inParam和outParam不包含子节点
                    }
                }
                else if (reader.NodeType == XmlNodeType.EndElement)
                {
                    // 如果读取器到达与当前处理节点同级的结束元素，则退出
                    if (reader.Depth == parentDepth)
                    {
                        return;
                    }
                }
            }
        }
        #endregion

        #region 使用XDocument
        private void LoadXmlAndPopulateTree1(string xmlFilePath,TreeListNodeModel currentParentNode, BindingList<TreeListNodeModel> nodeList)
        {
            XDocument doc = XDocument.Load(xmlFilePath);
            RecursiveXmlParse(doc.Root, currentParentNode, nodeList); // 从根元素开始解析
        }

        private void RecursiveXmlParse(XElement element, TreeListNodeModel parentNode, BindingList<TreeListNodeModel> nodeList)
        {
            foreach (XElement childElement in element.Elements())
            {
                string nodeName = childElement.Name.LocalName;
                // 只处理我们关心的节点类型
                if (nodeName.Equals("procedure", StringComparison.OrdinalIgnoreCase) ||
                    nodeName.Equals("inParam", StringComparison.OrdinalIgnoreCase) ||
                    nodeName.Equals("outParam", StringComparison.OrdinalIgnoreCase))
                {
                    int iconIndex = GetIconIndex(nodeName);
                    int group = nodeName.Equals("procedure", StringComparison.OrdinalIgnoreCase) ? 1 : 2;

                    // 创建并添加新节点到列表
                    var newNode = new TreeListNodeModel
                    {
                        ID = nodeIdCounter++,
                        ParentID = parentNode?.ID, //使用上一个节点的ID作为父ID，如果父节点为空，则此节点是顶级节点
                        Name = childElement.Attribute("name")?.Value ?? nodeName,
                        Group = group,
                        ImageIndex = iconIndex
                    };
                    nodeList.Add(newNode);
                    // 递归处理所有子元素
                    RecursiveXmlParse(childElement, parentNode, nodeList);
                }
                else
               {
                    RecursiveXmlParse(childElement, parentNode, nodeList);
                }
            }
        }
        #endregion

        /// <summary>
        /// treelist图标选择
        /// </summary>
        /// <param name="nodeName"></param>
        /// <returns></returns>
        private int GetIconIndex(string nodeName)
        {
            switch (nodeName.ToLower())
            {
                case "procedure":
                    return imageCollection1.Images.IndexOf(imageCollection1.Images["p.png"]);
                case "inparam":
                    return imageCollection1.Images.IndexOf(imageCollection1.Images["v.png"]);
                case "outparam":
                    return imageCollection1.Images.IndexOf(imageCollection1.Images["v.png"]);
                default:
                    return imageCollection1.Images.IndexOf(imageCollection1.Images["a.png"]);
            }
        }
        /// <summary>
        /// 节点分组为实现节点排序
        /// </summary>
        /// <param name="nodeName"></param>
        /// <returns></returns>
        private int GetGroupByNodeName(string nodeName)
        {
            // 根据节点名称获取对应的组索引
            // 这里简单实现，具体逻辑根据实际情况调整
            switch (nodeName.ToLower())
            {
                case "procedure":
                    return 1;
                case "inparam":
                case "outparam":
                    return 2;
                default:
                    return 0; // 表示其他类型或未分组
            }
        }

        /// <summary>
        /// 解析单个脚本文件并提取所有'import'标签的'prefix'属性
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private List<string> ParseXmlForImports(string filePath)
        {
            XDocument doc = XDocument.Load(filePath);
            List<string> prefixes = new List<string>();
            string trimmedPrefix = null;
            // 遍历所有元素，只处理 <import> 标签
            foreach (XElement element in doc.Descendants())
            {
                if (element.Name.LocalName.Equals("import", StringComparison.OrdinalIgnoreCase))
                {
                    var prefix = element.Attribute("prefix")?.Value;
                    if (!string.IsNullOrEmpty(prefix) && prefix.Length > 3)
                    {
                        // 去除前三个字符 'imp'
                        trimmedPrefix = prefix.Substring(3);
                        var aaa = nodeList.Where(n => n.FileId == trimmedPrefix).Select(n => n.FileTitle).FirstOrDefault();
                        if (aaa != null)
                        {
                            prefixes.Add(aaa);
                        }
                    }
                }
            }
            return prefixes;
        }

        /// <summary>
        /// 遍历目录并处理每个XML文件
        /// </summary>
        /// <param name="folderPath"></param>
        /// <returns></returns>
        public Dictionary<string, List<string>> ProcessXmlDirectory(string folderPath)
        {
            // 获取所有脚本文件
            var xmlFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
                .Where(file => file.EndsWith(".otx", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".zotx", StringComparison.OrdinalIgnoreCase));
            foreach (var file in xmlFiles)
            {
                var fileName = Path.GetFileName(file);
                var prefixes = ParseXmlForImports(file);
                fileDependencies[fileName] = prefixes;
            }
            return fileDependencies;
        }

        #region 实现关联图绘制

        private async Task DisplayDependenciesAsync(DiagramControl diagram, Dictionary<string, List<string>> dependencies)
        {
            var nodePositions = new Dictionary<string, (float X, float Y)>();
            CalculateNodePositions(dependencies, nodePositions);
            int batchSize = 100; // 减小批处理大小以减轻UI线程负担
            List<Task> tasks = new List<Task>();
            List<DiagramItem> tempItems = new List<DiagramItem>();
            foreach (var dep in dependencies)
            {
                var sourceNode = GetOrCreateDiagramNode(dep.Key, nodePositions[dep.Key], diagram);
                var childNodes = new List<DiagramShape>();
                foreach (var child in dep.Value)
                {
                    var childNode = GetOrCreateDiagramNode(child, nodePositions[child], diagram);
                    childNodes.Add(childNode);
                    var connector = new DiagramConnector { BeginItem = sourceNode, EndItem = childNode };
                    tempItems.Add(connector);
                }
                // 仅在UI线程添加节点
                diagram.Invoke((MethodInvoker)delegate
                {
                    diagram.BeginUpdate();
                    try
                    {
                        if (!diagram.Items.Contains(sourceNode))
                            diagram.Items.Add(sourceNode);
                        childNodes.ForEach(n => {
                            if (!diagram.Items.Contains(n))
                                diagram.Items.Add(n);
                        });
                    }
                    finally
                    {
                        diagram.EndUpdate();
                    }
                });
                if (tempItems.Count >= batchSize)
                {
                    var currentBatch = tempItems.ToList(); // 复制当前批次
                    tempItems.Clear(); // 清空列表以开始新的批次
                    tasks.Add(UpdateDiagramAsync(diagram, currentBatch));
                }
            }
            if (tempItems.Count > 0)
            {
                tasks.Add(UpdateDiagramAsync(diagram, tempItems)); // 添加剩余的项
            }
            await Task.WhenAll(tasks); // 等待所有批次完成
        }

        private async Task UpdateDiagramAsync(DiagramControl diagram, List<DiagramItem> items)
        {
            await Task.Run(() => diagram.Invoke((MethodInvoker)delegate
            {
                diagram.BeginUpdate();
                try
                {
                    items.ForEach(item => diagram.Items.Add(item));
                }
                finally
                {
                    diagram.EndUpdate();
                }
            }));
        }
        #endregion

        /// <summary>
        /// 生成关联图节点
        /// </summary>
        /// <param name="key"></param>文件名
        /// <param name="position"></param>
        /// <param name="diagram"></param>
        /// <returns></returns>
        private DiagramShape GetOrCreateDiagramNode(string key, (float X, float Y) position, DiagramControl diagram)
        {
            var existingNode = diagram.Items.OfType<DiagramShape>().FirstOrDefault(s => s.Content.ToString() == key);//查询节点是否已存在
            if (existingNode != null)
                return existingNode;
            // 设置字体大小
            Font font = new Font("Arial", 10);
            // 计算文本长度
            SizeF textSize = MeasureString(key, font);
            return new DiagramShape
            {
                Content = key,
                Position = new PointFloat(position.X, position.Y),
                Size = new SizeF(textSize.Width , textSize.Height)
            }; 
        }
        /// <summary>
        /// 计算和分配关联图节点位置
        /// </summary>
        /// <param name="dependencies"></param>
        /// <param name="nodePositions"></param>
        private void CalculateNodePositions(Dictionary<string, List<string>> dependencies, Dictionary<string, (float X, float Y)> nodePositions)
        {
            int maxPerRow = 5;
            int rowHeight = 100;
            float x = 10, y = 10;
            int count = 0;

            foreach (var dep in dependencies.Keys)
            {
                if (!nodePositions.ContainsKey(dep))
                {
                    var textSize = MeasureString(dep, new Font("Arial", 10));
                    float columnWidth = textSize.Width + 50; // 为文本增加一些额外空间

                    nodePositions[dep] = (x, y);
                    count++;
                    if (count >= maxPerRow)
                    {
                        x = 10;
                        y += rowHeight;
                        count = 0;
                    }
                    else
                    {
                        x += columnWidth;
                    }
                }  

                // 对于每个子节点
                float childY = y + rowHeight;
                foreach (var child in dependencies[dep])
                {
                    if (!nodePositions.ContainsKey(child))
                    {
                        var childTextSize = MeasureString(child, new Font("Arial", 10));
                        float childColumnWidth = childTextSize.Width + 20;
                        nodePositions[child] = (x, childY);
                        childY += rowHeight; // 这里仍然使用固定行高，可以根据需要调整
                        x += childColumnWidth; // 水平位置递增
                    }
                }
            }
        }


        private HashSet<string> visitedNodes = new HashSet<string>();  // 避免无限递归

        private void DisplayDependenciesForSelectedFile(string selectedFileName)
        {
            if (visitedNodes.Contains(selectedFileName))
                return;

            visitedNodes.Add(selectedFileName);

            Console.WriteLine($"Processing {selectedFileName}");  // 调试信息

            if (fileDependencies.TryGetValue(selectedFileName, out var dependencies))
            {
                var nodePositions = new Dictionary<string, (float X, float Y)>();
                // 首先加入选中的节点，确保它在依赖项中
                CalculateNodePositions(new Dictionary<string, List<string>> { { selectedFileName, dependencies } }, nodePositions);

                var parentNode = GetOrCreateDiagramNode(selectedFileName, nodePositions[selectedFileName], diagramControl1);
                diagramControl1.Items.Add(parentNode);  // 确保父节点被添加

                foreach (var dep in dependencies)
                {
                    if (!visitedNodes.Contains(dep))  // 避免重复处理节点
                    {
                        var childNode = GetOrCreateDiagramNode(dep, nodePositions[dep], diagramControl1);
                        diagramControl1.Items.Add(childNode);  // 确保子节点被添加
                        var connector = new DiagramConnector { BeginItem = parentNode, EndItem = childNode };
                        diagramControl1.Items.Add(connector);

                        // 递归绘制子节点的依赖
                        DisplayDependenciesForSelectedFile(dep);
                    }
                }
            }

            diagramControl1.Refresh();  // 刷新控件以显示新的依赖关系图
        }

        /// <summary>
        /// 将鼠标点击脚本的关联脚本绘制出来
        /// </summary>
        /// <param name="selectedFileName"></param>
        private void DisplayDependenciesForSelectedFile1(string selectedFileName)
        {
            try
            {
                // 清除旧的图形元素
                diagramControl1.Items.Clear();
                // 获取选中文件的依赖
                if (fileDependencies.TryGetValue(selectedFileName, out var dependencies))
                {
                    _logger.Info("已生成"+ selectedFileName+"脚本关联图！");
                    var nodePositions = new Dictionary<string, (float X, float Y)>();
                    // 首先加入选中的节点，确保它在依赖项中
                    nodePositions[selectedFileName] = (10, 30); // 这里设置初始位置，可以根据需要调整
                    // 计算节点位置                                           
                    CalculateNodePositions(new Dictionary<string, List<string>> { { selectedFileName, dependencies } }, nodePositions);
                    // 添加父节点
                    var parentNode = GetOrCreateDiagramNode(selectedFileName, nodePositions[selectedFileName], diagramControl1);
                    diagramControl1.Items.Add(parentNode);  // 确保父节点被添加
                    // 添加子节点和连接线                                        
                    foreach (var dep in dependencies)
                    {
                        var childNode = GetOrCreateDiagramNode(dep, nodePositions[dep], diagramControl1);
                        diagramControl1.Items.Add(childNode);  // 确保子节点被添加
                        var connector = new DiagramConnector { BeginItem = parentNode, EndItem = childNode };
                        diagramControl1.Items.Add(connector);
                    }
                }
                else
                {
                    _logger.Info(selectedFileName + "无关联脚本");
                }
                // 刷新控件以显示更新
                diagramControl1.Refresh();
            }
            catch (Exception ex)
            {
                _logger.Error(selectedFileName + "脚本关联图生成失败！" + ex.ToString());
            }
        }

        /// <summary>
        /// 截取文件名字符串
        /// </summary>
        /// <param name="fullText"></param>
        /// <returns></returns>
        private string ExtractFileName(string fullText)
        {
            // 使用空格作为分隔符，分割字符串
            string[] parts = fullText.Split(new char[] { ' ' }, 2);
            // 第一部分将是文件名
            if (parts.Length > 0)
            {
                return parts[0];
            }
            return fullText; //如果没有空格，返回原始字符串(因为我在之前的处理中，如果json中没有版本号的脚本我就设置他的节点名为文件名)
        }

        /// <summary>
        /// 计算文件名长度，设置合适的节点大小
        /// </summary>
        /// <param name="text"></param>
        /// <param name="font"></param>
        /// <returns></returns>
        private SizeF MeasureString(string text, Font font)
        {
            using (var g = Graphics.FromImage(new Bitmap(1, 1)))
            {
                return g.MeasureString(text, font);
            }
        }

        public static Bitmap CaptureControl(Control control)
        {
            Bitmap controlBitmap = new Bitmap(control.Width, control.Height);
            control.DrawToBitmap(controlBitmap, new Rectangle(0, 0, control.Width, control.Height));
            return controlBitmap;
        }

        public void SaveTreeListAsImage(TreeList treeList, string filename)
        {
            using (Bitmap bmp = CaptureControl(treeList))
            {
                bmp.Save(filename, System.Drawing.Imaging.ImageFormat.Png); // 保存为 PNG 格式
            }
        }


    }
}
