using DevExpress.Export.Xl;
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
using treelist.model;

namespace treelist
{
    public partial class Form1 : DevExpress.XtraEditors.XtraForm
    {
        private int nodeId = 0; // 用于生成唯一的节点ID
        // 在Form1类中定义一个列表作为数据源
        private List<TreeListNodeModel> nodeList = new List<TreeListNodeModel>();
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
        private void InitializeTreeList()
        {
            treeList1.Columns.Add(new TreeListColumn{Caption = "Name", FieldName = "Name", VisibleIndex = 0 });
            treeList1.Columns.Add(new TreeListColumn { Caption = "Group", FieldName = "Group", VisibleIndex = 1 , Visible = false });
            treeList1.Columns["Group"].SortOrder = SortOrder.Ascending;
            treeList1.KeyFieldName = "ID";
            treeList1.ParentFieldName = "ParentID";
        }
        //点击按钮，选择XML文件地址
        private void LoadXmlButton_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                string selectedFolder = folderBrowserDialog.SelectedPath;
                // 异步执行加载和解析操作
                Task.Run(() => LoadFiles(selectedFolder));
            }
        }
        int nodeIdCounter = 1;

        private void LoadFiles(string folderPath)
        {
            // 步骤1: 解析 JSON 文件，创建节点到 TreeListNode 的映射
            var jsonFilePath = Path.Combine(folderPath, "C:\\work\\ScriptList.json");
            var jsonEcuMapping = LoadJsonData(jsonFilePath);
            List<TreeListNodeModel> nodeList = new List<TreeListNodeModel>();
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
                        var xmlContent = LoadXmlDocument(xmlFile);
                        PopulateXmlToTree(xmlContent, aaa, nodeList);
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
                                GitDate = scriptDto.gitDate
                            };
                            nodeList.Add(aaa);
                            var xmlContent = LoadXmlDocument(xmlFile);
                            PopulateXmlToTree(xmlContent, aaa, nodeList);
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
                                GitDate = scriptDto.gitDate
                            };
                            nodeList.Add(aaa);
                            var xmlContent = LoadXmlDocument(xmlFile);
                            PopulateXmlToTree(xmlContent, aaa, nodeList);
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
                //treeList1.ExpandAll();
            }));
        }
        //json文件解析
        private List<ScriptList> LoadJsonData(string jsonFilePath)
        {
            string json = File.ReadAllText(jsonFilePath);
            return JsonConvert.DeserializeObject<List<ScriptList>>(json);
        }
        //xml文件解析
        private XmlNode LoadXmlDocument(string xmlFilePath)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlFilePath);
            return xmlDoc.DocumentElement;
        }

        private void PopulateXmlToTree(XmlNode xmlNode, TreeListNodeModel parentNode, List<TreeListNodeModel> nodeList)
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
                else if (parentNode == null)
                {
                    if (xmlNode.NodeType == XmlNodeType.Element)
                    {
                        var nodeName = xmlNode.Attributes?["name"]?.InnerText ?? xmlNode.Name;
                        var TreeListNodeModel = new TreeListNodeModel
                        {
                            ID = nodeIdCounter++,
                            ParentID = parentNode.ID,  // 使用父节点的ID作为ParentID
                            Name = nodeName,
                            ImageIndex = folderIconIndex
                            // 其他属性根据需要设置
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
        // 在其他部分调用此方法，暂无用
        public void LoadXmlAndPopulateTree(string xmlFilePath)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlFilePath);
            List<TreeListNodeModel> nodeList = new List<TreeListNodeModel>();
            // 绑定数据源
            treeList1.DataSource = nodeList;
            treeList1.KeyFieldName = "ID";
            treeList1.ParentFieldName = "ParentID";
            treeList1.RefreshDataSource();
        }

    }
}
