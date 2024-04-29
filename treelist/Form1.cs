﻿using DevExpress.Export.Xl;
using DevExpress.XtraDiagram;
using DevExpress.XtraEditors.Filtering;
using DevExpress.XtraPrinting.Native.WebClientUIControl;
using DevExpress.XtraTreeList;
using DevExpress.XtraTreeList.Columns;
using DevExpress.XtraTreeList.Nodes;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
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
        //private List<TreeListNodeModel> nodeList = new List<TreeListNodeModel>();
        BindingList<TreeListNodeModel> nodeList = new BindingList<TreeListNodeModel>();
        
        public Form1()
        {
            
            InitializeComponent();
            InitializeTreeList();
            this.Load += new EventHandler(Form1_Load); // 添加窗体的 Load 事件处理程序
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // 计算并设置panelControl1的底部Padding
            int bottomPadding = this.panelControl2.Height;
            this.panelControl1.Padding = new Padding(this.panelControl1.Padding.Left, this.panelControl1.Padding.Top, this.panelControl1.Padding.Right, bottomPadding);
            // 确保树形列表控件填充其父控件，考虑到Padding
            this.treeList1.Dock = DockStyle.Fill;
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
            // 可以在这里添加逻辑来处理节点焦点改变事件
            if (e.Node != null)
            {
                Console.WriteLine("Selected Node: " + e.Node.GetValue("Name"));
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
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                string selectedFolder = folderBrowserDialog.SelectedPath;
                
                // 异步执行加载和解析操作
                Task.Run(() => LoadFiles(selectedFolder)); 
                    // 异步执行加载和解析操作
                ProcessXmlDirectory(selectedFolder);
                DisplayDependencies(diagramControl1, fileDependencies);


            }
            
        }
        int nodeIdCounter = 1;

        /// <summary>
        /// treelist节点数据生成
        /// </summary>
        /// <param name="folderPath">json文件地址</param>
        private void LoadFiles(string folderPath)
        {
            // 步骤1: 解析 JSON 文件，创建节点到 TreeListNode 的映射
            var jsonFilePath = Path.Combine(folderPath, "C:\\work\\ScriptList.json");
            var jsonEcuMapping = LoadJsonData(jsonFilePath);
            //List<TreeListNodeModel> nodeList = new List<TreeListNodeModel>();
            //BindingList<TreeListNodeModel> nodeList = new BindingList<TreeListNodeModel>();
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
            // 加载 XML 文件并添加到节点数据中
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
                        //var xmlContent = LoadXmlDocument(xmlFile);
                        //PopulateXmlToTree(xmlContent, aaa, nodeList);
                        //ParseXmlUsingXmlReaderAndPopulateTree(xmlFile, aaa, nodeList);
                        LoadXmlAndPopulateTree1(xmlFile, aaa, nodeList);
                    }
                }
                if (scriptDto != null)
                {
                    if (scriptDto.scGenericComponentDTOS != null)
                    {
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
                            //var xmlContent = LoadXmlDocument(xmlFile);
                            //PopulateXmlToTree(xmlContent, aaa, nodeList);
                            //ParseXmlUsingXmlReaderAndPopulateTree(xmlFile, aaa, nodeList);
                            LoadXmlAndPopulateTree1(xmlFile, aaa, nodeList);
                        }
                    }
                    else
                    {
                        if (!nodeList.Any(n => n.Name == xmlFileName))
                        {
                            TreeListNodeModel aaa = new TreeListNodeModel
                            {
                                ID = nodeIdCounter++,//xmlFileName + "["+scriptDto.gitRevision+"]"+ "["+scriptDto.gitDate+"]"
                                ParentID = nodeList.Where(n => n.Name == scriptType).Select(n => n.ID).FirstOrDefault(),//scriptDto.scGenericComponentDTOS?.FirstOrDefault()?.ecu
                                Name = xmlFileName + "[" + scriptDto.gitRevision + "] " + "[" + scriptDto.gitDate + "]",
                                FileTitle = scriptDto.fileTitle,
                                GitRevision = scriptDto.gitRevision,
                                GitDate = scriptDto.gitDate,
                                FileId = scriptDto.id,
                                Title = scriptDto.title
                            };
                            nodeList.Add(aaa);
                            //var xmlContent = LoadXmlDocument(xmlFile);
                            //PopulateXmlToTree(xmlContent, aaa, nodeList);
                            LoadXmlAndPopulateTree1(xmlFile, aaa, nodeList);
                            //ParseXmlUsingXmlReaderAndPopulateTree(xmlFile, aaa, nodeList);
                        }
                    }
                }
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
            string json = File.ReadAllText(jsonFilePath);
            return JsonConvert.DeserializeObject<List<ScriptList>>(json);
        }

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
                        ParentID = parentNode?.ID, // 使用上一个节点的ID作为父ID，如果父节点为空，则此节点是顶级节点
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

        // 方法来解析单个XML文件并提取所有'import'标签的'prefix'属性
        private List<string> ParseXmlForImports(string filePath)
        {
            XDocument doc = XDocument.Load(filePath);
            List<string> prefixes = new List<string>();

            // 遍历所有元素，只处理 <import> 标签
            foreach (XElement element in doc.Descendants())
            {
                if (element.Name.LocalName.Equals("import", StringComparison.OrdinalIgnoreCase))
                {
                    var prefix = element.Attribute("prefix")?.Value;
                    if (!string.IsNullOrEmpty(prefix) && prefix.Length > 3)
                    {
                        // 去除前三个字符 'imp'
                        var trimmedPrefix = prefix.Substring(3);
                        prefixes.Add(trimmedPrefix);
                    }
                }
            }

            return prefixes;
        }

        Dictionary<string, List<string>> fileDependencies = new Dictionary<string, List<string>>();
        // 方法来遍历目录并处理每个XML文件
        public Dictionary<string, List<string>> ProcessXmlDirectory(string folderPath)
        {

            // 获取所有XML文件
            //var xmlFiles = Directory.GetFiles(directoryPath, "*.xml", SearchOption.AllDirectories);
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

        //public Dictionary<string, List<string>> bidui(Dictionary<string, List<string>> fileDependencies, BindingList<TreeListNodeModel> nodeList)
        //{

        //    Dictionary<string, List<string>> fileDependencies = new Dictionary<string, List<string>>();
        //    // 获取所有XML文件
        //    //var xmlFiles = Directory.GetFiles(directoryPath, "*.xml", SearchOption.AllDirectories);
        //    var xmlFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
        //        .Where(file => file.EndsWith(".otx", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".zotx", StringComparison.OrdinalIgnoreCase));
        //    foreach (var file in xmlFiles)
        //    {
        //        var fileName = Path.GetFileName(file);
        //        var prefixes = ParseXmlForImports(file);
        //        fileDependencies[fileName] = prefixes;
        //    }

        //    return fileDependencies;
        //}


        private void DisplayDependencies(DiagramControl diagram, Dictionary<string, List<string>> dependencies)
        {
            diagram.BeginUpdate();  // 开始更新控件，这有助于性能优化
            try
            {
                // 清除现有的图形元素
                diagram.Items.Clear();

                Dictionary<string, DiagramShape> createdNodes = new Dictionary<string, DiagramShape>();

                foreach (var file in dependencies)
                {
                    if (!createdNodes.TryGetValue(file.Key, out var sourceNode))
                    {
                        // 创建 source 节点
                        sourceNode = new DiagramShape() { Content = file.Key };
                        diagram.Items.Add(sourceNode);
                        createdNodes[file.Key] = sourceNode;
                    }

                    foreach (var dependentFile in file.Value)
                    {
                        if (!createdNodes.TryGetValue(dependentFile, out var targetNode))
                        {
                            // 创建 target 节点
                            targetNode = new DiagramShape() { Content = dependentFile };
                            diagram.Items.Add(targetNode);
                            createdNodes[dependentFile] = targetNode;
                        }

                        // 创建连接器
                        var connector = new DiagramConnector() { BeginItem = sourceNode, EndItem = targetNode };
                        diagram.Items.Add(connector);
                    }
                }
            }
            finally
            {
                diagram.EndUpdate();  // 结束更新控件
            }
        }


    }
}
